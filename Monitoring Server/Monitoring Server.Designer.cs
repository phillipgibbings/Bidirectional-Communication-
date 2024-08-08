namespace Monitoring_Server
{
    partial class MonitoringServer
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
            menuStrip1 = new MenuStrip();
            menuStartServer = new ToolStripMenuItem();
            clearTextToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            tabcServerConsole = new TabControl();
            tpLogs = new TabPage();
            txtMonitoringServer = new TextBox();
            tpHeartbeats = new TabPage();
            txtHeartbeats = new TextBox();
            statusStrip1 = new StatusStrip();
            lblServerStatus = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            tabcServerConsole.SuspendLayout();
            tpLogs.SuspendLayout();
            tpHeartbeats.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuStartServer, clearTextToolStripMenuItem, settingsToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1179, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // menuStartServer
            // 
            menuStartServer.Name = "menuStartServer";
            menuStartServer.Size = new Size(78, 20);
            menuStartServer.Text = "Start Server";
            menuStartServer.Click += btnStartServer_Click;
            // 
            // clearTextToolStripMenuItem
            // 
            clearTextToolStripMenuItem.Alignment = ToolStripItemAlignment.Right;
            clearTextToolStripMenuItem.Name = "clearTextToolStripMenuItem";
            clearTextToolStripMenuItem.Size = new Size(70, 20);
            clearTextToolStripMenuItem.Text = "Clear Text";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(61, 20);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // tabcServerConsole
            // 
            tabcServerConsole.Controls.Add(tpLogs);
            tabcServerConsole.Controls.Add(tpHeartbeats);
            tabcServerConsole.Dock = DockStyle.Fill;
            tabcServerConsole.Location = new Point(0, 24);
            tabcServerConsole.Name = "tabcServerConsole";
            tabcServerConsole.SelectedIndex = 0;
            tabcServerConsole.Size = new Size(1179, 612);
            tabcServerConsole.TabIndex = 2;
            // 
            // tpLogs
            // 
            tpLogs.Controls.Add(txtMonitoringServer);
            tpLogs.Location = new Point(4, 24);
            tpLogs.Name = "tpLogs";
            tpLogs.Padding = new Padding(3);
            tpLogs.Size = new Size(1171, 584);
            tpLogs.TabIndex = 0;
            tpLogs.Text = "Logs";
            tpLogs.UseVisualStyleBackColor = true;
            // 
            // txtMonitoringServer
            // 
            txtMonitoringServer.BackColor = Color.Black;
            txtMonitoringServer.BorderStyle = BorderStyle.FixedSingle;
            txtMonitoringServer.Dock = DockStyle.Fill;
            txtMonitoringServer.ForeColor = Color.Lime;
            txtMonitoringServer.Location = new Point(3, 3);
            txtMonitoringServer.Multiline = true;
            txtMonitoringServer.Name = "txtMonitoringServer";
            txtMonitoringServer.ScrollBars = ScrollBars.Vertical;
            txtMonitoringServer.Size = new Size(1165, 578);
            txtMonitoringServer.TabIndex = 1;
            txtMonitoringServer.WordWrap = false;
            // 
            // tpHeartbeats
            // 
            tpHeartbeats.Controls.Add(txtHeartbeats);
            tpHeartbeats.Location = new Point(4, 24);
            tpHeartbeats.Name = "tpHeartbeats";
            tpHeartbeats.Padding = new Padding(3);
            tpHeartbeats.Size = new Size(1171, 584);
            tpHeartbeats.TabIndex = 1;
            tpHeartbeats.Text = "Client Heart Beats";
            tpHeartbeats.UseVisualStyleBackColor = true;
            // 
            // txtHeartbeats
            // 
            txtHeartbeats.BackColor = Color.Black;
            txtHeartbeats.BorderStyle = BorderStyle.FixedSingle;
            txtHeartbeats.Dock = DockStyle.Fill;
            txtHeartbeats.ForeColor = Color.Lime;
            txtHeartbeats.Location = new Point(3, 3);
            txtHeartbeats.Multiline = true;
            txtHeartbeats.Name = "txtHeartbeats";
            txtHeartbeats.Size = new Size(1165, 578);
            txtHeartbeats.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblServerStatus });
            statusStrip1.Location = new Point(0, 614);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1179, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblServerStatus
            // 
            lblServerStatus.Name = "lblServerStatus";
            lblServerStatus.Size = new Size(84, 17);
            lblServerStatus.Text = "lblServerStatus";
            // 
            // MonitoringServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1179, 636);
            Controls.Add(statusStrip1);
            Controls.Add(tabcServerConsole);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MonitoringServer";
            Text = "Monitoring Server";
            Load += MonitoringServer_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tabcServerConsole.ResumeLayout(false);
            tpLogs.ResumeLayout(false);
            tpLogs.PerformLayout();
            tpHeartbeats.ResumeLayout(false);
            tpHeartbeats.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem menuStartServer;
        private ToolStripMenuItem clearTextToolStripMenuItem;
        private TabControl tabcServerConsole;
        private TabPage tpLogs;
        private TextBox txtMonitoringServer;
        private TabPage tpHeartbeats;
        private TextBox txtHeartbeats;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblServerStatus;
    }
}
