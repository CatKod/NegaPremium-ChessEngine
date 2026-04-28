using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace NegaPremium {

    /// <summary>
    /// Represents a game between two players. 
    /// </summary>
    public sealed class Game {

        // Graphics constants. 
        private static readonly SolidBrush OverlayBrush = new SolidBrush(Color.FromArgb(190, Color.White));
        private static readonly SolidBrush MessageBrush = new SolidBrush(Color.Black);
        private static readonly Font MessageFont = new Font("Arial", 20);

        // Specifies the game state. 
        private enum GameState { NotStarted, Ingame, Stopped, WhiteWon, BlackWon, Draw };

        // Game fields. 
        private Position _initialPosition;
        private List<Int32> _moves = new List<Int32>();
        private List<Type> _types = new List<Type>();
        private GameState _state = GameState.NotStarted;
        private ManualResetEvent _waitForStop = new ManualResetEvent(false);
        private String _message;
        private String _date;

        public IPlayer White;
        public IPlayer Black;

        /// <summary>
        /// Constructs a Game with the given players and initial position. If the 
        /// initial position is not specified the default starting chess position is 
        /// used. 
        /// </summary>
        /// <param name="white">The player to play as white.</param>
        /// <param name="black">The player to play as black.</param>
        /// <param name="fen">An optional FEN for the starting position.</param>
        public Game(IPlayer white, IPlayer black, String fen = Position.StartingFEN) {
            White = white;
            Black = black;
            Start(fen);
        }

        /// <summary>
        /// Starts a game between the two players starting from the position with the 
        /// given FEN. This method is non-blocking. 
        /// </summary>
        /// <param name="fen">The FEN of the position to start the game from.</param>
        public void Start(String fen = Position.StartingFEN) {
            Start(Position.Create(fen));
        }

        /// <summary>
        /// Starts a game between the two players starting from the given position. 
        /// This method is non-blocking and does not modify the given position. 
        /// </summary>
        /// <param name="position">The position to start the game from.</param>
        public void Start(Position position) {
            _date = DateTime.Now.ToString("yyyy.MM.dd");
            _initialPosition = position;
            Play(position);
        }

        /// <summary>
        /// Starts play between the two players on the current position for the game. 
        /// This method is non-blocking and does not modify the given position. 
        /// </summary>
        /// <param name="p">The position to start playing from.</param>
        private void Play(Position p) {
            Position position = p.DeepClone();
            VisualPosition.Set(position);
            _state = GameState.Ingame;
            _waitForStop.Reset();

            new Thread(new ThreadStart(() => {
                while (true) {
                    IPlayer player = (position.SideToMove == Colour.White) ? White : Black;
                    List<Int32> legalMoves = position.LegalMoves();

                    // Adjudicate checkmate and stalemate. 
                    if (legalMoves.Count == 0) {
                        if (position.InCheck(position.SideToMove)) {
                            _message = "Checkmate. " + Stringify.Colour(1 - position.SideToMove) + " wins!";
                            _state = player.Equals(White) ? GameState.BlackWon : GameState.WhiteWon;
                        } else {
                            _message = "Stalemate. It's a draw!";
                            _state = GameState.Draw;
                        }
                    }

                    // Adjudicate draw.  
                    if (position.InsufficientMaterial()) {
                        _message = "Draw by insufficient material!";
                        _state = GameState.Draw;
                    }
                    if (player is Engine && player.AcceptsDraw) {
                        if (position.FiftyMovesClock >= 100) {
                            _message = "Draw by fifty-move rule!";
                            _state = GameState.Draw;
                        }
                        if (position.HasRepeated(3)) {
                            _message = "Draw by threefold repetition!";
                            _state = GameState.Draw;
                        }
                    }

                    // Consider game end. 
                    if (_state != GameState.Ingame) {
                        _waitForStop.Set();
                        return;
                    }

                    // Get move from player. 
                    Position copy = position.DeepClone();
                    Int32 move = player.GetMove(copy);
                    if (!position.Equals(copy))
                        Terminal.WriteLine("Board modified!");

                    // Consider game stop. 
                    if (_state != GameState.Ingame) {
                        _waitForStop.Set();
                        return;
                    }

                    // Make the move. 
                    position.Make(move);
                    VisualPosition.Make(move);
                    _moves.Add(move);
                    _types.Add(player.GetType());
                }
            })) {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Stops play between the two players. 
        /// </summary>
        public void End() {
            _state = GameState.Stopped;
            White.Stop();
            Black.Stop();
            _waitForStop.WaitOne();
        }

        /// <summary>
        /// Resets play between the two players so that the game is restored to the 
        /// state at which no moves have been played. 
        /// </summary>
        public void Reset() {
            _state = GameState.NotStarted;
            _moves.Clear();
            _types.Clear();
            White.Reset();
            Black.Reset();
        }

        /// <summary>
        /// Offers a draw to the engine if applicable. 
        /// </summary>
        public void OfferDraw() {
            IPlayer offeree = White is Engine ? White : Black;
            if (offeree.AcceptsDraw) {
                End();
                _message = "Draw by agreement!";
                _state = GameState.Draw;
            } else
                MessageBox.Show("The draw offer was declined.");
        }

        /// <summary>
        /// Handles a mouse up event. 
        /// </summary>
        /// <param name="e">The mouse event.</param>
        public void MouseUpHandler(MouseEventArgs e) {
            if (White is Human)
                (White as Human).MouseUpHandler(e);
            if (Black is Human)
                (Black as Human).MouseUpHandler(e);
        }

        /// <summary>
        /// Draws the position and animations associated with the game. 
        /// </summary>
        /// <param name="g">The drawing surface.</param>
        public void Draw(Graphics g)
        {
            VisualPosition.DrawDarkSquares(g);
            White.Draw(g);
            Black.Draw(g);
            VisualPosition.DrawPieces(g);

            if (_state != GameState.Ingame && _state != GameState.Stopped)
            {
                g.FillRectangle(OverlayBrush, 0, 0, VisualPosition.Width, VisualPosition.Width);
                g.DrawString(_message, MessageFont, MessageBrush, 20, 20);
            }
        }


        /// <summary>
        /// Undoes the last move made by a human player. 
        /// </summary>
        public void UndoMove() {
            End();
            Int32 length = 0;
            for (Int32 i = _types.Count - 1; i >= 0; i--)
                if (_types[i] == typeof(Human)) {
                    length = i;
                    break;
                }
            _moves.RemoveRange(length, _moves.Count - length);
            _types.RemoveRange(length, _types.Count - length);
            Position position = _initialPosition.DeepClone();
            _moves.ForEach(move => {
                position.Make(move);
            });
            Play(position);
        }

        /// <summary>
        /// Returns the FEN string of the position in the game. 
        /// </summary>
        /// <returns>The FEN of the position in the game.</returns>
        public String GetFEN() {
            Position position = _initialPosition.DeepClone();
            _moves.ForEach(move => {
                position.Make(move);
            });
            return position.GetFEN();
        }

        /// <summary>
        /// Saves the PGN string of the game to a file with the given path.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        public void SavePGN(String path) {
            File.WriteAllText(path, GetPGN());
        }

        /// <summary>
        /// Returns the PGN string of the game.
        /// </summary>
        /// <returns>The PGN string of the game.</returns>
        public String GetPGN() {
            StringBuilder sb = new StringBuilder();
            sb.Append("[Date \"" + _date + "\"]");
            sb.Append(Environment.NewLine);
            sb.Append("[White \"" + White.Name + "\"]");
            sb.Append(Environment.NewLine);
            sb.Append("[Black \"" + Black.Name + "\"]");
            sb.Append(Environment.NewLine);
            String result = "*";
            switch (_state) {
                case GameState.WhiteWon:
                    result = "1-0";
                    break;
                case GameState.BlackWon:
                    result = "0-1";
                    break;
                case GameState.Draw:
                    result = "1/2-1/2";
                    break;
            }
            sb.Append("[Result \"" + result + "\"]");
            sb.Append(Environment.NewLine);

            String initialFEN = _initialPosition.GetFEN();
            if (initialFEN != Position.StartingFEN) {
                sb.Append("[SetUp \"1\"]");
                sb.Append(Environment.NewLine);
                sb.Append("[FEN \"" + initialFEN + "\"]");
                sb.Append(Environment.NewLine);
            }

            sb.Append(Environment.NewLine);
            sb.Append(Stringify.MovesAlgebraically(_initialPosition, _moves, StringifyOptions.Proper));
            if (result != "*")
                sb.Append(" " + result);

            return sb.ToString();
        }
    }
}
