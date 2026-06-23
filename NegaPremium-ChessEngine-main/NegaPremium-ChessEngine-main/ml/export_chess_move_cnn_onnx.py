from __future__ import annotations

import argparse
import json
from pathlib import Path

import torch
from torch import nn

ML_ROOT = Path(__file__).resolve().parent
DEFAULT_MODEL_DIR = ML_ROOT / "models" / "chess_move_matrix_cnn"


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


def main() -> None:
    parser = argparse.ArgumentParser(description="Export chess move CNN to ONNX.")
    parser.add_argument("--model-dir", type=Path, default=DEFAULT_MODEL_DIR)
    parser.add_argument("--opset", type=int, default=17)
    args = parser.parse_args()

    model_dir = args.model_dir
    model_path = model_dir / "model.pt"
    metadata_path = model_dir / "metadata.json"
    onnx_path = model_dir / "model.onnx"

    metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    num_classes = int(metadata["num_classes"])

    model = ChessMoveCNN(num_classes=num_classes)
    state_dict = torch.load(model_path, map_location="cpu")
    model.load_state_dict(state_dict)
    model.eval()

    dummy_input = torch.randn(1, 18, 8, 8, dtype=torch.float32)
    torch.onnx.export(
        model,
        dummy_input,
        onnx_path,
        export_params=True,
        opset_version=args.opset,
        do_constant_folding=True,
        input_names=["input"],
        output_names=["logits"],
        dynamic_axes={"input": {0: "batch_size"}, "logits": {0: "batch_size"}},
    )
    print(onnx_path)


if __name__ == "__main__":
    main()
