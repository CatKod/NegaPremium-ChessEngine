namespace NegaPremium {
    partial class Settings {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.start = new System.Windows.Forms.Button();
            this.whitePanel = new System.Windows.Forms.GroupBox();
            this.whiteComputer = new System.Windows.Forms.RadioButton();
            this.whiteHuman = new System.Windows.Forms.RadioButton();
            this.blackPanel = new System.Windows.Forms.GroupBox();
            this.blackComputer = new System.Windows.Forms.RadioButton();
            this.blackHuman = new System.Windows.Forms.RadioButton();
            this.whiteSearchPanel = new System.Windows.Forms.GroupBox();
            this.whiteClassicMode = new System.Windows.Forms.RadioButton();
            this.whiteHillMode = new System.Windows.Forms.RadioButton();
            this.whiteHillClimbingv2Mode = new System.Windows.Forms.RadioButton();
            this.blackSearchPanel = new System.Windows.Forms.GroupBox();
            this.blackClassicMode = new System.Windows.Forms.RadioButton();
            this.blackHillMode = new System.Windows.Forms.RadioButton();
            this.blackHillClimbingv2Mode = new System.Windows.Forms.RadioButton();
            this.whitePanel.SuspendLayout();
            this.blackPanel.SuspendLayout();
            this.whiteSearchPanel.SuspendLayout();
            this.blackSearchPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // start
            // 
            this.start.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.start.Location = new System.Drawing.Point(12, 98);
            this.start.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(452, 33);
            this.start.TabIndex = 0;
            this.start.Text = "Start";
            this.start.UseVisualStyleBackColor = true;
            this.start.Click += new System.EventHandler(this.StartClick);
            // 
            // whitePanel
            // 
            this.whitePanel.Controls.Add(this.whiteComputer);
            this.whitePanel.Controls.Add(this.whiteHuman);
            this.whitePanel.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.whitePanel.Location = new System.Drawing.Point(12, 10);
            this.whitePanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.whitePanel.Name = "whitePanel";
            this.whitePanel.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.whitePanel.Size = new System.Drawing.Size(212, 80);
            this.whitePanel.TabIndex = 2;
            this.whitePanel.TabStop = false;
            this.whitePanel.Text = "White";
            // 
            // whiteComputer
            // 
            this.whiteComputer.AutoSize = true;
            this.whiteComputer.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.whiteComputer.Location = new System.Drawing.Point(13, 49);
            this.whiteComputer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.whiteComputer.Name = "whiteComputer";
            this.whiteComputer.Size = new System.Drawing.Size(102, 24);
            this.whiteComputer.TabIndex = 1;
            this.whiteComputer.TabStop = true;
            this.whiteComputer.Text = "Computer";
            this.whiteComputer.UseVisualStyleBackColor = true;
            // 
            // whiteHuman
            // 
            this.whiteHuman.AutoSize = true;
            this.whiteHuman.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.whiteHuman.Location = new System.Drawing.Point(13, 21);
            this.whiteHuman.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.whiteHuman.Name = "whiteHuman";
            this.whiteHuman.Size = new System.Drawing.Size(75, 24);
            this.whiteHuman.TabIndex = 0;
            this.whiteHuman.TabStop = true;
            this.whiteHuman.Text = "Human";
            this.whiteHuman.UseVisualStyleBackColor = true;
            // 
            // blackPanel
            // 
            this.blackPanel.Controls.Add(this.blackComputer);
            this.blackPanel.Controls.Add(this.blackHuman);
            this.blackPanel.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.blackPanel.Location = new System.Drawing.Point(248, 10);
            this.blackPanel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.blackPanel.Name = "blackPanel";
            this.blackPanel.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.blackPanel.Size = new System.Drawing.Size(212, 80);
            this.blackPanel.TabIndex = 3;
            this.blackPanel.TabStop = false;
            this.blackPanel.Text = "Black";
            // 
            // blackComputer
            // 
            this.blackComputer.AutoSize = true;
            this.blackComputer.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.blackComputer.Location = new System.Drawing.Point(13, 49);
            this.blackComputer.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.blackComputer.Name = "blackComputer";
            this.blackComputer.Size = new System.Drawing.Size(102, 24);
            this.blackComputer.TabIndex = 1;
            this.blackComputer.TabStop = true;
            this.blackComputer.Text = "Computer";
            this.blackComputer.UseVisualStyleBackColor = true;
            // 
            // blackHuman
            // 
            this.blackHuman.AutoSize = true;
            this.blackHuman.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.blackHuman.Location = new System.Drawing.Point(13, 21);
            this.blackHuman.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.blackHuman.Name = "blackHuman";
            this.blackHuman.Size = new System.Drawing.Size(75, 24);
            this.blackHuman.TabIndex = 0;
            this.blackHuman.TabStop = true;
            this.blackHuman.Text = "Human";
            this.blackHuman.UseVisualStyleBackColor = true;
            // 
            // searchPanel
            // 
            this.whiteSearchPanel.Controls.Add(this.whiteClassicMode);
            this.whiteSearchPanel.Controls.Add(this.whiteHillMode);
            this.whiteSearchPanel.Controls.Add(this.whiteHillClimbingv2Mode);
            this.blackSearchPanel.Controls.Add(this.blackClassicMode);
            this.blackSearchPanel.Controls.Add(this.blackHillMode);
            this.blackSearchPanel.Controls.Add(this.blackHillClimbingv2Mode);
            this.whiteSearchPanel.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.whiteSearchPanel.Location = new System.Drawing.Point(12, 140);
            this.whiteSearchPanel.Margin = new System.Windows.Forms.Padding(4);
            this.whiteSearchPanel.Name = "whiteSearchPanel";
            this.whiteSearchPanel.Padding = new System.Windows.Forms.Padding(4);
            this.whiteSearchPanel.Size = new System.Drawing.Size(212, 112);
            this.whiteSearchPanel.TabIndex = 4;
            this.whiteSearchPanel.TabStop = false;
            this.whiteSearchPanel.Text = "White Search Mode";
            // 
            // whiteClassicMode
            // 
            this.whiteClassicMode.AutoSize = true;
            this.whiteClassicMode.Checked = true;
            this.whiteClassicMode.Location = new System.Drawing.Point(13, 21);
            this.whiteClassicMode.Name = "whiteClassicMode";
            this.whiteClassicMode.Size = new System.Drawing.Size(161, 24);
            this.whiteClassicMode.TabIndex = 0;
            this.whiteClassicMode.TabStop = true;
            this.whiteClassicMode.Text = "Nega Premium";
            this.whiteClassicMode.UseVisualStyleBackColor = true;
            // 
            // whiteHillMode
            // 
            this.whiteHillMode.AutoSize = true;
            this.whiteHillMode.Location = new System.Drawing.Point(13, 48);
            this.whiteHillMode.Name = "whiteHillMode";
            this.whiteHillMode.Size = new System.Drawing.Size(165, 24);
            this.whiteHillMode.TabIndex = 1;
            this.whiteHillMode.Text = "Hill Climbing";
            this.whiteHillMode.UseVisualStyleBackColor = true;
            // 
            // whiteHillClimbingv2Mode
            // 
            this.whiteHillClimbingv2Mode.AutoSize = true;
            this.whiteHillClimbingv2Mode.Location = new System.Drawing.Point(13, 75);
            this.whiteHillClimbingv2Mode.Name = "whiteHillClimbingv2Mode";
            this.whiteHillClimbingv2Mode.Size = new System.Drawing.Size(165, 24);
            this.whiteHillClimbingv2Mode.TabIndex = 2;
            this.whiteHillClimbingv2Mode.Text = "HillClimbingv2";
            this.whiteHillClimbingv2Mode.UseVisualStyleBackColor = true;
            // 
            // blackSearchPanel
            // 
            this.blackSearchPanel.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.blackSearchPanel.Location = new System.Drawing.Point(248, 140);
            this.blackSearchPanel.Margin = new System.Windows.Forms.Padding(4);
            this.blackSearchPanel.Name = "blackSearchPanel";
            this.blackSearchPanel.Padding = new System.Windows.Forms.Padding(4);
            this.blackSearchPanel.Size = new System.Drawing.Size(212, 112);
            this.blackSearchPanel.TabIndex = 5;
            this.blackSearchPanel.TabStop = false;
            this.blackSearchPanel.Text = "Black Search Mode";
            // 
            // blackClassicMode
            // 
            this.blackClassicMode.AutoSize = true;
            this.blackClassicMode.Checked = true;
            this.blackClassicMode.Location = new System.Drawing.Point(13, 21);
            this.blackClassicMode.Name = "blackClassicMode";
            this.blackClassicMode.Size = new System.Drawing.Size(161, 24);
            this.blackClassicMode.TabIndex = 0;
            this.blackClassicMode.TabStop = true;
            this.blackClassicMode.Text = "Nega Premium";
            this.blackClassicMode.UseVisualStyleBackColor = true;
            // 
            // blackHillMode
            // 
            this.blackHillMode.AutoSize = true;
            this.blackHillMode.Location = new System.Drawing.Point(13, 48);
            this.blackHillMode.Name = "blackHillMode";
            this.blackHillMode.Size = new System.Drawing.Size(165, 24);
            this.blackHillMode.TabIndex = 1;
            this.blackHillMode.Text = "Hill Climbing";
            this.blackHillMode.UseVisualStyleBackColor = true;
            // 
            // blackHillClimbingv2Mode
            // 
            this.blackHillClimbingv2Mode.AutoSize = true;
            this.blackHillClimbingv2Mode.Location = new System.Drawing.Point(13, 75);
            this.blackHillClimbingv2Mode.Name = "blackHillClimbingv2Mode";
            this.blackHillClimbingv2Mode.Size = new System.Drawing.Size(165, 24);
            this.blackHillClimbingv2Mode.TabIndex = 2;
            this.blackHillClimbingv2Mode.Text = "HillClimbingv2";
            this.blackHillClimbingv2Mode.UseVisualStyleBackColor = true;
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 266);
            this.Controls.Add(this.blackSearchPanel);
            this.Controls.Add(this.whiteSearchPanel);
            this.Controls.Add(this.blackPanel);
            this.Controls.Add(this.whitePanel);
            this.Controls.Add(this.start);
            this.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.whitePanel.ResumeLayout(false);
            this.whitePanel.PerformLayout();
            this.blackPanel.ResumeLayout(false);
            this.blackPanel.PerformLayout();
            this.whiteSearchPanel.ResumeLayout(false);
            this.whiteSearchPanel.PerformLayout();
            this.blackSearchPanel.ResumeLayout(false);
            this.blackSearchPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button start;
        private System.Windows.Forms.GroupBox whitePanel;
        private System.Windows.Forms.RadioButton whiteComputer;
        private System.Windows.Forms.RadioButton whiteHuman;
        private System.Windows.Forms.GroupBox blackPanel;
        private System.Windows.Forms.RadioButton blackComputer;
        private System.Windows.Forms.RadioButton blackHuman;
        private System.Windows.Forms.GroupBox whiteSearchPanel;
        private System.Windows.Forms.RadioButton whiteClassicMode;
        private System.Windows.Forms.RadioButton whiteHillMode;
        private System.Windows.Forms.RadioButton whiteHillClimbingv2Mode;
        private System.Windows.Forms.GroupBox blackSearchPanel;
        private System.Windows.Forms.RadioButton blackClassicMode;
        private System.Windows.Forms.RadioButton blackHillMode;
        private System.Windows.Forms.RadioButton blackHillClimbingv2Mode;
    }
}