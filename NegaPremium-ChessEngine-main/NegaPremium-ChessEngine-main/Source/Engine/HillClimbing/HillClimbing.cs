using System;
using System.Collections.Generic;

namespace NegaPremium {

    /// <summary>
    /// Encapsulates a time-aware hill-climbing search mode for comparison.
    /// </summary>
    public sealed partial class Engine : IPlayer {

        private const Double HillClimbingTargetTimeLimit = 3.0;
        private const Double HillClimbingStopMargin = 0.20;
        private const Int32 HillClimbingQuiescenceLimit = 8;
        private const Int32 HillClimbingMateScore = CheckmateValue;
        private const Int32 HillClimbingLateMoveReductionDepth = 3;
        private const Int32 HillClimbingLateMoveReductionMove = 3;
        private const Int32 HillClimbingNullMoveReduction = 2;

        /// <summary>
        /// Returns a move selected by iterative deepening hill climbing.
        /// </summary>
        private Int32 HillClimbingSearch(Position position, Int32 depth, Int32 alpha, Int32 beta, Int32 ply, Boolean inCheck, Boolean allowNull) {
            if (position == null)
                return Move.Invalid;

            List<Int32> rootMoves = position.LegalMoves();
            if (rootMoves.Count == 0)
                return Move.Invalid;

            if (Restrictions.Output == OutputType.GUI) {
                Terminal.Clear();
                Terminal.WriteLine("FEN: " + position.GetFEN());
                Terminal.WriteLine();
                Terminal.WriteLine("Depth   Value    Principal Variation");
                Terminal.WriteLine("-----------------------------------------------------------------------");
            }

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

                OrderMoves(rootMoves, ply);

                for (Int32 i = 0; i < rootMoves.Count; i++) {
                    if (TimeExpired()) {
                        _abortSearch = true;
                        break;
                    }

                    Int32 move = rootMoves[i];
                    position.Make(move);

                    Int32 value;
                    Boolean childInCheck = position.InCheck(position.SideToMove);
                    if (currentDepth > 1)
                        value = -SearchNode(position, currentDepth - 1, ply + 1, -localBeta, -localAlpha, childInCheck);
                    else
                        value = -Evaluator.Evaluate(position);

                    if (IsExactMate(position, childInCheck))
                        value = Math.Max(value, CheckmateValue - currentDepth);

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
                    principalVariation = GetHillClimbingPrincipalVariation(position, iterationBestMove, currentDepth);
                    WriteHillClimbingLog(position, currentDepth, bestValue, principalVariation);
                }
            }

            _stopwatch.Stop();
            _abortSearch = false;

            if (Restrictions.Output == OutputType.GUI) {
                Terminal.WriteLine("-----------------------------------------------------------------------");
                Terminal.WriteLine("FEN: " + position.GetFEN());
                Terminal.WriteLine();
                Terminal.WriteLine(position.ToString(
                    String.Format("HillClimbing {0} ({1}-bit)", Version, IntPtr.Size * 8),
                    String.Format("Search time        {0:0} ms", _stopwatch.Elapsed.TotalMilliseconds),
                    String.Format("Search speed       {0:0} kN/s", _totalNodes / Math.Max(_stopwatch.Elapsed.TotalMilliseconds, 1.0)),
                    String.Format("Nodes visited      {0}", _totalNodes),
                    String.Format("Moves processed    {0}", _movesSearched),
                    String.Format("Quiescence nodes   {0:0.00 %}", (Double)_quiescenceNodes / Math.Max(_totalNodes, 1)),
                    String.Format("Futility skips     {0:0.00 %}", (Double)_futileMoves / Math.Max(_movesSearched, 1)),
                    String.Format("Hash cutoffs       {0:0.00 %}", (Double)_hashCutoffs / Math.Max(_hashProbes, 1)),
                    String.Format("Hash move found    {0:0.00 %}", (Double)_hashMoveMatches / Math.Max(_hashMoveChecks, 1)),
                    String.Format("Killer move found  {0:0.00 %}", (Double)_killerMoveMatches / Math.Max(_killerMoveChecks, 1)),
                    String.Format("Static evaluation  {0:+0.00;-0.00}", Evaluator.Evaluate(position) / 100.0)));
                Terminal.WriteLine("HillClimbing final evaluation: {0:+0.00;-0.00}", Evaluator.Evaluate(position) / 100.0);
                Terminal.WriteLine();
            }

            return bestMove != Move.Invalid ? bestMove : rootMoves[0];
        }

