using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace NegaPremium {
    public sealed class VisualPiece {

        /// <summary>
        /// The offset for centering pieces on squares when drawing.
        /// </summary>
        public static readonly Point PieceOffset = new Point(-4, 2);

        /// <summary>
        /// The font for drawing pieces. 
        /// </summary>
        public static readonly Font PieceFont = new Font("Tahoma", 30);

        // Add a dictionary for PNG images.
        private static readonly Dictionary<Int32, Image> PieceImages = new Dictionary<Int32, Image>();

        /// <summary>
        /// The piece to represent. 
        /// </summary>
        public Int32 ActualPiece {
            get;
            private set;
        }

        /// <summary>
        /// The real location of the visual piece. 
        /// </summary>
        private Point _real;

        /// <summary>
        /// The dynamic location of the visual piece for animation. 
        /// </summary>
        private PointF _dynamic;

        /// <summary>
        /// Contructs a visual piece at a given location. 
        /// </summary>
        /// <param name="piece">The piece to represent.</param>
        /// <param name="x">The x coodinate.</param>
        /// <param name="y">The y coodinate.</param>
        public VisualPiece(Int32 piece, Int32 x, Int32 y) {
            this.ActualPiece = piece;
            _real = new Point(x, y);
            _dynamic = new PointF(x, y);
        }

        /// <summary>
        /// Draws the visual piece. 
        /// </summary>
        /// <param name="g">The graphics surface to draw on.</param>
        public void Draw(Graphics g) {
            PointF location = new PointF(_dynamic.X, _dynamic.Y);
            if (VisualPosition.Rotated) {
                location.X = VisualPosition.SquareWidth * 7 - location.X;
                location.Y = VisualPosition.SquareWidth * 7 - location.Y;
            }
            DrawAt(g, ActualPiece, location);
        }

        /// <summary>
        /// Promotes the piece represented to the given piece. 
        /// </summary>
        /// <param name="promotion">The new piece to represent.</param>
        public void Promote(Int32 promotion) {
            ActualPiece = promotion;
        }

        /// <summary>
        /// Moves the piece to the given location.
        /// </summary>
        /// <param name="point">The location to move the piece to.</param>
        public void MoveTo(Point point) {
            Single easing = VisualPosition.Animations ? VisualPosition.AnimationEasing : 1;
            Point current = _real = point;

            while (true) {
                _dynamic.X += (_real.X - _dynamic.X) * easing;
                _dynamic.Y += (_real.Y - _dynamic.Y) * easing;

                if (Math.Abs(_real.X - _dynamic.X) < 1 && Math.Abs(_real.Y - _dynamic.Y) < 1) {
                    _dynamic.X = _real.X;
                    _dynamic.Y = _real.Y;
                    return;
                }

                // Another move has been made with the same piece. 
                if (current.X != _real.X || current.Y != _real.Y)
                    return;

                Thread.Sleep(VisualPosition.AnimationInterval);
            }
        }

        /// <summary>
        /// Returns whether the visual piece is at the given location.
        /// </summary>
        /// <param name="point">The location to check.</param>
        /// <returns>Whether the visual piece is at the given location.</returns>
        public Boolean IsAt(Point point) {
            return IsAt(point.X, point.Y);
        }

        /// <summary>
        /// Returns whether the visual piece is at the given location.
        /// </summary>
        /// <param name="x">The x coordinate of the location.</param>
        /// <param name="y">The y coordinate of the location.</param>
        /// <returns>Whether the visual piece is at the given location.</returns>
        public Boolean IsAt(Int32 x, Int32 y) {
            return _real.X == x && _real.Y == y;
        }

        /// <summary>
        /// Draws the piece at the given location.
        /// </summary>
        public static void DrawAt(Graphics g, Int32 piece, PointF location) {
            if (piece != Piece.Empty) {
                // Sử dụng đúng các thuộc tính màu sắc từ VisualPosition.
                location.X += VisualPosition.SquareWidth / 2 - VisualPosition.SquareWidth / 2;
                location.Y += VisualPosition.SquareWidth / 2 - VisualPosition.SquareWidth / 2;

                if (PieceImages.TryGetValue(piece, out Image img)) {
                    Rectangle destRect = new Rectangle((int)location.X, (int)location.Y, VisualPosition.SquareWidth, VisualPosition.SquareWidth);
                    g.DrawImage(img, destRect);
                }
            }
        }

        // New static constructor with relative paths for images.
        static VisualPiece() {
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            var pieceNameMap = new Dictionary<Int32, string> {
                { Colour.White | Piece.King, "white-king" },
                { Colour.White | Piece.Queen, "white-queen" },
                { Colour.White | Piece.Rook, "white-rook" },
                { Colour.White | Piece.Bishop, "white-bishop" },
                { Colour.White | Piece.Knight, "white-knight" },
                { Colour.White | Piece.Pawn, "white-pawn" },
                { Colour.Black | Piece.King, "black-king" },
                { Colour.Black | Piece.Queen, "black-queen" },
                { Colour.Black | Piece.Rook, "black-rook" },
                { Colour.Black | Piece.Bishop, "black-bishop" },
                { Colour.Black | Piece.Knight, "black-knight" },
                { Colour.Black | Piece.Pawn, "black-pawn" },
            };

            foreach (var kv in pieceNameMap) {
                string fileName = kv.Value + ".png";
                string imgPath = Path.Combine(root, fileName);
                if (!File.Exists(imgPath))
                    throw new FileNotFoundException($"Piece image not found: {imgPath}");
                Image img = Image.FromFile(imgPath);
                PieceImages.Add(kv.Key, img);
            }
        }
    }
}
