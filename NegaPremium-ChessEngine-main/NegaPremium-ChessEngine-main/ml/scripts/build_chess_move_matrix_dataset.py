from __future__ import annotations

import argparse
import csv
import json
from pathlib import Path

import chess
import numpy as np

DATASET_REF = "dev102/predicting-pro-chess-moves"
SOURCE_URL = "https://www.kaggle.com/datasets/dev102/predicting-pro-chess-moves"
ML_ROOT = Path(__file__).resolve().parents[1]
DATASET_DIR = ML_ROOT / "datasets" / "chess-moves-kaggle"
RAW_DIR = DATASET_DIR / "raw"
PROCESSED_DIR = DATASET_DIR / "processed" / "matrix"
DEFAULT_MAX_ROWS = 50000
DEFAULT_SHARD_SIZE = 50000
DEFAULT_PLANES = 18
TABULAR_EXTENSIONS = {".csv", ".tsv", ".txt"}
PROMOTION_LABELS = ("none", "queen", "rook", "bishop", "knight")
POLICY_LABEL_COUNT = 64 * 64 * len(PROMOTION_LABELS)

PIECE_PLANES = {
    (chess.PAWN, chess.WHITE): 0,
    (chess.KNIGHT, chess.WHITE): 1,
    (chess.BISHOP, chess.WHITE): 2,
    (chess.ROOK, chess.WHITE): 3,
    (chess.QUEEN, chess.WHITE): 4,
    (chess.KING, chess.WHITE): 5,
    (chess.PAWN, chess.BLACK): 6,
    (chess.KNIGHT, chess.BLACK): 7,
    (chess.BISHOP, chess.BLACK): 8,
    (chess.ROOK, chess.BLACK): 9,
    (chess.QUEEN, chess.BLACK): 10,
    (chess.KING, chess.BLACK): 11,
}

PROMOTION_TO_INDEX = {
    None: 0,
    chess.QUEEN: 1,
    chess.ROOK: 2,
    chess.BISHOP: 3,
    chess.KNIGHT: 4,
}


def open_text(path: Path):
    for encoding in ("utf-8-sig", "utf-8", "latin-1"):
        try:
            handle = path.open("r", encoding=encoding, newline="")
            handle.readline()
            handle.seek(0)
            return handle
        except UnicodeDecodeError:
            continue
    return path.open("r", encoding="utf-8", errors="replace", newline="")


def sniff_dialect(path: Path) -> csv.Dialect:
    if path.suffix.lower() == ".tsv":
        return csv.excel_tab

    with open_text(path) as input_file:
        sample = input_file.read(8192)

    try:
        return csv.Sniffer().sniff(sample, delimiters=",;\t|")
    except csv.Error:
        return csv.excel


def normalize_column_name(name: str) -> str:
    return "".join(char for char in name.lower() if char.isalnum())


def find_columns(fieldnames: list[str] | None) -> tuple[str | None, str | None]:
    if not fieldnames:
        return None, None

    normalized = {normalize_column_name(name): name for name in fieldnames}
    fen_column = None
    move_column = None

    for normalized_name, original_name in normalized.items():
        if normalized_name == "fen" or "fen" in normalized_name:
            fen_column = original_name
            break

    move_candidates = ("move", "bestmove", "uci", "nextmove")
    for normalized_name, original_name in normalized.items():
        if normalized_name in move_candidates or any(
            candidate in normalized_name for candidate in move_candidates
        ):
            move_column = original_name
            break

    return fen_column, move_column


def iter_tabular_files(raw_dir: Path) -> list[Path]:
    return sorted(
        path
        for path in raw_dir.rglob("*")
        if path.is_file() and path.suffix.lower() in TABULAR_EXTENSIONS
    )


def find_source_file(raw_dir: Path) -> tuple[Path, csv.Dialect, str, str, list[str]]:
    for path in iter_tabular_files(raw_dir):
        dialect = sniff_dialect(path)
        with open_text(path) as input_file:
            reader = csv.DictReader(input_file, dialect=dialect)
            fen_column, move_column = find_columns(reader.fieldnames)
            if fen_column and move_column:
                return path, dialect, fen_column, move_column, reader.fieldnames or []

    raise SystemExit(
        f"No tabular file with FEN and move columns was found under: {raw_dir}"
    )


