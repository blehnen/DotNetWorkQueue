namespace ConsoleView
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPageOutput = new System.Windows.Forms.TabPage();
            this.logControlCommand = new ConsoleView.LogControl();
            this.tabPageStatus = new System.Windows.Forms.TabPage();
            this.queueStatusControl1 = new ConsoleView.QueueStatusControl();
            this.tabPageLogging = new System.Windows.Forms.TabPage();
            this.logControl1 = new ConsoleView.LogControl();
            this.shellControl1 = new ShellControlV2.ShellControl();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControlMain.SuspendLayout();
            this.tabPageOutput.SuspendLayout();
            this.tabPageStatus.SuspendLayout();
            this.tabPageLogging.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.shellControl1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControlMain);
            this.splitContainer1.Size = new System.Drawing.Size(855, 404);
            this.splitContainer1.SplitterDistance = 133;
            this.splitContainer1.TabIndex = 0;
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPageOutput);
            this.tabControlMain.Controls.Add(this.tabPageStatus);
            this.tabControlMain.Controls.Add(this.tabPageLogging);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(855, 267);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPageOutput
            // 
            this.tabPageOutput.Controls.Add(this.logControlCommand);
            this.tabPageOutput.Location = new System.Drawing.Point(4, 22);
            this.tabPageOutput.Name = "tabPageOutput";
            this.tabPageOutput.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOutput.Size = new System.Drawing.Size(847, 241);
            this.tabPageOutput.TabIndex = 0;
            this.tabPageOutput.Text = "Console output";
            this.tabPageOutput.UseVisualStyleBackColor = true;
            // 
            // logControlCommand
            // 
            this.logControlCommand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logControlCommand.Location = new System.Drawing.Point(3, 3);
            this.logControlCommand.Name = "logControlCommand";
            this.logControlCommand.Size = new System.Drawing.Size(841, 235);
            this.logControlCommand.TabIndex = 0;
            // 
            // tabPageStatus
            // 
            this.tabPageStatus.Controls.Add(this.queueStatusControl1);
            this.tabPageStatus.Location = new System.Drawing.Point(4, 22);
            this.tabPageStatus.Name = "tabPageStatus";
            this.tabPageStatus.Size = new System.Drawing.Size(847, 241);
            this.tabPageStatus.TabIndex = 4;
            this.tabPageStatus.Text = "Status";
            this.tabPageStatus.UseVisualStyleBackColor = true;
            // 
            // queueStatusControl1
            // 
            this.queueStatusControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.queueStatusControl1.Location = new System.Drawing.Point(0, 0);
            this.queueStatusControl1.Name = "queueStatusControl1";
            this.queueStatusControl1.Size = new System.Drawing.Size(847, 241);
            this.queueStatusControl1.TabIndex = 0;
            // 
            // tabPageLogging
            // 
            this.tabPageLogging.Controls.Add(this.logControl1);
            this.tabPageLogging.Location = new System.Drawing.Point(4, 22);
            this.tabPageLogging.Name = "tabPageLogging";
            this.tabPageLogging.Size = new System.Drawing.Size(847, 241);
            this.tabPageLogging.TabIndex = 3;
            this.tabPageLogging.Text = "Log";
            this.tabPageLogging.UseVisualStyleBackColor = true;
            // 
            // logControl1
            // 
            this.logControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logControl1.Location = new System.Drawing.Point(0, 0);
            this.logControl1.Name = "logControl1";
            this.logControl1.Size = new System.Drawing.Size(847, 241);
            this.logControl1.TabIndex = 0;
            // 
            // shellControl1
            // 
            this.shellControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.shellControl1.Location = new System.Drawing.Point(0, 0);
            this.shellControl1.Name = "shellControl1";
            this.shellControl1.Prompt = ">>>";
            this.shellControl1.ShellTextBackColor = System.Drawing.Color.Black;
            this.shellControl1.ShellTextFont = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.shellControl1.ShellTextForeColor = System.Drawing.Color.LawnGreen;
            this.shellControl1.Size = new System.Drawing.Size(855, 133);
            this.shellControl1.TabIndex = 0;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(855, 404);
            this.Controls.Add(this.splitContainer1);
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SQL Server Producer";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControlMain.ResumeLayout(false);
            this.tabPageOutput.ResumeLayout(false);
            this.tabPageStatus.ResumeLayout(false);
            this.tabPageLogging.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageOutput;
        private System.Windows.Forms.TabPage tabPageLogging;
        private LogControl logControl1;
        private System.Windows.Forms.TabPage tabPageStatus;
        private QueueStatusControl queueStatusControl1;
        private LogControl logControlCommand;
        private ShellControlV2.ShellControl shellControl1;
    }
}

