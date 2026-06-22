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
DEFAULT_PLANES = 18
TABULAR_EXTENSIONS = {".csv", ".tsv", ".txt"}
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


def iter_encoded_rows(
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
                "skipped": skipped,
            }

            if max_rows > 0 and accepted >= max_rows:
                break


def build_dataset(
    source_path: Path,
    dialect: csv.Dialect,
    fen_column: str,
    move_column: str,
    planes: int,
    max_rows: int,
) -> dict[str, object]:
    matrices: list[np.ndarray] = []
    move_uci: list[str] = []
    move_from: list[int] = []
    move_to: list[int] = []
    move_promotion: list[int] = []
    fens: list[str] = []
    source_rows: list[int] = []
    skipped = 0

    for item in iter_encoded_rows(
        source_path=source_path,
        dialect=dialect,
        fen_column=fen_column,
        move_column=move_column,
        planes=planes,
        max_rows=max_rows,
    ):
        move = item["move"]
        matrices.append(item["matrix"])
        move_uci.append(move.uci())
        move_from.append(move.from_square)
        move_to.append(move.to_square)
        move_promotion.append(PROMOTION_TO_INDEX.get(move.promotion, 0))
        fens.append(str(item["fen"]))
        source_rows.append(int(item["source_row"]))
        skipped = int(item["skipped"])

    if not matrices:
        raise SystemExit("No valid FEN/move rows were encoded.")

    move_labels = sorted(set(move_uci))
    move_label_to_index = {move: index for index, move in enumerate(move_labels)}
    y = np.asarray([move_label_to_index[move] for move in move_uci], dtype=np.int64)

    return {
        "x": np.stack(matrices).astype(np.float32),
        "y": y,
        "move_uci": np.asarray(move_uci),
        "move_from_square": np.asarray(move_from, dtype=np.int64),
        "move_to_square": np.asarray(move_to, dtype=np.int64),
        "move_promotion": np.asarray(move_promotion, dtype=np.int64),
        "move_labels": np.asarray(move_labels),
        "fen": np.asarray(fens),
        "source_rows": np.asarray(source_rows, dtype=np.int64),
        "skipped": skipped,
    }


def write_outputs(
    output_file: Path,
    dataset: dict[str, object],
    source_path: Path,
    fieldnames: list[str],
    fen_column: str,
    move_column: str,
    planes: int,
    max_rows: int,
) -> None:
    output_file.parent.mkdir(parents=True, exist_ok=True)
    np.savez_compressed(
        output_file,
        x=dataset["x"],
        y=dataset["y"],
        move_uci=dataset["move_uci"],
        move_from_square=dataset["move_from_square"],
        move_to_square=dataset["move_to_square"],
        move_promotion=dataset["move_promotion"],
        move_labels=dataset["move_labels"],
        fen=dataset["fen"],
        source_rows=dataset["source_rows"],
    )

    x = dataset["x"]
    manifest = {
        "name": "Chess move matrix dataset",
        "kaggle_ref": DATASET_REF,
        "source_url": SOURCE_URL,
        "task": "best_move_prediction",
        "source_file": source_path.as_posix(),
        "source_columns": fieldnames,
        "fen_column": fen_column,
        "move_column": move_column,
        "output_file": output_file.as_posix(),
        "max_rows": max_rows,
        "encoded_rows": int(x.shape[0]),
        "skipped_rows": int(dataset["skipped"]),
        "matrix_key": "x",
        "label_key": "y",
        "matrix_shape": list(x.shape),
        "matrix_dtype": "float32",
        "move_label_count": int(len(dataset["move_labels"])),
        "move_format": "UCI",
        "board_planes": {
            "count": planes,
            "0_11": "piece planes: WP, WN, WB, WR, WQ, WK, BP, BN, BB, BR, BQ, BK",
            "12": "side to move, 1 for white and 0 for black",
            "13_16": "castling rights: white king-side, white queen-side, black king-side, black queen-side",
            "17": "en-passant target square",
        },
        "labels": {
            "y": "index into move_labels",
            "move_from_square": "python-chess square index, a1=0 through h8=63",
            "move_to_square": "python-chess square index, a1=0 through h8=63",
            "move_promotion": "0 none, 1 queen, 2 rook, 3 bishop, 4 knight",
        },
    }
    output_file.with_suffix(".manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )


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
        help="Output .npz file. Defaults to processed/matrix/chess_move_matrices_<rows>.npz.",
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
        help="Overwrite an existing output .npz file.",
    )
    args = parser.parse_args()

    if args.max_rows < 0:
        raise SystemExit("--max-rows must be zero or greater.")

    row_suffix = "all" if args.max_rows == 0 else str(args.max_rows)
    output_file = args.output_file or (
        PROCESSED_DIR / f"chess_move_matrices_{row_suffix}_p{args.planes}.npz"
    )
    if output_file.exists() and not args.overwrite:
        raise SystemExit(
            f"Output already exists: {output_file}\n"
            "Pass `--overwrite` to rebuild it."
        )

    downloaded_path = RAW_DIR
    if not args.skip_download:
        downloaded_path = download_dataset(force_download=args.force_download)

    source_path, dialect, fen_column, move_column, fieldnames = find_source_file(RAW_DIR)
    dataset = build_dataset(
        source_path=source_path,
        dialect=dialect,
        fen_column=fen_column,
        move_column=move_column,
        planes=args.planes,
        max_rows=args.max_rows,
    )
    write_outputs(
        output_file=output_file,
        dataset=dataset,
        source_path=source_path,
        fieldnames=fieldnames,
        fen_column=fen_column,
        move_column=move_column,
        planes=args.planes,
        max_rows=args.max_rows,
    )

    x = dataset["x"]
    y = dataset["y"]
    print(f"Raw dataset: {downloaded_path}")
    print(f"Source file: {source_path}")
    print(f"Output matrix: {output_file}")
    print(f"x shape: {x.shape}, dtype: {x.dtype}")
    print(f"y shape: {y.shape}, dtype: {y.dtype}")
    print(f"move labels: {len(dataset['move_labels'])}")
    print(f"skipped rows: {dataset['skipped']}")


if __name__ == "__main__":
    main()
