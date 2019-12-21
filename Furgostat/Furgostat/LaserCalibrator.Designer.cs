namespace Furgostat
{
    partial class LaserCalibrator
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.addSampleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCalibrationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tubeSwitchingTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addSampleToolStripMenuItem,
            this.fitToolStripMenuItem,
            this.saveCalibrationToolStripMenuItem,
            this.tubeSwitchingTimeToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // addSampleToolStripMenuItem
            // 
            this.addSampleToolStripMenuItem.Name = "addSampleToolStripMenuItem";
            this.addSampleToolStripMenuItem.Size = new System.Drawing.Size(83, 20);
            this.addSampleToolStripMenuItem.Text = "Add Sample";
            this.addSampleToolStripMenuItem.Click += new System.EventHandler(this.addSampleToolStripMenuItem_Click_1);
            // 
            // fitToolStripMenuItem
            // 
            this.fitToolStripMenuItem.Name = "fitToolStripMenuItem";
            this.fitToolStripMenuItem.Size = new System.Drawing.Size(32, 20);
            this.fitToolStripMenuItem.Text = "Fit";
            this.fitToolStripMenuItem.Click += new System.EventHandler(this.fitToolStripMenuItem_Click_1);
            // 
            // saveCalibrationToolStripMenuItem
            // 
            this.saveCalibrationToolStripMenuItem.Name = "saveCalibrationToolStripMenuItem";
            this.saveCalibrationToolStripMenuItem.Size = new System.Drawing.Size(104, 20);
            this.saveCalibrationToolStripMenuItem.Text = "Save Calibration";
            this.saveCalibrationToolStripMenuItem.Click += new System.EventHandler(this.saveCalibrationToolStripMenuItem_Click_1);
            // 
            // tubeSwitchingTimeToolStripMenuItem
            // 
            this.tubeSwitchingTimeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox1});
            this.tubeSwitchingTimeToolStripMenuItem.Name = "tubeSwitchingTimeToolStripMenuItem";
            this.tubeSwitchingTimeToolStripMenuItem.Size = new System.Drawing.Size(131, 20);
            this.tubeSwitchingTimeToolStripMenuItem.Text = "Tube Switching Time";
            this.tubeSwitchingTimeToolStripMenuItem.Click += new System.EventHandler(this.tubeSwitchingTimeToolStripMenuItem_Click);
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 23);
            this.toolStripTextBox1.Text = "5";
            this.toolStripTextBox1.Click += new System.EventHandler(this.toolStripTextBox1_Click);
            // 
            // LaserCalibrator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "LaserCalibrator";
            this.Text = "LaserCalibrator";
            this.Load += new System.EventHandler(this.LaserCalibrator_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem addSampleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveCalibrationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tubeSwitchingTimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
    }
}