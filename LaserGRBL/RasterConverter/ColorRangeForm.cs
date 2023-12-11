//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LaserGRBL.RasterConverter
{
	public partial class ColorRangeForm : Form
    {
        private Rectangle imageRect;
        public Color pixelColor;
        public ColorRangeForm()
		{
			InitializeComponent();
		}

        internal void SetImg(Image img)
        {
           this.PbSelectColor.Image = img; 
            imageRect = CalculateImageRect();
        }

        private decimal GetMaxQuality()
		{
			return Settings.GetObject("Raster Hi-Res", false) ? 50 : 20;
		}

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void ColorRangeForm_SizeChanged(object sender, EventArgs e)
        {
            imageRect = CalculateImageRect();
        }
        private Rectangle CalculateImageRect()
        {
            // 计算图片在控件中的位置和大小
            int imageWidth = PbSelectColor.Image.Width;
            int imageHeight = PbSelectColor.Image.Height;
            int controlWidth = PbSelectColor.Width;
            int controlHeight = PbSelectColor.Height;

            int x = (controlWidth - imageWidth) / 2;
            int y = (controlHeight - imageHeight) / 2;

            return new Rectangle(x, y, imageWidth, imageHeight);
        }
        private void PbSelectColor_MouseMove(object sender, MouseEventArgs e)
        {
            if (PbSelectColor.Image == null)
                return;
            if (imageRect.Contains(e.Location))
            {
                var imageBitmap = new Bitmap(PbSelectColor.Image);
                // 获取鼠标指针位置的像素颜色
                int x = e.X - imageRect.X;
                int y = e.Y - imageRect.Y;
                if (x >= 0 && y >= 0 && x < imageBitmap.Width && y < imageBitmap.Height)
                {
                     pixelColor = imageBitmap.GetPixel(x, y);
                    if (pixelColor.A == 255) { 
                    BackColor = pixelColor;
                    this.ForeColor= pixelColor;
                    PbColorPreview.BackColor = pixelColor;
                    }
                }
            }
        }

    }
}