def download_dataset(force_download: bool) -> Path:
    if iter_tabular_files(RAW_DIR) and not force_download:
        return RAW_DIR

    try:
        import kagglehub
    except ImportError as exc:
        raise SystemExit(
            "Missing dependency: install with `python -m pip install -r ml/requirements.txt`."
        ) from exc

    RAW_DIR.mkdir(parents=True, exist_ok=True)
    return Path(
        kagglehub.dataset_download(
            DATASET_REF,
            output_dir=str(RAW_DIR),
            force_download=force_download,
        )
    )


def square_to_matrix_coords(square: chess.Square) -> tuple[int, int]:
    return 7 - chess.square_rank(square), chess.square_file(square)


def board_to_matrix(board: chess.Board, planes: int) -> np.ndarray:
    matrix = np.zeros((8, 8, planes), dtype=np.float32)

    for square, piece in board.piece_map().items():
        row, col = square_to_matrix_coords(square)
        plane = PIECE_PLANES[(piece.piece_type, piece.color)]
        matrix[row, col, plane] = 1.0

    if planes >= 13:
        matrix[:, :, 12] = 1.0 if board.turn == chess.WHITE else 0.0
    if planes >= 17:
        matrix[:, :, 13] = 1.0 if board.has_kingside_castling_rights(chess.WHITE) else 0.0
        matrix[:, :, 14] = 1.0 if board.has_queenside_castling_rights(chess.WHITE) else 0.0
        matrix[:, :, 15] = 1.0 if board.has_kingside_castling_rights(chess.BLACK) else 0.0
        matrix[:, :, 16] = 1.0 if board.has_queenside_castling_rights(chess.BLACK) else 0.0
    if planes >= 18 and board.ep_square is not None:
        row, col = square_to_matrix_coords(board.ep_square)
        matrix[row, col, 17] = 1.0

    return matrix


def parse_best_move(board: chess.Board, raw_move: str) -> chess.Move:
    move_text = raw_move.strip()
    if not move_text:
        raise ValueError("empty move")

    if move_text.lower().startswith("bestmove "):
        move_text = move_text.split(None, 1)[1].strip()

    uci_candidate = move_text.lower()
    try:
        move = chess.Move.from_uci(uci_candidate)
        if move in board.legal_moves:
            return move
    except ValueError:
        pass

    return board.parse_san(move_text)


def move_to_policy_index(move: chess.Move) -> int:
    promotion_index = PROMOTION_TO_INDEX.get(move.promotion, 0)
    return ((move.from_square * 64) + move.to_square) * len(PROMOTION_LABELS) + promotion_index


def policy_index_to_parts(policy_index: int) -> tuple[int, int, int]:
    promotion_count = len(PROMOTION_LABELS)
    promotion_index = policy_index % promotion_count
    square_pair = policy_index // promotion_count
    to_square = square_pair % 64
    from_square = square_pair // 64
    return from_square, to_square, promotion_index


