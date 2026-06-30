using System;
using System.Collections.Generic;

namespace NegaPremium {

    /// <summary>
    /// Encapsulates the component of the Nega Premium chess engine
    /// responsible for computing the principal variation to output. 
    /// </summary>
    public sealed partial class Engine : IPlayer {

        /// <summary>
        /// Returns a string that describes the given principal variation. 
        /// </summary>
        /// <param name="position">The position the principal variation is to be played on.</param>
        /// <param name="depth">The depth of the search that yielded the principal variation.</param>
        /// <param name="value">The value of the search that yielded the principal variation.</param>
        /// <returns>A string that describes the given principal variation.</returns>
        private String CreatePVString(Position position, Int32 depth, Int32 value) {
            List<Int32> pv = GetCurrentPV();
            Boolean isMate = Math.Abs(value) > NearCheckmateValue;
            Int32 movesToMate = (CheckmateValue - Math.Abs(value) + 1) / 2;

            switch (Restrictions.Output) {

                // Return standard output. 
                case OutputType.GUI:
                    String depthString = depth.ToString();
                    String valueString = isMate ? (value > 0 ? "+Mate " : "-Mate ") + movesToMate :
                                                  (value / 100.0).ToString("+0.00;-0.00");
                    return String.Format(PVFormat, depthString, valueString, FormatPrincipalVariation(position, pv));

                // Return UCI output. 
                case OutputType.UCI:
                    String score = isMate ? "mate " + (value < 0 ? "-" : "") + movesToMate :
                                            "cp " + value;
                    Double elapsed = _stopwatch.Elapsed.TotalMilliseconds;
                    Int64 nps = (Int64)(1000 * _totalNodes / elapsed);

                    return String.Format("info depth {0} score {1} time {2} nodes {3} nps {4} pv {5}", depth, score, (Int32)elapsed, _totalNodes, nps, Stringify.Moves(pv));
            }
            return null;
        }

        /// <summary>
        /// Prepends the given move to the principal variation at the given ply.
        /// </summary>
        /// <param name="move">The move to prepend to the principal variation.</param>
        /// <param name="ply">The ply the move was made at.</param>
        private void PrependCurrentPV(Int32 move, Int32 ply) {
            _pvMoves[ply][0] = move;
            for (Int32 j = 0; j < _pvLength[ply + 1]; j++)
                _pvMoves[ply][j + 1] = _pvMoves[ply + 1][j];
            _pvLength[ply] = _pvLength[ply + 1] + 1;
        }

        /// <summary>
        /// Returns the principal variation of the most recent search.
        /// </summary>
        /// <returns>The principal variation of the most recent search.</returns>
        private List<Int32> GetCurrentPV() {
            List<Int32> variation = new List<Int32>();
            for (Int32 i = 0; i < _pvLength[0]; i++)
                variation.Add(_pvMoves[0][i]);
            return variation;
        }

        private static String FormatPrincipalVariation(Position position, List<Int32> moves)
        {
            if (position == null || moves == null || moves.Count == 0) return String.Empty;

            string rawPv = Stringify.MovesAlgebraically(position, moves);

            string[] rawTokens = rawPv.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> sanMoves = new List<string>();
            foreach (string token in rawTokens)
            {
                if (!token.Contains("."))
                {
                    sanMoves.Add(token);
                }
            }

            System.Text.StringBuilder pv = new System.Text.StringBuilder();
            string[] fenParts = position.GetFEN().Split(' ');
            int currentMoveNumber = 1;
            bool isBlackToMove = false;

            if (fenParts.Length >= 2 && fenParts[1] == "b") isBlackToMove = true;
            if (fenParts.Length >= 6) int.TryParse(fenParts[5], out currentMoveNumber);

            for (int i = 0; i < sanMoves.Count; i++)
            {
                if (i > 0) pv.Append(" ");

                if (isBlackToMove)
                {
                    if (i == 0) pv.Append(currentMoveNumber).Append("... ");
                    pv.Append(sanMoves[i]);
                    isBlackToMove = false;
                    currentMoveNumber++;
                }
                else
                {
                    pv.Append(currentMoveNumber).Append(". ").Append(sanMoves[i]);
                    isBlackToMove = true;
                }
            }
            return pv.ToString().Trim();
        }
    }
}
