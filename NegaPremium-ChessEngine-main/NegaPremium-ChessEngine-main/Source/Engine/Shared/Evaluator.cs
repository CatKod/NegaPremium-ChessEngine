using System;

namespace NegaPremium
{
    /// <summary>
    /// Shared Static Evaluator component. / Trạm Đánh giá tĩnh dùng chung cho toàn hệ thống.
    /// Manages evaluation terms, positional piece matrices (PSTs), and material weights.
    /// Quản lý toàn bộ thế giới quan, điểm số quân cờ, ma trận vị trí tách biệt khỏi Search.
    /// </summary>
    public static class Evaluator
    {
        // =======================================================================
        // 1. EVALUATION CONSTANTS & FIELDS / HẰNG SỐ VÀ MẢNG TRỌNG SỐ LƯỢNG GIÁ
        // =======================================================================

        public static Int32 KingOnOpenFileValue = -58;
        public static Int32 KingAdjacentToOpenFileValue = -42;

        public static Int32[][] QueenToEnemyKingSpatialValue = new Int32[64][];
        public static Int32[] QueenDistanceToEnemyKingValue = { 0, 17, 8, 4, 0, -4, -8, -12 };

        public static Int32 BishopPairValue = 29;
        public static Int32[] BishopMobilityValue = { -25, -12, -3, 0, 2, 5, 8, 10, 12, 13, 15, 17, 18, 18 };

        public static Int32[][] KnightToEnemyKingSpatialValue = new Int32[64][];
        public static Int32[] KnightDistanceToEnemyKingValue = { 0, 8, 8, 6, 4, 0, -4, -6, -8, -10, -12, -13, -15, -17, -25 };
        public static Int32[] KnightMovesToEnemyKingValue = { 0, 21, 8, 0, -4, -8, -12 };
        public static Int32[] KnightMobilityValue = { -21, -8, -2, 0, 2, 5, 8, 10, 12 };

        public static Int32 PawnEndgameGainValue = 17;
        public static Int32 PawnNearKingValue = 14;
        public static Int32 DoubledPawnValue = -21;
        public static Int32 IsolatedPawnValue = -17;
        public static Int32 PassedPawnValue = 25;
        public static Int32 PawnAttackValue = 17;
        public static Int32 PawnDefenceValue = 6;
        public static Int32 PawnDeficiencyValue = -29;

        public static readonly Int32[] PieceValue = new Int32[14];
        public static Int32 TempoValue = 6;

        public static readonly UInt64[] PawnShieldBitboard = new UInt64[64];
        public static readonly UInt64[] ShortAdjacentFilesBitboard = new UInt64[64];
        public static readonly UInt64[][] PawnBlockadeBitboard = { new UInt64[64], new UInt64[64] };
        public static readonly UInt64[][] ShortForwardFileBitboard = { new UInt64[64], new UInt64[64] };
        public const UInt64 NotAFileBitboard = 0xFEFEFEFEFEFEFEFEUL;
        public const UInt64 NotHFileBitboard = 0x7F7F7F7F7F7F7F7FUL;

        public static readonly Int32[][] RectilinearDistance = new Int32[64][];
        public static readonly Int32[][] ChebyshevDistance = new MyArray64(); // Tránh lỗi lặp khởi tạo
        public static readonly Int32[][] KnightMoveDistance = new Int32[64][];
        public static Single PhaseCoefficient;

        private static readonly UInt64[] _minorAttackBitboard = new UInt64[2];
        private static readonly UInt64[] _pawnAttackBitboard = new UInt64[2];

        // Class giả lập mảng lồng để giữ tính an toàn của mảng tĩnh
        private class MyArray64
        {
            private readonly Int32[][] _data = new Int32[64][];
            public Int32[] this[Int32 index]
            {
                get { return _data[index]; }
                set { _data[index] = value; }
            }
            public static implicit operator Int32[][](MyArray64 target) { return target._data; }
        }

