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
            components = new System.ComponentModel.Container();
            btnPingTheServer = new Button();
            txtConsole = new TextBox();
            btnClearText = new Button();
            SuspendLayout();
            // 
            // btnPingTheServer
            // 
            btnPingTheServer.Location = new Point(12, 12);
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
            txtConsole.ForeColor = Color.Lime;
            txtConsole.Location = new Point(140, 12);
            txtConsole.Multiline = true;
            txtConsole.Name = "txtConsole";
            txtConsole.ScrollBars = ScrollBars.Vertical;
            txtConsole.Size = new Size(421, 426);
            txtConsole.TabIndex = 1;
            txtConsole.WordWrap = false;
            // 
            // btnClearText
            // 
            btnClearText.Location = new Point(12, 415);
            btnClearText.Name = "btnClearText";
            btnClearText.Size = new Size(122, 23);
            btnClearText.TabIndex = 2;
            btnClearText.Text = "Clear Text";
            btnClearText.UseVisualStyleBackColor = true;
            btnClearText.Click += btnClearText_Click;
            // 
            // Client
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(570, 450);
            Controls.Add(btnClearText);
            Controls.Add(txtConsole);
            Controls.Add(btnPingTheServer);
            Name = "Client";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Client";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnPingTheServer;
        private TextBox txtConsole;
        private Button btnClearText;
    }
}
