# Machine Learning Workspace

This folder is reserved for CNN experiments that sit beside the C# chess engine.
Keep the engine source under `Source/` unchanged, and place ML data, notebooks,
training scripts, model checkpoints, and experiment output here.

## Current dataset

- Dataset: Kaggle `krithiik/chess-pieces`
- Task fit: supervised image classification for chess piece recognition
- Classes: 12 piece-color classes, 25 JPG images per class
- Metadata path: `datasets/chess-pieces-kaggle/metadata`

Raw images are intentionally not kept in Git. Download them locally when needed:

```powershell
python -m pip install -r ml/requirements.txt
python ml/scripts/download_chess_pieces_dataset.py
python ml/scripts/prepare_chess_pieces_dataset.py
python ml/scripts/preprocess_chess_pieces_images.py --image-size 128 --overwrite
```

The preparation step creates deterministic `train`, `validation`, and `test`
CSV files for CNN training. Add `--copy-images` if a framework needs a physical
`processed/{split}/{label}` folder layout.

The preprocessing step creates CNN-ready inputs under
`datasets/chess-pieces-kaggle/processed/cnn_input_128`:

- `images/{split}/{label}/*.png`: RGB images resized to `128x128`
- `arrays/{split}.npz`: normalized `float32` tensors in `[0, 1]`
- `preprocessing_manifest.json`: input size, class list, and split counts

Generated outputs such as trained models, run logs, processed data, and train
image folders should be written under ignored folders like `ml/models/`,
`ml/runs/`, or `ml/datasets/**/processed/`.