        // =======================================================================
        // 2. PIECE SQUARE TABLES (PST) / MA TRẬN ĐIỂM VỊ TRÍ QUÂN CỜ TĨNH
        // =======================================================================

        public static readonly Int32[][] KingOpeningPositionValue = {
            new Int32[] {
                -25,-33,-33,-33,-33,-33,-33,-25,
                -25,-33,-33,-33,-33,-33,-33,-25,
                -25,-33,-33,-33,-33,-33,-33,-25,
                -25,-33,-33,-33,-33,-33,-33,-25,
                -25,-33,-33,-33,-33,-33,-33,-25,
                 -8,-17,-17,-17,-17,-17,-17, -8,
                 17, 17,  0,  0,  0,  0, 17, 17,
                 21, 25, 12,  0,  0, 12, 25, 21
            },
            new Int32[64],
        };

        public static readonly Int32[][] KingEndgamePositionValue = {
            new Int32[] {
                -42,-33,-25,-17,-17,-25,-33,-42,
                -33,-17, -8, -8, -8, -8,-17,-33,
                -25, -8, 17, 21, 21, 17, -8,-25,
                -25, -8, 21, 29, 29, 21, -8,-25,
                -25, -8, 21, 29, 29, 21, -8,-25,
                -25, -8, 17, 21, 21, 17, -8,-25,
                -33,-17, -8, -8, -8, -8,-17,-33,
                -42,-33,-25,-17,-17,-25,-33,-42
            },
            new Int32[64],
        };

        public static readonly Int32[][] QueenOpeningPositionValue = {
            new Int32[] {
                -17,-12, -8,  8,  0, -8,-12,-17,
                 -8,  0,  0,  0,  0,  0,  0, -8,
                 -8,  0,  4,  4,  4,  4,  0, -8,
                 -4,  0,  4,  4,  4,  4,  0, -4,
                 -4,  0,  4,  4,  4,  4,  0, -4,
                 -8,  0,  4,  4,  4,  4,  0, -8,
                 -8,  0,  0,  0,  0,  0,  0, -8,
                -17,-12, -8,  8,  0, -8,-12,-17
            },
            new Int32[64],
        };

        public static readonly Int32[][] RookPositionValue = {
            new Int32[] {
                  0,  0,  0,  0,  0,  0,  0,  0,
                  4,  8,  8,  8,  8,  8,  8,  4,
                 -4,  0,  0,  0,  0,  0,  0, -4,
                 -4,  0,  0,  0,  0,  0,  0, -4,
                 -4,  0,  0,  0,  0,  0,  0, -4,
                 -4,  0,  0,  0,  0,  0,  0, -4,
                 -4,  0,  0,  0,  0,  0,  0, -4,
                  0,  0,  4,  4,  4,  4,  0,  0
            },
            new Int32[64],
        };

        public static readonly Int32[][] BishopPositionValue = {
            new Int32[] {
                -17, -8, -8, -8, -8, -8, -8,-17,
                 -8,  0,  0,  0,  0,  0,  0, -8,
                 -8,  0,  4,  8,  8,  4,  0, -8,
                 -8,  4,  4,  8,  8,  4,  4, -8,
                 -8,  4, 12,  8,  8, 12,  4, -8,
                 -8, 12,  8, 12, 12,  8, 12, -8,
                 -8, 12,  0,  0,  0,  0, 12, -8,
                -17, -8, -8, -8, -8, -8, -8,-17
            },
            new Int32[64],
        };

        public static readonly Int32[][] KnightOpeningPositionValue = {
            new Int32[] {
                -25,-17,-17,-17,-17,-17,-17,-25,
                -17,-12,  0,  8,  8,  0,-12,-17,
                -17,  0,  8, 12, 12,  8,  0,-17,
                -17,  8, 12, 17, 17, 12,  8,-17,
                -17,  8, 12, 17, 17, 12,  8,-17,
                -17,  4,  8, 12, 12,  8,  4,-17,
                -17,-12,  0,  8,  8,  0,-12,-17,
                -25,-17,-17,-17,-17,-17,-17,-25
            },
            new Int32[64],
        };

