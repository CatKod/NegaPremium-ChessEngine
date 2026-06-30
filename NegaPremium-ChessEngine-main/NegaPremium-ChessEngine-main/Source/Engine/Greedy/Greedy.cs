using System;
using System.Collections.Generic;

namespace NegaPremium {

    /// <summary>
    /// Encapsulates a time-aware hill-climbing search mode for comparison.
    /// </summary>
    public sealed partial class Engine : IPlayer {

        private const Double GreedyTargetTimeLimit = 3.0;
        private const Double GreedyStopMargin = 0.20;
        private const Int32 GreedyQuiescenceLimit = 8;
        private const Int32 GreedyMateScore = CheckmateValue;
        private const Int32 GreedyMateDistanceOffset = 1;
        private const Int32 GreedyLateMoveReductionDepth = 3;
        private const Int32 GreedyLateMoveReductionMove = 3;
        private const Int32 GreedyNullMoveReduction = 2;

        /// <summary>
        /// Returns a move selected by iterative deepening hill climbing.
        /// </summary>
        private Int32 GreedySearch(Position position, Int32 depth, Int32 alpha, Int32 beta, Int32 ply, Boolean inCheck, Boolean allowNull) {
            if (position == null)
                return Move.Invalid;

            _totalNodes = 0;
            _movesSearched = 0;
            _quiescenceNodes = 0;

            List<Int32> rootMoves = position.LegalMoves();
            if (rootMoves.Count == 0)
                return Move.Invalid;

            _abortSearch = false;
            _stopwatch.Restart();

            Int32 maxDepth = Math.Max(1, depth);
            Int32 bestMove = rootMoves[0];
            Int32 bestValue = -Infinity;
            String principalVariation = String.Empty;

            for (Int32 currentDepth = 1; currentDepth <= maxDepth; currentDepth++) {
                if (TimeExpired())
                    break;

                Int32 iterationBestMove = Move.Invalid;
                Int32 iterationBestValue = -Infinity;
                Int32 localAlpha = alpha;
                Int32 localBeta = beta;

                OrderMoves(position, rootMoves, ply);

                for (Int32 i = 0; i < rootMoves.Count; i++) {
                    if (TimeExpired()) {
                        _abortSearch = true;
                        break;
                    }

                    Int32 move = rootMoves[i];
                    position.Make(move);

                    Boolean childInCheck = position.InCheck(position.SideToMove);
                    Int32 value = GetTerminalScore(position, childInCheck, ply + 1);
                    if (value == Int32.MinValue)
                    {
                        if (currentDepth > 1)
                            value = -SearchNode(position, currentDepth - 1, ply + 1, -localBeta, -localAlpha, childInCheck);
                        else
                            value = -Evaluator.Evaluate(position);
                    }

                    position.Unmake(move);

                    if (value > iterationBestValue) {
                        iterationBestValue = value;
                        iterationBestMove = move;
                    }

                    if (value > localAlpha)
                        localAlpha = value;
                    if (localAlpha >= localBeta)
                        break;
                }

                if (_abortSearch || TimeExpired())
                    break;

                if (iterationBestMove != Move.Invalid) {
                    bestMove = iterationBestMove;
                    bestValue = iterationBestValue;
                    PromoteRootMove(rootMoves, iterationBestMove);
                    principalVariation = GetGreedyPrincipalVariation(position, iterationBestMove, currentDepth);
                    WriteGreedyLog(position, currentDepth, bestValue, principalVariation);
                }
            }

            _abortSearch = false;
            return bestMove != Move.Invalid ? bestMove : rootMoves[0];
        }

