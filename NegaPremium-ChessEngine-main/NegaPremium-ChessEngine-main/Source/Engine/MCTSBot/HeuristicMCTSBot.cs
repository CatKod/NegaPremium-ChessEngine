using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NegaPremium
{
    /// <summary>
    /// Bot "Tư duy lai" (Heuristic MCTS Bot) - Phiên bản Triệt tiêu Nhiễu (AlphaZero Style).
    /// Áp dụng Lượng giá trực tiếp tại Nút lá và Ưu tiên nước đi Chiến thuật.
    /// </summary>
    public sealed class HeuristicMCTSBot : IPlayer
    {
        public String Name => "Heuristic MCTS";
        public Boolean IsAI => true;
        public Boolean AcceptsDraw => false;

        private Boolean _abortSearch;
        private Int32 _timeLimit;
        private Stopwatch _stopwatch;
        private static readonly System.Random _rand = new System.Random();

        // ========================================================
        // 1. CẤU TRÚC NÚT MCTS
        // ========================================================
        private class MCTSNode
        {
            public Int32 Move;
            public MCTSNode Parent;
            public List<MCTSNode> Children;
            public List<Int32> UnexploredMoves;

            public Int32 Visits;
            public Double TotalScore;

            public MCTSNode(Int32 move, MCTSNode parent, Position pos)
            {
                Move = move;
                Parent = parent;
                Children = new List<MCTSNode>();

                // Lấy tất cả nước đi hợp lệ
                List<Int32> moves = pos.LegalMoves();

                // ĐÃ SỬA: Sắp xếp để MCTS ưu tiên Mở rộng các nước Ăn quân trước
                // Giúp MCTS không bị mù chiến thuật ở các độ sâu đầu tiên
                moves.Sort((a, b) =>
                {
                    bool isCapA = NegaPremium.Move.Capture(a) != Piece.Empty;
                    bool isCapB = NegaPremium.Move.Capture(b) != Piece.Empty;
                    return isCapB.CompareTo(isCapA); // Đẩy True (Ăn quân) lên đầu
                });

                UnexploredMoves = moves;
                Visits = 0;
                TotalScore = 0.0;
            }
        }

        // ========================================================
        // 2. VÒNG LẶP MCTS CHÍNH
        // ========================================================
        public Int32 GetMove(Position position)
        {
            if (Restrictions.Output == OutputType.GUI)
            {
                Terminal.WriteLine();
                Terminal.WriteLine("{0,-8}{1,-9}{2}", "Sims", "Win Rate", "Principal Variation");
                Terminal.WriteLine("-----------------------------------------------------------------------");
            }

            _abortSearch = false;
            _timeLimit = Restrictions.MoveTime;
            _stopwatch = Stopwatch.StartNew();

            MCTSNode root = new MCTSNode(NegaPremium.Move.Invalid, null, position);
            long nextPrintTime = 1000;

            while (_stopwatch.ElapsedMilliseconds < _timeLimit && !_abortSearch)
            {
                MCTSNode node = root;

                // --- PHASE 1: LỰA CHỌN ---
                while (node.UnexploredMoves.Count == 0 && node.Children.Count > 0)
                {
                    node = SelectBestUCB1(node);
                    position.Make(node.Move);
                }

                // --- PHASE 2: MỞ RỘNG (ƯU TIÊN CHIẾN THUẬT) ---
                if (node.UnexploredMoves.Count > 0)
                {
                    // Lấy nước đi đầu tiên (Đã được sắp xếp ưu tiên ăn quân)
                    int moveToExpand = node.UnexploredMoves[0];
                    node.UnexploredMoves.RemoveAt(0);

                    position.Make(moveToExpand);
                    MCTSNode child = new MCTSNode(moveToExpand, node, position);
                    node.Children.Add(child);
                    node = child;
                }

                // --- PHASE 3: LƯỢNG GIÁ TĨNH (TRỰC TIẾP NHƯ ALPHAZERO) ---
                double whiteWinProb = EvaluateLeafNode(position);

                // --- PHASE 4: LAN TRUYỀN NGƯỢC ---
                while (node != null)
                {
                    node.Visits++;
                    int playerWhoMoved = 1 - position.SideToMove;

                    if (playerWhoMoved == Colour.White)
                        node.TotalScore += whiteWinProb;
                    else
                        node.TotalScore += (1.0 - whiteWinProb);

                    if (node.Move != NegaPremium.Move.Invalid)
                    {
                        position.Unmake(node.Move);
                    }
                    node = node.Parent;
                }

                // --- HIỂN THỊ LOG UI ---
                if (Restrictions.Output == OutputType.GUI && _stopwatch.ElapsedMilliseconds > nextPrintTime)
                {
                    List<Int32> pv = GetPrincipalVariation(root);
                    if (pv.Count > 0 && root.Visits > 0)
                    {
                        double winRate = (root.Children.Count > 0 ? (GetBestChild(root).TotalScore / GetBestChild(root).Visits) : 0) * 100;
                        string winStr = winRate.ToString("0.0") + "%";
                        string pvStr = Stringify.MovesAlgebraically(position, pv);

                        Terminal.OverwriteLineAt(Terminal.CursorTop, string.Format("{0,-8}{1,-9}{2}", root.Visits, winStr, pvStr));
                    }
                    nextPrintTime += 1000;
                }
            }
            _stopwatch.Stop();

            // ========================================================
            // 3. TỔNG KẾT
            // ========================================================
            StatisticsLogger.LogGUI(
                position,
                Name,
                _stopwatch.Elapsed.TotalMilliseconds,
                root.Visits,
                movesProcessed: 0
            );

            MCTSNode bestNode = GetBestChild(root);
            return bestNode != null ? bestNode.Move : position.LegalMoves()[0];
        }

        // ========================================================
        // 4. HÀM BỔ TRỢ MCTS
        // ========================================================
        private MCTSNode SelectBestUCB1(MCTSNode node)
        {
            MCTSNode bestNode = null;
            double bestUCB1 = double.MinValue;
            // Ép khai thác nhiều hơn bằng cách giảm hằng số Khám phá (C)
            double C = 0.5;

            foreach (MCTSNode child in node.Children)
            {
                if (child.Visits == 0) return child;
                double exploit = child.TotalScore / child.Visits;
                double explore = C * Math.Sqrt(Math.Log(node.Visits) / child.Visits);
                double ucb1 = exploit + explore;

                if (ucb1 > bestUCB1)
                {
                    bestUCB1 = ucb1;
                    bestNode = child;
                }
            }
            return bestNode;
        }

        private MCTSNode GetBestChild(MCTSNode root)
        {
            MCTSNode bestChild = null;
            int maxVisits = -1;
            foreach (MCTSNode child in root.Children)
            {
                if (child.Visits > maxVisits)
                {
                    maxVisits = child.Visits;
                    bestChild = child;
                }
            }
            return bestChild;
        }

        private List<Int32> GetPrincipalVariation(MCTSNode root)
        {
            List<Int32> pv = new List<Int32>();
            MCTSNode current = root;

            while (current != null && current.Children.Count > 0)
            {
                MCTSNode bestChild = GetBestChild(current);
                if (bestChild != null && bestChild.Visits > 0)
                {
                    pv.Add(bestChild.Move);
                    current = bestChild;
                }
                else break;
            }
            return pv;
        }

        // ========================================================
        // 5. ĐÁNH GIÁ NÚT LÁ (THAY THẾ HOÀN TOÀN MÔ PHỎNG RANDOM)
        // ========================================================
        private double EvaluateLeafNode(Position position)
        {
            List<Int32> moves = position.LegalMoves();
            if (moves.Count == 0)
            {
                if (position.InCheck(position.SideToMove))
                    return (1 - position.SideToMove == Colour.White) ? 1.0 : 0.0;
                return 0.5; // Hòa
            }

            // Gọi Evaluator ngay lập tức (Tốc độ ánh sáng, không nhiễu)
            int eval = Evaluator.Evaluate(position);

            if (position.SideToMove == Colour.Black)
                eval = -eval;

            // Thu hẹp dải Sigmoid (200 thay vì 400) để MCTS cảm nhận sự chênh lệch sắc nét hơn
            return 1.0 / (1.0 + Math.Exp(-eval / 200.0));
        }

        public void Stop() { _abortSearch = true; }
        public void Reset() { }
        public void Draw(System.Drawing.Graphics g) { }
    }
}