        public static readonly Int32[][] PawnPositionValue = {
            new Int32[] {
                  0,  0,  0,  0,  0,  0,  0,  0,
                 75, 75, 75, 75, 75, 75, 75, 75,
                 25, 25, 29, 29, 29, 29, 25, 25,
                  4,  8, 12, 21, 21, 12,  8,  4,
                  0,  4,  8, 17, 17,  8,  4,  0,
                  4, -4, -8,  4,  4, -8, -4,  4,
                  4,  8,  8,-17,-17,  8,  8,  4,
                  0,  0,  0,  0,  0,  0,  0,  0
            },
            new Int32[64],
        };

        public static readonly Int32[][] PassedPawnEndgamePositionValue = {
            new Int32[] {
                  0,  0,  0,  0,  0,  0,  0,  0,
                100,100,100,100,100,100,100,100,
                 52, 52, 52, 52, 52, 52, 52, 52,
                 31, 31, 31, 31, 31, 31, 31, 31,
                 22, 22, 22, 22, 22, 22, 22, 22,
                 17, 17, 17, 17, 17, 17, 17, 17,
                  8,  8,  8,  8,  8,  8,  8,  8,
                  0,  0,  0,  0,  0,  0,  0,  0
            },
            new Int32[64],
        };

        // =======================================================================
        // 3. STATIC DATA INITIALIZER / LUỒNG TỰ ĐỘNG KHỞI TẠO MA TRẬN ĐIỂM
        // =======================================================================

