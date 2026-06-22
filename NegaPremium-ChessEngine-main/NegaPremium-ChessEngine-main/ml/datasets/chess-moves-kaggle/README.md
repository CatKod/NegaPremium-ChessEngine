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

File đầu ra được ghi tại:

```text
ml/datasets/chess-moves-kaggle/processed/matrix/chess_move_matrices_50000_p18.npz
```

Bên trong file `.npz`:

- `x`: ma trận bàn cờ, shape `(N, 8, 8, 18)`
- `y`: nhãn số của nước đi tốt nhất
- `move_labels`: dùng để đổi nhãn `y` về nước đi UCI
- `move_uci`: nước đi tốt nhất gốc của từng dòng
- `move_from_square`, `move_to_square`, `move_promotion`: nhãn phụ nếu muốn train model theo hướng dự đoán ô đi/tới
- `fen`: FEN gốc của từng dòng

Ý nghĩa 18 lớp trong ma trận:

- `0..11`: vị trí các quân, gồm quân trắng rồi quân đen theo thứ tự tốt, mã, tượng, xe, hậu, vua
- `12`: bên tới lượt đi
- `13..16`: quyền nhập thành
- `17`: ô en-passant nếu có
