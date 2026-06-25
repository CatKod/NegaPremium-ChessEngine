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
            // Thiết lập tiêu đề và kích thước cố định cho cửa sổ
            this.Text = "Nega Premium";
            this.ClientSize = new Size(380, 145);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            Controls.Clear();

            // Thay thế Panel bằng GroupBox cho phe Trắng
            GroupBox whiteGroup = new GroupBox
            {
                Text = "White",
                Size = new Size(165, 75),
                Location = new Point(15, 10)
            };

            cboWhite = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 30),
                Width = 135
            };
            cboWhite.Items.AddRange(PlayerFactory.GetAvailableModes());
            cboWhite.SelectedIndex = 0;
            whiteGroup.Controls.Add(cboWhite);

            // Thay thế Panel bằng GroupBox cho phe Đen
            GroupBox blackGroup = new GroupBox
            {
                Text = "Black",
                Size = new Size(165, 75),
                Location = new Point(200, 10)
            };

            cboBlack = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 30),
                Width = 135
            };
            cboBlack.Items.AddRange(PlayerFactory.GetAvailableModes());
            cboBlack.SelectedIndex = 0;
            blackGroup.Controls.Add(cboBlack);

            // Nút Start
            Button start = new Button
            {
                Text = "Start",
                Location = new Point(15, 95),
                Size = new Size(350, 35)
            };
            start.Click += StartClick;

            // Thêm các Control vào form
            Controls.Add(whiteGroup);
            Controls.Add(blackGroup);
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