        static Evaluator()
        {
            // Khởi tạo giá trị cơ bản cho các quân cờ / Initialize piece raw scores
            PieceValue[Piece.King] = 3000;
            PieceValue[Piece.Queen] = 1025;
            PieceValue[Piece.Rook] = 575;
            PieceValue[Piece.Bishop] = 370;
            PieceValue[Piece.Knight] = 350;
            PieceValue[Piece.Pawn] = 100;
            for (Int32 piece = Piece.Min; piece <= Piece.Max; piece += 2)
                PieceValue[Colour.Black | piece] = PieceValue[piece];
            PieceValue[Piece.Empty] = 0;

            // Tính hệ số giai đoạn ván cờ / Parse Phase Coefficient
            PhaseCoefficient += PieceValue[Piece.Queen];
            PhaseCoefficient += 2 * PieceValue[Piece.Rook];
            PhaseCoefficient += 2 * PieceValue[Piece.Bishop];
            PhaseCoefficient += 2 * PieceValue[Piece.Knight];
            PhaseCoefficient += 8 * PieceValue[Piece.Pawn];
            PhaseCoefficient = 1 / PhaseCoefficient;

            for (Int32 square = 0; square < 64; square++)
            {
                // Đồng bộ và dịch gương bàn cờ cho Đen / Mirror PST tables for Black side
                Int32 reflected = Position.File(square) + (7 - Position.Rank(square)) * 8;
                KingOpeningPositionValue[Colour.Black][square] = KingOpeningPositionValue[Colour.White][reflected];
                KingEndgamePositionValue[Colour.Black][square] = KingEndgamePositionValue[Colour.White][reflected];
                QueenOpeningPositionValue[Colour.Black][square] = QueenOpeningPositionValue[Colour.White][reflected];
                RookPositionValue[Colour.Black][square] = RookPositionValue[Colour.White][reflected];
                BishopPositionValue[Colour.Black][square] = BishopPositionValue[Colour.White][reflected];
                KnightOpeningPositionValue[Colour.Black][square] = KnightOpeningPositionValue[Colour.White][reflected];
                PawnPositionValue[Colour.Black][square] = PawnPositionValue[Colour.White][reflected];
                PassedPawnEndgamePositionValue[Colour.Black][square] = PassedPawnEndgamePositionValue[Colour.White][reflected];

                // Khởi tạo mặt nạ lá chắn Tốt quanh Vua / Initialize pawn shield tables
                PawnShieldBitboard[square] = Bit.File[square];
                if (Position.File(square) > 0)
                    PawnShieldBitboard[square] |= Bit.File[square - 1];
                if (Position.File(square) < 7)
                    PawnShieldBitboard[square] |= Bit.File[square + 1];
                PawnShieldBitboard[square] &= Bit.FloodFill(square, 2);

                // Khởi tạo mặt nạ các cột lân cận / Initialize short adjacent files tables
                if (Position.File(square) > 0)
                    ShortAdjacentFilesBitboard[square] |= Bit.File[square - 1] & Bit.FloodFill(square - 1, 3);
                if (Position.File(square) < 7)
                    ShortAdjacentFilesBitboard[square] |= Bit.File[square + 1] & Bit.FloodFill(square + 1, 3);

                // Khởi tạo hệ thống phong tỏa Tốt / Initialize pawn blockade tables
                PawnBlockadeBitboard[Colour.White][square] = Bit.RayN[square];
                if (Position.File(square) > 0)
                    PawnBlockadeBitboard[Colour.White][square] |= Bit.RayN[square - 1];
                if (Position.File(square) < 7)
                    PawnBlockadeBitboard[Colour.White][square] |= Bit.RayN[square + 1];
                PawnBlockadeBitboard[Colour.Black][square] = Bit.RayS[square];
                if (Position.File(square) > 0)
                    PawnBlockadeBitboard[Colour.Black][square] |= Bit.RayS[square - 1];
                if (Position.File(square) < 7)
                    PawnBlockadeBitboard[Colour.Black][square] |= Bit.RayS[square + 1];

                ShortForwardFileBitboard[Colour.White][square] = Bit.RayN[square] & Bit.FloodFill(square, 3);
                ShortForwardFileBitboard[Colour.Black][square] = Bit.RayS[square] & Bit.FloodFill(square, 3);

                // Khởi tạo bảng khoảng cách học / Initialize distance geometric tables
                RectilinearDistance[square] = new Int32[64];
                for (Int32 to = 0; to < 64; to++)
                    RectilinearDistance[square][to] = Math.Abs(Position.File(square) - Position.File(to)) + Math.Abs(Position.Rank(square) - Position.Rank(to));

                ChebyshevDistance[square] = new Int32[64];
                for (Int32 to = 0; to < 64; to++)
                    ChebyshevDistance[square][to] = Math.Max(Math.Abs(Position.File(square) - Position.File(to)), Math.Abs(Position.Rank(square) - Position.Rank(to)));

                KnightMoveDistance[square] = new Int32[64];
                for (Int32 i = 0; i < KnightMoveDistance[square].Length; i++)
                    KnightMoveDistance[square][i] = 6;
                for (Int32 moves = 1; moves <= 5; moves++)
                {
                    UInt64 moveBitboard = Attack.KnightFill(square, moves);
                    for (Int32 to = 0; to < 64; to++)
                    {
                        if ((moveBitboard & (1UL << to)) != 0 && moves < KnightMoveDistance[square][to])
                            KnightMoveDistance[square][to] = moves;
                    }
                }
            }

            // Gộp phần khởi tạo hình học không gian từ Constructor cũ sang cấu trúc static
            for (Int32 square = 0; square < 64; square++)
            {
                QueenToEnemyKingSpatialValue[square] = new Int32[64];
                for (Int32 to = 0; to < 64; to++)
                    QueenToEnemyKingSpatialValue[square][to] = QueenDistanceToEnemyKingValue[ChebyshevDistance[square][to]];

                KnightToEnemyKingSpatialValue[square] = new Int32[64];
                for (Int32 to = 0; to < 64; to++)
                    KnightToEnemyKingSpatialValue[square][to] =
                        KnightDistanceToEnemyKingValue[RectilinearDistance[square][to]] +
                        KnightMovesToEnemyKingValue[KnightMoveDistance[square][to]];
            }
        }