        /// <summary>
        /// Depth-limited recursive search used by hill climbing.
        /// </summary>
        private Int32 SearchNode(Position position, Int32 depth, Int32 ply, Int32 alpha, Int32 beta, Boolean inCheck) {
            if (position == null)
                return Move.Invalid;

            _totalNodes++;

            if (TimeExpired()) {
                _abortSearch = true;
                return Evaluator.Evaluate(position);
            }

            if (depth <= 0)
                return Quiescence(position, ply, alpha, beta, inCheck);

            List<Int32> moves = position.LegalMoves();
            if (moves.Count == 0)
                return inCheck ? -CheckmateValue + ply : 0;

            OrderMoves(position, moves, ply);

            if (CanApplyNullMove(position, inCheck, depth)) {
                position.MakeNull();
                Int32 nullValue = -SearchNode(position, depth - 1 - GreedyNullMoveReduction, ply + 1, -beta, -beta + 1, false);
                position.UnmakeNull();
                if (nullValue >= beta)
                    return nullValue;
            }

            Int32 bestValue = -Infinity;
            for (Int32 i = 0; i < moves.Count; i++) {
                if (TimeExpired()) {
                    _abortSearch = true;
                    break;
                }

                Int32 move = moves[i];
                Boolean tactical = inCheck || position.CausesCheck(move) || Move.IsCapture(move) || Move.IsPromotion(move) || Move.IsEnPassant(move);
                Boolean reducible = i >= GreedyLateMoveReductionMove && depth >= GreedyLateMoveReductionDepth && !tactical;

                position.Make(move);
                Boolean childInCheck = position.InCheck(position.SideToMove);
                Int32 value;
                if (reducible)
                    value = -SearchNode(position, depth - 2, ply + 1, -alpha - 1, -alpha, childInCheck);
                else if (i > 0)
                    value = -SearchNode(position, depth - 1, ply + 1, -alpha - 1, -alpha, childInCheck);
                else
                    value = -SearchNode(position, depth - 1, ply + 1, -beta, -alpha, childInCheck);

                if (value > alpha)
                    value = -SearchNode(position, depth - 1, ply + 1, -beta, -alpha, childInCheck);

                Int32 terminalScore = GetTerminalScore(position, childInCheck, ply + 1);
                if (terminalScore != Int32.MinValue)
                    value = Math.Max(value, terminalScore);
                position.Unmake(move);

                if (value > bestValue)
                    bestValue = value;
                if (value > alpha)
                    alpha = value;
                if (alpha >= beta)
                    break;
            }

            return bestValue == -Infinity ? Evaluator.Evaluate(position) : bestValue;
        }

        /// <summary>
        /// Returns a quiescence score for noisy leaf positions.
        /// </summary>
        private Int32 Quiescence(Position position, Int32 ply, Int32 alpha, Int32 beta, Boolean inCheck) {
            if (position == null)
                return Move.Invalid;

            _quiescenceNodes++;
            _totalNodes++;

            if (TimeExpired()) {
                _abortSearch = true;
                return Evaluator.Evaluate(position);
            }

            if (ply >= GreedyQuiescenceLimit)
                return Evaluator.Evaluate(position);

            Int32 standPat = Evaluator.Evaluate(position);
            if (!inCheck) {
                if (standPat >= beta)
                    return standPat;
                if (standPat > alpha)
                    alpha = standPat;
            }

            List<Int32> moves = position.LegalMoves();
            if (moves.Count == 0)
                return inCheck ? -CheckmateValue + ply : 0;

            OrderMoves(position, moves, ply);

            Int32 bestValue = standPat;
            for (Int32 i = 0; i < moves.Count; i++) {
                if (TimeExpired()) {
                    _abortSearch = true;
                    break;
                }

                Int32 move = moves[i];
                Boolean tactical = inCheck || position.CausesCheck(move) || Move.IsCapture(move) || Move.IsPromotion(move) || Move.IsEnPassant(move);
                if (!tactical)
                    continue;

                _movesSearched++;

                position.Make(move);
                Boolean childInCheck = position.InCheck(position.SideToMove);
                Int32 value = -Quiescence(position, ply + 1, -beta, -alpha, childInCheck);
                position.Unmake(move);

                if (value > bestValue)
                    bestValue = value;
                if (value >= beta)
                    return value;
                if (value > alpha)
                    alpha = value;
            }

            return bestValue;
        }

        private Boolean TimeExpired() {
            return _stopwatch.IsRunning && _stopwatch.Elapsed.TotalSeconds >= GreedyTargetTimeLimit - GreedyStopMargin;
        }

        private static void OrderMoves(Position position, List<Int32> moves, Int32 ply) {
            moves.Sort((left, right) => MoveOrderingScore(position, right, ply).CompareTo(MoveOrderingScore(position, left, ply)));
        }

        private static Int32 GetTerminalScore(Position position, Boolean inCheck, Int32 ply) {
            if (position == null)
                return Int32.MinValue;

            List<Int32> legalMoves = position.LegalMoves();
            if (legalMoves.Count == 0)
                return inCheck ? -CheckmateValue + ply : 0;

            return Int32.MinValue;
        }

