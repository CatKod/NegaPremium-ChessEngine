using NegaPremium.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NegaPremium {

    /// <summary>
    /// Represents a settings dialog box. 
    /// </summary>
    partial class Settings : Form {

        /// <summary>
        /// Constructs a Settings window. 
        /// </summary>
        public Settings() {
            InitializeComponent();
            Icon = LoadIconSafely();
        }

        private static Icon LoadIconSafely() {
            try {
                using (Stream stream = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon.ico"))) {
                    return new Icon(stream);
                }
            } catch {
                return SystemIcons.Application;
            }
        }

        /// <summary>
        /// Handles the Start button click. 
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The raised event.</param>
        private void StartClick(Object sender, EventArgs e) {

            // Open the GUI interface only if both players have been chosen. 
            if ((whiteHuman.Checked || whiteComputer.Checked) && (blackHuman.Checked || blackComputer.Checked)) {
                IPlayer white = whiteHuman.Checked ? new Human() : new Engine() as IPlayer;
                IPlayer black = blackHuman.Checked ? new Human() : new Engine() as IPlayer;

                SearchMode whiteMode = whiteHillMode.Checked
                    ? SearchMode.HillClimbing
                    : whiteHillClimbingv2Mode.Checked
                        ? SearchMode.HillClimbingv2
                        : SearchMode.Classic;
                SearchMode blackMode = blackHillMode.Checked
                    ? SearchMode.HillClimbing
                    : blackHillClimbingv2Mode.Checked
                        ? SearchMode.HillClimbingv2
                        : SearchMode.Classic;
                if (white is Engine whiteEngine)
                    whiteEngine.Mode = whiteMode;
                if (black is Engine blackEngine)
                    blackEngine.Mode = blackMode;

                // If both players are human there's no need for the Engine Output window. 
                if (white is Human && black is Human)
                    Terminal.Hide();
                else
                    Restrictions.MoveTime = 3000;

                // Display the GUI window and hide this window. 
                new Window() { Game = new Game(white, black) }.Show();
                Visible = false;
                ShowInTaskbar = false;
            } else
                MessageBox.Show("Please select a player for each side.");
        }        
    }
}