        // =======================================================================
        // 4. MAIN STATIC EVALUATION CORE LOGIC / LOGIC CHẤM ĐIỂM CHÍNH TOÀN CỤC
        // =======================================================================

        /// <summary>
        /// Calculates the static score of the current position. / Đánh giá tĩnh điểm số bàn cờ.
        /// </summary>
        public static Int32 Evaluate(Position position)
        {
            UInt64[] bitboard = position.Bitboard;
            Single opening = PhaseCoefficient * Math.Min(position.Material[Colour.White], position.Material[Colour.Black]);
            Single endgame = 1 - opening;

            _pawnAttackBitboard[Colour.White] = (bitboard[Colour.White | Piece.Pawn] & NotAFileBitboard) >> 9
                                              | (bitboard[Colour.White | Piece.Pawn] & NotHFileBitboard) >> 7;
            _pawnAttackBitboard[Colour.Black] = (bitboard[Colour.Black | Piece.Pawn] & NotAFileBitboard) << 7
                                              | (bitboard[Colour.Black | Piece.Pawn] & NotHFileBitboard) << 9;
            Single totalValue = TempoValue;

            for (Int32 colour = Colour.White; colour <= Colour.Black; colour++)
            {
                UInt64 targetBitboard = ~bitboard[colour] & ~_pawnAttackBitboard[1 - colour];
                UInt64 pawnBitboard = bitboard[colour | Piece.Pawn];
                UInt64 enemyPawnBitboard = bitboard[(1 - colour) | Piece.Pawn];
                UInt64 allPawnBitboard = pawnBitboard | enemyPawnBitboard;
                Int32 enemyKingSquare = Bit.Read(bitboard[(1 - colour) | Piece.King]);
                Single value = position.Material[colour];

                // Chấm điểm Vua / Evaluate king
                Int32 square = Bit.Read(bitboard[colour | Piece.King]);
                value += opening * KingOpeningPositionValue[colour][square] + endgame * KingEndgamePositionValue[colour][square];
                value += opening * PawnNearKingValue * Bit.Count(PawnShieldBitboard[square] & pawnBitboard);

                if ((allPawnBitboard & Bit.File[square]) == 0)
                    value += opening * KingOnOpenFileValue;

                if (Position.File(square) > 0 && (allPawnBitboard & Bit.File[square - 1]) == 0)
                    value += opening * KingAdjacentToOpenFileValue;

                if (Position.File(square) < 7 && (allPawnBitboard & Bit.File[square + 1]) == 0)
                    value += opening * KingAdjacentToOpenFileValue;

                // Chấm điểm Tượng / Evaluate bishops
                UInt64 pieceBitboard = bitboard[colour | Piece.Bishop];
                _minorAttackBitboard[colour] = 0;

                if ((pieceBitboard & (pieceBitboard - 1)) != 0)
                    value += BishopPairValue;

                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += BishopPositionValue[colour][square];

                    UInt64 pseudoMoveBitboard = Attack.Bishop(square, position.OccupiedBitboard);
                    value += BishopMobilityValue[Bit.Count(targetBitboard & pseudoMoveBitboard)];
                    _minorAttackBitboard[colour] |= pseudoMoveBitboard;
                }

                // Chấm điểm Mã / Evaluate knights
                pieceBitboard = bitboard[colour | Piece.Knight];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += opening * KnightOpeningPositionValue[colour][square];
                    value += endgame * KnightToEnemyKingSpatialValue[square][enemyKingSquare];

                    UInt64 pseudoMoveBitboard = Attack.Knight(square);
                    value += KnightMobilityValue[Bit.Count(targetBitboard & pseudoMoveBitboard)];
                    _minorAttackBitboard[colour] |= pseudoMoveBitboard;
                }

