namespace Client
{
    partial class Client
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnPingTheServer = new Button();
            txtConsole = new TextBox();
            btnClearText = new Button();
            btnRegisterTerminal = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // btnPingTheServer
            // 
            btnPingTheServer.Location = new Point(3, 3);
            btnPingTheServer.Name = "btnPingTheServer";
            btnPingTheServer.Size = new Size(122, 23);
            btnPingTheServer.TabIndex = 0;
            btnPingTheServer.Text = "Ping The Server";
            btnPingTheServer.UseVisualStyleBackColor = true;
            btnPingTheServer.Click += RequestPingButton_Click;
            // 
            // txtConsole
            // 
            txtConsole.BackColor = Color.Black;
            txtConsole.BorderStyle = BorderStyle.FixedSingle;
            txtConsole.Dock = DockStyle.Fill;
            txtConsole.ForeColor = Color.Lime;
            txtConsole.Location = new Point(131, 3);
            txtConsole.Multiline = true;
            txtConsole.Name = "txtConsole";
            tableLayoutPanel1.SetRowSpan(txtConsole, 6);
            txtConsole.ScrollBars = ScrollBars.Vertical;
            txtConsole.Size = new Size(1001, 703);
            txtConsole.TabIndex = 1;
            txtConsole.WordWrap = false;
            // 
            // btnClearText
            // 
            btnClearText.Location = new Point(3, 61);
            btnClearText.Name = "btnClearText";
            btnClearText.Size = new Size(122, 23);
            btnClearText.TabIndex = 2;
            btnClearText.Text = "Clear Text";
            btnClearText.UseVisualStyleBackColor = true;
            btnClearText.Click += btnClearText_Click;
            // 
            // btnRegisterTerminal
            // 
            btnRegisterTerminal.Location = new Point(3, 32);
            btnRegisterTerminal.Name = "btnRegisterTerminal";
            btnRegisterTerminal.Size = new Size(122, 23);
            btnRegisterTerminal.TabIndex = 3;
            btnRegisterTerminal.Text = "Register Terminal";
            btnRegisterTerminal.UseVisualStyleBackColor = true;
            btnRegisterTerminal.Click += RegisterTerminalButton_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(btnRegisterTerminal, 0, 1);
            tableLayoutPanel1.Controls.Add(txtConsole, 1, 0);
            tableLayoutPanel1.Controls.Add(btnClearText, 0, 2);
            tableLayoutPanel1.Controls.Add(btnPingTheServer, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 6;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.Size = new Size(753, 699);
            tableLayoutPanel1.TabIndex = 4;
            // 
            // Client
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(753, 699);
            Controls.Add(tableLayoutPanel1);
            Name = "Client";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Client";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button btnPingTheServer;
        private TextBox txtConsole;
        private Button btnClearText;
        private Button btnRegisterTerminal;
        private TableLayoutPanel tableLayoutPanel1;
    }
}
