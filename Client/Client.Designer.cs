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
            btnUpdateDatabase = new Button();
            btnUpdatefiles = new Button();
            statusStrip1 = new StatusStrip();
            pbFiles = new ToolStripProgressBar();
            lblHashingFile = new ToolStripStatusLabel();
            tableLayoutPanel1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // btnPingTheServer
            // 
            btnPingTheServer.Location = new Point(3, 32);
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
            tableLayoutPanel1.SetRowSpan(txtConsole, 29);
            txtConsole.ScrollBars = ScrollBars.Vertical;
            txtConsole.Size = new Size(909, 883);
            txtConsole.TabIndex = 1;
            txtConsole.WordWrap = false;
            // 
            // btnClearText
            // 
            btnClearText.Location = new Point(3, 863);
            btnClearText.Name = "btnClearText";
            btnClearText.Size = new Size(122, 23);
            btnClearText.TabIndex = 2;
            btnClearText.Text = "Clear Text";
            btnClearText.UseVisualStyleBackColor = true;
            btnClearText.Click += btnClearText_Click;
            // 
            // btnRegisterTerminal
            // 
            btnRegisterTerminal.Location = new Point(3, 3);
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
            tableLayoutPanel1.Controls.Add(txtConsole, 1, 0);
            tableLayoutPanel1.Controls.Add(btnPingTheServer, 0, 1);
            tableLayoutPanel1.Controls.Add(btnRegisterTerminal, 0, 0);
            tableLayoutPanel1.Controls.Add(btnClearText, 0, 28);
            tableLayoutPanel1.Controls.Add(btnUpdateDatabase, 0, 2);
            tableLayoutPanel1.Controls.Add(btnUpdatefiles, 0, 3);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 24;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            tableLayoutPanel1.Size = new Size(1043, 889);
            tableLayoutPanel1.TabIndex = 4;
            // 
            // btnUpdateDatabase
            // 
            btnUpdateDatabase.Location = new Point(3, 61);
            btnUpdateDatabase.Name = "btnUpdateDatabase";
            btnUpdateDatabase.Size = new Size(122, 23);
            btnUpdateDatabase.TabIndex = 4;
            btnUpdateDatabase.Text = "Update DB";
            btnUpdateDatabase.UseVisualStyleBackColor = true;
            btnUpdateDatabase.Click += btnUpdateDatabase_Click;
            // 
            // btnUpdatefiles
            // 
            btnUpdatefiles.Location = new Point(3, 90);
            btnUpdatefiles.Name = "btnUpdatefiles";
            btnUpdatefiles.Size = new Size(122, 23);
            btnUpdatefiles.TabIndex = 5;
            btnUpdatefiles.Text = "Download Updates";
            btnUpdatefiles.UseVisualStyleBackColor = true;
            btnUpdatefiles.Click += btnUpdatefiles_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { pbFiles, lblHashingFile });
            statusStrip1.Location = new Point(0, 889);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.RenderMode = ToolStripRenderMode.Professional;
            statusStrip1.Size = new Size(1043, 22);
            statusStrip1.SizingGrip = false;
            statusStrip1.TabIndex = 5;
            statusStrip1.Text = "statusStrip1";
            // 
            // pbFiles
            // 
            pbFiles.Name = "pbFiles";
            pbFiles.Size = new Size(100, 16);
            pbFiles.Visible = false;
            // 
            // lblHashingFile
            // 
            lblHashingFile.Name = "lblHashingFile";
            lblHashingFile.Size = new Size(82, 17);
            lblHashingFile.Text = "lblHashingFile";
            lblHashingFile.Visible = false;
            // 
            // Client
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1043, 911);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Client";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Client";
            Load += Client_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnPingTheServer;
        private TextBox txtConsole;
        private Button btnClearText;
        private Button btnRegisterTerminal;
        private TableLayoutPanel tableLayoutPanel1;
        private StatusStrip statusStrip1;
        private Button btnUpdateDatabase;
        private Button btnUpdatefiles;
        private ToolStripProgressBar pbFiles;
        private ToolStripStatusLabel lblHashingFile;
    }
}
