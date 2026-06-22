from __future__ import annotations

import argparse
import csv
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image, ImageOps

ML_ROOT = Path(__file__).resolve().parents[1]
DATASET_DIR = ML_ROOT / "datasets" / "chess-pieces-kaggle"
METADATA_DIR = DATASET_DIR / "metadata"
PROCESSED_DIR = DATASET_DIR / "processed"
SPLITS = ("train", "validation", "test")
DEFAULT_IMAGE_SIZE = 128


def resize_filter() -> int:
    try:
        return Image.Resampling.LANCZOS
    except AttributeError:
        return Image.LANCZOS


def read_rows(split_name: str) -> list[dict[str, str]]:
    metadata_path = METADATA_DIR / f"{split_name}.csv"
    if not metadata_path.exists():
        raise SystemExit(
            f"Missing metadata file: {metadata_path}\n"
            "Run `python ml/scripts/prepare_chess_pieces_dataset.py` first."
        )

    with metadata_path.open("r", encoding="utf-8", newline="") as input_file:
        return list(csv.DictReader(input_file))


def load_class_indices() -> list[dict[str, object]]:
    class_path = METADATA_DIR / "class_indices.json"
    if not class_path.exists():
        raise SystemExit(
            f"Missing class index file: {class_path}\n"
            "Run `python ml/scripts/prepare_chess_pieces_dataset.py` first."
        )
    return json.loads(class_path.read_text(encoding="utf-8"))


def preprocess_image(image_path: Path, image_size: int) -> tuple[Image.Image, np.ndarray]:
    if not image_path.exists():
        raise SystemExit(f"Missing source image: {image_path}")

    with Image.open(image_path) as source:
        image = ImageOps.exif_transpose(source).convert("RGB")
        image = image.resize((image_size, image_size), resize_filter())
        normalized = np.asarray(image, dtype=np.float32) / 255.0

    return image, normalized


def write_split(
    split_name: str,
    rows: list[dict[str, str]],
    output_dir: Path,
    image_size: int,
    write_images: bool,
    write_arrays: bool,
) -> dict[str, int]:
    images: list[np.ndarray] = []
    labels: list[int] = []
    source_paths: list[str] = []
    processed_paths: list[str] = []

    for row in rows:
        source_path = DATASET_DIR / row["relative_path"]
        image, normalized = preprocess_image(source_path, image_size=image_size)

        label = row["label"]
        output_path = output_dir / "images" / split_name / label / f"{source_path.stem}.png"
        if write_images:
            output_path.parent.mkdir(parents=True, exist_ok=True)
            image.save(output_path, format="PNG", optimize=True)

        if write_arrays:
            images.append(normalized)
            labels.append(int(row["class_index"]))
            source_paths.append(row["relative_path"])
            processed_paths.append(output_path.relative_to(output_dir).as_posix())

    if write_arrays:
        array_dir = output_dir / "arrays"
        array_dir.mkdir(parents=True, exist_ok=True)
        np.savez_compressed(
            array_dir / f"{split_name}.npz",
            x=np.stack(images).astype(np.float32),
            y=np.asarray(labels, dtype=np.int64),
            source_paths=np.asarray(source_paths),
            processed_paths=np.asarray(processed_paths),
        )

    return {
        "images": len(rows),
        "height": image_size,
        "width": image_size,
        "channels": 3,
    }


def write_manifest(
    output_dir: Path,
    image_size: int,
    split_counts: dict[str, dict[str, int]],
    class_indices: list[dict[str, object]],
    write_images: bool,
    write_arrays: bool,
) -> None:
    manifest = {
        "source_dataset": "chess-pieces-kaggle",
        "input_metadata_root": "metadata",
        "image_size": [image_size, image_size],
        "channels": 3,
        "color_mode": "rgb",
        "array_dtype": "float32",
        "normalization": "pixel / 255.0",
        "pixel_range": [0.0, 1.0],
        "outputs": {
            "resized_images": write_images,
            "normalized_npz_arrays": write_arrays,
        },
        "splits": split_counts,
        "classes": class_indices,
    }
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir / "preprocessing_manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )
    (output_dir / "class_indices.json").write_text(
        json.dumps(class_indices, indent=2) + "\n",
        encoding="utf-8",
    )


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Resize, RGB-convert, and normalize chess-piece images for CNN input."
    )
    parser.add_argument(
        "--image-size",
        type=int,
        default=DEFAULT_IMAGE_SIZE,
        help="Square CNN input size in pixels.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=None,
        help="Output folder for preprocessed images and arrays.",
    )
    parser.add_argument(
        "--no-images",
        action="store_true",
        help="Skip writing resized PNG images.",
    )
    parser.add_argument(
        "--no-arrays",
        action="store_true",
        help="Skip writing normalized .npz tensor arrays.",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Remove the output folder before preprocessing.",
    )
    args = parser.parse_args()

    if args.image_size <= 0:
        raise SystemExit("--image-size must be greater than zero.")
    if args.no_images and args.no_arrays:
        raise SystemExit("Nothing to write: remove --no-images or --no-arrays.")

    output_dir = args.output_dir or PROCESSED_DIR / f"cnn_input_{args.image_size}"
    if args.overwrite and output_dir.exists():
        shutil.rmtree(output_dir)

    class_indices = load_class_indices()
    split_counts: dict[str, dict[str, int]] = {}
    for split_name in SPLITS:
        rows = read_rows(split_name)
        split_counts[split_name] = write_split(
            split_name=split_name,
            rows=rows,
            output_dir=output_dir,
            image_size=args.image_size,
            write_images=not args.no_images,
            write_arrays=not args.no_arrays,
        )

    write_manifest(
        output_dir=output_dir,
        image_size=args.image_size,
        split_counts=split_counts,
        class_indices=class_indices,
        write_images=not args.no_images,
        write_arrays=not args.no_arrays,
    )

    print(f"Preprocessed CNN input written to: {output_dir}")
    print(f"Image size: {args.image_size}x{args.image_size} RGB")
    print("Normalization: float32 pixel / 255.0")
    for split_name, info in split_counts.items():
        print(f"- {split_name}: {info['images']} images")


if __name__ == "__main__":
    main()
