# Dataset Ma Trận Cờ Và Nước Đi Tốt Nhất

Nguồn: https://www.kaggle.com/datasets/dev102/predicting-pro-chess-moves

Dataset này dùng để tạo input cho CNN. Mỗi dòng dữ liệu gồm một thế cờ và nước
đi tốt nhất tương ứng. Script sẽ chuyển thế cờ thành ma trận số, còn nước đi
tốt nhất được mã hóa thành nhãn để model học dự đoán.

## Tóm tắt dataset

- Kaggle ref: `dev102/predicting-pro-chess-moves`
- License: CC0: Public Domain
- Cột dữ liệu gốc: `FEN`, `Move`
- Định dạng nước đi: UCI, ví dụ `e2e4`
- Dung lượng raw theo Kaggle API: khoảng 673 MB
- Raw data có commit vào Git không: không

## Tạo input ma trận số

Chạy từ thư mục gốc của project:

```powershell
python -m pip install -r ml/requirements.txt
python ml/scripts/build_chess_move_matrix_dataset.py --max-rows 50000 --overwrite
```

Lệnh trên tạo một file mẫu 50,000 dòng tại:

```text
ml/datasets/chess-moves-kaggle/processed/matrix/chess_move_matrices_50000_p18.npz
```

Để xử lý toàn bộ dataset và chia thành nhiều file:

```powershell
python ml/scripts/build_chess_move_matrix_dataset.py --max-rows 0 --shard-size 50000 --skip-download --overwrite
```

Sau đó chia toàn bộ shard thành `train`, `val`, `test`:

```powershell
python ml/scripts/split_chess_move_shards.py --overwrite
```

Các shard được ghi vào:

```text
ml/datasets/chess-moves-kaggle/processed/matrix/shards_all_p18_n50000
```

Các split được ghi vào:

```text
ml/datasets/chess-moves-kaggle/processed/matrix/splits_80_10_10_p18_n50000
```

Kết quả split mặc định dùng seed `42` và tỉ lệ gần `80/10/10`:

- `train`: 8,965,932 dòng, 180 shard
- `val`: 1,100,000 dòng, 22 shard
- `test`: 1,100,000 dòng, 22 shard

Script split dùng hardlink nếu hệ thống hỗ trợ, nên có đủ file trong từng thư
mục split nhưng không nhân đôi dung lượng thật trên ổ đĩa.

Bên trong mỗi file `.npz`:

- `x`: ma trận bàn cờ, shape `(N, 8, 8, 18)`
- `y`: mã policy cố định của nước đi tốt nhất
- `move_uci`: nước đi tốt nhất gốc của từng dòng
- `move_from_square`, `move_to_square`, `move_promotion`: nhãn phụ nếu muốn train model theo hướng dự đoán ô đi/tới
- `fen`: FEN gốc của từng dòng

Mã `y` được tính theo công thức:

```text
y = ((from_square * 64) + to_square) * 5 + promotion_index
```

Trong đó `promotion_index` là `0` nếu không phong cấp, `1` hậu, `2` xe,
`3` tượng, `4` mã. Tổng số nhãn policy cố định là `20480`.

Ý nghĩa 18 lớp trong ma trận:

- `0..11`: vị trí các quân, gồm quân trắng rồi quân đen theo thứ tự tốt, mã, tượng, xe, hậu, vua
- `12`: bên tới lượt đi
- `13..16`: quyền nhập thành
- `17`: ô en-passant nếu có
