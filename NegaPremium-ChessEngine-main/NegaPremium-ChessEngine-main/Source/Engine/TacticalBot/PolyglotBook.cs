using System;
using System.Collections.Generic;
using System.IO;

namespace NegaPremium
{
    /// <summary>
    /// Bộ đọc Sách khai cuộc chuẩn Polyglot (Định dạng .bin)
    /// Polyglot Opening Book Reader (.bin format)
    /// </summary>
    public static class PolyglotBook
    {
        // Mảng lưu 781 khóa ngẫu nhiên chuẩn quốc tế của Polyglot
        // Array storing 781 standard random keys of Polyglot
        private static readonly ulong[] Random64 = new ulong[781];

        static PolyglotBook()
        {
            // Trình tạo số giả ngẫu nhiên (PRNG) chuẩn của Polyglot để sinh khóa
            // Standard Polyglot PRNG to generate keys
            ulong seed = 1070372;
            for (int i = 0; i < 781; i++)
            {
                seed ^= seed >> 12;
                seed ^= seed << 25;
                seed ^= seed >> 27;
                // Sử dụng 'unchecked' để ép C# cho phép tràn số an toàn như C++
                // Use 'unchecked' to allow safe integer overflow like C++
                Random64[i] = unchecked(seed * 2685821657736338717UL);
            }
        }

        /// <summary>
        /// Sinh mã băm từ chuỗi FEN để đồng bộ tuyệt đối với sách.
        /// Generate hash key from FEN string for absolute synchronization with the book.
        /// </summary>
        public static ulong GetKeyFromFEN(string fen)
        {
            ulong key = 0;
            string[] parts = fen.Split(' ');

            // 1. Phân tích vị trí quân cờ / Parse piece positions
            int rank = 7;
            int file = 0;
            foreach (char c in parts[0])
            {
                if (c == '/') { rank--; file = 0; }
                else if (char.IsDigit(c)) { file += c - '0'; }
                else
                {
                    int polyPiece = "pPnNbBrRqQkK".IndexOf(c);
                    if (polyPiece != -1)
                    {
                        // Công thức ô cờ: a1 = 0, h8 = 63.
                        // Square formula: a1 = 0, h8 = 63.
                        int square = 8 * rank + file;
                        key ^= Random64[64 * polyPiece + square];
                    }
                    file++;
                }
            }

            // 2. Quyền nhập thành / Castling rights
            if (parts.Length > 2)
            {
                string castling = parts[2];
                if (castling.Contains("K")) key ^= Random64[768];
                if (castling.Contains("Q")) key ^= Random64[769];
                if (castling.Contains("k")) key ^= Random64[770];
                if (castling.Contains("q")) key ^= Random64[771];
            }

            // 3. Bắt tốt qua đường / En Passant
            if (parts.Length > 3)
            {
                string ep = parts[3];
                if (ep != "-")
                {
                    int epFile = ep[0] - 'a';
                    key ^= Random64[772 + epFile];
                }
            }

            // 4. Lượt đi hiện tại / Side to move
            // Polyglot chỉ mã hóa (XOR) khi đến lượt Trắng đi / Polyglot only XORs when it's White's turn
            if (parts.Length > 1 && parts[1] == "w")
            {
                key ^= Random64[780];
            }

            return key;
        }

        /// <summary>
        /// Dùng thuật toán Tìm kiếm Nhị phân để quét sách và chọn nước có Weight cao nhất.
        /// Use Binary Search to scan the book and select the move with the highest Weight.
        /// </summary>
        public static string GetBestMove(string bookPath, ulong polyglotKey)
        {
            if (!File.Exists(bookPath)) return null;

            try
            {
                using (FileStream fs = new FileStream(bookPath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    long low = 0;
                    long high = (fs.Length / 16) - 1;
                    long firstMatch = -1;

                    // 1. Tìm vị trí đầu tiên khớp mã băm / Find the first matched hash
                    while (low <= high)
                    {
                        long mid = low + (high - low) / 2;
                        fs.Seek(mid * 16, SeekOrigin.Begin);

                        ulong key = ReadEndian(br.ReadUInt64());

                        if (key < polyglotKey) low = mid + 1;
                        else if (key > polyglotKey) high = mid - 1;
                        else
                        {
                            firstMatch = mid;
                            break; // Đã tìm thấy một bản ghi / Found an entry
                        }
                    }

                    if (firstMatch != -1)
                    {
                        // 2. Quét lùi để tìm bản ghi đầu tiên của thế cờ này
                        // Scan backward to find the very first entry of this position
                        long start = firstMatch;
                        while (start > 0)
                        {
                            fs.Seek((start - 1) * 16, SeekOrigin.Begin);
                            if (ReadEndian(br.ReadUInt64()) == polyglotKey) start--;
                            else break;
                        }

                        // 3. Quét tới để chọn nước đi có Trọng số (Weight) cao nhất
                        // Scan forward to select the move with the highest Weight
                        string bestMove = null;
                        int maxWeight = -1;

                        fs.Seek(start * 16, SeekOrigin.Begin);
                        while (fs.Position < fs.Length)
                        {
                            ulong key = ReadEndian(br.ReadUInt64());
                            if (key != polyglotKey) break; // Hết thế cờ này / End of this position

                            ushort moveData = (ushort)((br.ReadByte() << 8) | br.ReadByte());
                            ushort weight = (ushort)((br.ReadByte() << 8) | br.ReadByte());
                            br.ReadUInt32(); // Bỏ qua Learn / Skip Learn bytes

                            // Nếu trọng số cao hơn thì cập nhật nước đi tốt nhất
                            // Update best move if the weight is higher
                            if (weight > maxWeight)
                            {
                                maxWeight = weight;
                                bestMove = DecodeMove(moveData);
                            }
                        }
                        return bestMove;
                    }
                }
            }
            catch (Exception) { return null; }

            return null;
        }

        // Đảo Byte (Do Polyglot lưu kiểu Big-Endian, Windows dùng Little-Endian)
        // Byte swapping (Polyglot uses Big-Endian, Windows uses Little-Endian)
        private static ulong ReadEndian(ulong value)
        {
            return ((value & 0x00000000000000FFUL) << 56) |
                   ((value & 0x000000000000FF00UL) << 40) |
                   ((value & 0x0000000000FF0000UL) << 24) |
                   ((value & 0x00000000FF000000UL) << 8) |
                   ((value & 0x000000FF00000000UL) >> 8) |
                   ((value & 0x0000FF0000000000UL) >> 24) |
                   ((value & 0x00FF000000000000UL) >> 40) |
                   ((value & 0xFF00000000000000UL) >> 56);
        }

        private static string DecodeMove(ushort move)
        {
            int toFile = move & 7;
            int toRow = (move >> 3) & 7;
            int fromFile = (move >> 6) & 7;
            int fromRow = (move >> 9) & 7;
            int promotion = (move >> 12) & 7;

            char[] files = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
            char[] rows = { '1', '2', '3', '4', '5', '6', '7', '8' };

            string result = $"{files[fromFile]}{rows[fromRow]}{files[toFile]}{rows[toRow]}";

            if (promotion != 0)
            {
                char[] promos = { '?', 'n', 'b', 'r', 'q' };
                result += promos[promotion];
            }
            return result;
        }
    }
}