# Training chess move model

Run a CNN classifier on the processed chess matrix dataset.

## Example

```powershell
python ml/train/train_chess_move_model.py --dataset ml/datasets/chess-moves-kaggle/processed/matrix/chess_move_matrices_50000_p18.npz --epochs 15 --batch-size 64
```

Outputs are written to `ml/models/chess_move_matrix_cnn/` by default:

- `model.pt`
- `config.json`
- `metadata.json`

The model learns to classify the best move from an encoded board state.
