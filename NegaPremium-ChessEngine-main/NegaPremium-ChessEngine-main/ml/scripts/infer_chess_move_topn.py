from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

import numpy as np
import torch
from torch import nn

try:
    import chess
except ImportError as exc:  # pragma: no cover
    raise SystemExit("python-chess is required for inference. Install it with: pip install python-chess") from exc


class ChessMoveCNN(nn.Module):
    def __init__(self, num_classes: int) -> None:
        super().__init__()
        self.features = nn.Sequential(
            nn.Conv2d(18, 64, kernel_size=3, padding=1),
            nn.BatchNorm2d(64),
            nn.ReLU(inplace=True),
            nn.Conv2d(64, 64, kernel_size=3, padding=1),
            nn.BatchNorm2d(64),
            nn.ReLU(inplace=True),
            nn.MaxPool2d(2),
            nn.Conv2d(64, 128, kernel_size=3, padding=1),
            nn.BatchNorm2d(128),
            nn.ReLU(inplace=True),
            nn.Conv2d(128, 128, kernel_size=3, padding=1),
            nn.BatchNorm2d(128),
            nn.ReLU(inplace=True),
            nn.AdaptiveAvgPool2d((1, 1)),
        )
        self.classifier = nn.Sequential(
            nn.Flatten(),
            nn.Dropout(0.25),
            nn.Linear(128, 256),
            nn.ReLU(inplace=True),
            nn.Dropout(0.25),
            nn.Linear(256, num_classes),
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        return self.classifier(self.features(x))


def board_to_matrix(board: chess.Board) -> np.ndarray:
    planes = np.zeros((8, 8, 18), dtype=np.float32)
    piece_map = {
        chess.PAWN: 0,
        chess.KNIGHT: 1,
        chess.BISHOP: 2,
        chess.ROOK: 3,
        chess.QUEEN: 4,
        chess.KING: 5,
    }

    for square, piece in board.piece_map().items():
        row = 7 - chess.square_rank(square)
        col = chess.square_file(square)
        base = 0 if piece.color == chess.WHITE else 6
        planes[row, col, base + piece_map[piece.piece_type]] = 1.0

    planes[:, :, 12] = 1.0 if board.turn == chess.WHITE else 0.0
    planes[:, :, 13] = 1.0 if board.has_kingside_castling_rights(chess.WHITE) else 0.0
    planes[:, :, 14] = 1.0 if board.has_queenside_castling_rights(chess.WHITE) else 0.0
    planes[:, :, 15] = 1.0 if board.has_kingside_castling_rights(chess.BLACK) else 0.0
    planes[:, :, 16] = 1.0 if board.has_queenside_castling_rights(chess.BLACK) else 0.0
    planes[:, :, 17] = 1.0 if board.ep_square is not None else 0.0
    return planes


def normalize_label(label: str) -> str:
    return label.strip()


def load_model(model_dir: Path, device: torch.device) -> tuple[nn.Module, list[str]]:
    metadata_path = model_dir / "metadata.json"
    config_path = model_dir / "config.json"
    model_path = model_dir / "model.pt"

    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    num_classes = int(metadata.get("num_classes") or 0)
    if num_classes <= 0:
        raise SystemExit("metadata.json does not contain a valid num_classes field")
    if not model_path.exists():
        raise SystemExit(f"Missing model file: {model_path}")
    if not config_path.exists():
        raise SystemExit(f"Missing config file: {config_path}")

    labels = [normalize_label(str(label)) for label in metadata.get("move_labels", [])]
    if len(labels) != num_classes:
        raise SystemExit("metadata.json move_labels length does not match num_classes")

    model = ChessMoveCNN(num_classes=num_classes).to(device)
    state = torch.load(model_path, map_location=device)
    model.load_state_dict(state)
    model.eval()
    return model, labels


@torch.no_grad()
def predict_topn(model: nn.Module, labels: list[str], fen: str, top_n: int, device: torch.device) -> list[dict[str, Any]]:
    board = chess.Board(fen)
    matrix = board_to_matrix(board)
    inputs = torch.from_numpy(matrix).permute(2, 0, 1).unsqueeze(0).to(device)
    logits = model(inputs)
    probs = torch.softmax(logits, dim=1).squeeze(0)
    scores = torch.topk(probs, k=min(top_n, probs.numel()))

    results: list[dict[str, Any]] = []
    for score, index in zip(scores.values.tolist(), scores.indices.tolist()):
        label = labels[int(index)]
        results.append({"label": label, "score": float(score)})
    return results


def main() -> None:
    parser = argparse.ArgumentParser(description="Return top-N chess move predictions for a FEN.")
    parser.add_argument("--model-dir", type=Path, required=True)
    parser.add_argument("--fen", type=str, required=True)
    parser.add_argument("--top-n", type=int, default=10)
    parser.add_argument("--device", type=str, default="cpu")
    args = parser.parse_args()

    device = torch.device(args.device)
    model, labels = load_model(args.model_dir, device)
    results = predict_topn(model, labels, args.fen, args.top_n, device)
    for item in results:
        print(f"{item['label']}\t{item['score']:.8f}")


if __name__ == "__main__":
    main()