        /// <summary>
        /// Depth-limited recursive search used by hill climbing.
        /// </summary>
        private Int32 SearchNode(Position position, Int32 depth, Int32 ply, Int32 alpha, Int32 beta, Boolean inCheck) {
            if (position == null)
                return Move.Invalid;

            if (TimeExpired()) {
                _abortSearch = true;
                return Evaluator.Evaluate(position);
            }

            if (depth <= 0)
                return Quiescence(position, ply, alpha, beta, inCheck);

            List<Int32> moves = position.LegalMoves();
            if (moves.Count == 0)
                return Evaluator.Evaluate(position);

            OrderMoves(moves, ply);

            if (CanApplyNullMove(position, inCheck, depth)) {
                position.MakeNull();
                Int32 nullValue = -SearchNode(position, depth - 1 - HillClimbingNullMoveReduction, ply + 1, -beta, -beta + 1, false);
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
                Boolean reducible = i >= HillClimbingLateMoveReductionMove && depth >= HillClimbingLateMoveReductionDepth && !tactical;

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

                if (IsExactMate(position, childInCheck))
                    value = Math.Max(value, CheckmateValue - (ply + 1));
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

            if (TimeExpired()) {
                _abortSearch = true;
                return Evaluator.Evaluate(position);
            }

            if (ply >= HillClimbingQuiescenceLimit)
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
                return standPat;

            OrderMoves(moves, ply);

            Int32 bestValue = standPat;
            for (Int32 i = 0; i < moves.Count; i++) {
                if (TimeExpired()) {
                    _abortSearch = true;
                    break;
                }

                Int32 move = moves[i];
                Boolean tactical = inCheck || Move.IsCapture(move) || Move.IsPromotion(move) || Move.IsEnPassant(move);
                if (!tactical)
                    continue;

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
            return _stopwatch.IsRunning && _stopwatch.Elapsed.TotalSeconds >= HillClimbingTargetTimeLimit - HillClimbingStopMargin;
        }

        private static void OrderMoves(List<Int32> moves, Int32 ply) {
            moves.Sort((left, right) => MoveOrderingScore(right, ply).CompareTo(MoveOrderingScore(left, ply)));
        }

        private static Boolean IsExactMate(Position position, Boolean inCheck) {
            if (position == null)
                return false;

            List<Int32> legalMoves = position.LegalMoves();
            return legalMoves.Count == 0 && inCheck;
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

        private static String GetHillClimbingPrincipalVariation(Position position, Int32 bestMove, Int32 depth) {
            if (position == null || bestMove == Move.Invalid || depth <= 0)
                return String.Empty;

            Position probe = position.DeepClone();
            List<Int32> moves = new List<Int32>();
            moves.Add(bestMove);
            probe.Make(bestMove);

            for (Int32 remaining = depth - 1; remaining > 0; remaining--) {
                List<Int32> legalMoves = probe.LegalMoves();
                if (legalMoves.Count == 0)
                    break;

                Int32 reply = legalMoves[0];
                moves.Add(reply);
                probe.Make(reply);
            }

            return Stringify.MovesAlgebraically(position.DeepClone(), moves, StringifyOptions.Proper);
        }

        private static void WriteHillClimbingLog(Position position, Int32 depth, Int32 value, String principalVariation) {
            if (Restrictions.Output != OutputType.GUI)
                return;

            String valueText = value >= CheckmateValue
                ? "+Mate " + Math.Max(1, (CheckmateValue - value + 1) / 2)
                : String.Format("{0:+0.00;-0.00}", value / 100.0);

            Terminal.WriteLine("{0,-7}{1,-9}{2}", depth, valueText, principalVariation);
        }

        /// <summary>
        /// Writes the final HillClimbing search statistics.
        /// </summary>
        private void WriteHillClimbingStatistics(Position position, Double elapsed) {
            if (Restrictions.Output != OutputType.GUI)
                return;

            Terminal.WriteLine("-----------------------------------------------------------------------");
            Terminal.WriteLine("FEN: " + position.GetFEN());
            Terminal.WriteLine();
            Terminal.WriteLine(position.ToString(
                String.Format("HillClimbing {0} ({1}-bit)", Version, IntPtr.Size * 8),
                String.Format("Search time        {0:0} ms", elapsed),
                String.Format("Search speed       {0:0} kN/s", _totalNodes / Math.Max(elapsed, 1.0)),
                String.Format("Nodes visited      {0}", _totalNodes),
                String.Format("Moves processed    {0}", _movesSearched),
                String.Format("Quiescence nodes   {0:0.00 %}", (Double)_quiescenceNodes / Math.Max(_totalNodes, 1)),
                String.Format("Quiescence limit   {0}", HillClimbingQuiescenceLimit),
                String.Format("Stop margin        {0:0.00} s", HillClimbingStopMargin),
                String.Format("Target time        {0:0.00} s", HillClimbingTargetTimeLimit),
                String.Format("Static evaluation  {0:+0.00;-0.00}", Evaluator.Evaluate(position) / 100.0)));
            Terminal.WriteLine("HillClimbing final evaluation: {0:+0.00;-0.00}", Evaluator.Evaluate(position) / 100.0);
            Terminal.WriteLine();
        }

        private static void PromoteRootMove(List<Int32> moves, Int32 bestMove) {
            Int32 index = moves.IndexOf(bestMove);
            if (index > 0) {
                moves.RemoveAt(index);
                moves.Insert(0, bestMove);
            }
        }

        private static Int32 MoveOrderingScore(Int32 move, Int32 ply) {
            Int32 score = 0;

            if (Move.IsPromotion(move))
                score += 1_000_000 + Evaluator.PieceValue[Move.Special(move)] * 32;

            if (Move.IsCapture(move)) {
                Int32 captured = Move.Capture(move);
                Int32 piece = Move.Piece(move);
                score += 500_000 + Evaluator.PieceValue[captured] * 16 - Evaluator.PieceValue[piece];
            }

            if (Move.IsEnPassant(move))
                score += 400_000;

            if (Move.IsCastle(move))
                score += 20_000;

            score += Math.Max(0, 2_000 - ply * 8);
            return score;
        }
    }
}
