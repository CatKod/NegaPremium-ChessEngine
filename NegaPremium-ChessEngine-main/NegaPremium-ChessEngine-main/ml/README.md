# Không gian Machine Learning

Thư mục này dùng cho phần thử nghiệm CNN bên cạnh chess engine C#.
Không đặt dữ liệu hoặc model ML vào `Source/`; phần source engine C# nên giữ
riêng, còn dữ liệu, script xử lý, checkpoint model và kết quả thí nghiệm đặt ở
đây.

## Dataset hiện tại

- Dataset: Kaggle `dev102/predicting-pro-chess-moves`
- Bài toán: học có giám sát để dự đoán nước đi tốt nhất từ một thế cờ
- Input: thế cờ dạng FEN được mã hóa thành ma trận bàn cờ
- Output cần dự đoán: nước đi tốt nhất dạng UCI, ví dụ `e2e4`

File raw tải từ Kaggle không được lưu vào Git để repo không bị nặng.

Để tạo nhanh một file mẫu 50,000 dòng:

```powershell
python -m pip install -r ml/requirements.txt
python ml/scripts/build_chess_move_matrix_dataset.py --max-rows 50000 --overwrite
```

Để xử lý toàn bộ dataset và chia thành nhiều file:

```powershell
python ml/scripts/build_chess_move_matrix_dataset.py --max-rows 0 --shard-size 50000 --skip-download --overwrite
```

Sau khi có toàn bộ shard, chia thành `train`, `val`, `test`:

```powershell
python ml/scripts/split_chess_move_shards.py --overwrite
```

Script trên sẽ tải dataset FEN/nước đi nếu máy chưa có, chuyển từng FEN thành
ma trận số của bàn cờ, đọc nước đi tốt nhất, rồi ghi ra các file `.npz` sẵn
sàng đưa vào CNN.

File mẫu 50,000 dòng nằm ở:

```text
datasets/chess-moves-kaggle/processed/matrix/chess_move_matrices_50000_p18.npz
```

Các shard toàn bộ dataset nằm ở:

```text
datasets/chess-moves-kaggle/processed/matrix/shards_all_p18_n50000
```

Các split train/val/test nằm ở:

```text
datasets/chess-moves-kaggle/processed/matrix/splits_80_10_10_p18_n50000
```

Kết quả split mặc định:

- `train`: 8,965,932 dòng, 180 shard
- `val`: 1,100,000 dòng, 22 shard
- `test`: 1,100,000 dòng, 22 shard

Trong mỗi file `.npz` có:

- `x`: ma trận bàn cờ, shape `(N, 8, 8, 18)`, kiểu dữ liệu `float32`
- `y`: mã policy cố định của nước đi tốt nhất
- `move_uci`: nước đi tốt nhất dạng UCI, ví dụ `g1f3`
- `move_from_square`, `move_to_square`, `move_promotion`

Mã `y` được tính theo công thức:

```text
y = ((from_square * 64) + to_square) * 5 + promotion_index
```

Trong đó `promotion_index` là `0` nếu không phong cấp, `1` hậu, `2` xe,
`3` tượng, `4` mã. Cách mã hóa này giúp mọi shard dùng chung một không gian
nhãn gồm `20480` giá trị.

Các file sinh ra như model đã train, log chạy, dữ liệu đã xử lý và ma trận đầu
ra nên đặt trong những thư mục đã bị ignore như `ml/models/`, `ml/runs/`, hoặc
`ml/datasets/**/processed/`.