        private static Boolean CanApplyNullMove(Position position, Boolean inCheck, Int32 depth) {
            if (position == null || inCheck || depth <= 2)
                return false;

            List<Int32> legalMoves = position.LegalMoves();
            if (legalMoves.Count <= 2)
                return false;

            Int32 colour = position.SideToMove;
            return position.Bitboard[colour] != (position.Bitboard[colour | Piece.King] | position.Bitboard[colour | Piece.Pawn]);
        }

        private static String GetGreedyPrincipalVariation(Position position, Int32 bestMove, Int32 depth)
        {
            if (position == null || bestMove == Move.Invalid || depth <= 0)
                return String.Empty;

            Position probe = position.DeepClone();
            List<Int32> moves = new List<Int32>();
            moves.Add(bestMove);
            probe.Make(bestMove);

            for (Int32 remaining = depth - 1; remaining > 0; remaining--)
            {
                List<Int32> legalMoves = probe.LegalMoves();
                if (legalMoves.Count == 0)
                    break;

                Int32 reply = legalMoves[0];
                moves.Add(reply);
                probe.Make(reply);
            }

            // Tự động xây dựng chuỗi PV theo phong cách nối tiếp của Chess.com
            System.Text.StringBuilder pv = new System.Text.StringBuilder();

            // 1. Phân tích số lượt đi và phe nào đang đánh từ FEN thực tế
            string[] fenParts = position.GetFEN().Split(' ');
            int currentMoveNumber = 1;
            bool isBlackToMove = false;

            if (fenParts.Length >= 2 && fenParts[1] == "b")
            {
                isBlackToMove = true;
            }
            if (fenParts.Length >= 6)
            {
                int.TryParse(fenParts[5], out currentMoveNumber);
            }

            // 2. Dịch từng nước đi và tự động đánh số chuẩn PGN
            probe = position.DeepClone();
            for (int i = 0; i < moves.Count; i++)
            {
                if (i > 0) pv.Append(" "); // Khoảng trắng giữa các nước đi

                string algebraicMove = Stringify.MoveAlgebraically(probe, moves[i]);

                if (isBlackToMove)
                {
                    // Nếu là nước đầu tiên của PV mà vào lượt Đen
                    if (i == 0) pv.Append(currentMoveNumber).Append("... ");

                    pv.Append(algebraicMove);
                    isBlackToMove = false;
                    currentMoveNumber++; // Hết lượt Đen là sang lượt mới, tăng số đếm
                }
                else
                {
                    // Lượt Trắng luôn đi kèm số đếm
                    pv.Append(currentMoveNumber).Append(". ").Append(algebraicMove);
                    isBlackToMove = true;
                }

                probe.Make(moves[i]);
            }

            return pv.ToString().Trim();
        }

        private static void WriteGreedyLog(Position position, Int32 depth, Int32 value, String principalVariation)
        {
            if (Restrictions.Output != OutputType.GUI)
                return;

            String valueText = Math.Abs(value) >= CheckmateValue - 1000
                ? (value > 0 ? "+Mate " : "-Mate ") + Math.Max(1, (CheckmateValue - Math.Abs(value) + GreedyMateDistanceOffset) / 2)
                : String.Format("{0:+0.00;-0.00}", value / 100.0);

            Terminal.WriteLine("{0,-8}{1,-9}{2}", depth, valueText, principalVariation);
        }

        private static void PromoteRootMove(List<Int32> moves, Int32 bestMove) {
            Int32 index = moves.IndexOf(bestMove);
            if (index > 0) {
                moves.RemoveAt(index);
                moves.Insert(0, bestMove);
            }
        }

        private static Int32 MoveOrderingScore(Position position, Int32 move, Int32 ply) {
            Int32 score = 0;

            if (Move.IsPromotion(move))
                score += 1_000_000 + Evaluator.PieceValue[Move.Special(move)] * 32;

            if (Move.IsCapture(move)) {
                Int32 captured = Move.Capture(move);
                Int32 piece = Move.Piece(move);
                score += 500_000 + Evaluator.PieceValue[captured] * 16 - Evaluator.PieceValue[piece];
            }

            if (position != null && position.CausesCheck(move))
                score += 750_000;

            if (Move.IsEnPassant(move))
                score += 400_000;

            if (Move.IsCastle(move))
                score += 20_000;

            score += Math.Max(0, 2_000 - ply * 8);
            return score;
        }
    }
}