                // Chấm điểm Hậu / Evaluate queens
                pieceBitboard = bitboard[colour | Piece.Queen];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += opening * QueenOpeningPositionValue[colour][square];
                    value += endgame * QueenToEnemyKingSpatialValue[square][enemyKingSquare];
                }

                // Chấm điểm Xe / Evaluate rooks
                pieceBitboard = bitboard[colour | Piece.Rook];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += RookPositionValue[colour][square];
                }

                // Chấm điểm Tốt / Evaluate pawns
                Int32 pawns = 0;
                pieceBitboard = bitboard[colour | Piece.Pawn];
                while (pieceBitboard != 0)
                {
                    square = Bit.Pop(ref pieceBitboard);
                    value += PawnPositionValue[colour][square];
                    pawns++;

                    if ((ShortForwardFileBitboard[colour][square] & pawnBitboard) != 0)
                        value += DoubledPawnValue;

                    else if ((PawnBlockadeBitboard[colour][square] & enemyPawnBitboard) == 0)
                        value += PassedPawnValue + endgame * PassedPawnEndgamePositionValue[colour][square];

                    if ((ShortAdjacentFilesBitboard[square] & pawnBitboard) == 0)
                        value += IsolatedPawnValue;
                }
                value += (pawns == 0) ? PawnDeficiencyValue : pawns * endgame * PawnEndgameGainValue;

                // Tấn công và phòng ngự cận chiến / Tactics threat evaluation
                UInt64 victimBitboard = bitboard[(1 - colour)] ^ enemyPawnBitboard;
                value += PawnAttackValue * Bit.CountSparse(_pawnAttackBitboard[colour] & victimBitboard);

                UInt64 lowValueBitboard = bitboard[colour | Piece.Bishop] | bitboard[colour | Piece.Knight] | bitboard[colour | Piece.Pawn];
                value += PawnDefenceValue * Bit.Count(_pawnAttackBitboard[colour] & lowValueBitboard);

                if (colour == position.SideToMove)
                    totalValue += value;
                else
                    totalValue -= value;
            }

            // Pha ăn quân tức thì / Immediate asymmetric captures evaluation
            {
                Int32 colour = position.SideToMove;

                if ((_pawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Queen]) != 0)
                    totalValue += PieceValue[Piece.Queen] - PieceValue[Piece.Pawn];
                else if ((_minorAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Queen]) != 0)
                    totalValue += PieceValue[Piece.Queen] - PieceValue[Piece.Bishop];
                else if ((_pawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Rook]) != 0)
                    totalValue += PieceValue[Piece.Rook] - PieceValue[Piece.Pawn];
                else if ((_pawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Bishop]) != 0)
                    totalValue += PieceValue[Piece.Bishop] - PieceValue[Piece.Pawn];
                else if ((_pawnAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Knight]) != 0)
                    totalValue += PieceValue[Piece.Knight] - PieceValue[Piece.Pawn];
                else if ((_minorAttackBitboard[colour] & bitboard[(1 - colour) | Piece.Rook]) != 0)
                    totalValue += PieceValue[Piece.Rook] - PieceValue[Piece.Bishop];
            }

            return (Int32)totalValue;
        }

        // =======================================================================
        // 5. STATIC EXCHANGE EVALUATION (SEE) / THUẬT TOÁN ĐỔI QUÂN TĨNH SEE
        // =======================================================================

        public static Int32 EvaluateStaticExchange(Position position, Int32 move)
        {
            Int32 from = Move.From(move);
            Int32 to = Move.To(move);
            Int32 piece = Move.Piece(move);
            Int32 capture = Move.Capture(move);

            position.Bitboard[piece] ^= 1UL << from;
            position.OccupiedBitboard ^= 1UL << from;
            position.Square[to] = piece;

            Int32 value = 0;
            if (Move.IsPromotion(move))
            {
                Int32 promotion = Move.Special(move);
                position.Square[to] = promotion;
                value += PieceValue[promotion] - PieceValue[Piece.Pawn];
            }
            value += PieceValue[capture] - EvaluateStaticExchange(position, 1 - position.SideToMove, to);

            position.Bitboard[piece] ^= 1UL << from;
            position.OccupiedBitboard ^= 1UL << from;
            position.Square[to] = capture;

            return value;
        }

        public static Int32 EvaluateStaticExchange(Position position, Int32 colour, Int32 square)
        {
            Int32 value = 0;
            Int32 from = SmallestAttackerSquare(position, colour, square);
            if (from != Position.InvalidSquare)
            {
                Int32 piece = position.Square[from];
                Int32 capture = position.Square[square];

                position.Bitboard[piece] ^= 1UL << from;
                position.OccupiedBitboard ^= 1UL << from;
                position.Square[square] = piece;

                value = Math.Max(0, PieceValue[capture] - EvaluateStaticExchange(position, 1 - colour, square));

                position.Bitboard[piece] ^= 1UL << from;
                position.OccupiedBitboard ^= 1UL << from;
                position.Square[square] = capture;
            }
            return value;
        }

        public static Int32 SmallestAttackerSquare(Position position, Int32 colour, Int32 square)
        {
            // Kiểm tra quân Tốt tấn công / Try pawns
            UInt64 sourceBitboard = position.Bitboard[colour | Piece.Pawn] & Attack.Pawn(square, 1 - colour);
            if (sourceBitboard != 0)
                return Bit.Scan(sourceBitboard);

            // Kiểm tra quân Mã tấn công / Try knights
            sourceBitboard = position.Bitboard[colour | Piece.Knight] & Attack.Knight(square);
            if (sourceBitboard != 0)
                return Bit.Scan(sourceBitboard);

            // Kiểm tra quân Tượng tấn công / Try bishops
            UInt64 bishopAttackBitboard = UInt64.MaxValue;
            if ((position.Bitboard[colour | Piece.Bishop] & Bit.Diagonals[square]) != 0)
            {
                bishopAttackBitboard = Attack.Bishop(square, position.OccupiedBitboard);
                sourceBitboard = position.Bitboard[colour | Piece.Bishop] & bishopAttackBitboard;
                if (sourceBitboard != 0)
                    return Bit.Scan(sourceBitboard);
            }

            // Kiểm tra quân Xe tấn công / Try rooks
            UInt64 rookAttackBitboard = UInt64.MaxValue;
            if ((position.Bitboard[colour | Piece.Rook] & Bit.Axes[square]) != 0)
            {
                rookAttackBitboard = Attack.Rook(square, position.OccupiedBitboard);
                sourceBitboard = position.Bitboard[colour | Piece.Rook] & rookAttackBitboard;
                if (sourceBitboard != 0)
                    return Bit.Scan(sourceBitboard);
            }

            // Kiểm tra quân Hậu tấn công / Try queens
            if ((position.Bitboard[colour | Piece.Queen] & (Bit.Diagonals[square] | Bit.Axes[square])) != 0)
            {
                if (bishopAttackBitboard == UInt64.MaxValue)
                    bishopAttackBitboard = Attack.Bishop(square, position.OccupiedBitboard);
                if (rookAttackBitboard == UInt64.MaxValue)
                    rookAttackBitboard = Attack.Rook(square, position.OccupiedBitboard);

                sourceBitboard = position.Bitboard[colour | Piece.Queen] & (bishopAttackBitboard | rookAttackBitboard);
                if (sourceBitboard != 0)
                    return Bit.Scan(sourceBitboard);
            }

            // Kiểm tra quân Vua tấn công / Try king
            sourceBitboard = position.Bitboard[colour | Piece.King] & Attack.King(square);
            if (sourceBitboard != 0)
                return Bit.Read(sourceBitboard);

            return Position.InvalidSquare;
        }
    }
}