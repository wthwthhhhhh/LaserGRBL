namespace LaserGRBL.RasterConverter
{
	partial class ColorRangeForm
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
            this.PbSelectColor = new System.Windows.Forms.PictureBox();
            this.PbColorPreview = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.PbSelectColor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PbColorPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // PbSelectColor
            // 
            this.PbSelectColor.Cursor = System.Windows.Forms.Cursors.Cross;
            this.PbSelectColor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PbSelectColor.Location = new System.Drawing.Point(0, 0);
            this.PbSelectColor.Name = "PbSelectColor";
            this.PbSelectColor.Size = new System.Drawing.Size(1462, 874);
            this.PbSelectColor.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.PbSelectColor.TabIndex = 0;
            this.PbSelectColor.TabStop = false;
            this.PbSelectColor.Click += new System.EventHandler(this.pictureBox1_Click);
            this.PbSelectColor.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PbSelectColor_MouseMove);
            // 
            // PbColorPreview
            // 
            this.PbColorPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PbColorPreview.Location = new System.Drawing.Point(1419, 12);
            this.PbColorPreview.Name = "PbColorPreview";
            this.PbColorPreview.Size = new System.Drawing.Size(31, 29);
            this.PbColorPreview.TabIndex = 1;
            this.PbColorPreview.TabStop = false;
            // 
            // ColorRangeForm
            // 
            this.ClientSize = new System.Drawing.Size(1462, 874);
            this.Controls.Add(this.PbColorPreview);
            this.Controls.Add(this.PbSelectColor);
            this.Name = "ColorRangeForm";
            this.Text = "选择颜色";
            this.SizeChanged += new System.EventHandler(this.ColorRangeForm_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.PbSelectColor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PbColorPreview)).EndInit();
            this.ResumeLayout(false);

		}


        #endregion

        private System.Windows.Forms.PictureBox PbSelectColor;
        private System.Windows.Forms.PictureBox PbColorPreview;
    }
}