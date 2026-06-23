using NegaPremium.Properties;
using System;
using System.Drawing;
<<<<<<< HEAD
using System.IO;
using System.Threading;
=======
>>>>>>> hoang
using System.Windows.Forms;

namespace NegaPremium
{
    /// <summary>
    /// Represents a settings dialog box. 
    /// </summary>
    public partial class Settings : Form
    {
        private ComboBox cboWhite;
        private ComboBox cboBlack;

        /// <summary>
        /// Constructs a Settings window. 
        /// </summary>
        public Settings()
        {
            InitializeComponent();
<<<<<<< HEAD
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
=======

            // Protect Designer: Stop execution if opened in design mode to prevent errors.
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return;

            Icon = Resources.Icon;
>>>>>>> hoang
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Protect Designer: Only construct the UI during actual runtime.
            if (DesignMode) return;

            this.ClientSize = new Size(380, 145);

            whitePanel.Size = new Size(165, 75);
            whitePanel.Location = new Point(15, 10);

            blackPanel.Size = new Size(165, 75);
            blackPanel.Location = new Point(195, 10);

            start.Size = new Size(345, 35);
            start.Location = new Point(15, 95);

            whitePanel.Controls.Clear();
            blackPanel.Controls.Clear();

            // Create a ComboBox to select the mode for White.
            cboWhite = new ComboBox();
            cboWhite.DropDownStyle = ComboBoxStyle.DropDownList;
            cboWhite.Location = new Point(15, 30);
            cboWhite.Width = 135;
            cboWhite.Items.AddRange(PlayerFactory.GetAvailableModes());
            cboWhite.SelectedIndex = 0; // Default is Human.
            whitePanel.Controls.Add(cboWhite);

            // Create a ComboBox to select the mode for Black.
            cboBlack = new ComboBox();
            cboBlack.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBlack.Location = new Point(15, 30);
            cboBlack.Width = 135;
            cboBlack.Items.AddRange(PlayerFactory.GetAvailableModes());
            cboBlack.SelectedIndex = 0;
            blackPanel.Controls.Add(cboBlack);
        }

        /// <summary>
        /// Handles the Start button click. 
        /// </summary>
        private void StartClick(Object sender, EventArgs e)
        {
            string whiteMode = cboWhite.SelectedItem.ToString();
            string blackMode = cboBlack.SelectedItem.ToString();

            IPlayer white = PlayerFactory.CreatePlayer(whiteMode);
            IPlayer black = PlayerFactory.CreatePlayer(blackMode);

<<<<<<< HEAD
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
=======
            if (white is Human && black is Human)
                Terminal.Hide();
            else
                Restrictions.MoveTime = 3000;
>>>>>>> hoang

            new Window() { Game = new Game(white, black) }.Show();
            Visible = false;
            ShowInTaskbar = false;
        }
    }
}