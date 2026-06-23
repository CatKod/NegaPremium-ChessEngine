from __future__ import annotations

import argparse
import json
import math
import random
from dataclasses import asdict, dataclass
from pathlib import Path

import numpy as np
import torch
from torch import nn
from torch.utils.data import DataLoader, Dataset, Subset

ML_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DATASET = ML_ROOT / "datasets" / "chess-moves-kaggle" / "processed" / "matrix" / "chess_move_matrices_50000_p18.npz"
DEFAULT_OUTPUT_DIR = ML_ROOT / "models" / "chess_move_matrix_cnn"


@dataclass(frozen=True)
class TrainConfig:
    dataset_path: str
    output_dir: str
    epochs: int
    batch_size: int
    learning_rate: float
    weight_decay: float
    val_split: float
    seed: int
    num_workers: int
    device: str


class ChessMoveDataset(Dataset):
    def __init__(self, dataset_path: Path) -> None:
        data = np.load(dataset_path, allow_pickle=True)
        self.x = data["x"].astype(np.float32)
        self.y = data["y"].astype(np.int64)
        self.move_labels = data["move_labels"]
        self.move_uci = data.get("move_uci")

    def __len__(self) -> int:
        return int(self.x.shape[0])

    def __getitem__(self, index: int):
        x = torch.from_numpy(self.x[index]).permute(2, 0, 1).contiguous()
        y = torch.tensor(self.y[index], dtype=torch.long)
        return x, y


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


