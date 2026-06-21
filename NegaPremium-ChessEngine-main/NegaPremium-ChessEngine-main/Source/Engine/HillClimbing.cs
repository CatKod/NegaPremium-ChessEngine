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

        /// <summary>
        /// Returns a move selected by iterative deepening hill climbing.
        /// </summary>
        private Int32 HillClimbingSearch(Position position, Int32 depth, Int32 alpha, Int32 beta, Int32 ply, Boolean inCheck, Boolean allowNull) {
            if (position == null)
                return Move.Invalid;

            List<Int32> rootMoves = position.LegalMoves();
            if (rootMoves.Count == 0)
                return Move.Invalid;

            _abortSearch = false;
            _stopwatch.Restart();

            Int32 maxDepth = Math.Max(1, depth);
            Int32 bestMove = rootMoves[0];
            Int32 bestValue = -Infinity;

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
                        value = Evaluate(position);

                    position.Unmake(move);

                    if (position.SideToMove == Colour.Black)
                        value = -value;

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
                }
            }

            _stopwatch.Stop();
            _abortSearch = false;
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
                return Evaluate(position);
            }

            if (depth <= 0)
                return Quiescence(position, ply, alpha, beta, inCheck);

            List<Int32> moves = position.LegalMoves();
            if (moves.Count == 0)
                return Evaluate(position);

            OrderMoves(moves, ply);

            Int32 bestValue = -Infinity;
            for (Int32 i = 0; i < moves.Count; i++) {
                if (TimeExpired()) {
                    _abortSearch = true;
                    break;
                }

                Int32 move = moves[i];
                position.Make(move);
                Boolean childInCheck = position.InCheck(position.SideToMove);
                Int32 value = -SearchNode(position, depth - 1, ply + 1, -beta, -alpha, childInCheck);
                position.Unmake(move);

                if (position.SideToMove == Colour.Black)
                    value = -value;

                if (value > bestValue)
                    bestValue = value;
                if (value > alpha)
                    alpha = value;
                if (alpha >= beta)
                    break;
            }

            return bestValue == -Infinity ? Evaluate(position) : bestValue;
        }

        /// <summary>
        /// Returns a quiescence score for noisy leaf positions.
        /// </summary>
        private Int32 Quiescence(Position position, Int32 ply, Int32 alpha, Int32 beta, Boolean inCheck) {
            if (position == null)
                return Move.Invalid;

            if (TimeExpired()) {
                _abortSearch = true;
                return Evaluate(position);
            }

            if (ply >= HillClimbingQuiescenceLimit)
                return Evaluate(position);

            Int32 standPat = Evaluate(position);
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

                if (position.SideToMove == Colour.Black)
                    value = -value;

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
                score += 1_000_000 + PieceValue[Move.Special(move)] * 32;

            if (Move.IsCapture(move)) {
                Int32 captured = Move.Capture(move);
                Int32 piece = Move.Piece(move);
                score += 500_000 + PieceValue[captured] * 16 - PieceValue[piece];
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
