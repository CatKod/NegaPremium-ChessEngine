Nega Premium
=============

Nega Premium là một engine cờ vua sử dụng đồ họa PNG để hiển thị quân cờ, được viết bằng C#. Nó được phát triển từ đầu để tìm hiểu về lập trình cờ vua và tìm kiếm cây trò chơi, với giao diện đồ họa thân thiện với người dùng. Theo mặc định, nó chạy với GUI riêng và sử dụng hình ảnh PNG chất lượng cao cho các quân cờ, đồng thời hỗ trợ giao thức UCI khi được cung cấp tham số dòng lệnh -u. Trong chế độ UCI/dòng lệnh, nó cũng chấp nhận các lệnh như perft và divide.

![Demo image](image.png)

Các tính năng chung:
- Chạy với GUI riêng theo mặc định
- Chạy ở chế độ UCI/dòng lệnh với đối số -u
- Cung cấp chế độ phân tích với multi PV trong GUI
- Chấp nhận các lệnh perft và divide trong chế độ dòng lệnh

Các tính năng tìm kiếm:
- Tìm kiếm biến thể chính
- Làm sâu hơn lặp đi lặp lại
- Bảng chuyển vị
- Heuristic nước đi vô hiệu
- Heuristic nước đi sát thủ
- Heuristic MVV/LVA
- Cắt tỉa phù phiếm
- Giảm nước đi muộn
- Tìm kiếm yên tĩnh với SEE
- Phát hiện hòa
- Cắt tỉa khoảng cách chiếu hết
- Heuristic kiểm soát thời gian
- Multi PV

Các tính năng đánh giá:
- Nội suy giai đoạn
- Bảng quân-ô
- Đánh giá khả năng di chuyển
- Đánh giá cấu trúc tốt
- Đánh giá bắt quân đơn giản

**Hướng dẫn chạy chương trình bằng Visual Studio 2022:**

1. **Cài đặt Visual Studio 2022:**
   - Tải và cài đặt Visual Studio 2022 (Community Edition)
   - Trong quá trình cài đặt, chọn workload ".NET Desktop Development"

2. **Mở và chạy project:**
   - Mở file `Nega Premium.sln`
   - Chọn Build > Build Solution (hoặc nhấn F6)
   - Nhấn F5 để chạy chương trình
   - Để chạy ở chế độ UCI: Chuột phải vào Project > Properties > Debug > Command line arguments: thêm "-u"

**Chi tiết các thuật toán AI chính:**

1. **Thuật toán Minimax và Alpha-Beta Pruning:**
   - **Nguyên lý Minimax:**
     + Là thuật toán tìm kiếm đối kháng (adversarial search)
     + MAX (máy) tìm nước đi tối đa hóa lợi thế
     + MIN (đối thủ) tìm nước đi tối thiểu hóa lợi thế
     + Độ sâu tìm kiếm: Không giới hạn cứng, phụ thuộc vào thời gian cho phép
     + Thời gian mặc định: 3000ms/nước đi
     + Có thể đạt độ sâu 12-15 ply trong thời gian tìm kiếm tiêu chuẩn
   
   - **Kiểm soát độ sâu:**
     + Sử dụng Iterative Deepening để tối ưu thời gian
     + Bắt đầu từ độ sâu 1, tăng dần đến khi hết thời gian
     + Lưu best move của mỗi độ sâu để đảm bảo luôn có nước đi tốt nhất có thể
     + Tự động điều chỉnh độ sâu dựa trên độ phức tạp của vị trí

2. **Đánh giá vị trí (Evaluation):**
   - Giá trị quân: Tốt(1), Mã(3), Tượng(3.25), Xe(5), Hậu(9)
   - Vị trí quân trên bàn cờ (Piece Square Tables)
   - Kiểm soát trung tâm
   - Cấu trúc tốt (Pawn Structure)
   - Bảo vệ vua (King Safety)

3. **Bảng chuyển vị (Transposition Table):**
   - Sử dụng Zobrist Hashing
   - Lưu trữ vị trí đã phân tích
   - Kích thước mặc định: 64MB

4. **Tối ưu tìm kiếm:**
   - Null Move Pruning: Bỏ qua các nước đi không có lợi
   - Quiescence Search: Tránh horizon effect
   - Late Move Reduction: Giảm độ sâu cho các nước đi kém
   - Killer Move Heuristic: Ưu tiên các nước đi đã gây khó khăn

5. **Sắp xếp nước đi (Move Ordering):**
   - PV Move: Nước từ Principal Variation
   - Captures: MVV/LVA (Most Valuable Victim/Least Valuable Attacker)
   - Killer Moves
   - History Heuristic

6. **Kiểm soát thời gian:**
   - Iterative Deepening
   - Time Management dựa trên số nước đi còn lại
   - Instant Move khi chỉ có 1 nước đi hợp lệ

Các tính năng đồ họa:
- Hiển thị quân cờ bằng hình ảnh PNG chất lượng cao
- Animations cho các nước đi
- Giao diện người dùng trực quan
- Hỗ trợ xoay bàn cờ
- Đánh dấu các nước đi hợp lệ