def set_seed(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    torch.cuda.manual_seed_all(seed)
    torch.backends.cudnn.deterministic = True
    torch.backends.cudnn.benchmark = False


def split_indices(length: int, val_split: float, seed: int) -> tuple[list[int], list[int]]:
    indices = list(range(length))
    rng = random.Random(seed)
    rng.shuffle(indices)
    val_size = max(1, int(math.floor(length * val_split))) if length > 1 else 0
    val_indices = indices[:val_size]
    train_indices = indices[val_size:] or indices[:1]
    return train_indices, val_indices


@torch.no_grad()
def evaluate(model: nn.Module, loader: DataLoader, criterion: nn.Module, device: torch.device) -> tuple[float, float]:
    model.eval()
    total_loss = 0.0
    total_correct = 0
    total_examples = 0

    for inputs, targets in loader:
        inputs = inputs.to(device)
        targets = targets.to(device)
        logits = model(inputs)
        loss = criterion(logits, targets)
        total_loss += float(loss.item()) * inputs.size(0)
        preds = logits.argmax(dim=1)
        total_correct += int((preds == targets).sum().item())
        total_examples += int(inputs.size(0))

    if total_examples == 0:
        return 0.0, 0.0
    return total_loss / total_examples, total_correct / total_examples


def train_one_epoch(model: nn.Module, loader: DataLoader, criterion: nn.Module, optimizer: torch.optim.Optimizer, device: torch.device) -> tuple[float, float]:
    model.train()
    total_loss = 0.0
    total_correct = 0
    total_examples = 0

    for inputs, targets in loader:
        inputs = inputs.to(device)
        targets = targets.to(device)

        optimizer.zero_grad(set_to_none=True)
        logits = model(inputs)
        loss = criterion(logits, targets)
        loss.backward()
        optimizer.step()

        total_loss += float(loss.item()) * inputs.size(0)
        preds = logits.argmax(dim=1)
        total_correct += int((preds == targets).sum().item())
        total_examples += int(inputs.size(0))

    return total_loss / total_examples, total_correct / total_examples


def save_artifacts(output_dir: Path, model: nn.Module, config: TrainConfig, metadata: dict[str, object]) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    torch.save(model.state_dict(), output_dir / "model.pt")
    (output_dir / "config.json").write_text(json.dumps(asdict(config), indent=2) + "\n", encoding="utf-8")
    (output_dir / "metadata.json").write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Train a CNN to predict chess moves from board matrices.")
    parser.add_argument("--dataset", type=Path, default=DEFAULT_DATASET, help="Path to processed .npz dataset.")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR, help="Directory to save checkpoints and metadata.")
    parser.add_argument("--epochs", type=int, default=15)
    parser.add_argument("--batch-size", type=int, default=64)
    parser.add_argument("--learning-rate", type=float, default=1e-3)
    parser.add_argument("--weight-decay", type=float, default=1e-4)
    parser.add_argument("--val-split", type=float, default=0.1)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--num-workers", type=int, default=0)
    parser.add_argument("--device", type=str, default="cuda" if torch.cuda.is_available() else "cpu")
    args = parser.parse_args()

    if not args.dataset.exists():
        raise SystemExit(f"Dataset not found: {args.dataset}")
    if not 0.0 < args.val_split < 1.0:
        raise SystemExit("--val-split must be between 0 and 1.")

    print("[1/7] Seeding RNGs...")
    set_seed(args.seed)
    device = torch.device(args.device)
    print(f"[2/7] Using device: {device}")
    print(f"[3/7] Loading dataset: {args.dataset}")

    dataset = ChessMoveDataset(args.dataset)
    print(f"[4/7] Dataset loaded: {len(dataset)} samples, {len(dataset.move_labels)} move labels")
    train_indices, val_indices = split_indices(len(dataset), args.val_split, args.seed)
    print(f"[5/7] Split dataset -> train: {len(train_indices)}, val: {len(val_indices)}")
    train_loader = DataLoader(Subset(dataset, train_indices), batch_size=args.batch_size, shuffle=True, num_workers=args.num_workers)
    val_loader = DataLoader(Subset(dataset, val_indices), batch_size=args.batch_size, shuffle=False, num_workers=args.num_workers)
    print(f"[6/7] DataLoaders ready with batch_size={args.batch_size}, num_workers={args.num_workers}")

    num_classes = int(len(dataset.move_labels))
    model = ChessMoveCNN(num_classes=num_classes).to(device)
    criterion = nn.CrossEntropyLoss()
    optimizer = torch.optim.AdamW(model.parameters(), lr=args.learning_rate, weight_decay=args.weight_decay)
    scheduler = torch.optim.lr_scheduler.ReduceLROnPlateau(optimizer, mode="min", factor=0.5, patience=2)
    print(f"[7/7] Model initialized with {num_classes} classes. Starting training for {args.epochs} epochs...")

    config = TrainConfig(
        dataset_path=str(args.dataset),
        output_dir=str(args.output_dir),
        epochs=args.epochs,
        batch_size=args.batch_size,
        learning_rate=args.learning_rate,
        weight_decay=args.weight_decay,
        val_split=args.val_split,
        seed=args.seed,
        num_workers=args.num_workers,
        device=str(device),
    )

    best_val_loss = float("inf")
    best_epoch = -1
    history: list[dict[str, float]] = []

    for epoch in range(1, args.epochs + 1):
        train_loss, train_acc = train_one_epoch(model, train_loader, criterion, optimizer, device)
        val_loss, val_acc = evaluate(model, val_loader, criterion, device)
        scheduler.step(val_loss)

        history.append(
            {
                "epoch": float(epoch),
                "train_loss": float(train_loss),
                "train_acc": float(train_acc),
                "val_loss": float(val_loss),
                "val_acc": float(val_acc),
                "lr": float(optimizer.param_groups[0]["lr"]),
            }
        )

        print(
            f"Epoch {epoch:03d}/{args.epochs} | "
            f"train_loss={train_loss:.4f} train_acc={train_acc:.4f} | "
            f"val_loss={val_loss:.4f} val_acc={val_acc:.4f}"
        )

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_epoch = epoch
            save_artifacts(
                args.output_dir,
                model,
                config,
                {
                    "best_epoch": best_epoch,
                    "best_val_loss": best_val_loss,
                    "num_classes": num_classes,
                    "train_size": len(train_indices),
                    "val_size": len(val_indices),
                    "history": history,
                    "move_labels": [str(label) for label in dataset.move_labels.tolist()],
                },
            )

    print(f"Training complete. Best epoch: {best_epoch}, best val loss: {best_val_loss:.4f}")
    print(f"Artifacts saved to: {args.output_dir}")


if __name__ == "__main__":
    main()
