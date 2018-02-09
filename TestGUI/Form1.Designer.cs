namespace TestGUI
{
    partial class Form1
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
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.viewer1 = new TestGUI.Viewer();
            this.scanImageViewer1 = new ConfocalUILib.ScanImageViewer();
            this.rulerControl1 = new ConfocalUILib.RulerControl();
            this.rulerControl2 = new ConfocalUILib.RulerControl();
            this.ruller1 = new ConfocalUILib.Ruller();
            this.SuspendLayout();
            // 
            // viewer1
            // 
            this.viewer1.Location = new System.Drawing.Point(62, 32);
            this.viewer1.Name = "viewer1";
            this.viewer1.SelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Rectangle;
            this.viewer1.Size = new System.Drawing.Size(387, 231);
            this.viewer1.TabIndex = 0;
            // 
            // scanImageViewer1
            // 
            this.scanImageViewer1.Location = new System.Drawing.Point(455, 32);
            this.scanImageViewer1.Name = "scanImageViewer1";
            this.scanImageViewer1.SelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.None;
            this.scanImageViewer1.Size = new System.Drawing.Size(445, 231);
            this.scanImageViewer1.TabIndex = 1;
            // 
            // rulerControl1
            // 
            this.rulerControl1.ActualSize = true;
            this.rulerControl1.BackColor = System.Drawing.Color.Silver;
            this.rulerControl1.BorderStyle = System.Windows.Forms.Border3DStyle.Raised;
            this.rulerControl1.DivisionMarkFactor = 5;
            this.rulerControl1.Divisions = 10;
            this.rulerControl1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.rulerControl1.ForeColor = System.Drawing.Color.Black;
            this.rulerControl1.Location = new System.Drawing.Point(0, 387);
            this.rulerControl1.MajorInterval = 100;
            this.rulerControl1.MiddleMarkFactor = 3;
            this.rulerControl1.MouseTrackingOn = true;
            this.rulerControl1.Name = "rulerControl1";
            this.rulerControl1.Orientation = ConfocalUILib.enumOrientation.orHorizontal;
            this.rulerControl1.RulerAlignment = ConfocalUILib.enumRulerAlignment.raBottomOrRight;
            this.rulerControl1.ScaleMode = ConfocalUILib.enumScaleMode.smPoints;
            this.rulerControl1.Size = new System.Drawing.Size(1016, 37);
            this.rulerControl1.StartValue = 0D;
            this.rulerControl1.TabIndex = 2;
            this.rulerControl1.Text = "rulerControl1";
            this.rulerControl1.VerticalNumbers = true;
            this.rulerControl1.ZoomFactor = 1D;
            // 
            // rulerControl2
            // 
            this.rulerControl2.ActualSize = true;
            this.rulerControl2.BackColor = System.Drawing.Color.Silver;
            this.rulerControl2.BorderStyle = System.Windows.Forms.Border3DStyle.Raised;
            this.rulerControl2.DivisionMarkFactor = 5;
            this.rulerControl2.Divisions = 10;
            this.rulerControl2.ForeColor = System.Drawing.Color.Black;
            this.rulerControl2.Location = new System.Drawing.Point(20, 32);
            this.rulerControl2.MajorInterval = 100;
            this.rulerControl2.MiddleMarkFactor = 3;
            this.rulerControl2.MouseTrackingOn = true;
            this.rulerControl2.Name = "rulerControl2";
            this.rulerControl2.Orientation = ConfocalUILib.enumOrientation.orVertical;
            this.rulerControl2.RulerAlignment = ConfocalUILib.enumRulerAlignment.raBottomOrRight;
            this.rulerControl2.ScaleMode = ConfocalUILib.enumScaleMode.smPoints;
            this.rulerControl2.Size = new System.Drawing.Size(36, 231);
            this.rulerControl2.StartValue = 0D;
            this.rulerControl2.TabIndex = 3;
            this.rulerControl2.Text = "rulerControl2";
            this.rulerControl2.VerticalNumbers = true;
            this.rulerControl2.ZoomFactor = 1D;
            // 
            // ruller1
            // 
            this.ruller1.AutoShiftMajorTickTo10Div = true;
            this.ruller1.BackColor = System.Drawing.Color.Gray;
            this.ruller1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ruller1.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.ruller1.Length = 100;
            this.ruller1.Location = new System.Drawing.Point(0, 357);
            this.ruller1.MajorTickHeightPrecentage = 0.8D;
            this.ruller1.MajorTicks = 10;
            this.ruller1.MinorTickHeightPrecentage = 0.5D;
            this.ruller1.MinorTicks = 10;
            this.ruller1.Name = "ruller1";
            this.ruller1.Size = new System.Drawing.Size(1016, 30);
            this.ruller1.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 424);
            this.Controls.Add(this.ruller1);
            this.Controls.Add(this.rulerControl2);
            this.Controls.Add(this.rulerControl1);
            this.Controls.Add(this.scanImageViewer1);
            this.Controls.Add(this.viewer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Viewer viewer1;
        private ConfocalUILib.ScanImageViewer scanImageViewer1;
        private ConfocalUILib.RulerControl rulerControl1;
        private ConfocalUILib.RulerControl rulerControl2;
        private ConfocalUILib.Ruller ruller1;
    }
}

