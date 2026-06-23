using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NegaPremium {

    /// <summary>
    /// Provides a simple benchmark runner for comparing search modes on the same
    /// set of FEN positions.
    /// </summary>
    public sealed partial class Engine : IPlayer {

        /// <summary>
        /// Benchmarks the available search modes on the provided FEN positions.
        /// </summary>
        /// <param name="fens">The positions to test.</param>
        /// <returns>A formatted benchmark report.</returns>
        public String Benchmark(IEnumerable<String> fens) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SearchMode,FEN,Move,ElapsedMs,Nodes,Evaluation");

            SearchMode originalMode = Mode;
            try {
                foreach (String fen in fens) {
                    Position position = Position.Create(fen);

                    foreach (SearchMode mode in Enum.GetValues(typeof(SearchMode))) {
                        Reset();
                        Mode = mode;

                        Position copy = position.DeepClone();
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        Int32 move = GetMove(copy);
                        stopwatch.Stop();

                        Int32 evaluation = Evaluator.Evaluate(copy);
                        sb.AppendLine(String.Format(
                            "{0},\"{1}\",\"{2}\",{3:0.00},{4},{5}",
                            mode,
                            fen.Replace("\"", "\"\""),
                            Stringify.Move(move),
                            stopwatch.Elapsed.TotalMilliseconds,
                            Nodes,
                            evaluation));
                    }
                }
            } finally {
                Mode = originalMode;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Benchmarks the available search modes on positions loaded from a file.
        /// </summary>
        /// <param name="fenFilePath">The file containing one FEN per line.</param>
        /// <returns>A formatted benchmark report.</returns>
        public String BenchmarkFromFile(String fenFilePath) {
            List<String> fens = new List<String>();
            foreach (String line in File.ReadAllLines(fenFilePath)) {
                String fen = line.Trim();
                if (fen.Length > 0 && !fen.StartsWith("#", StringComparison.Ordinal))
                    fens.Add(fen);
            }
            return Benchmark(fens);
        }
    }
}
