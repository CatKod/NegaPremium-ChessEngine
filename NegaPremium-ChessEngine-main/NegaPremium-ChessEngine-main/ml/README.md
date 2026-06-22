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

File raw tải từ Kaggle không được lưu vào Git để repo không bị nặng. Khi cần
tạo tập input cho CNN, chạy:

```powershell
python -m pip install -r ml/requirements.txt
python ml/scripts/build_chess_move_matrix_dataset.py --max-rows 50000 --overwrite
```

Script trên sẽ tải dataset FEN/nước đi nếu máy chưa có, chuyển từng FEN thành
ma trận số của bàn cờ, đọc nước đi tốt nhất, rồi ghi ra file `.npz` sẵn sàng
đưa vào CNN.

File kết quả nằm ở:

```text
datasets/chess-moves-kaggle/processed/matrix/chess_move_matrices_50000_p18.npz
```

Trong file `.npz` có:

- `x`: ma trận bàn cờ, shape `(N, 8, 8, 18)`, kiểu dữ liệu `float32`
- `y`: nhãn số của nước đi tốt nhất
- `move_labels`: bảng ánh xạ từ nhãn `y` về nước đi UCI
- `move_uci`, `move_from_square`, `move_to_square`, `move_promotion`

Các file sinh ra như model đã train, log chạy, dữ liệu đã xử lý và ma trận đầu
ra nên đặt trong những thư mục đã bị ignore như `ml/models/`, `ml/runs/`, hoặc
`ml/datasets/**/processed/`.
