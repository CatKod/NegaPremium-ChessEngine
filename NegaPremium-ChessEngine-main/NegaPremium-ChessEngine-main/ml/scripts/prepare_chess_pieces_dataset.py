from __future__ import annotations

import argparse
import csv
import json
import random
import re
import shutil
from pathlib import Path

ML_ROOT = Path(__file__).resolve().parents[1]
DATASET_DIR = ML_ROOT / "datasets" / "chess-pieces-kaggle"
RAW_CLASS_ROOT = DATASET_DIR / "raw" / "all_resized_into_sub_folders_640"
METADATA_DIR = DATASET_DIR / "metadata"
PROCESSED_DIR = DATASET_DIR / "processed"
IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}
DEFAULT_SEED = 42


def normalize_label(class_name: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", class_name.lower()).strip("_")


def collect_images(class_root: Path) -> list[dict[str, object]]:
    if not class_root.exists():
        raise SystemExit(
            f"Missing raw class folder: {class_root}\n"
            "Run `python ml/scripts/download_chess_pieces_dataset.py` first."
        )

    class_dirs = sorted(path for path in class_root.iterdir() if path.is_dir())
    rows: list[dict[str, object]] = []

    for class_index, class_dir in enumerate(class_dirs):
        class_name = class_dir.name
        label = normalize_label(class_name)
        image_paths = sorted(
            path
            for path in class_dir.iterdir()
            if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS
        )
        for image_path in image_paths:
            rows.append(
                {
                    "relative_path": image_path.relative_to(DATASET_DIR).as_posix(),
                    "class_name": class_name,
                    "label": label,
                    "class_index": class_index,
                }
            )

    if not rows:
        raise SystemExit(f"No images found in: {class_root}")

    return rows


def assign_splits(rows: list[dict[str, object]], seed: int) -> list[dict[str, object]]:
    rng = random.Random(seed)
    grouped: dict[str, list[dict[str, object]]] = {}
    for row in rows:
        grouped.setdefault(str(row["class_name"]), []).append(row)

    split_rows: list[dict[str, object]] = []
    for class_name in sorted(grouped):
        class_rows = grouped[class_name][:]
        rng.shuffle(class_rows)

        total = len(class_rows)
        validation_count = max(1, round(total * 0.15))
        test_count = max(1, round(total * 0.15))
        train_count = total - validation_count - test_count
        if train_count <= 0:
            raise SystemExit(f"Not enough images to split class: {class_name}")

        split_plan = (
            [("train", train_count)]
            + [("validation", validation_count)]
            + [("test", test_count)]
        )

        start = 0
        for split_name, count in split_plan:
            for row in class_rows[start : start + count]:
                split_rows.append({"split": split_name, **row})
            start += count

    return sorted(
        split_rows,
        key=lambda row: (str(row["split"]), int(row["class_index"]), str(row["relative_path"])),
    )


def write_csv(path: Path, rows: list[dict[str, object]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = ["split", "relative_path", "class_name", "label", "class_index"]
    with path.open("w", encoding="utf-8", newline="") as output_file:
        writer = csv.DictWriter(output_file, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def write_class_index(path: Path, rows: list[dict[str, object]]) -> None:
    classes = []
    seen = set()
    for row in sorted(rows, key=lambda item: int(item["class_index"])):
        class_index = int(row["class_index"])
        if class_index in seen:
            continue
        seen.add(class_index)
        classes.append(
            {
                "class_index": class_index,
                "class_name": row["class_name"],
                "label": row["label"],
            }
        )

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(classes, indent=2) + "\n", encoding="utf-8")


def copy_processed_images(rows: list[dict[str, object]]) -> None:
    if PROCESSED_DIR.exists():
        shutil.rmtree(PROCESSED_DIR)

    for row in rows:
        source = DATASET_DIR / str(row["relative_path"])
        destination = (
            PROCESSED_DIR
            / str(row["split"])
            / str(row["label"])
            / source.name
        )
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Prepare deterministic CNN metadata for the Kaggle chess pieces dataset."
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=DEFAULT_SEED,
        help="Seed used for deterministic train/validation/test splits.",
    )
    parser.add_argument(
        "--copy-images",
        action="store_true",
        help="Also copy images into processed/{train,validation,test}/label folders.",
    )
    args = parser.parse_args()

    rows = collect_images(RAW_CLASS_ROOT)
    split_rows = assign_splits(rows, seed=args.seed)

    write_csv(METADATA_DIR / "all.csv", split_rows)
    for split_name in ["train", "validation", "test"]:
        write_csv(
            METADATA_DIR / f"{split_name}.csv",
            [row for row in split_rows if row["split"] == split_name],
        )
    write_class_index(METADATA_DIR / "class_indices.json", split_rows)

    if args.copy_images:
        copy_processed_images(split_rows)

    split_counts = {
        split_name: sum(1 for row in split_rows if row["split"] == split_name)
        for split_name in ["train", "validation", "test"]
    }
    print(f"Metadata written to: {METADATA_DIR}")
    print(f"Classes: {len({row['class_name'] for row in split_rows})}")
    print(f"Images: {len(split_rows)}")
    for split_name, count in split_counts.items():
        print(f"- {split_name}: {count}")


if __name__ == "__main__":
    main()
