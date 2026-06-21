from __future__ import annotations

import argparse
import shutil
from pathlib import Path

DATASET_REF = "krithiik/chess-pieces"
ML_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT_DIR = ML_ROOT / "datasets" / "chess-pieces-kaggle" / "raw"
CLASS_ROOT = "all_resized_into_sub_folders_640"


def count_images(class_root: Path) -> dict[str, int]:
    return {
        item.name: len([path for path in item.rglob("*") if path.is_file()])
        for item in sorted(class_root.iterdir())
        if item.is_dir()
    }


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Download the Kaggle chess pieces image dataset for CNN training."
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help="Destination folder for raw Kaggle files.",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force KaggleHub to re-download the dataset.",
    )
    args = parser.parse_args()

    try:
        import kagglehub
    except ImportError as exc:
        raise SystemExit(
            "Missing dependency: install with `python -m pip install -r ml/requirements.txt`."
        ) from exc

    args.output_dir.mkdir(parents=True, exist_ok=True)
    downloaded_path = Path(
        kagglehub.dataset_download(
            DATASET_REF,
            output_dir=str(args.output_dir),
            force_download=args.force,
        )
    )

    marker_dir = args.output_dir / ".complete"
    if marker_dir.exists():
        shutil.rmtree(marker_dir)

    class_root = args.output_dir / CLASS_ROOT
    if not class_root.exists():
        raise SystemExit(f"Expected class folder not found: {class_root}")

    counts = count_images(class_root)
    print(f"Dataset downloaded to: {downloaded_path}")
    print(f"Class root: {class_root}")
    print(f"Classes: {len(counts)}")
    print(f"Images: {sum(counts.values())}")
    for name, count in counts.items():
        print(f"- {name}: {count}")


if __name__ == "__main__":
    main()
