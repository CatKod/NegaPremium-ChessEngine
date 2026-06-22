using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NegaPremium
{
    /// <summary>
    /// Bot "Chiến binh kỷ luật" (Tactical Alpha-Beta).
    /// Phiên bản Tối ưu hóa API Bitwise có hỗ trợ Sách Khai Cuộc Polyglot.
    /// Bitwise API Optimized Version with Polyglot Opening Book support.
    /// </summary>
    public sealed class TacticalBot : IPlayer
    {
        public String Name
        {
            get { return "Tactical Alpha-Beta"; }
        }

        public Boolean IsAI
        {
            get { return true; }
        }

        public Boolean AcceptsDraw
        {
            get { return false; }
        }

        private Boolean _abortSearch;
        private Int32 _nodes;
        private Int32 _timeLimit;
        private Int32 _currentPly;
        private Stopwatch _stopwatch;
        private Int32 _bestScore;

        // Evaluation Noise
        private static readonly System.Random _rand = new System.Random();

        private readonly Int32[][] _pvMoves = new Int32[128][];
        private readonly Int32[] _pvLength = new Int32[128];

        public TacticalBot()
        {
            for (Int32 i = 0; i < 128; i++)
            {
                _pvMoves[i] = new Int32[128];
            }
        }

        public Int32 GetMove(Position position)
        {
            if (Restrictions.Output == OutputType.GUI)
            {
                Terminal.WriteLine();
                Terminal.WriteLine("{0,-8}{1,-9}{2}", "Depth", "Value", "Principal Variation");
                Terminal.WriteLine("-----------------------------------------------------------------------");
            }

            _abortSearch = false;
            _nodes = 0;
            _currentPly = 0;
            _pvLength[0] = 0;
            _timeLimit = Restrictions.MoveTime;
            _stopwatch = Stopwatch.StartNew();

            // --- TRA CỨU SÁCH KHAI CUỘC (OPENING BOOK LOOKUP) ---
            string fileName = "komodo.bin";
            string bookPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Books", fileName);

            // Nếu chạy trong Visual Studio (bin/Debug), tự động tìm ngược ra thư mục gốc
            // If running in Visual Studio (bin/Debug), automatically search up the directory tree
            if (!System.IO.File.Exists(bookPath))
            {
                bookPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Books", fileName));
            }

            // Báo lỗi ra Terminal thay vì Console ẩn / Print error to Terminal instead of hidden Console
            if (!System.IO.File.Exists(bookPath))
            {
                if (Restrictions.Output == OutputType.GUI)
                {
                    Terminal.WriteLine($"[CẢNH BÁO] Don't found the book: {bookPath}");
                    Terminal.WriteLine("-----------------------------------------------------------------------");
                }
            }
            else
            {
                ulong polyKey = PolyglotBook.GetKeyFromFEN(position.GetFEN());
                string bookMoveString = PolyglotBook.GetBestMove(bookPath, polyKey);

                if (!string.IsNullOrEmpty(bookMoveString))
                {
                    List<Int32> legalMoves = position.LegalMoves();
                    foreach (Int32 m in legalMoves)
                    {
                        int fromSq = Move.From(m);
                        int toSq = Move.To(m);

                        char fromFile = (char)('a' + (fromSq % 8));
                        char fromRank = (char)('1' + (7 - (fromSq / 8)));
                        char toFile = (char)('a' + (toSq % 8));
                        char toRank = (char)('1' + (7 - (toSq / 8)));

                        string moveCoord = $"{(char)('a' + Position.File(fromSq))}{(char)('1' + Position.Rank(fromSq))}{(char)('a' + Position.File(toSq))}{(char)('1' + Position.Rank(toSq))}";

                        if (Move.IsPromotion(m))
                        {
                            int promo = Move.Special(m) & Piece.Mask;
                            if (promo == Piece.Queen) moveCoord += "q";
                            else if (promo == Piece.Rook) moveCoord += "r";
                            else if (promo == Piece.Bishop) moveCoord += "b";
                            else if (promo == Piece.Knight) moveCoord += "n";
                        }

                        if (moveCoord == bookMoveString || Stringify.Move(m) == bookMoveString)
                        {
                            // In lịch sử sách ra Terminal / Log the book hit to Terminal
                            if (Restrictions.Output == OutputType.GUI)
                            {
                                Terminal.WriteLine($"[BOOK HIT] TacticalBot play with book: {bookMoveString}");
                                Terminal.WriteLine("-----------------------------------------------------------------------");
                            }

                            _stopwatch.Stop();
                            return m;
                        }
                    }
                }
            }
            // --- KẾT THÚC TRA CỨU SÁCH (END OF OPENING BOOK LOOKUP) ---

            Int32 bestMove = Move.Invalid;
            Int32 depth = 1;

            while (depth <= Restrictions.Depth)
            {
                _bestScore = -1000000;

                Int32 move = SearchRoot(position, depth, -1000000, 1000000);

                if (_abortSearch) break;

                bestMove = move;

                if (Restrictions.Output == OutputType.GUI)
                {
                    List<Int32> pv = new List<Int32>();
                    for (Int32 i = 0; i < _pvLength[0]; i++)
                    {
                        pv.Add(_pvMoves[0][i]);
                    }

                    Boolean isMate = Math.Abs(_bestScore) > 90000;
                    Int32 movesToMate = (100000 - Math.Abs(_bestScore) + 1) / 2;

                    String valueString = isMate ? (_bestScore > 0 ? "+Mate " : "-Mate ") + movesToMate :
                                                  (_bestScore / 100.0).ToString("+0.00;-0.00");

                    String movesString = Stringify.MovesAlgebraically(position, pv);

                    Terminal.WriteLine("{0,-8}{1,-9}{2}", depth.ToString(), valueString, movesString);
                }

                depth++;
                if (_stopwatch.ElapsedMilliseconds > _timeLimit / 2) break;
            }

            _stopwatch.Stop();

            if (Restrictions.Output == OutputType.GUI)
            {
                Terminal.WriteLine("-----------------------------------------------------------------------");
                Terminal.WriteLine($"Nodes visited      {_nodes}");
                Terminal.WriteLine($"Search time        {_stopwatch.ElapsedMilliseconds} ms");
                Terminal.WriteLine();
            }

            return bestMove;
        }

        private Int32 SearchRoot(Position position, Int32 depth, Int32 alpha, Int32 beta)
        {
            _pvLength[0] = 0;
            List<Int32> moves = position.LegalMoves();
            Int32[] moveArray = moves.ToArray();
            Single[] values = new Single[moveArray.Length];

            ScoreMoves(moveArray, values);
            Sort(moveArray, values);

            Int32 bestMove = Move.Invalid;
            Int32 bestScore = -1000000;

            foreach (Int32 move in moveArray)
            {
                _currentPly++;
                position.Make(move);
                Int32 score = -AlphaBeta(position, depth - 1, -beta, -alpha);
                position.Unmake(move);
                _currentPly--;

                if (_abortSearch) return Move.Invalid;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                    _bestScore = score;

                    _pvMoves[0][0] = move;
                    for (Int32 j = 0; j < _pvLength[1]; j++)
                    {
                        _pvMoves[0][j + 1] = _pvMoves[1][j];
                    }
                    _pvLength[0] = _pvLength[1] + 1;
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }
            return bestMove;
        }

        private Int32 AlphaBeta(Position position, Int32 depth, Int32 alpha, Int32 beta)
        {
            _nodes++;
            _pvLength[_currentPly] = 0;

            if (_nodes % 1000 == 0 && _stopwatch.ElapsedMilliseconds > _timeLimit)
            {
                _abortSearch = true;
                return 0;
            }

            if (depth <= 0)
            {
                return Quiescence(position, alpha, beta);
            }

            List<Int32> moves = position.LegalMoves();
            if (moves.Count == 0)
            {
                return position.InCheck(position.SideToMove) ? -100000 + _currentPly : 0;
            }

            Int32[] moveArray = moves.ToArray();
            Single[] values = new Single[moveArray.Length];
            ScoreMoves(moveArray, values);
            Sort(moveArray, values);

            foreach (Int32 move in moveArray)
            {
                _currentPly++;
                position.Make(move);
                Int32 score = -AlphaBeta(position, depth - 1, -beta, -alpha);
                position.Unmake(move);
                _currentPly--;

                if (_abortSearch) return 0;
                if (score >= beta) return beta;

                if (score > alpha)
                {
                    alpha = score;
                    _pvMoves[_currentPly][0] = move;
                    for (Int32 j = 0; j < _pvLength[_currentPly + 1]; j++)
                    {
                        _pvMoves[_currentPly][j + 1] = _pvMoves[_currentPly + 1][j];
                    }
                    _pvLength[_currentPly] = _pvLength[_currentPly + 1] + 1;
                }
            }

            return alpha;
        }

        private Int32 Quiescence(Position position, Int32 alpha, Int32 beta)
        {
            _nodes++;
            _pvLength[_currentPly] = 0;

            if (_nodes % 1000 == 0 && _stopwatch.ElapsedMilliseconds > _timeLimit)
            {
                _abortSearch = true;
                return 0;
            }

            Int32 standPat = Evaluate(position);

            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            List<Int32> moves = position.LegalMoves();
            List<Int32> captures = new List<Int32>(16);

            foreach (Int32 move in moves)
            {
                Int32 victim = (move >> 16) & 15;
                if (victim != Piece.Empty || Move.IsEnPassant(move))
                {
                    captures.Add(move);
                }
            }

            Int32[] moveArray = captures.ToArray();
            Single[] values = new Single[moveArray.Length];
            ScoreMoves(moveArray, values);
            Sort(moveArray, values);

            foreach (Int32 move in moveArray)
            {
                _currentPly++;
                position.Make(move);
                Int32 score = -Quiescence(position, -beta, -alpha);
                position.Unmake(move);
                _currentPly--;

                if (_abortSearch) return 0;
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }

        private void ScoreMoves(Int32[] moves, Single[] values)
        {
            for (Int32 i = 0; i < moves.Length; i++)
            {
                Int32 move = moves[i];
                // Dùng API hệ thống để lấy quân bị bắt thay vì dịch Bit thủ công
                Int32 victim = Move.Capture(move);

                if (victim != Piece.Empty || Move.IsEnPassant(move))
                {
                    if (Move.IsEnPassant(move)) victim = Piece.Pawn;
                    else victim &= Piece.Mask; // Lọc bỏ màu, chỉ lấy loại quân

                    Int32 attacker = Move.Piece(move) & Piece.Mask;

                    // Ưu tiên cao cho việc dùng quân nhỏ ăn quân lớn
                    // Prioritize capturing high-value pieces with low-value pieces
                    values[i] = (Evaluator.PieceValue[victim] * 10) - Evaluator.PieceValue[attacker];
                }
                else
                {
                    values[i] = 0;
                }
            }
        }

        /// <summary>
        /// Gọi hàm Đánh giá từ thư mục Shared dùng chung.
        /// Call the Evaluation function from the shared module.
        /// </summary>
        private Int32 Evaluate(Position position)
        {
            // Lấy điểm số cơ bản từ "bộ não" Nega Premium / Get base score from shared evaluator
            Int32 baseScore = Evaluator.Evaluate(position);

            // Thêm độ nhiễu siêu nhỏ (-4 đến +4) để Bot đánh đa dạng / Add evaluation noise for variety
            return baseScore + _rand.Next(-4, 5);
        }

        private void Sort(Int32[] moves, Single[] values)
        {
            for (Int32 i = 1; i < moves.Length; i++)
            {
                Int32 j = i;
                while (j > 0 && values[j] > values[j - 1])
                {
                    Single tempVal = values[j]; values[j] = values[j - 1]; values[j - 1] = tempVal;
                    Int32 tempMove = moves[j]; moves[j] = moves[j - 1]; moves[j - 1] = tempMove;
                    j--;
                }
            }
        }

        public void Stop() { _abortSearch = true; }

        public void Reset()
        {
            _nodes = 0;
            _currentPly = 0;
        }

        public void Draw(System.Drawing.Graphics g) { }
    }
}