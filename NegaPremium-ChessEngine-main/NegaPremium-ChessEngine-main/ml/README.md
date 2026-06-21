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
```

The preparation step creates deterministic `train`, `validation`, and `test`
CSV files for CNN training. Add `--copy-images` if a framework needs a physical
`processed/{split}/{label}` folder layout.

Generated outputs such as trained models, run logs, processed data, and train
image folders should be written under ignored folders like `ml/models/`,
`ml/runs/`, or `ml/datasets/**/processed/`.
