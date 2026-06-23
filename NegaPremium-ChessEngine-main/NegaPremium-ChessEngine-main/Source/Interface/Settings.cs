using NegaPremium.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
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
            Icon = LoadIconSafely();
            BuildRuntimeLayout();
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

        private void BuildRuntimeLayout()
        {
            this.ClientSize = new Size(380, 145);

            Controls.Clear();

            Panel whitePanel = new Panel
            {
                Size = new Size(165, 75),
                Location = new Point(15, 10)
            };

            Panel blackPanel = new Panel
            {
                Size = new Size(165, 75),
                Location = new Point(195, 10)
            };

            Button start = new Button
            {
                Size = new Size(345, 35),
                Location = new Point(15, 95),
                Text = "Start"
            };
            start.Click += StartClick;

            cboWhite = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 30),
                Width = 135
            };
            cboWhite.Items.AddRange(PlayerFactory.GetAvailableModes());
            cboWhite.SelectedIndex = 0;
            whitePanel.Controls.Add(cboWhite);

            cboBlack = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 30),
                Width = 135
            };
            cboBlack.Items.AddRange(PlayerFactory.GetAvailableModes());
            cboBlack.SelectedIndex = 0;
            blackPanel.Controls.Add(cboBlack);

            Controls.Add(whitePanel);
            Controls.Add(blackPanel);
            Controls.Add(start);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Protect Designer: Only construct the UI during actual runtime.
            if (DesignMode) return;

            BuildRuntimeLayout();
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