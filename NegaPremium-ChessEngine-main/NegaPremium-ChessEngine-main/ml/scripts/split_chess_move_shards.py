from __future__ import annotations

import argparse
import json
import os
import random
import shutil
from pathlib import Path

ML_ROOT = Path(__file__).resolve().parents[1]
DATASET_DIR = ML_ROOT / "datasets" / "chess-moves-kaggle"
DEFAULT_SOURCE_DIR = DATASET_DIR / "processed" / "matrix" / "shards_all_p18_n50000"
DEFAULT_OUTPUT_DIR = DATASET_DIR / "processed" / "matrix" / "splits_80_10_10_p18_n50000"
DEFAULT_SEED = 42
DEFAULT_RATIOS = (0.8, 0.1, 0.1)
SPLIT_NAMES = ("train", "val", "test")


def load_manifest(source_dir: Path) -> dict[str, object]:
    manifest_path = source_dir / "shards_manifest.json"
    if not manifest_path.exists():
        raise SystemExit(f"Missing shard manifest: {manifest_path}")
    return json.loads(manifest_path.read_text(encoding="utf-8"))


def parse_ratios(raw_ratios: str) -> tuple[float, float, float]:
    parts = [float(item.strip()) for item in raw_ratios.split(",")]
    if len(parts) != 3:
        raise SystemExit("--ratios must contain exactly three values: train,val,test")
    if any(item <= 0 for item in parts):
        raise SystemExit("--ratios values must be greater than zero")

    total = sum(parts)
    return tuple(item / total for item in parts)  # type: ignore[return-value]


def ensure_clean_output(output_dir: Path, overwrite: bool) -> None:
    if output_dir.exists():
        has_files = any(output_dir.rglob("*"))
        if has_files and not overwrite:
            raise SystemExit(
                f"Output split folder already exists: {output_dir}\n"
                "Pass `--overwrite` to rebuild it."
            )
        if overwrite:
            shutil.rmtree(output_dir)
    for split_name in SPLIT_NAMES:
        (output_dir / split_name).mkdir(parents=True, exist_ok=True)


def choose_split(
    assigned_rows: dict[str, int],
    target_rows: dict[str, int],
    shard_rows: int,
) -> str:
    deficits = {
        split_name: target_rows[split_name] - assigned_rows[split_name]
        for split_name in SPLIT_NAMES
    }
    positive = {
        split_name: deficit
        for split_name, deficit in deficits.items()
        if deficit > 0
    }
    if positive:
        return max(positive, key=positive.get)

    return min(
        SPLIT_NAMES,
        key=lambda split_name: assigned_rows[split_name] + shard_rows - target_rows[split_name],
    )


def link_or_copy(source: Path, destination: Path, mode: str) -> str:
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists():
        destination.unlink()

    if mode == "copy":
        shutil.copy2(source, destination)
        return "copy"

    try:
        os.link(source, destination)
        return "hardlink"
    except OSError:
        if mode == "hardlink":
            raise
        shutil.copy2(source, destination)
        return "copy"


def split_shards(
    source_dir: Path,
    output_dir: Path,
    ratios: tuple[float, float, float],
    seed: int,
    mode: str,
    overwrite: bool,
) -> dict[str, object]:
    manifest = load_manifest(source_dir)
    shards = list(manifest["shards"])
    if not shards:
        raise SystemExit("Source manifest contains no shards.")

    total_rows = int(manifest["encoded_rows"])
    target_rows = {
        "train": round(total_rows * ratios[0]),
        "val": round(total_rows * ratios[1]),
        "test": total_rows - round(total_rows * ratios[0]) - round(total_rows * ratios[1]),
    }
    assigned_rows = {split_name: 0 for split_name in SPLIT_NAMES}
    split_shards_map: dict[str, list[dict[str, object]]] = {
        split_name: [] for split_name in SPLIT_NAMES
    }

    shuffled = shards[:]
    random.Random(seed).shuffle(shuffled)
    ensure_clean_output(output_dir, overwrite=overwrite)

    link_modes = set()
    for shard in shuffled:
        source_path = Path(str(shard["file"]))
        if not source_path.is_absolute():
            source_path = source_dir / source_path.name
        if not source_path.exists():
            raise SystemExit(f"Missing source shard file: {source_path}")

        shard_rows = int(shard["rows"])
        split_name = choose_split(assigned_rows, target_rows, shard_rows)
        destination = output_dir / split_name / source_path.name
        link_mode = link_or_copy(source_path, destination, mode=mode)
        link_modes.add(link_mode)

        assigned_rows[split_name] += shard_rows
        split_shards_map[split_name].append(
            {
                "file": destination.as_posix(),
                "source_file": source_path.as_posix(),
                "rows": shard_rows,
                "size_bytes": destination.stat().st_size,
                "first_source_row": int(shard["first_source_row"]),
                "last_source_row": int(shard["last_source_row"]),
            }
        )

    split_manifest = {
        "source_manifest": (source_dir / "shards_manifest.json").as_posix(),
        "source_dir": source_dir.as_posix(),
        "output_dir": output_dir.as_posix(),
        "seed": seed,
        "ratios": {
            "train": ratios[0],
            "val": ratios[1],
            "test": ratios[2],
        },
        "link_modes": sorted(link_modes),
        "split_strategy": "deterministic shuffled whole-shard split",
        "total_rows": total_rows,
        "policy_label_count": manifest.get("policy_label_count"),
        "matrix_shape_per_row": manifest.get("matrix_shape_per_row"),
        "splits": {},
    }
    for split_name in SPLIT_NAMES:
        files = split_shards_map[split_name]
        split_manifest["splits"][split_name] = {
            "rows": assigned_rows[split_name],
            "shards": len(files),
            "path": (output_dir / split_name).as_posix(),
            "files": files,
        }

    (output_dir / "split_manifest.json").write_text(
        json.dumps(split_manifest, indent=2) + "\n",
        encoding="utf-8",
    )
    return split_manifest


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Split full chess-move matrix shards into train/val/test folders."
    )
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=DEFAULT_SOURCE_DIR,
        help="Folder containing full-data shards and shards_manifest.json.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help="Output folder for train/val/test split shards.",
    )
    parser.add_argument(
        "--ratios",
        default=",".join(str(item) for item in DEFAULT_RATIOS),
        help="Split ratios as train,val,test. Default: 0.8,0.1,0.1.",
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=DEFAULT_SEED,
        help="Seed used to shuffle shard assignment deterministically.",
    )
    parser.add_argument(
        "--mode",
        choices=("auto", "hardlink", "copy"),
        default="auto",
        help="Use hardlinks by default; auto falls back to copy if hardlink fails.",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Overwrite an existing split output folder.",
    )
    args = parser.parse_args()

    ratios = parse_ratios(args.ratios)
    split_manifest = split_shards(
        source_dir=args.source_dir,
        output_dir=args.output_dir,
        ratios=ratios,
        seed=args.seed,
        mode=args.mode,
        overwrite=args.overwrite,
    )

    print(f"Output dir: {args.output_dir}")
    print(f"Total rows: {split_manifest['total_rows']}")
    print(f"Link modes: {', '.join(split_manifest['link_modes'])}")
    for split_name in SPLIT_NAMES:
        info = split_manifest["splits"][split_name]
        print(f"- {split_name}: {info['rows']} rows, {info['shards']} shards")


if __name__ == "__main__":
    main()
