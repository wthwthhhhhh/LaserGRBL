namespace LaserGRBL
{
    partial class SetLineLenForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBox_LineLength = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.Btn_SetLineLen = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(24, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(346, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "请输入画笔到固定点之间的距离";
            // 
            // textBox_LineLength
            // 
            this.textBox_LineLength.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.textBox_LineLength.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBox_LineLength.Location = new System.Drawing.Point(505, 202);
            this.textBox_LineLength.Name = "textBox_LineLength";
            this.textBox_LineLength.Size = new System.Drawing.Size(101, 33);
            this.textBox_LineLength.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(612, 218);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "毫米";
            // 
            // Btn_SetLineLen
            // 
            this.Btn_SetLineLen.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.Btn_SetLineLen.Location = new System.Drawing.Point(337, 389);
            this.Btn_SetLineLen.Name = "Btn_SetLineLen";
            this.Btn_SetLineLen.Size = new System.Drawing.Size(101, 34);
            this.Btn_SetLineLen.TabIndex = 2;
            this.Btn_SetLineLen.Text = "确定";
            this.Btn_SetLineLen.UseVisualStyleBackColor = true;
            this.Btn_SetLineLen.Click += new System.EventHandler(this.Btn_SetLineLen_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.ErrorImage = global::LaserGRBL.Strings.校准机器位置;
            this.pictureBox1.Image = global::LaserGRBL.Strings.校准机器位置;
            this.pictureBox1.ImageLocation = "";
            this.pictureBox1.InitialImage = global::LaserGRBL.Strings.校准机器位置;
            this.pictureBox1.Location = new System.Drawing.Point(28, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(724, 359);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // SetLineLenForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(780, 437);
            this.Controls.Add(this.Btn_SetLineLen);
            this.Controls.Add(this.textBox_LineLength);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SetLineLenForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "校准机器位置";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox_LineLength;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button Btn_SetLineLen;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}