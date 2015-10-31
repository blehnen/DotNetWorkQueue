namespace ShellControlV2
{
    partial class ShellControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;

            this._shellTextBox = new ShellTextBox();
            this.SuspendLayout();
            // 
            // shellTextBox
            // 
            this._shellTextBox.AcceptsReturn = true;
            this._shellTextBox.AcceptsTab = true;
            this._shellTextBox.BackColor = System.Drawing.Color.Black;
            this._shellTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._shellTextBox.ForeColor = System.Drawing.Color.LawnGreen;
            this._shellTextBox.Location = new System.Drawing.Point(0, 0);
            this._shellTextBox.Multiline = true;
            this._shellTextBox.Name = "_shellTextBox";
            this._shellTextBox.Prompt = ">>>";
            this._shellTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this._shellTextBox.BackColor = System.Drawing.Color.Black;
            this._shellTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this._shellTextBox.ForeColor = System.Drawing.Color.LawnGreen;
            this._shellTextBox.Size = new System.Drawing.Size(232, 216);
            this._shellTextBox.TabIndex = 0;
            this._shellTextBox.Text = "";
            // 
            // ShellControl
            // 
            this.Controls.Add(this._shellTextBox);
            this.Name = "ShellControl";
            this.Size = new System.Drawing.Size(232, 216);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
