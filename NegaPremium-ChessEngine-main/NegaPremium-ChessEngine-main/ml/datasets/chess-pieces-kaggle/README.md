# Kaggle Chess Pieces Dataset

Source: https://www.kaggle.com/datasets/krithiik/chess-pieces

This dataset was added for CNN-based chess piece recognition experiments. It is
kept outside the C# engine source so model training can evolve independently
from the existing WinForms chess engine.

## Dataset summary

- Kaggle ref: `krithiik/chess-pieces`
- License: Community Data License Agreement - Permissive - Version 1.0
- Downloaded on: 2026-06-21
- Image task: classification
- Image format: JPG
- Image size from Kaggle metadata: 640x640
- Total images: 300
- Class count: 12
- Images per class: 25

## Training-ready path

Use this folder as the class root:

```text
ml/datasets/chess-pieces-kaggle/raw/all_resized_into_sub_folders_640
```

Its structure follows the standard class-per-folder layout:

```text
all_resized_into_sub_folders_640/
  Black bishop/
  Black king/
  Black knight/
  Black pawn/
  Black queen/
  Black rook/
  White bishop/
  White king/
  White knight/
  White pawn/
  White queen/
  White rook/
```

## Re-download

From the project root, run:

```powershell
python -m pip install -r ml/requirements.txt
python ml/scripts/download_chess_pieces_dataset.py
```

Keep `raw/` read-only in training code. Put train/validation/test splits,
augmented images, and resized variants under `processed/` or `splits/`.
