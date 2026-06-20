using NegaPremium.Properties;
using System;
using System.Drawing;
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

            // Protect Designer: Stop execution if opened in design mode to prevent errors.
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime)
                return;

            Icon = Resources.Icon;
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
            cboBlack.SelectedIndex = 1; // Default is Nega Premium.
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

            if (white is Human && black is Human)
                Terminal.Hide();
            else
                Restrictions.MoveTime = 3000;

            new Window() { Game = new Game(white, black) }.Show();
            Visible = false;
            ShowInTaskbar = false;
        }
    }
}