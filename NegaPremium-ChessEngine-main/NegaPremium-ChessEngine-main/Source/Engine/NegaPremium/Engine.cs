using System;
using System.Collections.Generic;
using System.Drawing;

namespace NegaPremium {

    /// <summary>
    /// Encapsulates the main IPlayer interface of the Nega Premium chess engine. 
    /// </summary>
    public sealed partial class Engine : IPlayer {

        /// <summary>
        /// Selects which search algorithm to use.
        /// </summary>
        public SearchMode Mode { get; set; } = SearchMode.Classic;
        /// Engine is AI. 
        /// </summary>
        public Boolean IsAI
        {
            get { 
                return true; 
            }
        }

        /// <summary>
        /// The number of nodes visited during the most recent search. 
        /// </summary>
        public Int64 Nodes {
            get {
                return _totalNodes;
            }
        }

        /// <summary>
        /// The size of the transposition table in megabytes. 
        /// </summary>
        public Int32 HashAllocation {
            get {
                return _table.Size >> 20;
            }
            set {
                if (value != _table.Size >> 20)
                    _table = new HashTable(value << 20);
            }
        }

        /// <summary>
        /// Whether to use experimental features. 
        /// </summary>
        public Boolean IsExperimental { get; set; }

        /// <summary>
        /// The name of the engine based on its current search mode.
        /// </summary>
        public String Name
        {
            get
            {
                if (Mode == SearchMode.Greedy) return "Greedy " + Version;
                if (Mode == SearchMode.Greedyv2) return "Greedyv2 " + Version;

                return "Nega Premium " + Version;
            }
        }

        /// <summary>
        /// Whether the engine is willing to accept a draw offer. 
        /// </summary>
        public Boolean AcceptsDraw {
            get {
                return _finalAlpha <= DrawValue;
            }
        }

        /// <summary>
        /// Returns the best move as determined by the engine. This method may write 
        /// output to the terminal. 
        /// </summary>
        /// <param name="position">The position to analyse.</param>
        /// <returns>The best move as determined by the engine.</returns>
        public Int32 GetMove(Position position) {
            if (Restrictions.Output == OutputType.GUI) {
                //Terminal.Clear();
                Terminal.WriteLine(PVFormat, "Depth", "Value", "Principal Variation");
                Terminal.WriteLine("-----------------------------------------------------------------------");
            }

            // Initialize variables to prepare for search. 
            _abortSearch = false;
            _pvLength[0] = 0;
            _totalNodes = 0;
            _quiescenceNodes = 0;
            _referenceNodes = 0;
            _hashProbes = 0;
            _hashCutoffs = 0;
            _hashMoveChecks = 0;
            _hashMoveMatches = 0;
            _killerMoveChecks = 0;
            _killerMoveMatches = 0;
            _futileMoves = 0;
            _movesSearched = 0;
            _stopwatch.Reset();
            _stopwatch.Start();

            // Perform the search. 
            Int32 move = Move.Invalid;
            String botName = "Nega Premium " + Version;

            if (Mode == SearchMode.Greedy)
            {
                move = GreedySearch(position, Restrictions.Depth, -Infinity, Infinity, 0, position.InCheck(position.SideToMove), true);
                botName = "Greedy " + Version;
            }
            else if (Mode == SearchMode.Greedyv2)
            {
                move = Greedyv2Search(position, Restrictions.Depth, -Infinity, Infinity, 0, position.InCheck(position.SideToMove), true);
                botName = "Greedyv2 " + Version;
            }
            else
            {
                move = Search(position);
            }

            _stopwatch.Stop();

            if (Restrictions.Output == OutputType.GUI)
            {
                StatisticsLogger.LogGUI(
                    position,
                    botName,
                    _stopwatch.Elapsed.TotalMilliseconds,
                    _totalNodes,
                    _movesSearched,
                    _quiescenceNodes,
                    _futileMoves,
                    _hashProbes,
                    _hashCutoffs,
                    _hashMoveChecks,
                    _hashMoveMatches,
                    _killerMoveChecks,
                    _killerMoveMatches
                );
            }

            return move;
        }

        /// <summary>
        /// Stops the search if applicable. 
        /// </summary>
        public void Stop() {
            _abortSearch = true;
        }

        /// <summary>
        /// Resets the engine. 
        /// </summary>
        public void Reset() {
            _table.Clear();
            for (Int32 i = 0; i < _killerMoves.Length; i++)
                Array.Clear(_killerMoves[i], 0, _killerMoves[i].Length);
            _finalAlpha = 0;
            _rootAlpha = 0;
            _totalNodes = 0;
        }

        /// <summary>
        /// The principal variation of the most recent search.
        /// </summary>
        public List<Int32> PrincipalVariation {
            get;
            private set;
        }

        /// <summary>
        /// Returns the principal variation of the most recent search.
        /// </summary>
        /// <returns>The principal variation of the most recent search.</returns>
        public List<Int32> GetPrincipalVariation() {
            List<Int32> variation = new List<Int32>();
            for (Int32 i = 0; i < _pvLength[0]; i++)
                variation.Add(_pvMoves[0][i]);
            return variation;
        }

        /// <summary>
        /// Draws the player's graphical elements. 
        /// </summary>
        /// <param name="g">The drawing surface.</param>
        public void Draw(Graphics g) { }
    }
}
