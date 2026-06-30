using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace NegaPremium {

    public sealed partial class Engine : IPlayer {

        private const Int32 Greedyv2TopN = 10;
        private const Int32 Greedyv2MateScore = CheckmateValue;
        private const Int32 Greedyv2LateMoveReductionDepth = 3;
        private const Int32 Greedyv2LateMoveReductionMove = 3;
        private const Int32 Greedyv2NullMoveReduction = 2;
        private static readonly String Greedyv2ModelPath = Path.Combine("ml", "models", "chess_move_matrix_cnn", "model.onnx");
        private static readonly String Greedyv2MetadataPath = Path.Combine("ml", "models", "chess_move_matrix_cnn", "metadata.json");
        private static readonly InferenceSession Greedyv2Session = CreateGreedyv2Session();
        private static readonly Lazy<List<String>> Greedyv2Labels = new Lazy<List<String>>(LoadGreedyv2Labels);

        private Int32 Greedyv2Search(Position position, Int32 depth, Int32 alpha, Int32 beta, Int32 ply, Boolean inCheck, Boolean allowNull) {
            if (position == null)
                return Move.Invalid;

            _totalNodes = 0;
            _movesSearched = 0;
            _quiescenceNodes = 0;

            List<Int32> rootMoves = position.LegalMoves();
            if (rootMoves.Count == 0)
                return Move.Invalid;

            if (Restrictions.Output == OutputType.GUI)
            {
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

            for (Int32 currentDepth = 1; currentDepth <= maxDepth; currentDepth++) {
                if (TimeExpired())
                    break;

                List<Int32> candidateMoves = GetGreedyv2Candidates(position, rootMoves, Greedyv2TopN);
                if (candidateMoves.Count == 0)
                    candidateMoves = new List<Int32>(rootMoves);

                Int32 iterationBestMove = Move.Invalid;
                Int32 iterationBestValue = -Infinity;
                Int32 localAlpha = alpha;
                Int32 localBeta = beta;

                for (Int32 i = 0; i < candidateMoves.Count; i++) {
                    if (TimeExpired()) {
                        _abortSearch = true;
                        break;
                    }

                    Int32 move = candidateMoves[i];
                    if (!IsLegalMove(position, move))
                        continue;

                    position.Make(move);
                    Boolean childInCheck = position.InCheck(position.SideToMove);
                    Int32 value = currentDepth > 1
                        ? -SearchNodev2(position, currentDepth - 1, ply + 1, -localBeta, -localAlpha, childInCheck)
                        : -Evaluator.Evaluate(position);

                    if (IsExactMateV2(position, childInCheck))
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
                    WriteGreedyLog(position, currentDepth, bestValue, GetGreedyPrincipalVariation(position, iterationBestMove, currentDepth));
                }
            }

            _abortSearch = false;
            return bestMove != Move.Invalid ? bestMove : rootMoves[0];
        }

        private Int32 SearchNodev2(Position position, Int32 depth, Int32 ply, Int32 alpha, Int32 beta, Boolean inCheck) {
            if (position == null)
                return Move.Invalid;

            _totalNodes++;

            if (TimeExpired()) {
                _abortSearch = true;
                return Evaluator.Evaluate(position);
            }

            if (depth <= 0)
                return Quiescence(position, ply, alpha, beta, inCheck);

            List<Int32> moves = GetGreedyv2Candidates(position, position.LegalMoves(), Greedyv2TopN);
            if (moves.Count == 0)
                moves = position.LegalMoves();

            if (CanApplyNullMoveV2(position, inCheck, depth)) {
                position.MakeNull();
                Int32 nullValue = -SearchNodev2(position, depth - 1 - Greedyv2NullMoveReduction, ply + 1, -beta, -beta + 1, false);
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
                if (!IsLegalMove(position, move))
                    continue;

                Boolean tactical = inCheck || position.CausesCheck(move) || Move.IsCapture(move) || Move.IsPromotion(move) || Move.IsEnPassant(move);
                Boolean reducible = i >= Greedyv2LateMoveReductionMove && depth >= Greedyv2LateMoveReductionDepth && !tactical;

                position.Make(move);
                Boolean childInCheck = position.InCheck(position.SideToMove);
                Int32 value;
                if (reducible)
                    value = -SearchNodev2(position, depth - 2, ply + 1, -alpha - 1, -alpha, childInCheck);
                else if (i > 0)
                    value = -SearchNodev2(position, depth - 1, ply + 1, -alpha - 1, -alpha, childInCheck);
                else
                    value = -SearchNodev2(position, depth - 1, ply + 1, -beta, -alpha, childInCheck);

                if (value > alpha)
                    value = -SearchNodev2(position, depth - 1, ply + 1, -beta, -alpha, childInCheck);

                if (IsExactMateV2(position, childInCheck))
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

        private static Boolean IsLegalMove(Position position, Int32 move) {
            if (position == null || move == Move.Invalid)
                return false;
            foreach (Int32 legal in position.LegalMoves())
                if (legal == move)
                    return true;
            return false;
        }

        private static Boolean IsExactMateV2(Position position, Boolean inCheck) {
            if (position == null)
                return false;

            List<Int32> legalMoves = position.LegalMoves();
            return legalMoves.Count == 0 && inCheck;
        }

        private static Boolean CanApplyNullMoveV2(Position position, Boolean inCheck, Int32 depth) {
            if (position == null || inCheck || depth <= 2)
                return false;

            List<Int32> legalMoves = position.LegalMoves();
            if (legalMoves.Count <= 2)
                return false;

            Int32 colour = position.SideToMove;
            return position.Bitboard[colour] != (position.Bitboard[colour | Piece.King] | position.Bitboard[colour | Piece.Pawn]);
        }

        private static String NormalizeMoveToken(String value) {
            return new String((value ?? String.Empty).Where(c => !Char.IsWhiteSpace(c) && c != '+' && c != '#').ToArray()).ToLowerInvariant();
        }

        private List<Int32> GetGreedyv2Candidates(Position position, List<Int32> legalMoves, Int32 topN) {
            List<Int32> result = new List<Int32>();
            if (position == null || legalMoves == null || legalMoves.Count == 0)
                return result;

            Dictionary<String, Int32> lookup = new Dictionary<String, Int32>();
            foreach (Int32 move in legalMoves) {
                String simple = NormalizeMoveToken(Stringify.Move(move));
                String algebraic = NormalizeMoveToken(Stringify.MoveAlgebraically(position.DeepClone(), move));
                if (!lookup.ContainsKey(simple))
                    lookup[simple] = move;
                if (!lookup.ContainsKey(algebraic))
                    lookup[algebraic] = move;
            }

            List<(Int32 move, Double score)> scoredMoves = new List<(Int32 move, Double score)>();
            foreach (var prediction in InvokeGreedyv2Model(position, legalMoves.Count)) {
                String key = NormalizeMoveToken(prediction.label);
                if (!lookup.TryGetValue(key, out Int32 move))
                    continue;
                if (result.Contains(move))
                    continue;
                scoredMoves.Add((move, prediction.score));
            }

            foreach (var item in scoredMoves.OrderByDescending(x => x.score)) {
                result.Add(item.move);
                if (result.Count >= topN)
                    break;
            }

            return result;
        }

        private static InferenceSession CreateGreedyv2Session() {
            try {
                String modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Greedyv2ModelPath);
                if (!File.Exists(modelPath))
                    return null;
                SessionOptions options = new SessionOptions();
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                return new InferenceSession(modelPath, options);
            } catch {
                return null;
            }
        }

        private IEnumerable<(String label, Double score)> InvokeGreedyv2Model(Position position, Int32 topN) {
            List<(String label, Double score)> results = new List<(String label, Double score)>();
            if (Greedyv2Session == null || position == null)
                return results;

            try {
                Single[] inputData = BuildGreedyv2Input(position);
                DenseTensor<Single> input = new DenseTensor<Single>(inputData, new[] { 1, 18, 8, 8 });
                using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> output = Greedyv2Session.Run(new[] { NamedOnnxValue.CreateFromTensor("input", input) })) {
                    float[] logits = output.First().AsEnumerable<Single>().ToArray();
                    float[] probabilities = Softmax(logits);
                    int take = Math.Min(topN, probabilities.Length);
                    for (int i = 0; i < take; i++) {
                        int bestIndex = -1;
                        float bestValue = float.NegativeInfinity;
                        for (int j = 0; j < probabilities.Length; j++) {
                            if (probabilities[j] > bestValue) {
                                bestValue = probabilities[j];
                                bestIndex = j;
                            }
                        }
                        if (bestIndex < 0)
                            break;
                        String label = bestIndex >= 0 && bestIndex < Greedyv2Labels.Value.Count ? Greedyv2Labels.Value[bestIndex] : String.Empty;
                        results.Add((label, bestValue));
                        probabilities[bestIndex] = float.NegativeInfinity;
                    }
                }

                if (Restrictions.Output == OutputType.GUI && results.Count > 0) {
                    Terminal.WriteLine("CNN top-{0} predictions for FEN: {1}", topN, position.GetFEN());
                    for (Int32 i = 0; i < results.Count; i++) {
                        Terminal.WriteLine("  {0,2}. {1,-16} {2:+0.0000;-0.0000}", i + 1, String.IsNullOrWhiteSpace(results[i].label) ? "<unmapped>" : results[i].label, results[i].score);
                    }
                    Terminal.WriteLine();
                }
            } catch {
            }

            return results;
        }

        private static float[] BuildGreedyv2Input(Position position) {
            float[] input = new float[1 * 18 * 8 * 8];
            for (Int32 square = 0; square < 64; square++) {
                Int32 piece = position.Square[square];
                if (piece == Piece.Empty)
                    continue;
                Int32 rank = 7 - Position.Rank(square);
                Int32 file = Position.File(square);
                Int32 type = piece & Piece.Mask;
                Int32 basePlane = (piece & Colour.Black) != 0 ? 6 : 0;
                Int32 planeOffset = type == Piece.Pawn ? 0 : type == Piece.Knight ? 1 : type == Piece.Bishop ? 2 : type == Piece.Rook ? 3 : type == Piece.Queen ? 4 : type == Piece.King ? 5 : -1;
                if (planeOffset < 0)
                    continue;
                input[((basePlane + planeOffset) * 8 + rank) * 8 + file] = 1f;
            }

            for (Int32 rank = 0; rank < 8; rank++)
                for (Int32 file = 0; file < 8; file++) {
                    Int32 index = (12 * 8 + rank) * 8 + file;
                    input[index] = position.SideToMove == Colour.White ? 1f : 0f;
                    input[((13 * 8) + rank) * 8 + file] = position.CastleKingside[Colour.White] > 0 ? 1f : 0f;
                    input[((14 * 8) + rank) * 8 + file] = position.CastleQueenside[Colour.White] > 0 ? 1f : 0f;
                    input[((15 * 8) + rank) * 8 + file] = position.CastleKingside[Colour.Black] > 0 ? 1f : 0f;
                    input[((16 * 8) + rank) * 8 + file] = position.CastleQueenside[Colour.Black] > 0 ? 1f : 0f;
                    input[((17 * 8) + rank) * 8 + file] = position.EnPassantSquare != Position.InvalidSquare ? 1f : 0f;
                }

            return input;
        }

        private static float[] Softmax(float[] logits) {
            float max = logits.Max();
            float[] exp = new float[logits.Length];
            float sum = 0f;
            for (int i = 0; i < logits.Length; i++) {
                exp[i] = (float)Math.Exp(logits[i] - max);
                sum += exp[i];
            }
            if (sum <= 0f)
                return logits.Select(_ => 0f).ToArray();
            for (int i = 0; i < exp.Length; i++)
                exp[i] /= sum;
            return exp;
        }

        private static List<String> LoadGreedyv2Labels() {
            try {
                String metadataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Greedyv2MetadataPath);
                if (!File.Exists(metadataPath))
                    return new List<String>();
                String text = File.ReadAllText(metadataPath);
                Match match = Regex.Match(text, "\\\"move_labels\\\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                if (!match.Success)
                    return new List<String>();
                return Regex.Matches(match.Groups[1].Value, "\\\"(.*?)\\\"").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            } catch {
                return new List<String>();
            }
        }
    }
}
