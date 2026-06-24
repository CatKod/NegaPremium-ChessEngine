using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace NegaPremium
{

    /// <summary>
    /// Encapsulates the declarations component of the Nega Premium chess engine. 
    /// Quản lý các cấu hình, hằng số thời gian và biến dùng cho Thuật toán Tìm kiếm (Search).
    /// </summary>
    public sealed partial class Engine : IPlayer
    {
        public enum SearchMode
        {
            Classic,
            Tactical,
            Mcts,
            Greedy,
            Greedyv2
        }

        // ========================================================
        // 1. MISCELLANEOUS & FORMATTING / ĐỊNH DẠNG VÀ PHIÊN BẢN
        // ========================================================
        public static readonly String Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

        private static readonly String PVFormat = "{0,-" + DepthWidth + "}{1,-" + ValueWidth + "}{2}";
        private const Int32 SingleVariationDepth = 5;
        private const Int32 DepthWidth = 8;
        private const Int32 ValueWidth = 9;

        // ========================================================
        // 2. SEARCH CONSTANTS / HẰNG SỐ TÌM KIẾM
        // ========================================================
        public const Int32 DepthLimit = 64;
        public const Int32 PlyLimit = DepthLimit + 64;
        public const Int32 MovesLimit = 256;
        public const Int32 DefaultHashAllocation = 64;
        public const Int32 NodeResolution = 1000;
        public const Int32 CheckmateValue = 100000;
        public const Int32 NearCheckmateValue = CheckmateValue - PlyLimit;
        public const Int32 Infinity = 110000;

        public Int32 AspirationWindow = 17;
        public Int32 NullMoveReduction = 3;
        public Int32 NullMoveAggressiveDepth = 7;
        public Int32 NullMoveAggressiveDivisor = 5;
        public Int32 LateMoveReduction = 2;
        public Single HashMoveValue = 60F;
        public Int32 KillerMovesAllocation = 2;
        public Single KillerMoveValue = 0.9F;
        public Single KillerMoveSlotValue = -0.01F;
        public Single QueenPromotionMoveValue = 1F;
        public Int32[] FutilityMargin = { 0, 104, 125, 250, 271, 375 };
        public Int32 DrawValue = -30;

        // ========================================================
        // 3. TIME CONTROLS / KIỂM SOÁT THỜI GIAN
        // ========================================================
        private const Double TimeControlsExpectedLatency = 55;
        private const Double TimeControlsContinuationThreshold = 0.7;
        private const Double TimeControlsResearchThreshold = 0.5;
        private const Double TimeControlsResearchExtension = 0.8;
        private const Int32 TimeControlsLossResolution = 40;
        private const Double TimeControlsLossThreshold = 0.5;
        private static readonly Double[] TimeControlsLossExtension = { 0, 0.1, 0.6, 1.2, 1.5 };

        // ========================================================
        // 4. SEARCH VARIABLES / CÁC BIẾN HOẠT ĐỘNG CỦA ALPHA-BETA
        // ========================================================
        private HashTable _table = new HashTable(DefaultHashAllocation << 20);
        private readonly Int32[][] _generatedMoves = new Int32[PlyLimit][];
        private readonly Int32[][] _pvMoves = new Int32[PlyLimit][];
        private readonly Int32[] _pvLength = new Int32[PlyLimit];
        private readonly Int32[][] _killerMoves = new Int32[PlyLimit][];
        private readonly Single[] _moveValues = new Single[MovesLimit];
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private Boolean _abortSearch = true;
        private Double _timeLimit;
        private Double _timeExtension;
        private Double _timeExtensionLimit;
        private Int32 _finalAlpha = 0;
        private Int32 _rootAlpha = 0;
        private Int64 _totalNodes;
        private Int64 _quiescenceNodes;
        private Int64 _referenceNodes;
        private Int64 _hashProbes;
        private Int64 _hashCutoffs;
        private Int64 _hashMoveChecks;
        private Int64 _hashMoveMatches;
        private Int64 _killerMoveChecks;
        private Int64 _killerMoveMatches;
        private Int64 _movesSearched;
        private Int64 _futileMoves;

        // ========================================================
        // 5. CONSTRUCTOR / KHỞI TẠO BỘ NHỚ TÌM KIẾM
        // ========================================================
        public Engine()
        {
            // Đã xóa phần khởi tạo Mảng lượng giá (Đã chuyển sang Evaluator)
            // Chỉ giữ lại khởi tạo mảng phục vụ Tìm kiếm
            for (Int32 i = 0; i < _generatedMoves.Length; i++)
                _generatedMoves[i] = new Int32[MovesLimit];
            for (Int32 i = 0; i < _pvMoves.Length; i++)
                _pvMoves[i] = new Int32[PlyLimit];
            for (Int32 i = 0; i < _killerMoves.Length; i++)
                _killerMoves[i] = new Int32[KillerMovesAllocation];
        }
    }
}