using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NegaPremium {
    public partial class Window : Form {

        /// <summary>
        /// The height of the menu bar. 
        /// </summary>
        public const Int32 MenuHeight = 24;

        /// <summary>
        /// The game associated with the window. 
        /// </summary>
        public Game Game {
            get { return _game; }
            set {
                _game = value;
                UpdateMenu();
            }
        }
        private Game _game;

        /// <summary>
        /// The target number of milliseconds between draw frames. 
        /// </summary>
        private const Int32 DrawInterval = 8;

        /// <summary>
        /// Constructs a Window for the specified Game.
        /// </summary>
        /// <param name="game">The game to associate with the window.</param>
        public Window() {
            InitializeComponent();

            Icon = LoadIconSafely();

            // bật double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint 
                   | ControlStyles.UserPaint 
                   | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;

            // Initialize properties and fields. 
            ClientSize = new Size(VisualPosition.Width, VisualPosition.Width + MenuHeight);

            // Initialize event handlers. 
            MouseUp += MouseUpHandler;
            Paint += DrawHandler;

            // Close the application when the window is closed. 
            FormClosed += (sender, e) => {
                Application.Exit();
            };

            // Set the background colour to the light colour of the chessboard so we 
            // don't need to draw the light squares. 
            BackColor = VisualPosition.LightSquareColor;

            // Start draw thread. 
            new Thread(new ThreadStart(() => {
                while (true) {
                    Invalidate();
                    Thread.Sleep(DrawInterval);
                }
            })) {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// Handles a mouse up event. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The mouse event.</param>
        public void MouseUpHandler(Object sender, MouseEventArgs e) {
            Game?.MouseUpHandler(e);
        }

        /// <summary>
        /// Draws the Window. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The paint event.</param>
        private void DrawHandler(Object sender, PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            g.CompositingQuality = CompositingQuality.HighSpeed;

            // Translate down so the chessboard can be draw from (0, 0). 
            g.TranslateTransform(0, MenuHeight);

            if (Game != null)
                Game.Draw(g);
            else {
                VisualPosition.DrawDarkSquares(g);
                VisualPosition.DrawPieces(g);
            }
        }

        /// <summary>
        /// Updates which menu components are enabled or checked. 
        /// </summary>
        private static Icon LoadIconSafely() {
            try {
                using (Stream stream = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico"))) {
                    return new Icon(stream);
                }
            } catch {
                return SystemIcons.Application;
            }
        }

        private void UpdateMenu() {
            Boolean hasGame = Game != null;

            // Update File menu.
            savePGNMenuItem.Enabled = hasGame;
            enterFENMenuItem.Enabled = hasGame;
            copyFENMenuItem.Enabled = hasGame;

            // Update Game menu.
            offerDrawMenuItem.Enabled = hasGame;
            restartMenuItem.Enabled = hasGame;
            undoMoveMenuItem.Enabled = hasGame;

            // Update Display menu.
            rotateBoardMenuItem.Checked = VisualPosition.Rotated;
            animationsMenuItem.Checked = VisualPosition.Animations;

            if (hasGame) {
                Boolean hasHuman = Game.White is Human || Game.Black is Human;
                Boolean hasEngine = Game.White is Engine || Game.Black is Engine;

                // Update File menu.
                saveOuputMenuItem.Enabled = hasEngine;

                // Update Game menu.
                offerDrawMenuItem.Enabled = hasHuman && hasEngine;
                undoMoveMenuItem.Enabled = hasHuman;

                // Update Engine menu.
                searchTimeMenuItem.Enabled = hasEngine;
                searchDepthMenuItem.Enabled = hasEngine;
                searchNodesMenuItem.Enabled = hasEngine;
                hashSizeMenuItem.Enabled = hasEngine;
                multiPVMenuItem.Enabled = hasEngine;
                searchModeMenuItem.Enabled = hasEngine;
                classicModeMenuItem.Checked = hasEngine && GetEngine().Mode == Engine.SearchMode.Classic;
                GreedyModeMenuItem.Checked = hasEngine && GetEngine().Mode == Engine.SearchMode.Greedy;
            }
        }

        /// <summary>
        /// Handles the Save PGN button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SavePGNClick(Object sender, EventArgs e) {
            using (SaveFileDialog dialog = new SaveFileDialog()) {
                dialog.Title = "Save PGN";
                dialog.Filter = "PGN File|*.pgn|Text File|*.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    Game.SavePGN(dialog.FileName);
            }
        }

        /// <summary>
        /// Handles the Save Output button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SaveOutputClick(Object sender, EventArgs e) {
            using (SaveFileDialog dialog = new SaveFileDialog()) {
                dialog.Title = "Save Engine Output";
                dialog.Filter = "Text File|*.txt";
                if (dialog.ShowDialog() == DialogResult.OK)
                    Terminal.SaveText(dialog.FileName);
            }
        }

        /// <summary>
        /// Handles the Enter FEN button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void EnterFENClick(Object sender, EventArgs e) {
            if (Game != null) {
                String fen = InputBox.Show("Please enter the FEN string.");
                if (fen.Length > 0) {
                    Game.End();
                    Game.Reset();
                    Game.Start(fen);
                }
            }
        }

        /// <summary>
        /// Handles the Copy FEN button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void CopyFENClick(Object sender, EventArgs e) {
            Clipboard.SetText(Game.GetFEN());
        }

        /// <summary>
        /// Handles the Offer Draw button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void OfferDrawClick(Object sender, EventArgs e) {
            Game?.OfferDraw();
        }

        /// <summary>
        /// Handles the Restart button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void RestartClick(Object sender, EventArgs e) {
            Game?.End();
            Game?.Reset();
            Game?.Start();
        }

        /// <summary>
        /// Handles the Undo Move button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void UndoMoveClick(Object sender, EventArgs e) {
            Game?.UndoMove();
        }

        /// <summary>
        /// Handles the Search Time button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SearchTimeClick(Object sender, EventArgs e) {
            while (true) {
                String input = InputBox.Show("Please specify the search time in milliseconds.", Restrictions.MoveTime.ToString());
                if (Int32.TryParse(input, out Int32 value) && value > 0) {
                    Restrictions.MoveTime = value;
                    break;
                } else
                    MessageBox.Show("Input must be a positive integer.");
            }
        }

        /// <summary>
        /// Handles the Search Depth button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SearchDepthClick(Object sender, EventArgs e) {
            while (true) {
                String input = InputBox.Show("Please specify the search depth.", Restrictions.Depth.ToString());
                if (Int32.TryParse(input, out Int32 value) && value > 0) {
                    Restrictions.Depth = value;
                    break;
                } else
                    MessageBox.Show("Input must be a positive integer.");
            }
        }

        /// <summary>
        /// Handles the Search Nodes button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void SearchNodesClick(Object sender, EventArgs e) {
            while (true) {
                String input = InputBox.Show("Please specify the nodes limit.", Restrictions.Nodes.ToString());
                if (Int64.TryParse(input, out Int64 value) && value > 0) {
                    Restrictions.Nodes = value;
                    break;
                } else
                    MessageBox.Show("Input must be a positive integer.");
            }
        }

        /// <summary>
        /// Handles the Hash Size button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void HashSizeClick(Object sender, EventArgs e) {
            while (true) {
                Engine engine = Game.White as Engine ?? Game.Black as Engine;
                String input = InputBox.Show("Please specify the hash size in megabytes.", engine.HashAllocation.ToString());
                if (Int32.TryParse(input, out Int32 value) && value > 0) {
                    if (Game.White is Engine)
                        (Game.White as Engine).HashAllocation = value;
                    if (Game.Black is Engine)
                        (Game.Black as Engine).HashAllocation = value;
                    return;
                } else
                    MessageBox.Show("Input must be a positive integer.");
            }
        }

        /// <summary>
        /// Handles the Multi PV button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void MultiPVClick(Object sender, EventArgs e) {
            while (true) {
                String input = InputBox.Show("Please specify the number of principal variations.", Restrictions.PrincipalVariations.ToString());
                if (Int32.TryParse(input, out Int32 value) && value > 0) {
                    Restrictions.PrincipalVariations = value;
                    break;
                } else
                    MessageBox.Show("Input must be a positive integer.");
            }
        }

        /// <summary>
        /// Handles the Rotate Board button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void RotateBoardClick(Object sender, EventArgs e) {
            VisualPosition.Rotated ^= true;
            UpdateMenu();
        }

        /// <summary>
        /// Handles the Animations button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void AnimationsClick(Object sender, EventArgs e) {
            VisualPosition.Animations ^= true;
            UpdateMenu();
        }

        /// <summary>
        /// Handles the About button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void AboutClick(Object sender, EventArgs e) {
            MessageBox.Show("Nega Premium is a chess engine written in C#, developed to learn about game tree searching. Its playing strength has been and will continue to steadily increase as more techniques are added to its arsenal. \n\nNega Team");
        }

        private Engine GetEngine() {
            return Game?.White as Engine ?? Game?.Black as Engine;
        }

        private void ClassicModeClick(Object sender, EventArgs e) {
            Engine engine = GetEngine();
            if (engine != null) {
                engine.Mode = Engine.SearchMode.Classic;
                UpdateMenu();
            }
        }

        private void GreedyModeClick(Object sender, EventArgs e) {
            Engine engine = GetEngine();
            if (engine != null) {
                engine.Mode = Engine.SearchMode.Greedy;
                UpdateMenu();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            // Vẽ bàn cờ.
            for (int rank = 0; rank < 8; rank++) {
                for (int file = 0; file < 8; file++) {
                    Color squareColor = (file + rank) % 2 == 0
                        ? VisualPosition.LightSquareColor // Sử dụng LightSquareColor.
                        : VisualPosition.DarkSquareColor; // Sử dụng DarkSquareColor.

                    using (Brush brush = new SolidBrush(squareColor)) {
                        g.FillRectangle(brush, file * VisualPosition.SquareWidth, rank * VisualPosition.SquareWidth, VisualPosition.SquareWidth, VisualPosition.SquareWidth);
                    }
                }
            }

            // Vẽ các quân cờ.
            VisualPosition.DrawPieces(g);
        }

        private void Window_Load(object sender, EventArgs e)
        {

        }
    }
}