def clean_output_dir(output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    for pattern in ("*.npz", "*.manifest.json", "shards_manifest.json"):
        for path in output_dir.glob(pattern):
            path.unlink()


def new_batch() -> dict[str, list[object]]:
    return {
        "x": [],
        "y": [],
        "move_uci": [],
        "move_from_square": [],
        "move_to_square": [],
        "move_promotion": [],
        "fen": [],
        "source_rows": [],
    }


def add_to_batch(
    batch: dict[str, list[object]],
    matrix: np.ndarray,
    move: chess.Move,
    fen: str,
    source_row: int,
) -> None:
    promotion_index = PROMOTION_TO_INDEX.get(move.promotion, 0)
    batch["x"].append(matrix)
    batch["y"].append(move_to_policy_index(move))
    batch["move_uci"].append(move.uci())
    batch["move_from_square"].append(move.from_square)
    batch["move_to_square"].append(move.to_square)
    batch["move_promotion"].append(promotion_index)
    batch["fen"].append(fen)
    batch["source_rows"].append(source_row)


def batch_size(batch: dict[str, list[object]]) -> int:
    return len(batch["y"])


def write_npz(path: Path, batch: dict[str, list[object]]) -> dict[str, object]:
    x = np.stack(batch["x"]).astype(np.float32)
    y = np.asarray(batch["y"], dtype=np.int64)

    np.savez_compressed(
        path,
        x=x,
        y=y,
        move_uci=np.asarray(batch["move_uci"]),
        move_from_square=np.asarray(batch["move_from_square"], dtype=np.int64),
        move_to_square=np.asarray(batch["move_to_square"], dtype=np.int64),
        move_promotion=np.asarray(batch["move_promotion"], dtype=np.int64),
        fen=np.asarray(batch["fen"]),
        source_rows=np.asarray(batch["source_rows"], dtype=np.int64),
    )

    return {
        "file": path.as_posix(),
        "rows": int(y.shape[0]),
        "size_bytes": path.stat().st_size,
        "first_source_row": int(batch["source_rows"][0]),
        "last_source_row": int(batch["source_rows"][-1]),
    }


def iter_encoded_positions(
    source_path: Path,
    dialect: csv.Dialect,
    fen_column: str,
    move_column: str,
    planes: int,
    max_rows: int,
):
    accepted = 0
    skipped = 0

    with open_text(source_path) as input_file:
        reader = csv.DictReader(input_file, dialect=dialect)
        for source_row, row in enumerate(reader, start=2):
            fen = (row.get(fen_column) or "").strip()
            raw_move = (row.get(move_column) or "").strip()
            if not fen or not raw_move:
                skipped += 1
                continue

            try:
                board = chess.Board(fen)
                move = parse_best_move(board, raw_move)
                matrix = board_to_matrix(board, planes=planes)
            except (ValueError, AssertionError):
                skipped += 1
                continue

            accepted += 1
            yield {
                "source_row": source_row,
                "fen": fen,
                "move": move,
                "matrix": matrix,
                "accepted": accepted,
                "skipped": skipped,
            }

            if max_rows > 0 and accepted >= max_rows:
                break


def write_manifest(
    manifest_path: Path,
    source_path: Path,
    fieldnames: list[str],
    fen_column: str,
    move_column: str,
    planes: int,
    max_rows: int,
    shard_size: int,
    encoded_rows: int,
    skipped_rows: int,
    shards: list[dict[str, object]],
) -> None:
    manifest = {
        "name": "Chess move matrix dataset",
        "kaggle_ref": DATASET_REF,
        "source_url": SOURCE_URL,
        "task": "best_move_prediction",
        "source_file": source_path.as_posix(),
        "source_columns": fieldnames,
        "fen_column": fen_column,
        "move_column": move_column,
        "max_rows": max_rows,
        "shard_size": shard_size,
        "encoded_rows": encoded_rows,
        "skipped_rows": skipped_rows,
        "matrix_key": "x",
        "label_key": "y",
        "matrix_shape_per_row": [8, 8, planes],
        "matrix_dtype": "float32",
        "policy_label_count": POLICY_LABEL_COUNT,
        "move_format": "UCI",
        "promotion_labels": list(PROMOTION_LABELS),
        "label_encoding": {
            "formula": "y = ((from_square * 64) + to_square) * 5 + promotion_index",
            "from_square": "python-chess square index, a1=0 through h8=63",
            "to_square": "python-chess square index, a1=0 through h8=63",
            "promotion_index": "0 none, 1 queen, 2 rook, 3 bishop, 4 knight",
        },
        "board_planes": {
            "count": planes,
            "0_11": "piece planes: WP, WN, WB, WR, WQ, WK, BP, BN, BB, BR, BQ, BK",
            "12": "side to move, 1 for white and 0 for black",
            "13_16": "castling rights: white king-side, white queen-side, black king-side, black queen-side",
            "17": "en-passant target square",
        },
        "shards": shards,
    }
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def build_shards(
    source_path: Path,
    dialect: csv.Dialect,
    fen_column: str,
    move_column: str,
    fieldnames: list[str],
    output_dir: Path,
    shard_size: int,
    planes: int,
    max_rows: int,
    overwrite: bool,
) -> None:
    if shard_size <= 0:
        raise SystemExit("--shard-size must be greater than zero.")

    if output_dir.exists() and any(output_dir.glob("*.npz")) and not overwrite:
        raise SystemExit(
            f"Output shards already exist in: {output_dir}\n"
            "Pass `--overwrite` to rebuild them."
        )
    if overwrite:
        clean_output_dir(output_dir)
    else:
        output_dir.mkdir(parents=True, exist_ok=True)

    shards: list[dict[str, object]] = []
    batch = new_batch()
    shard_index = 1
    encoded_rows = 0
    skipped_rows = 0

    for item in iter_encoded_positions(
        source_path=source_path,
        dialect=dialect,
        fen_column=fen_column,
        move_column=move_column,
        planes=planes,
        max_rows=max_rows,
    ):
        add_to_batch(
            batch=batch,
            matrix=item["matrix"],
            move=item["move"],
            fen=str(item["fen"]),
            source_row=int(item["source_row"]),
        )
        encoded_rows = int(item["accepted"])
        skipped_rows = int(item["skipped"])

        if batch_size(batch) >= shard_size:
            shard_path = output_dir / f"chess_move_matrices_p{planes}_shard_{shard_index:06d}.npz"
            shard = write_npz(shard_path, batch)
            shards.append(shard)
            print(
                f"Wrote shard {shard_index:06d}: {shard['rows']} rows, total={encoded_rows}",
                flush=True,
            )
            batch = new_batch()
            shard_index += 1

    if batch_size(batch) > 0:
        shard_path = output_dir / f"chess_move_matrices_p{planes}_shard_{shard_index:06d}.npz"
        shard = write_npz(shard_path, batch)
        shards.append(shard)
        print(
            f"Wrote shard {shard_index:06d}: {shard['rows']} rows, total={encoded_rows}",
            flush=True,
        )

    if not shards:
        raise SystemExit("No valid FEN/move rows were encoded.")

    write_manifest(
        manifest_path=output_dir / "shards_manifest.json",
        source_path=source_path,
        fieldnames=fieldnames,
        fen_column=fen_column,
        move_column=move_column,
        planes=planes,
        max_rows=max_rows,
        shard_size=shard_size,
        encoded_rows=encoded_rows,
        skipped_rows=skipped_rows,
        shards=shards,
    )

    total_size_mb = sum(int(shard["size_bytes"]) for shard in shards) / (1024 * 1024)
    print(f"Finished shards: {len(shards)}")
    print(f"Encoded rows: {encoded_rows}")
    print(f"Skipped rows: {skipped_rows}")
    print(f"Output dir: {output_dir}")
    print(f"Total shard size: {total_size_mb:.2f} MB")


def build_single_file(
    source_path: Path,
    dialect: csv.Dialect,
    fen_column: str,
    move_column: str,
    fieldnames: list[str],
    output_file: Path,
    planes: int,
    max_rows: int,
    overwrite: bool,
) -> None:
    if output_file.exists() and not overwrite:
        raise SystemExit(
            f"Output already exists: {output_file}\n"
            "Pass `--overwrite` to rebuild it."
        )

    batch = new_batch()
    encoded_rows = 0
    skipped_rows = 0

    for item in iter_encoded_positions(
        source_path=source_path,
        dialect=dialect,
        fen_column=fen_column,
        move_column=move_column,
        planes=planes,
        max_rows=max_rows,
    ):
        add_to_batch(
            batch=batch,
            matrix=item["matrix"],
            move=item["move"],
            fen=str(item["fen"]),
            source_row=int(item["source_row"]),
        )
        encoded_rows = int(item["accepted"])
        skipped_rows = int(item["skipped"])

    if batch_size(batch) == 0:
        raise SystemExit("No valid FEN/move rows were encoded.")

    output_file.parent.mkdir(parents=True, exist_ok=True)
    shard = write_npz(output_file, batch)
    write_manifest(
        manifest_path=output_file.with_suffix(".manifest.json"),
        source_path=source_path,
        fieldnames=fieldnames,
        fen_column=fen_column,
        move_column=move_column,
        planes=planes,
        max_rows=max_rows,
        shard_size=batch_size(batch),
        encoded_rows=encoded_rows,
        skipped_rows=skipped_rows,
        shards=[shard],
    )

    print(f"Output matrix: {output_file}")
    print(f"x shape: ({encoded_rows}, 8, 8, {planes}), dtype: float32")
    print(f"y shape: ({encoded_rows},), dtype: int64")
    print(f"policy labels: {POLICY_LABEL_COUNT}")
    print(f"skipped rows: {skipped_rows}")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Download FEN/best-move data and encode it as CNN board matrices."
    )
    parser.add_argument(
        "--max-rows",
        type=int,
        default=DEFAULT_MAX_ROWS,
        help="Maximum valid rows to encode. Use 0 to encode every row.",
    )
    parser.add_argument(
        "--shard-size",
        type=int,
        default=0,
        help=(
            "Rows per output shard. Use a positive value to write many .npz files; "
            f"recommended default for full data is {DEFAULT_SHARD_SIZE}."
        ),
    )
    parser.add_argument(
        "--planes",
        type=int,
        choices=(12, 18),
        default=DEFAULT_PLANES,
        help="Use 12 piece planes only, or 18 planes with turn/castling/en-passant context.",
    )
    parser.add_argument(
        "--output-file",
        type=Path,
        default=None,
        help="Single .npz output path when --shard-size is 0.",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=None,
        help="Output folder for shards when --shard-size is positive.",
    )
    parser.add_argument(
        "--skip-download",
        action="store_true",
        help="Use existing raw files instead of downloading from Kaggle.",
    )
    parser.add_argument(
        "--force-download",
        action="store_true",
        help="Force KaggleHub to re-download the dataset.",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Overwrite existing output files.",
    )
    args = parser.parse_args()

    if args.max_rows < 0:
        raise SystemExit("--max-rows must be zero or greater.")
    if args.shard_size < 0:
        raise SystemExit("--shard-size must be zero or greater.")

    downloaded_path = RAW_DIR
    if not args.skip_download:
        downloaded_path = download_dataset(force_download=args.force_download)

    source_path, dialect, fen_column, move_column, fieldnames = find_source_file(RAW_DIR)
    print(f"Raw dataset: {downloaded_path}")
    print(f"Source file: {source_path}")
    print(f"FEN column: {fen_column}")
    print(f"Move column: {move_column}")

    if args.shard_size > 0:
        row_suffix = "all" if args.max_rows == 0 else str(args.max_rows)
        output_dir = args.output_dir or (
            PROCESSED_DIR / f"shards_{row_suffix}_p{args.planes}_n{args.shard_size}"
        )
        build_shards(
            source_path=source_path,
            dialect=dialect,
            fen_column=fen_column,
            move_column=move_column,
            fieldnames=fieldnames,
            output_dir=output_dir,
            shard_size=args.shard_size,
            planes=args.planes,
            max_rows=args.max_rows,
            overwrite=args.overwrite,
        )
        return

    row_suffix = "all" if args.max_rows == 0 else str(args.max_rows)
    output_file = args.output_file or (
        PROCESSED_DIR / f"chess_move_matrices_{row_suffix}_p{args.planes}.npz"
    )
    build_single_file(
        source_path=source_path,
        dialect=dialect,
        fen_column=fen_column,
        move_column=move_column,
        fieldnames=fieldnames,
        output_file=output_file,
        planes=args.planes,
        max_rows=args.max_rows,
        overwrite=args.overwrite,
    )


if __name__ == "__main__":
    main()
