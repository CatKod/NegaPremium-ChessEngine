# Kaggle Chess Pieces Dataset

Source: https://www.kaggle.com/datasets/krithiik/chess-pieces

This dataset was added for CNN-based chess piece recognition experiments. It is
kept outside the C# engine source so model training can evolve independently
from the existing WinForms chess engine.

## Dataset summary

- Kaggle ref: `krithiik/chess-pieces`
- License: Community Data License Agreement - Permissive - Version 1.0
- Source metadata captured on: 2026-06-21
- Image task: classification
- Image format: JPG
- Image size from Kaggle metadata: 640x640
- Total images: 300
- Class count: 12
- Images per class: 25

## Tracked files

This folder keeps only lightweight, reviewable dataset metadata:

```text
dataset_manifest.json
metadata/all.csv
metadata/train.csv
metadata/validation.csv
metadata/test.csv
metadata/class_indices.json
```

Raw Kaggle images are not committed to Git. This keeps the repository small and
avoids storing large binary files in history.

## Local raw path

After download, the image class root is:

```text
ml/datasets/chess-pieces-kaggle/raw/all_resized_into_sub_folders_640
```

That path follows the standard class-per-folder layout used by
`tf.keras.utils.image_dataset_from_directory(...)` and
`torchvision.datasets.ImageFolder(...)`.

## Download and prepare

From the project root, run:

```powershell
python -m pip install -r ml/requirements.txt
python ml/scripts/download_chess_pieces_dataset.py
python ml/scripts/prepare_chess_pieces_dataset.py
```

The default split is deterministic with seed `42`:

- Train: 204 images
- Validation: 48 images
- Test: 48 images

Use `--copy-images` on the prepare script when a training framework needs a
physical `processed/{split}/{label}` folder layout. Keep `raw/` read-only in
training code.
