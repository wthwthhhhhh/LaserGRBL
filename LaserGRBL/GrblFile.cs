//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using System.Collections;
using CsPotrace;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Web.UI.WebControls;
using static LaserGRBL.RasterConverter.ImageProcessor;
using CsPotrace.BezierToBiarc;
using System.Text.RegularExpressions;
using System.IO;
using Svg;
using Svg.Transforms;
using Svg.Pathing;
using System.Globalization;

namespace LaserGRBL
{
	public class GrblFile : IEnumerable<GrblCommand>
	{
		public enum CartesianQuadrant { I, II, III, IV, Mix, Unknown }

		public delegate void OnFileLoadedDlg(long elapsed, string filename);
		public event OnFileLoadedDlg OnFileLoading;
		public event OnFileLoadedDlg OnFileLoaded;

		private List<GrblCommand> list = new List<GrblCommand>();
		private ProgramRange mRange = new ProgramRange();
		private TimeSpan mEstimatedTotalTime;

		public GrblFile()
		{

		}

		public GrblFile(decimal x, decimal y, decimal x1, decimal y1)
		{
			mRange.UpdateXYRange(new GrblCommand.Element('X', x), new GrblCommand.Element('Y', y), false);
			mRange.UpdateXYRange(new GrblCommand.Element('X', x1), new GrblCommand.Element('Y', y1), false);
		}

		public void SaveGCODE(string filename, bool header, bool footer, bool between, int cycles, bool useLFLineEndings, GrblCore core)
		{
			try
			{
				using (System.IO.StreamWriter sw = new System.IO.StreamWriter(filename))
				{
					if (useLFLineEndings)
						sw.NewLine = "\n";

					if (header)
						EvaluateAddLines(core, sw, Settings.GetObject("GCode.CustomHeader", GrblCore.GCODE_STD_HEADER));

					for (int i = 0; i < cycles; i++)
					{
						foreach (GrblCommand cmd in list)
							sw.WriteLine(cmd.Command);


						if (between && i < cycles - 1)
							EvaluateAddLines(core, sw, Settings.GetObject("GCode.CustomPasses", GrblCore.GCODE_STD_PASSES));
					}

					if (footer)
						EvaluateAddLines(core, sw, Settings.GetObject("GCode.CustomFooter", GrblCore.GCODE_STD_FOOTER));

					sw.Close();
				}
			}
			catch { }
		}

		private static void EvaluateAddLines(GrblCore core, System.IO.StreamWriter sw, string lines)
		{
			string[] arr = lines.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			foreach (string line in arr)
			{
				if (line.Trim().Length > 0)
				{
					string command = core.EvaluateExpression(line);
					if (!string.IsNullOrEmpty(command))
						sw.WriteLine(command);
				}
			}
		}

		public void LoadFile(string filename, bool append)
		{
			RiseOnFileLoading(filename);

			long start = Tools.HiResTimer.TotalMilliseconds;

			if (!append)
				list.Clear();

			mRange.ResetRange();
			if (System.IO.File.Exists(filename))
			{
				using (System.IO.StreamReader sr = new System.IO.StreamReader(filename))
				{
					string line = null;
					while ((line = sr.ReadLine()) != null)
						if ((line = line.Trim()).Length > 0)
						{
							GrblCommand cmd = new GrblCommand(line);
							if (!cmd.IsEmpty)
								list.Add(cmd);
						}
				}
			}
			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		public void LoadImportedSVG(string filename, bool append, GrblCore core)
		{
			RiseOnFileLoading(filename);

			long start = Tools.HiResTimer.TotalMilliseconds;

			if (!append)
				list.Clear();

			mRange.ResetRange();

			SvgConverter.GCodeFromSVG converter = new SvgConverter.GCodeFromSVG();
			converter.GCodeXYFeed = Settings.GetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", 1000);
			converter.UseLegacyBezier = !Settings.GetObject($"Vector.UseSmartBezier", true);

			string gcode = converter.convertFromFile(filename, core);
			string[] lines = gcode.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			foreach (string l in lines)
			{
				string line = l;
				if ((line = line.Trim()).Length > 0)
				{
					GrblCommand cmd = new GrblCommand(line);
					if (!cmd.IsEmpty)
						list.Add(cmd);
				}
			}

			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}


		private abstract class ColorSegment
		{
			public int mColor { get; set; }
			protected int mPixLen;

			public ColorSegment(int col, int len, bool rev)
			{
				mColor = col;
				mPixLen = rev ? -len : len;
			}

			public virtual bool IsSeparator
			{ get { return false; } }

			public bool Fast(L2LConf c)
			{ return c.pwm ? mColor == 0 : mColor <= 125; }

			public string formatnumber(int number, float offset, L2LConf c)
			{
				double dval = Math.Round(number / (c.vectorfilling ? c.fres : c.res) + offset, 3);
				return dval.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}

			// Format laser power value
			// grbl                    with pwm : color can be between 0 and configured SMax - S128
			// smoothiware             with pwm : Value between 0.00 and 1.00    - S0.50
			// Marlin : Laser power can not be defined as switch (Add in comment hard coded changes)
			public string FormatLaserPower(int color, L2LConf c)
			{
				if (c.firmwareType == Firmware.Smoothie)
					return string.Format(System.Globalization.CultureInfo.InvariantCulture, "S{0:0.00}", color / 255.0); //maybe scaling to UI maxpower VS config maxpower instead of fixed / 255.0 ?
																														 //else if (c.firmwareType == Firmware.Marlin)
																														 //	return "";
				else
					return string.Format(System.Globalization.CultureInfo.InvariantCulture, "S{0}", color);
			}

			public abstract string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c);
		}

		private class XSegment : ColorSegment
		{
			public XSegment(int col, int len, bool rev) : base(col, len, rev) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				cumX += mPixLen;

				if (c.pwm)
					return string.Format("X{0} {1}", formatnumber(cumX, c.oX, c), FormatLaserPower(mColor, c));
				else
					return string.Format("X{0} {1}", formatnumber(cumX, c.oX, c), Fast(c) ? c.lOff : c.lOn);
			}
		}

		private class YSegment : ColorSegment
		{
			public YSegment(int col, int len, bool rev) : base(col, len, rev) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				cumY += mPixLen;

				if (c.pwm)
					return string.Format("Y{0} {1}", formatnumber(cumY, c.oY, c), FormatLaserPower(mColor, c));
				else
					return string.Format("Y{0} {1}", formatnumber(cumY, c.oY, c), Fast(c) ? c.lOff : c.lOn);
			}
		}

		private class DSegment : ColorSegment
		{
			public DSegment(int col, int len, bool rev) : base(col, len, rev) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, GrblFile.L2LConf c)
			{
				cumX += mPixLen;
				cumY -= mPixLen;

				if (c.pwm)
					return string.Format("X{0} Y{1} {2}", formatnumber(cumX, c.oX, c), formatnumber(cumY, c.oY, c), FormatLaserPower(mColor, c));
				else
					return string.Format("X{0} Y{1} {2}", formatnumber(cumX, c.oX, c), formatnumber(cumY, c.oY, c), Fast(c) ? c.lOff : c.lOn);
			}
		}

		private class VSeparator : ColorSegment
		{
			public VSeparator() : base(0, 1, false) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				if (mPixLen < 0)
					throw new Exception();

				cumY += mPixLen;
				return string.Format("Y{0}", formatnumber(cumY, c.oY, c));
			}

			public override bool IsSeparator
			{ get { return true; } }
		}

		private class HSeparator : ColorSegment
		{
			public HSeparator() : base(0, 1, false) { }

			public override string ToGCodeNumber(ref int cumX, ref int cumY, L2LConf c)
			{
				if (mPixLen < 0)
					throw new Exception();

				cumX += mPixLen;
				return string.Format("X{0}", formatnumber(cumX, c.oX, c));
			}

			public override bool 
				IsSeparator
			{ get { return true; } }
		}

		public static bool RasterFilling(RasterConverter.ImageProcessor.Direction dir)
		{
			return dir == RasterConverter.ImageProcessor.Direction.Diagonal || dir == RasterConverter.ImageProcessor.Direction.Horizontal || dir == RasterConverter.ImageProcessor.Direction.Vertical;
		}
		public static bool VectorFilling(RasterConverter.ImageProcessor.Direction dir)
		{
			return dir == RasterConverter.ImageProcessor.Direction.NewDiagonal ||
			dir == RasterConverter.ImageProcessor.Direction.NewHorizontal ||
			dir == RasterConverter.ImageProcessor.Direction.NewVertical ||
			dir == RasterConverter.ImageProcessor.Direction.NewReverseDiagonal ||
			dir == RasterConverter.ImageProcessor.Direction.NewGrid ||
			dir == RasterConverter.ImageProcessor.Direction.NewDiagonalGrid ||
			dir == RasterConverter.ImageProcessor.Direction.NewCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewDiagonalCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewSquares ||
			dir == RasterConverter.ImageProcessor.Direction.NewZigZag ||
			dir == RasterConverter.ImageProcessor.Direction.NewHilbert ||
			dir == RasterConverter.ImageProcessor.Direction.NewInsetFilling;
		}

		public static bool TimeConsumingFilling(RasterConverter.ImageProcessor.Direction dir)
		{
			return
			dir == RasterConverter.ImageProcessor.Direction.NewCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewDiagonalCross ||
			dir == RasterConverter.ImageProcessor.Direction.NewSquares;
		}

		public void LoadImagePotrace(Bitmap bmp, string filename, bool UseSpotRemoval, int SpotRemoval, bool UseSmoothing, decimal Smoothing, bool UseOptimize, decimal Optimize, bool useOptimizeFast, L2LConf c, bool append, GrblCore core)
		{
			skipcmd = Settings.GetObject("Disable G0 fast skip", false) ? "G1" : "G0";

			RiseOnFileLoading(filename);

			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
			long start = Tools.HiResTimer.TotalMilliseconds;

			if (!append)
				list.Clear();

			//list.Add(new GrblCommand("G90")); //absolute (Moved to custom Header)

			mRange.ResetRange();

			Potrace.turdsize = (int)(UseSpotRemoval ? SpotRemoval : 2);
			Potrace.alphamax = UseSmoothing ? (double)Smoothing : 0.0;
			Potrace.opttolerance = UseOptimize ? (double)Optimize : 0.2;
			Potrace.curveoptimizing = UseOptimize; //optimize the path p, replacing sequences of Bezier segments by a single segment when possible.

			List<List<Curve>> plist =Potrace.PotraceTrace(bmp);
			List<List<Curve>> flist = null;
			if (list.Count > 0&& plist.Count>0) {
                list.Add(new GrblCommand(String.Format("CC{0}", c.penColor.ToArgb())));
            }

			if (VectorFilling(c.dir))
			{
				flist = PotraceClipper.BuildFilling(plist, bmp.Width, bmp.Height, c);
				flist = ParallelOptimizePaths(flist, 0 /*ComputeDirectionChangeCost(c, core, false)*/);
			}
			if (RasterFilling(c.dir))
			{
				using (Bitmap ptb = new Bitmap(bmp.Width, bmp.Height))
				{
					using (Graphics g = Graphics.FromImage(ptb))
					{
						double inset = Math.Max(1, c.res / c.fres); //bordino da togliere per finire un po' prima del bordo

						Potrace.Export2GDIPlus(plist, g, Brushes.Black, null, inset);

						using (Bitmap resampled = RasterConverter.ImageTransform.ResizeImage(ptb, new Size((int)(bmp.Width * c.fres / c.res) + 1, (int)(bmp.Height * c.fres / c.res) + 1), true, InterpolationMode.HighQualityBicubic))
						{
							if (c.pwm)
								list.Add(new GrblCommand(String.Format("{0} S0", c.lOn),c.penColor)); //laser on and power to zero
							else
								list.Add(new GrblCommand(String.Format($"{c.lOff} S{GrblCore.Configuration.MaxPWM}"), c.penColor)); //laser off and power to max power

							//set speed to markspeed
							// For marlin, need to specify G1 each time :
							// list.Add(new GrblCommand(String.Format("G1 F{0}", c.markSpeed)));
							list.Add(new GrblCommand(String.Format("F{0}", c.markSpeed)));

							c.vectorfilling = true;
							ImageLine2Line(resampled, c);

							//laser off
							list.Add(new GrblCommand(c.lOff));
						}
					}
				}
			}

			bool supportPWM = Settings.GetObject("Support Hardware PWM", false);


			if (supportPWM)
				list.Add(new GrblCommand($"{c.lOn} S0"));   //laser on and power to 0
			else
				list.Add(new GrblCommand($"{c.lOff} S{GrblCore.Configuration.MaxPWM}"));   //laser off and power to maxPower

             //边线                                                                              //跟踪边界
            if (plist != null && !c.onlyFill) //总是正确的
            {
                //优化快速运动
                if (useOptimizeFast)
                    plist = OptimizePaths(plist, 0 /*ComputeDirectionChangeCost(c, core, true)*/);
                else
                    plist.Reverse(); //

                List<string> gc = new List<string>();
                if (supportPWM)
                    gc.AddRange(Potrace.Export2GCode(plist, c.oX, c.oY, c.res, $"S{c.maxPower}", "S0", bmp.Size, skipcmd));
                else
                    gc.AddRange(Potrace.Export2GCode(plist, c.oX, c.oY, c.res, c.lOn, c.lOff, bmp.Size, skipcmd));

                // For marlin, need to specify G1 each time :
                //list.Add(new GrblCommand(String.Format("G1 F{0}", c.borderSpeed)));
                list.Add(new GrblCommand(String.Format("F{0}", c.borderSpeed)));
              
                foreach (string code in gc)
                    list.Add(new GrblCommand(code, c.penColor));
                list= OptimizeLine2Line(list, c);
            }
            //填充
            if (flist != null)
			{
				List<string> gc = new List<string>();
				if (supportPWM)
					gc.AddRange(Potrace.Export2GCode(flist, c.oX, c.oY, c.res, $"S{c.maxPower}", "S0", bmp.Size, skipcmd));
				else
					gc.AddRange(Potrace.Export2GCode(flist, c.oX, c.oY, c.res, c.lOn, c.lOff, bmp.Size, skipcmd));

				list.Add(new GrblCommand(String.Format("F{0}", c.markSpeed)));
				foreach (string code in gc)
					list.Add(new GrblCommand(code, c.penColor));
			}


			

			//if (supportPWM)
			//	gc = Potrace.Export2GCode(flist, c.oX, c.oY, c.res, $"S{c.maxPower}", "S0", bmp.Size, skipcmd);
			//else
			//	gc = Potrace.Export2GCode(flist, c.oX, c.oY, c.res, c.lOn, c.lOff, bmp.Size, skipcmd);

			//foreach (string code in gc)
			//	list.Add(new GrblCommand(code));


			//laser off (superflua??)
			if (supportPWM)
				list.Add(new GrblCommand(c.lOff));  //necessaria perché finisce con solo S0

			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		private void RiseOnFileLoaded(string filename, long elapsed)
		{
			if (OnFileLoaded != null)
				OnFileLoaded(elapsed, filename);
		}

		private void RiseOnFileLoading(string filename)
		{
			if (OnFileLoading != null)
				OnFileLoading(0, filename);
		}

        public class L2LConf
        {
            public double res;  // 分辨率
            public float oX;  // 原点X坐标
            public float oY;  // 原点Y坐标
            public int markSpeed;  // 标记速度
            public int borderSpeed;  // 边界速度
            public int minPower;  // 最小功率
            public int maxPower;  // 最大功率
            public string lOn;  // 激光打开指令
            public string lOff;  // 激光关闭指令
            public RasterConverter.ImageProcessor.Direction dir;  // 方向
            public bool pwm { get { return false; } set { } }  // 脉宽调制
            public double fres;  // 刷新频率
            public bool vectorfilling;  // 矢量填充
            public Firmware firmwareType;  // 固件类型
            public int resolution;  // 分辨率
            public float randomThreshold;  // 随机阈值
            public float amplitude;  // 振幅
            public float frequency;  // 频率
            public float lineWidth;  // 线宽
            public RasterConverter.ImageProcessor.LineTypeEnum lineType;  // 线宽
            public int optimizeSVG;  // 路径优化程度
            public bool onlyFill;  // 仅填充
            public Color penColor;  // 画笔颜色
        }
        public class RandomLineConf
        {
            public double res;
            public float oX;
            public float oY;
            public int markSpeed;
            public int borderSpeed;
            public int minPower;
            public int maxPower;
            public string lOn;
            public string lOff;
            public RasterConverter.ImageProcessor.Direction dir;
            public bool pwm { get { return false; } set { } }
            public double fres;
            public bool vectorfilling;
            public Firmware firmwareType;
        }
        private string skipcmd = "G0";
		public void LoadImageL2L(Bitmap bmp, string filename, L2LConf c, bool append, GrblCore core)
		{

			skipcmd = Settings.GetObject("Disable G0 fast skip", false) ? "G1" : "G0";

			RiseOnFileLoading(filename);

			bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

			long start = Tools.HiResTimer.TotalMilliseconds;

			if (!append)
				list.Clear();

			mRange.ResetRange();

            //absolute
            //list.Add(new GrblCommand("G90")); //(Moved to custom Header)

            //快速移动到offset(如果禁用G0则缓慢移动)，并设置标记速度
            list.Add(new GrblCommand(String.Format("{0} X{1} Y{2} F{3}", skipcmd, formatnumber(c.oX), formatnumber(c.oY), c.markSpeed)));
			if (c.pwm)
				list.Add(new GrblCommand(String.Format("{0} S0", c.lOn))); //激光打开，功率归零laser on and power to zero
            else
				list.Add(new GrblCommand($"{c.lOff} S{GrblCore.Configuration.MaxPWM}")); //关闭激光，将电源调至最大功率laser off and power to maxpower

            //设置速度为markspeed						
            // For marlin, need to specify G1 each time :
            //list.Add(new GrblCommand(String.Format("G1 F{0}", c.markSpeed)));
            //list.Add(new GrblCommand(String.Format("F{0}", c.markSpeed))); //replaced by the first move to offset and set speed

            ImageLine2Line(bmp, c);

			//laser off
			list.Add(new GrblCommand(c.lOff));

			//move fast to origin
			//list.Add(new GrblCommand("G0 X0 Y0")); //moved to custom footer

			Analyze();//计算耗时
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);
		}

		// For Marlin, as we sen M106 command, we need to know last color send
		//private int lastColorSend = 0;
		private void ImageLine2Line(Bitmap bmp, L2LConf c)
		{
			bool fast = true;
			List<ColorSegment> segments = GetSegments(bmp, c);
			List<GrblCommand> temp = new List<GrblCommand>();

			int cumX = 0;
			int cumY = 0;

			foreach (ColorSegment seg in segments)
			{
				bool changeGMode = (fast != seg.Fast(c)); //se veloce != dafareveloce

				if (seg.IsSeparator && !fast) //fast = previous segment contains S0 color
				{
					if (c.pwm)
						temp.Add(new GrblCommand("S0"));
					else
						temp.Add(new GrblCommand(c.lOff)); //laser off
				}

				fast = seg.Fast(c);

				// For marlin firmware, we must defined laser power before moving (unsing M106 or M107)
				// So we have to speficy gcode (G0 or G1) each time....
				//if (c.firmwareType == Firmware.Marlin)
				//{
				//	// Add M106 only if color has changed
				//	if (lastColorSend != seg.mColor)
				//		temp.Add(new GrblCommand(String.Format("M106 P1 S{0}", fast ? 0 : seg.mColor)));
				//	lastColorSend = seg.mColor;
				//	temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? "G0" : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
				//}
				//else
				//{

				if (changeGMode)
					temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
				else
					temp.Add(new GrblCommand(seg.ToGCodeNumber(ref cumX, ref cumY, c)));

				//}
			}

			temp = OptimizeLine2Line(temp, c);
			list.AddRange(temp);
		}
        private void ImageRandomLine(Bitmap bmp, L2LConf c)
        {
            bool fast = true;
            List<PointF> points = new List<PointF>();
            for (int y = 0; y < bmp.Height; y += c.resolution)
            {
                for (int x = 0; x < bmp.Width; x += (int)c.randomThreshold)
                {
                    float brightnessValue = GetBrightness(bmp.GetPixel(x, y));
                    if (brightnessValue < 127)
                    {
                        points.Add(new PointF(x, y));
                    }
                }
            }
            List<GrblCommand> temp = new List<GrblCommand>();

            Pen pen = new Pen(Color.FromArgb(80, 0, 0, 0), c.lineWidth);
            PointF prevPoint = PointF.Empty;

            foreach (PointF point in points)
            {
                float x = point.X;
                float y = point.Y;

                float angle = Map(Noise(x * c.frequency, y * c.frequency), 0, 1, 0, (float)(2 * Math.PI));
                float xOffset = (float)Math.Cos(angle) * c.amplitude;
                float yOffset = (float)Math.Sin(angle) * c.amplitude;
               
                    List<PointF> curvePoints = new List<PointF>();
                    curvePoints.Add(new PointF(x, y));

                    for (int j = 0; j <= c.resolution; j++)
                    {
                        float t = Map(j, 0, c.resolution, 0, 1);
                        float cx = x + xOffset * t;
                        float cy = y + yOffset * t;
                        curvePoints.Add(new PointF(cx, cy));
                    }

                    curvePoints.Add(new PointF(x + xOffset, y + yOffset));
					var begin = true;
					foreach (var nextPoint in curvePoints)
					{
                        if (!nextPoint.IsEmpty)
                        {
                            temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", string.Format("X{0} Y{1} ", formatnumber((int)nextPoint.X, c.oX, c), formatnumber((int)nextPoint.Y, c.oY, c)))));
                            if (begin)
							{
								temp.Add(new GrblCommand(c.lOn));
                                begin = false;
							}
							//else
							//{
							//    temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", string.Format("X{0} Y{1}", formatnumber((int)nextPoint.X, c.oX, c), formatnumber((int)nextPoint.Y, c.oY, c)))));
							//}
							//g.DrawLine(pen, prevPoint, nextPoint);

						}
                    }
                    temp.Add(new GrblCommand(c.lOff));
               

            }
            temp.Add(new GrblCommand(c.lOff));
            //foreach (ColorSegment seg in segments)
            //{
            //	bool changeGMode = (fast != seg.Fast(c)); //se veloce != dafareveloce

            //	if (seg.IsSeparator && !fast) //fast = previous segment contains S0 color 前一段包含S0颜色
            //	{
            //		if (c.pwm)
            //			temp.Add(new GrblCommand("S0"));
            //		else
            //			temp.Add(new GrblCommand(c.lOff)); //laser off
            //	}

            //	fast = seg.Fast(c);

            //	// For marlin firmware, we must defined laser power before moving (unsing M106 or M107)
            //	// So we have to speficy gcode (G0 or G1) each time....
            //	//if (c.firmwareType == Firmware.Marlin)
            //	//{
            //	//	// Add M106 only if color has changed
            //	//	if (lastColorSend != seg.mColor)
            //	//		temp.Add(new GrblCommand(String.Format("M106 P1 S{0}", fast ? 0 : seg.mColor)));
            //	//	lastColorSend = seg.mColor;
            //	//	temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? "G0" : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
            //	//}
            //	//else
            //	//{

            //	if (changeGMode)
            //		temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
            //	else
            //		temp.Add(new GrblCommand(seg.ToGCodeNumber(ref cumX, ref cumY, c)));
            //	return string.Format("X{0} {1}", formatnumber(cumX, c.oX, c), Fast(c) ? c.lOff : c.lOn);
            //	//}
            //}

            temp = OptimizeLine2Line(temp, c);
            list.AddRange(temp);
        }
        private void ImageOneLine(Bitmap bmp, L2LConf c)
        {
            bool fast = true;
            List<PointF> points = new List<PointF>();
            List<GrblCommand> temp = new List<GrblCommand>();
            switch (c.lineType)
            {
                case LineTypeEnum.Spiral:
					{
                        //float radius = 1.0f;
                        //float px = 0, py = 0, deg = 0, xc = bmp.Width / 2, yc = bmp.Height / 2, offset = c.frequency * 10, FrequencyCount = 1, x1 = 0, y1 = 0, arcLength = 0;


                        //while (px < bmp.Width && py < bmp.Height)
                        //{
                        //    float angle = (float)(deg * Math.PI / 180);
                        //    px = xc + (float)(Math.Cos(angle) * radius);
                        //    py = yc + (float)(Math.Sin(angle) * radius);
                        //    if (px > 0 && py > 0 && px < bmp.Width && py < bmp.Height)
                        //    {
                        //        float brightnessValue = GetBrightness(bmp.GetPixel((int)px, (int)py));
                        //        if (x1 > 0 && y1 > 0)
                        //        {
                        //            // 计算两个点相对于圆心的夹角
                        //            double angle1 = Math.Atan2(y1 - yc, x1 - xc);
                        //            double angle2 = Math.Atan2(py - yc, px - xc);

                        //            // 调整夹角为正值
                        //            if (angle1 < 0)
                        //            {
                        //                angle1 += 2 * Math.PI;
                        //            }
                        //            if (angle2 < 0)
                        //            {
                        //                angle2 += 2 * Math.PI;
                        //            }

                        //            // 计算弧长
                        //            arcLength = (float)Math.Abs(radius * (angle1 - angle2));
                        //        }
                        //        else
                        //        {
                        //            x1 = px;
                        //            y1 = py;
                        //        }
                        //        if (brightnessValue < 127 && arcLength > c.amplitude / 10)
                        //        {

                        //            // 计算角度
                        //            double anglec = Math.Atan2(py - yc, px - xc);

                        //            // 向圆心偏移的坐标
                        //            double r_inward = Math.Sqrt(Math.Pow(px - xc, 2) + Math.Pow(py - yc, 2));
                        //            double x_inward = xc + (r_inward - offset) * Math.Cos(angle);
                        //            double y_inward = yc + (r_inward - offset) * Math.Sin(angle);

                        //            points.Add(new PointF((float)x_inward, (float)y_inward));
                        //            points.Add(new PointF(px, py));
                        //            // 向外偏移的坐标
                        //            double r_outward = Math.Sqrt(Math.Pow(px - xc, 2) + Math.Pow(py - yc, 2));
                        //            double x_outward = xc + (r_outward + offset) * Math.Cos(angle);
                        //            double y_outward = yc + (r_outward + offset) * Math.Sin(angle);

                        //            points.Add(new PointF((float)x_outward, (float)y_outward));

                        //            x1 = px;
                        //            y1 = py;

                        //        }
                        //        else
                        //        {
                        //            points.Add(new PointF(px, py));
                        //        }
                        //    }
                        //    //螺旋半径每转动2度增加一次
                        //    radius = radius + (c.resolution / 1000f);
                        //    deg += c.randomThreshold / 10;
                        //    FrequencyCount++;
                        //}

                        points = GenerateSpiral(bmp, c.resolution / 100f, c.frequency, c.randomThreshold, c.amplitude);
						//temp.AddRange(ConvertPointsToGCode(points,c));
						foreach (PointF point in points)
						{
							float x = point.X;
							float y = point.Y;


							temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", string.Format("X{0} Y{1} ", formatnumber((int)x, c.oX, c), formatnumber((int)y, c.oY, c)))));
							if (fast)
							{
								temp.Add(new GrblCommand(c.lOn));
								fast = false;
							}

						}
					}
                    break;
				default: {
                        for (int y = 0; y < bmp.Height; y += c.resolution)
                        {
                            for (int x = 0; x < bmp.Width; x += (int)c.randomThreshold)
                            {
                                float brightnessValue = GetBrightness(bmp.GetPixel(x, y));
                                if (brightnessValue < 127)
                                {
                                    points.Add(new PointF(x, y));
                                }
                            }
                        }

                        PointF prevPoint = PointF.Empty;

                        foreach (PointF point in points)
                        {
                            float x = point.X;
                            float y = point.Y;

                            float angle = Map(Noise(x * c.frequency, y * c.frequency), 0, 1, 0, (float)(2 * Math.PI));
                            float xOffset = (float)Math.Cos(angle) * c.amplitude;
                            float yOffset = (float)Math.Sin(angle) * c.amplitude;


                            PointF nextPoint = new PointF(x + xOffset, y + yOffset);

                            if (!nextPoint.IsEmpty)
                            {
                                temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", string.Format("X{0} Y{1} ", formatnumber((int)nextPoint.X, c.oX, c), formatnumber((int)nextPoint.Y, c.oY, c)))));
                                if (fast)
                                {
                                    temp.Add(new GrblCommand(c.lOn));
                                    fast = false;
                                }

                            }

                            prevPoint = nextPoint;


                        }
                    }break;
			}
            temp.Add(new GrblCommand(c.lOff));
            //foreach (ColorSegment seg in segments)
            //{
            //	bool changeGMode = (fast != seg.Fast(c)); //se veloce != dafareveloce

            //	if (seg.IsSeparator && !fast) //fast = previous segment contains S0 color 前一段包含S0颜色
            //	{
            //		if (c.pwm)
            //			temp.Add(new GrblCommand("S0"));
            //		else
            //			temp.Add(new GrblCommand(c.lOff)); //laser off
            //	}

            //	fast = seg.Fast(c);

            //	// For marlin firmware, we must defined laser power before moving (unsing M106 or M107)
            //	// So we have to speficy gcode (G0 or G1) each time....
            //	//if (c.firmwareType == Firmware.Marlin)
            //	//{
            //	//	// Add M106 only if color has changed
            //	//	if (lastColorSend != seg.mColor)
            //	//		temp.Add(new GrblCommand(String.Format("M106 P1 S{0}", fast ? 0 : seg.mColor)));
            //	//	lastColorSend = seg.mColor;
            //	//	temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? "G0" : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
            //	//}
            //	//else
            //	//{

            //	if (changeGMode)
            //		temp.Add(new GrblCommand(String.Format("{0} {1}", fast ? skipcmd : "G1", seg.ToGCodeNumber(ref cumX, ref cumY, c))));
            //	else
            //		temp.Add(new GrblCommand(seg.ToGCodeNumber(ref cumX, ref cumY, c)));
            //	return string.Format("X{0} {1}", formatnumber(cumX, c.oX, c), Fast(c) ? c.lOff : c.lOn);
            //	//}
            //}

            temp = OptimizeLine2Line(temp, c);
            list.AddRange(temp);
        }
        public  List<GrblCommand> ConvertPointsToGCode(List<PointF> points, L2LConf c)
        {
            List<GrblCommand> gcode = new List<GrblCommand>();

            PointF previousPoint = PointF.Empty;
            bool inArcMode = true;

            foreach (PointF point in points)
            {
                if (previousPoint == PointF.Empty)
                {
					gcode.Add(new GrblCommand(String.Format("{0} {1}", "G0", string.Format("X{0} Y{1} ", formatnumber((int)point.X, c.oX, c), formatnumber((int)point.Y, c.oY, c)))));
                    // 第一个点
                   // gcodeBuilder.AppendFormat("G0 X{0} Y{1}", point.X, point.Y);
                }
                else
                {
                    if (inArcMode)
                    {
                        // 当前是圆弧模式，检查是否可以继续使用圆弧
                        float radius = GetDistance(previousPoint, point) / 2;
                        PointF center = CalculateArcCenter(previousPoint, point, radius);

                        if (center != PointF.Empty)
                        {
                            gcode.Add(new GrblCommand(String.Format("{0} {1}", "G2", string.Format("X{0} Y{1} I{2} J{3}", formatnumber((int)point.X, c.oX, c), formatnumber((int)point.Y, c.oY, c), center.X - previousPoint.X, center.Y - previousPoint.Y))));

                            // 可以继续使用圆弧
                            //gcodeBuilder.AppendFormat(" G2 X{0} Y{1} I{2} J{3}",
                            //    point.X, point.Y, center.X - previousPoint.X, center.Y - previousPoint.Y);
                            previousPoint = point;
                            continue;
                        }
                        //else
                        //{
                        //    // 无法继续使用圆弧，结束圆弧模式
                        //    //gcode.Add(gcodeBuilder.ToString());
                        //    //gcodeBuilder.Clear();
                        //    inArcMode = false;
                        //}
                    }
                    gcode.Add(new GrblCommand(String.Format("{0} {1}", "G1", string.Format("X{0} Y{1} ", formatnumber((int)point.X, c.oX, c), formatnumber((int)point.Y, c.oY, c)))));

                    // 使用直线运动
                    //gcodeBuilder.AppendFormat("G1 X{0} Y{1}", point.X, point.Y);
                }

                previousPoint = point;
            }

            //if (gcodeBuilder.Length > 0)
            //{
            //    // 添加最后一个指令
            //    gcode.Add(gcodeBuilder.ToString());
            //}

            return gcode;
        }

        public static float GetDistance(PointF p1, PointF p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static PointF CalculateArcCenter(PointF p1, PointF p2, float radius)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;

            float d = dx * dx + dy * dy;
            float determinant = radius * radius / d - 0.25f;

            if (determinant < 0)
            {
                return PointF.Empty; // 无法继续使用圆弧
            }

            float h = (float)Math.Sqrt(determinant);
            float centerX = 0.5f * (p1.X + p2.X) + h * (p1.Y - p2.Y);
            float centerY = 0.5f * (p1.Y + p2.Y) + h * (p2.X - p1.X);

            return new PointF(centerX, centerY);
        }
        private List<GrblCommand> OptimizeLine2Line(List<GrblCommand> temp, L2LConf c)
		{
			List<GrblCommand> rv = new List<GrblCommand>();

			decimal curX = (decimal)c.oX;
			decimal curY = (decimal)c.oY;
			bool cumulate = false;

			foreach (GrblCommand cmd in temp)
			{
				try
				{
					cmd.BuildHelper();

					bool oldcumulate = cumulate;

					if (c.pwm)
					{
						if (cmd.S != null) //is S command
						{
							if (cmd.S.Number == 0) //is S command with zero power
								cumulate = true;   //begin cumulate
							else
								cumulate = false;  //end cumulate
						}
					}
					else
					{
						if (cmd.IsLaserOFF)
							cumulate = true;   //begin cumulate
						else if (cmd.IsLaserON)
							cumulate = false;  //end cumulate
					}


					if (oldcumulate && !cumulate) //cumulate down front -> flush
					{
						if (c.pwm)
							rv.Add(new GrblCommand(string.Format("{0} X{1} Y{2} S0", skipcmd, formatnumber((double)curX), formatnumber((double)curY))));
						else
							rv.Add(new GrblCommand(string.Format("{0} X{1} Y{2} {3}", skipcmd, formatnumber((double)curX), formatnumber((double)curY), c.lOff)));

						//curX = curY = 0;
					}

					if (cmd.IsMovement)
					{
						if (cmd.X != null) curX = cmd.X.Number;
						if (cmd.Y != null) curY = cmd.Y.Number;
					}

					if (!cmd.IsMovement || !cumulate)
						rv.Add(cmd);
				}
				catch (Exception ex) { throw ex; }
				finally { cmd.DeleteHelper(); }
			}

			return rv;
		}
        private float Map(float value, float start1, float stop1, float start2, float stop2)
        {
            return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
        }

        private float GetBrightness(Color color)
        {
            return (color.R + color.G + color.B) / 3.0f;
        }
        Random r = new Random();
        private float Noise(float x, float y)
        {
            // 实现你的 noise 函数
            return (float)r.Next(50) / 100f;
        }
    //    private List<ColorSegment> GetSegmentsRandomLine(Bitmap bmp, L2LConf c)
    //    {
    //        bool uni = Settings.GetObject("Unidirectional Engraving", false);

    //        List<ColorSegment> rv = new List<ColorSegment>();
			
    //        if (!MustExitTH)
    //        {
               
    //        }
    //        if (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal || c.dir == RasterConverter.ImageProcessor.Direction.Vertical)
    //        {
    //            bool h = (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal); //horizontal/vertical水平垂直

    //            for (int i = 0; i < (h ? bmp.Height : bmp.Width); i++)
    //            //水平= i=row
    //            //--------
    //            //--------
    //            //--------
    //            //--------
    //            {
    //                bool d = uni || IsEven(i); //direct/reverse 正反 或单向绘制
    //                int prevCol = -1;
    //                int len = -1;

    //                for (int j = d ? 0 : (h ? bmp.Width - 1 : bmp.Height - 1); d ? (j < (h ? bmp.Width : bmp.Height)) : (j >= 0); j = (d ? j + 1 : j - 1))
    //                    //水平绘制 x= 
    //                    ExtractSegment(bmp, h ? j : i, h ? i : j, !d, ref len, ref prevCol, rv, c); //extract different segments

    //                if (h)
    //                    rv.Add(new XSegment(prevCol, len + 1, !d)); //close last segment
    //                else
    //                    rv.Add(new YSegment(prevCol, len + 1, !d)); //close last segment

    //                if (uni) // add "go back"
    //                {
    //                    if (h) rv.Add(new XSegment(0, bmp.Width, true));
    //                    else rv.Add(new YSegment(0, bmp.Height, true));
    //                }

    //                if (i < (h ? bmp.Height - 1 : bmp.Width - 1))
    //                {
    //                    if (h)
    //                        rv.Add(new VSeparator()); //new line
    //                    else
    //                        rv.Add(new HSeparator()); //new line
    //                }
    //            }
    //        }
    //        else if (c.dir == RasterConverter.ImageProcessor.Direction.Diagonal)
    //        {
    //            //based on: http://stackoverflow.com/questions/1779199/traverse-matrix-in-diagonal-strips
    //            //based on: http://stackoverflow.com/questions/2112832/traverse-rectangular-matrix-in-diagonal-strips

    //            /*

				//+------------+
				//|  -         |
				//|  -  -      |
				//+-------+    |
				//|  -  - |  - |
				//+-------+----+

				//*/


    //            //the algorithm runs along the matrix for diagonal lines (slice index)
    //            //z1 and z2 contains the number of missing elements in the lower right and upper left
    //            //the length of the segment can be determined as "slice - z1 - z2"
    //            //my modified version of algorithm reverses travel direction each slice

    //            rv.Add(new VSeparator()); //new line

    //            int w = bmp.Width;
    //            int h = bmp.Height;
    //            for (int slice = 0; slice < w + h - 1; ++slice)
    //            {
    //                bool d = uni || IsEven(slice); //direct/reverse

    //                int prevCol = -1;
    //                int len = -1;

    //                int z1 = slice < h ? 0 : slice - h + 1;
    //                int z2 = slice < w ? 0 : slice - w + 1;

    //                for (int j = (d ? z1 : slice - z2); d ? j <= slice - z2 : j >= z1; j = (d ? j + 1 : j - 1))
    //                    ExtractSegment(bmp, j, slice - j, !d, ref len, ref prevCol, rv, c); //extract different segments
    //                rv.Add(new DSegment(prevCol, len + 1, !d)); //close last segment

    //                //System.Diagnostics.Debug.WriteLine(String.Format("sl:{0} z1:{1} z2:{2}", slice, z1, z2));

    //                if (uni) // add "go back"
    //                {
    //                    int slen = (slice - z1 - z2) + 1;
    //                    rv.Add(new DSegment(0, slen, true));
    //                    //System.Diagnostics.Debug.WriteLine(slen);
    //                }

    //                if (slice < Math.Min(w, h) - 1) //first part of the image
    //                {
    //                    if (d && !uni)
    //                        rv.Add(new HSeparator()); //new line
    //                    else
    //                        rv.Add(new VSeparator()); //new line
    //                }
    //                else if (slice >= Math.Max(w, h) - 1) //third part of image
    //                {
    //                    if (d && !uni)
    //                        rv.Add(new VSeparator()); //new line
    //                    else
    //                        rv.Add(new HSeparator()); //new line
    //                }
    //                else //central part of the image
    //                {
    //                    if (w > h)
    //                        rv.Add(new HSeparator()); //new line
    //                    else
    //                        rv.Add(new VSeparator()); //new line
    //                }
    //            }
    //        }

    //        return rv;
    //    }
        private List<ColorSegment> GetSegments(Bitmap bmp, L2LConf c)
		{
			bool uni = Settings.GetObject("Unidirectional Engraving", false);

			List<ColorSegment> rv = new List<ColorSegment>();
			if (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal || c.dir == RasterConverter.ImageProcessor.Direction.Vertical)
			{
				bool h = (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal); //horizontal/vertical水平垂直

                for (int i = 0; i < (h ? bmp.Height : bmp.Width); i++)
                //水平= i=row
                //--------
                //--------
                //--------
                //--------
                {
                    bool d = uni || IsEven(i); //direct/reverse 正反 或单向绘制
					int prevCol = -1;
					int len = -1;

					for (int j = d ? 0 : (h ? bmp.Width - 1 : bmp.Height - 1); d ? (j < (h ? bmp.Width : bmp.Height)) : (j >= 0); j = (d ? j + 1 : j - 1))
						//水平绘制 x= 
						ExtractSegment(bmp, h ? j : i, h ? i : j, !d, ref len, ref prevCol, rv, c); //提取不同的片段

					if (h)
						rv.Add(new XSegment(prevCol, len + 1, !d)); //关闭最后一段
					else
						rv.Add(new YSegment(prevCol, len + 1, !d)); //关闭最后一段

					if (uni) // 添加“返回”
					{
						if (h) rv.Add(new XSegment(0, bmp.Width, true));
						else rv.Add(new YSegment(0, bmp.Height, true));
					}

					if (i < (h ? bmp.Height - 1 : bmp.Width - 1))
					{
						if (h)
							rv.Add(new VSeparator()); //新行
						else
							rv.Add(new HSeparator()); //新行
					}
				}
			}
			else if (c.dir == RasterConverter.ImageProcessor.Direction.Diagonal)
			{
				//based on: http://stackoverflow.com/questions/1779199/traverse-matrix-in-diagonal-strips
				//based on: http://stackoverflow.com/questions/2112832/traverse-rectangular-matrix-in-diagonal-strips

				/*

				+------------+
				|  -         |
				|  -  -      |
				+-------+    |
				|  -  - |  - |
				+-------+----+

				*/


				//的算法沿着矩阵对角线(切片索引)
				//z1, z2包含缺失的元素的数量在右下角,左上角
				//线段的长度可以确定为“片- z1 z2”
				//我的修改版本每个切片算法改变旅行的方向

				rv.Add(new VSeparator()); //new line

				int w = bmp.Width;
				int h = bmp.Height;
				for (int slice = 0; slice < w + h - 1; ++slice)
				{
					bool d = uni || IsEven(slice); //direct/reverse

					int prevCol = -1;
					int len = -1;

					int z1 = slice < h ? 0 : slice - h + 1;
					int z2 = slice < w ? 0 : slice - w + 1;

					for (int j = (d ? z1 : slice - z2); d ? j <= slice - z2 : j >= z1; j = (d ? j + 1 : j - 1))
						ExtractSegment(bmp, j, slice - j, !d, ref len, ref prevCol, rv, c); //extract different segments
					rv.Add(new DSegment(prevCol, len + 1, !d)); //close last segment

					//System.Diagnostics.Debug.WriteLine(String.Format("sl:{0} z1:{1} z2:{2}", slice, z1, z2));

					if (uni) // add "go back"
					{
						int slen = (slice - z1 - z2) + 1;
						rv.Add(new DSegment(0, slen, true));
						//System.Diagnostics.Debug.WriteLine(slen);
					}

					if (slice < Math.Min(w, h) - 1) //first part of the image
					{
						if (d && !uni)
							rv.Add(new HSeparator()); //new line
						else
							rv.Add(new VSeparator()); //new line
					}
					else if (slice >= Math.Max(w, h) - 1) //third part of image
					{
						if (d && !uni)
							rv.Add(new VSeparator()); //new line
						else
							rv.Add(new HSeparator()); //new line
					}
					else //central part of the image
					{
						if (w > h)
							rv.Add(new HSeparator()); //new line
						else
							rv.Add(new VSeparator()); //new line
					}
				}
			}

			return rv;
		}

		private void ExtractSegment(Bitmap image, int x, int y, bool reverse, ref int len, ref int prevCol, List<ColorSegment> rv, L2LConf c)
		{
			len++;
			int col = GetColor(image, x, y, c.minPower, c.maxPower, c.pwm);
			if (prevCol == -1)
				prevCol = col;

			if (prevCol != col)
			{
				if (c.dir == RasterConverter.ImageProcessor.Direction.Horizontal)
					rv.Add(new XSegment(prevCol, len, reverse));
				else if (c.dir == RasterConverter.ImageProcessor.Direction.Vertical)
					rv.Add(new YSegment(prevCol, len, reverse));
				else if (c.dir == RasterConverter.ImageProcessor.Direction.Diagonal)
					rv.Add(new DSegment(prevCol, len, reverse));

				len = 0;
			}

			prevCol = col;
		}

		private List<List<Curve>> ParallelOptimizePaths(List<List<Curve>> list, double changecost)
		{
			if (list == null || list.Count <= 1)
				return list;

			int maxblocksize = 2048;    //max number of List<Curve> to process in a single OptimizePaths operation

			int blocknum = (int)Math.Ceiling(list.Count / (double)maxblocksize);
			if (blocknum <= 1)
				return OptimizePaths(list, changecost);

			System.Diagnostics.Debug.WriteLine("Count: " + list.Count);

			Task<List<List<Curve>>>[] taskArray = new Task<List<List<Curve>>>[blocknum];
			for (int i = 0; i < taskArray.Length; i++)
				taskArray[i] = Task.Factory.StartNew((data) => OptimizePaths((List<List<Curve>>)data, changecost), GetTaskJob(i, taskArray.Length, list));
			Task.WaitAll(taskArray);

			List<List<Curve>> rv = new List<List<Curve>>();
			for (int i = 0; i < taskArray.Length; i++)
			{
				List<List<Curve>> lc = taskArray[i].Result;
				rv.AddRange(lc);
			}

			return rv;
		}

		private List<List<Curve>> GetTaskJob(int threadIndex, int threadCount, List<List<Curve>> list)
		{
			int from = (threadIndex * list.Count) / threadCount;
			int to = ((threadIndex + 1) * list.Count) / threadCount;

			List<List<Curve>> rv = list.GetRange(from, to - from);
			System.Diagnostics.Debug.WriteLine($"Thread {threadIndex}/{threadCount}: {rv.Count} [from {from} to {to}]");
			return rv;
		}

		private List<List<Curve>> OptimizePaths(List<List<Curve>> list, double changecost)
		{
			if (list.Count <= 1)
				return list;


			dPoint Origin = new dPoint(0, 0);
			int nearestToZero = 0;
			double bestDistanceToZero = Double.MaxValue;

			double[,] costs = new double[list.Count, list.Count];   //从曲线1的终点到曲线2的起点的二维旅行成本数组
            for (int c1 = 0; c1 < list.Count; c1++)                 //循环due volte在曲线列表
            {
				dPoint c1fa = list[c1].First().A;   //percorso第一段的起始点(每calcolo距离dallo零)
                                                    //dPoint c1la = list[c1].Last().A;	//percorso l'ulimo段的起始点(每calcolo direzione di uscita)
                dPoint c1lb = list[c1].Last().B;    //最后一段旅程的终点(计算到旅程的距离和游客和门票的方向)


                for (int c2 = 0; c2 < list.Count; c2++)             //有适当的指示diversi c1, c2
                {
					dPoint c2fa = list[c2].First().A;     //percorso第一段的起始点(距离percorsi和入口方向的计算)
                                                          //dPoint c2fb = list[c2].First().B;     //percorso第一段的终点(继续计算)

                    if (c1 == c2)
						costs[c1, c2] = double.MaxValue;  //点与stesso的距离(简并情况)
                    else
						costs[c1, c2] = SquareDistance(c1lb, c2fa); //TravelCost(c1la, c1lb, c2fa, c2fb, changecost);
				}

                //你会发现这是最糟糕的部分
                double distZero = SquareDistanceZero(c1fa);
				if (distZero < bestDistanceToZero)
				{
					nearestToZero = c1;
					bestDistanceToZero = distZero;
				}
			}

			//创建一个列表未浏览的地方
			List<int> unvisited = Enumerable.Range(0, list.Count).ToList();

			//选择最近的点
			List<List<CsPotrace.Curve>> bestPath = new List<List<Curve>>();

            //派对的个人是“最糟糕的零”
            bestPath.Add(list[nearestToZero]);
			unvisited.Remove(nearestToZero);
			int lastIndex = nearestToZero;
			
			while (unvisited.Count > 0)
			{
				int bestIndex = 0;
				double bestDistance = double.MaxValue;

				foreach (int nextIndex in unvisited)                    //所有关于“未访问”的信息
                {
					double dist = costs[lastIndex, nextIndex];
					if (dist < bestDistance)
					{
						bestIndex = nextIndex;                    //保存bestIndex

                        bestDistance = dist;                      //保存到risultato migliore                        
                    }
				}

				bestPath.Add(list[bestIndex]);
				unvisited.Remove(bestIndex);

				//保存最近的点
				lastIndex = bestIndex;                   //最后一个最好的trovato指数在接下来的分析点


            }

            return bestPath;
		}

		////questa funzione calcola il "costo" di un cambio di direzione
		////in termini di distanza che sarebbe possibile percorrere
		////nel tempo di una decelerazione da velocità di marcatura, a zero 
		//private double ComputeDirectionChangeCost(L2LConf c, GrblCore core, bool border)
		//{
		//	double speed = (border ? c.borderSpeed : c.markSpeed) / 60.0; //velocità di marcatura (mm/sec)
		//	double accel = core.Configuration != null ? (double)core.Configuration.AccelerationXY : 2000; //acceleration (mm/sec^2)
		//	double cost = (speed * speed) / (2 * accel); //(mm)
		//	cost = cost * c.res; //mm tradotti nella risoluzione immagine

		//	return cost;
		//}

		//private double TravelCost(dPoint s1a, dPoint s1b, dPoint s2a, dPoint s2b, double changecost)
		//{
		//	double d = Math.Sqrt(SquareDistance(s1b, s2a));
		//	double a1 = DirectionChange(s1a, s1b, s2a);
		//	double a2 = DirectionChange(s1b, s2a, s2b);
		//	double cd = d + changecost * a1 + changecost * a2;

		//	//System.Diagnostics.Debug.WriteLine($"{d}\t{a1}\t{a2}\t{cd}");
		//	return cd;
		//}

		private static double SquareDistance(dPoint a, dPoint b)
		{
			double dX = b.X - a.X;
			double dY = b.Y - a.Y;
			return ((dX * dX) + (dY * dY));
		}
		private static double SquareDistanceZero(dPoint a)
		{
			return ((a.X * a.X) + (a.Y * a.Y));
		}

		//questo metodo ritorna un fattore 0 se c'è continuità di direzione, 0.5 su angolo 90°, 1 se c'è inversione totale (180°)
		private double DirectionChange(dPoint p1, dPoint p2, dPoint p3)
		{
			double angleA = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X); //angolo del segmento corrente
			double angleB = Math.Atan2(p3.Y - p2.Y, p3.X - p2.X); //angolo della retta congiungente

			double angleAB = Math.Abs(Math.Abs(angleB) - Math.Abs(angleA)) ; //0 se stessa direzione, pigreco se inverte direzione
			double factor = angleAB / Math.PI;
			return factor;
		}


		private int GetColor(Bitmap I, int X, int Y, int min, int max, bool pwm)
		{
			Color C = I.GetPixel(X, Y);
			int rv = (255 - C.R) * C.A / 255;

			if (rv == 0)
				return 0; //zero is always zero
			else if (pwm)
				return rv * (max - min) / 255 + min; //scale to range
			else
				return rv;
		}
        public string formatnumber(int number, float offset, L2LConf c)
        {
            double dval = Math.Round(number / (c.vectorfilling ? c.fres : c.res) + offset, 3);
            return dval.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        public string formatnumber(double number)
		{ return number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }

		private static bool IsEven(int value)
		{ return value % 2 == 0; }

		public int Count
		{ get { return list.Count; } }

		public TimeSpan EstimatedTime { get { return mEstimatedTotalTime; } }


		//  II | I
		// ---------
		// III | IV
		public CartesianQuadrant Quadrant
		{
			get
			{
				if (!mRange.DrawingRange.ValidRange)
					return CartesianQuadrant.Unknown;
				else if (mRange.DrawingRange.X.Min >= 0 && mRange.DrawingRange.Y.Min >= 0)
					return CartesianQuadrant.I;
				else if (mRange.DrawingRange.X.Max <= 0 && mRange.DrawingRange.Y.Min >= 0)
					return CartesianQuadrant.II;
				else if (mRange.DrawingRange.X.Max <= 0 && mRange.DrawingRange.Y.Max <= 0)
					return CartesianQuadrant.III;
				else if (mRange.DrawingRange.X.Min >= 0 && mRange.DrawingRange.Y.Max <= 0)
					return CartesianQuadrant.IV;
				else
					return CartesianQuadrant.Mix;
			}
		}

		internal void DrawOnGraphics(Graphics g, Size size)
		{
			if (!mRange.MovingRange.ValidRange) return;

			GrblCommand.StatePositionBuilder spb = new GrblCommand.StatePositionBuilder();
			ProgramRange.XYRange scaleRange = mRange.MovingRange;

            //得到两个方向的比例因子。要保持长宽比，请使用较小的比例因子。Get scale factors for both directions. To preserve the aspect ratio, use the smaller scale factor.
            float zoom = scaleRange.Width > 0 && scaleRange.Height > 0 ? Math.Min((float)size.Width / (float)scaleRange.Width, (float)size.Height / (float)scaleRange.Height) * 0.95f : 1;


			ScaleAndPosition(g, size, scaleRange, zoom);
			DrawJobPreview(g, spb, zoom);
			DrawJobRange(g, size, zoom);

		}
		/// <summary>
		/// 预览渲染
		/// </summary>
		/// <param name="g"></param>
		/// <param name="spb"></param>
		/// <param name="zoom"></param>
		private void DrawJobPreview(Graphics g, GrblCommand.StatePositionBuilder spb, float zoom)
		{
			bool firstline = true; //used to draw the first line in a different color
			foreach (GrblCommand cmd in list)
			{
				try
				{
					cmd.BuildHelper();
					spb.AnalyzeCommand(cmd, false);


					if (spb.TrueMovement())
					{
						//Color linecolor = Color.FromArgb(spb.GetCurrentAlpha(mRange.SpindleRange), firstline ? ColorScheme.PreviewFirstMovement : spb.LaserBurning ? ColorScheme.PreviewLaserPower : ColorScheme.PreviewOtherMovement);

						using (Pen pen = GetPen(cmd.G?.ToString()=="G0"? Color.LightGray: cmd.penColor))
						{
							pen.ScaleTransform(1 / zoom, 1 / zoom);


							if (cmd.G?.ToString() == "G0" && !spb.LaserBurning)
							{
								pen.DashStyle = DashStyle.Dash;
								pen.Width = 1;
							}
							else if (!spb.LaserBurning)
							{
								//pen.DashStyle = DashStyle.Dash;
								//pen.DashPattern = new float[] { 10f, 10f };
								pen.Width = 1;
							}
							else {

                                pen.Width = 2;
                            }

                            if (spb.G0G1 && cmd.IsLinearMovement && pen.Color.A > 0)
							{
								g.DrawLine(pen, new PointF((float)spb.X.Previous, (float)spb.Y.Previous), new PointF((float)spb.X.Number, (float)spb.Y.Number));
							}
							else if (spb.G2G3 && cmd.IsArcMovement && pen.Color.A > 0)
							{
								GrblCommand.G2G3Helper ah = spb.GetArcHelper(cmd);

								if (ah.RectW > 0 && ah.RectH > 0)
								{
									try { g.DrawArc(pen, (float)ah.RectX, (float)ah.RectY, (float)ah.RectW, (float)ah.RectH, (float)(ah.StartAngle * 180 / Math.PI), (float)(ah.AngularWidth * 180 / Math.PI)); }
									catch { System.Diagnostics.Debug.WriteLine(String.Format("Ex drwing arc: W{0} H{1}", ah.RectW, ah.RectH)); }
								}
							}

						}

						firstline = false;
					}
				}
				catch (Exception ex) { throw ex; }
				finally { cmd.DeleteHelper(); }
			}
		}

		internal void LoadImageCenterline(Bitmap bmp, string filename, bool useCornerThreshold, int cornerThreshold, bool useLineThreshold, int lineThreshold, L2LConf conf, bool append, GrblCore core)
		{

			RiseOnFileLoading(filename);

			long start = Tools.HiResTimer.TotalMilliseconds;

			if (!append)
				list.Clear();

			mRange.ResetRange();

			string content = "";
            try
            {
                string optimizedSvgText = (Autotrace.BitmapToSvgString(bmp, useCornerThreshold, cornerThreshold, useLineThreshold, lineThreshold));//SvgOptimizer.OptimizeSvg
                content = SvgOptimizer.OptimizeSvg(optimizedSvgText,conf.optimizeSVG);
            }
			catch (Exception ex) { Logger.LogException("Centerline", ex); }

			SvgConverter.GCodeFromSVG converter = new SvgConverter.GCodeFromSVG();
			converter.GCodeXYFeed = Settings.GetObject("GrayScaleConversion.VectorizeOptions.BorderSpeed", 3000);
			converter.SvgScaleApply = true;
			converter.SvgMaxSize = (float)Math.Max(bmp.Width / 10.0, bmp.Height / 10.0);
			converter.UserOffset.X = Settings.GetObject("GrayScaleConversion.Gcode.Offset.X", 0F);
			converter.UserOffset.Y = Settings.GetObject("GrayScaleConversion.Gcode.Offset.Y", 0F);
			converter.UseLegacyBezier = !Settings.GetObject($"Vector.UseSmartBezier", true);

			string gcode = converter.convertFromText(content, core);
			string[] lines = gcode.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
           // ShortestPathOptimizer.OptimizePath(lines);
            foreach (string l in lines)
			{
				string line = l;
				if ((line = line.Trim()).Length > 0)
				{
					GrblCommand cmd = new GrblCommand(line);
					if (!cmd.IsEmpty)
						list.Add(cmd);
				}
			}

			Analyze();
			long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

			RiseOnFileLoaded(filename, elapsed);

		}
        public void LoadImageRandomLine(Bitmap bmp, string filename, L2LConf c, bool append, GrblCore core)
        {

            skipcmd = Settings.GetObject("Disable G0 fast skip", false) ? "G1" : "G0";

            RiseOnFileLoading(filename);

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            long start = Tools.HiResTimer.TotalMilliseconds;

            if (!append)
                list.Clear();

            mRange.ResetRange();

            //absolute
            //list.Add(new GrblCommand("G90")); //(Moved to custom Header)

            //快速移动到offset(如果禁用G0则缓慢移动)，并设置标记速度
            list.Add(new GrblCommand(String.Format("{0} X{1} Y{2} F{3}", skipcmd, formatnumber(c.oX), formatnumber(c.oY), c.markSpeed)));
            if (c.pwm)
                list.Add(new GrblCommand(String.Format("{0} S0", c.lOn))); //激光打开，功率归零laser on and power to zero
            else
                list.Add(new GrblCommand($"{c.lOff} S{GrblCore.Configuration.MaxPWM}")); //关闭激光，将电源调至最大功率laser off and power to maxpower

            //设置速度为markspeed						
            // For marlin, need to specify G1 each time :
            //list.Add(new GrblCommand(String.Format("G1 F{0}", c.markSpeed)));
            //list.Add(new GrblCommand(String.Format("F{0}", c.markSpeed))); //replaced by the first move to offset and set speed

            ImageRandomLine(bmp, c);

            //laser off
            list.Add(new GrblCommand(c.lOff));

            //move fast to origin
            //list.Add(new GrblCommand("G0 X0 Y0")); //moved to custom footer

            Analyze();
            long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

            RiseOnFileLoaded(filename, elapsed);
        }
        public void LoadImageOneLine(Bitmap bmp, string filename, L2LConf c, bool append, GrblCore core)
        {

            skipcmd = Settings.GetObject("Disable G0 fast skip", false) ? "G1" : "G0";

            RiseOnFileLoading(filename);

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            long start = Tools.HiResTimer.TotalMilliseconds;

            if (!append)
                list.Clear();

            mRange.ResetRange();

            //absolute
            //list.Add(new GrblCommand("G90")); //(Moved to custom Header)

            //快速移动到offset(如果禁用G0则缓慢移动)，并设置标记速度
            list.Add(new GrblCommand(String.Format("{0} X{1} Y{2} F{3}", skipcmd, formatnumber(c.oX), formatnumber(c.oY), c.markSpeed)));
            if (c.pwm)
                list.Add(new GrblCommand(String.Format("{0} S0", c.lOn))); //激光打开，功率归零laser on and power to zero
            else
                list.Add(new GrblCommand($"{c.lOff} S{GrblCore.Configuration.MaxPWM}")); //关闭激光，将电源调至最大功率laser off and power to maxpower

            //设置速度为markspeed						
            // For marlin, need to specify G1 each time :
            //list.Add(new GrblCommand(String.Format("G1 F{0}", c.markSpeed)));
            //list.Add(new GrblCommand(String.Format("F{0}", c.markSpeed))); //replaced by the first move to offset and set speed

            ImageOneLine(bmp, c);

            //laser off
            list.Add(new GrblCommand(c.lOff));

            //move fast to origin
            //list.Add(new GrblCommand("G0 X0 Y0")); //moved to custom footer

            Analyze();
            long elapsed = Tools.HiResTimer.TotalMilliseconds - start;

            RiseOnFileLoaded(filename, elapsed);
        }
        private void Analyze() //分析文件,并为每个命令构建全球范围和时机
		{
			GrblCommand.StatePositionBuilder spb = new GrblCommand.StatePositionBuilder();

			mRange.ResetRange();
			mRange.UpdateXYRange("X0", "Y0", false);
			mEstimatedTotalTime = TimeSpan.Zero;
			GrblCommand prevCommand = new GrblCommand("G0 Z5");

            for ( int i=0;i< list.Count;i++)
            {
                GrblCommand cmd = list[i];
                try
				{

                    GrblConfST conf = GrblCore.Configuration;
                    if (cmd.Command.Contains("G0") && !cmd.Command.Contains("Z") && !prevCommand.Command.Contains("Z5"))
					{
						list.Insert(i, new GrblCommand("G0 Z5"));
						cmd = list[i];
					}
					else if(cmd.Command.Contains("Z5")&& list.Count>i +1) 
					{
						if (spb.AnalyzeCommand(list[i+1], true, conf).Milliseconds < 50) {
                            list.RemoveAt(i);
                            cmd = list[i];
                        }
					}
					prevCommand = cmd;
					TimeSpan delay = spb.AnalyzeCommand(cmd, true, conf);

					mRange.UpdateSRange(spb.S);

					if (spb.LastArcHelperResult != null)
						mRange.UpdateXYRange(spb.LastArcHelperResult.BBox.X, spb.LastArcHelperResult.BBox.Y, spb.LastArcHelperResult.BBox.Width, spb.LastArcHelperResult.BBox.Height, spb.LaserBurning);
					else
						mRange.UpdateXYRange(spb.X, spb.Y, spb.LaserBurning);

					mEstimatedTotalTime += delay;
					cmd.SetOffset(mEstimatedTotalTime);
				}
				catch (Exception ex) { throw ex; }
				finally { cmd.DeleteHelper(); }
			}
		}

		private void ScaleAndPosition(Graphics g, Size s, ProgramRange.XYRange scaleRange, float zoom)
		{
			g.ResetTransform();
			float margin = 10;
			CartesianQuadrant q = Quadrant;
			if (q == CartesianQuadrant.Unknown || q == CartesianQuadrant.I)
			{
				//Scale and invert Y
				g.ScaleTransform(zoom, -zoom, MatrixOrder.Append);
				//Translate to position bottom-left
				g.TranslateTransform(margin, s.Height - margin, MatrixOrder.Append);
			}
			else if (q == CartesianQuadrant.II)
			{
				//Scale and invert Y
				g.ScaleTransform(zoom, -zoom, MatrixOrder.Append);
				//Translate to position bottom-left
				g.TranslateTransform(s.Width - margin, s.Height - margin, MatrixOrder.Append);
			}
			else if (q == CartesianQuadrant.III)
			{
				//Scale and invert Y
				g.ScaleTransform(zoom, -zoom, MatrixOrder.Append);
				//Translate to position bottom-left
				g.TranslateTransform(s.Width - margin, margin, MatrixOrder.Append);
			}
			else if (q == CartesianQuadrant.IV)
			{
				//Scale and invert Y
				g.ScaleTransform(zoom, -zoom, MatrixOrder.Append);
				//Translate to position bottom-left
				g.TranslateTransform(margin, margin, MatrixOrder.Append);
			}
			else
			{
				//Translate to center of gravity of the image
				g.TranslateTransform(-scaleRange.Center.X, -scaleRange.Center.Y, MatrixOrder.Append);
				//Scale and invert Y
				g.ScaleTransform(zoom, -zoom, MatrixOrder.Append);
				//Translate to center over the drawing area.
				g.TranslateTransform(s.Width / 2, s.Height / 2, MatrixOrder.Append);
			}

		}

		private void DrawJobRange(Graphics g, Size s, float zoom)
		{
			//RectangleF frame = new RectangleF(-s.Width / zoom, -s.Height / zoom, s.Width / zoom, s.Height / zoom);

			SizeF wSize = new SizeF(s.Width / zoom, s.Height / zoom);

			//draw cartesian plane
			using (Pen pen = GetPen(ColorScheme.PreviewText))
			{
				pen.ScaleTransform(1 / zoom, 1 / zoom);
				g.DrawLine(pen, -wSize.Width, 0.0f, wSize.Width, 0.0f);
				g.DrawLine(pen, 0, -wSize.Height, 0, wSize.Height);
			}

			//draw job range
			if (mRange.DrawingRange.ValidRange)
			{
				using (Pen pen = GetPen(ColorScheme.PreviewJobRange))
				{
					pen.DashStyle = DashStyle.Solid;
					pen.DashPattern = new float[] { 1.0f / zoom, 2.0f / zoom }; //pen.DashPattern = new float[] { 1f / zoom, 2f / zoom};
					pen.ScaleTransform(1.0f / zoom, 1.0f / zoom);

					g.DrawLine(pen, -wSize.Width, (float)mRange.DrawingRange.Y.Min, wSize.Width, (float)mRange.DrawingRange.Y.Min);
					g.DrawLine(pen, -wSize.Width, (float)mRange.DrawingRange.Y.Max, wSize.Width, (float)mRange.DrawingRange.Y.Max);
					g.DrawLine(pen, (float)mRange.DrawingRange.X.Min, -wSize.Height, (float)mRange.DrawingRange.X.Min, wSize.Height);
					g.DrawLine(pen, (float)mRange.DrawingRange.X.Max, -wSize.Height, (float)mRange.DrawingRange.X.Max, wSize.Height);

					CartesianQuadrant q = Quadrant;
					bool right = q == CartesianQuadrant.I || q == CartesianQuadrant.IV;
					bool top = q == CartesianQuadrant.I || q == CartesianQuadrant.II;

					string format = "0";
					if (mRange.DrawingRange.Width < 50 && mRange.DrawingRange.Height < 50)
						format = "0.0";

					DrawString(g, zoom, 0, mRange.DrawingRange.Y.Min, mRange.DrawingRange.Y.Min.ToString(format), false, true, !right, false, ColorScheme.PreviewText);
					DrawString(g, zoom, 0, mRange.DrawingRange.Y.Max, mRange.DrawingRange.Y.Max.ToString(format), false, true, !right, false, ColorScheme.PreviewText);
					DrawString(g, zoom, mRange.DrawingRange.X.Min, 0, mRange.DrawingRange.X.Min.ToString(format), true, false, false, top, ColorScheme.PreviewText);
					DrawString(g, zoom, mRange.DrawingRange.X.Max, 0, mRange.DrawingRange.X.Max.ToString(format), true, false, false, top, ColorScheme.PreviewText);
				}
			}

			//draw ruler
			using (Pen pen = GetPen(ColorScheme.PreviewRuler))
			{
				//pen.DashStyle = DashStyle.Dash;
				//pen.DashPattern = new float[] { 1.0f / zoom, 2.0f / zoom }; //pen.DashPattern = new float[] { 1f / zoom, 2f / zoom};
				pen.ScaleTransform(1.0f / zoom, 1.0f / zoom);
				CartesianQuadrant q = Quadrant;
				bool right = q == CartesianQuadrant.Unknown || q == CartesianQuadrant.I || q == CartesianQuadrant.IV; //l'oggetto si trova a destra
				bool top = q == CartesianQuadrant.Unknown || q == CartesianQuadrant.I || q == CartesianQuadrant.II; //l'oggetto si trova in alto

				string format = "0";

				if (mRange.DrawingRange.ValidRange && mRange.DrawingRange.Width < 50 && mRange.DrawingRange.Height < 50)
					format = "0.0";

				//scala orizzontale
				Tools.RulerStepCalculator hscale = new Tools.RulerStepCalculator(-wSize.Width, wSize.Width, (int)(2 * s.Width / 100));

				double h1 = (top ? -4.0 : 4.0) / zoom;
				double h2 = 1.8 * h1;
				double h3 = (top ? 1.0 : -1.0) / zoom;

				for (float d = (float)hscale.FirstSmall; d < wSize.Width; d += (float)hscale.SmallStep)
					g.DrawLine(pen, d, 0, d, (float)h1);

				for (float d = (float)hscale.FirstBig; d < wSize.Width; d += (float)hscale.BigStep)
					g.DrawLine(pen, d, 0, d, (float)h2);

				for (float d = (float)hscale.FirstBig; d < wSize.Width; d += (float)hscale.BigStep)
					DrawString(g, zoom, (decimal)d, (decimal)h3, d.ToString(format), false, false, !right, !top, ColorScheme.PreviewRuler);

				//scala verticale

				Tools.RulerStepCalculator vscale = new Tools.RulerStepCalculator(-wSize.Height, wSize.Height, (int)(2 * s.Height / 100));
				double v1 = (right ? -4.0 : 4.0) / zoom;
				double v2 = 1.8 * v1;
				double v3 = (right ? 2.5 : 0) / zoom;

				for (float d = (float)vscale.FirstSmall; d < wSize.Height; d += (float)vscale.SmallStep)
					g.DrawLine(pen, 0, d, (float)v1, d);

				for (float d = (float)vscale.FirstBig; d < wSize.Height; d += (float)vscale.BigStep)
					g.DrawLine(pen, 0, d, (float)v2, d);

				for (float d = (float)vscale.FirstBig; d < wSize.Height; d += (float)vscale.BigStep)
					DrawString(g, zoom, (decimal)v3, (decimal)d, d.ToString(format), false, false, right, !top, ColorScheme.PreviewRuler, -90);
			}
		}

		private Pen GetPen(Color color)
		{ return new Pen(color); }

		private static Brush GetBrush(Color color)
		{ return new SolidBrush(color); }

		private static void DrawString(Graphics g, float zoom, decimal curX, decimal curY, string text, bool centerX, bool centerY, bool subtractX, bool subtractY, Color color, float rotation = 0)
		{
			GraphicsState state = g.Save();
			g.ScaleTransform(1.0f, -1.0f);


			using (Font f = new Font(FontFamily.GenericMonospace, 8 * 1 / zoom))
			{
				float offsetX = 0;
				float offsetY = 0;

				SizeF ms = g.MeasureString(text, f);

				if (centerX)
					offsetX = ms.Width / 2;

				if (centerY)
					offsetY = ms.Height / 2;

				if (subtractX)
					offsetX += rotation == 0 ? ms.Width : ms.Height;

				if (subtractY)
					offsetY += rotation == 0 ? ms.Height : -ms.Width;

				using (Brush b = GetBrush(color))
				{ DrawRotatedTextAt(g, rotation, text, f, b, (float)curX - offsetX, (float)-curY - offsetY); }

			}
			g.Restore(state);
		}

		private static void DrawRotatedTextAt(Graphics g, float a, string text, Font f, Brush b, float x, float y)
		{
			GraphicsState state = g.Save(); // Save the graphics state.
			g.TranslateTransform(x, y);     //posiziona
			g.RotateTransform(a);           //ruota
			g.DrawString(text, f, b, 0, 0); // scrivi a zero, zero
			g.Restore(state);               // Restore the graphics state.
		}



		System.Collections.Generic.IEnumerator<GrblCommand> IEnumerable<GrblCommand>.GetEnumerator()
		{ return list.GetEnumerator(); }


		public System.Collections.IEnumerator GetEnumerator()
		{ return list.GetEnumerator(); }

		public ProgramRange Range { get { return mRange; } }

		public GrblCommand this[int index]
		{ get { return list[index]; } }

	}






	public class ProgramRange
	{
		public class XYRange
		{
			public class Range
			{
				public decimal Min;
				public decimal Max;

				public Range()
				{ ResetRange(); }

				public void UpdateRange(decimal val)
				{
					Min = Math.Min(Min, val);
					Max = Math.Max(Max, val);
				}

				public void ResetRange()
				{
					Min = decimal.MaxValue;
					Max = decimal.MinValue;
				}

				public bool ValidRange
				{ get { return Min != decimal.MaxValue && Max != decimal.MinValue; } }
			}

			public Range X = new Range();
			public Range Y = new Range();

			public void UpdateRange(GrblCommand.Element x, GrblCommand.Element y)
			{
				if (x != null) X.UpdateRange(x.Number);
				if (y != null) Y.UpdateRange(y.Number);
			}

			internal void UpdateRange(double rectX, double rectY, double rectW, double rectH)
			{
				X.UpdateRange((decimal)rectX);
				X.UpdateRange((decimal)(rectX + rectW));

				Y.UpdateRange((decimal)rectY);
				Y.UpdateRange((decimal)(rectY + rectH));
			}

			public void ResetRange()
			{
				X.ResetRange();
				Y.ResetRange();
			}

			public bool ValidRange
			{ get { return X.ValidRange && Y.ValidRange; } }

			public decimal Width
			{ get { return X.Max - X.Min; } }

			public decimal Height
			{ get { return Y.Max - Y.Min; } }

			public PointF Center
			{
				get
				{
					if (ValidRange)
						return new PointF((float)X.Min + (float)Width / 2.0f, (float)Y.Min + (float)Height / 2.0f);
					else
						return new PointF(0, 0);
				}
			}
		}

		public class SRange
		{
			public class Range
			{
				public decimal Min;
				public decimal Max;

				public Range()
				{ ResetRange(); }

				public void UpdateRange(decimal val)
				{
					Min = Math.Min(Min, val);
					Max = Math.Max(Max, val);
				}

				public void ResetRange()
				{
					Min = decimal.MaxValue;
					Max = decimal.MinValue;
				}

				public bool ValidRange
				{ get { return Min != Max && Min != decimal.MaxValue && Max != decimal.MinValue && Max > 0; } }
			}

			public Range S = new Range();

			public void UpdateRange(decimal s)
			{
				S.UpdateRange(s);
			}

			public void ResetRange()
			{
				S.ResetRange();
			}

			public bool ValidRange
			{ get { return S.ValidRange; } }
		}

		public XYRange DrawingRange = new XYRange();
		public XYRange MovingRange = new XYRange();
		public SRange SpindleRange = new SRange();

		public void UpdateXYRange(GrblCommand.Element X, GrblCommand.Element Y, bool drawing)
		{
			if (drawing) DrawingRange.UpdateRange(X, Y);
			MovingRange.UpdateRange(X, Y);
		}

		internal void UpdateXYRange(double rectX, double rectY, double rectW, double rectH, bool drawing)
		{
			if (drawing) DrawingRange.UpdateRange(rectX, rectY, rectW, rectH);
			MovingRange.UpdateRange(rectX, rectY, rectW, rectH);
		}

		public void UpdateSRange(GrblCommand.Element S)
		{ if (S != null) SpindleRange.UpdateRange(S.Number); }

		public void ResetRange()
		{
			DrawingRange.ResetRange();
			MovingRange.ResetRange();
			SpindleRange.ResetRange();
		}

	}
    public class GCodeLine
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        // Other properties that represent G-code information (e.g., G, M, F, etc.)

        public GCodeLine(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class ShortestPathOptimizer
    {
        public static List<GCodeLine> OptimizePath(List<GCodeLine> lines)
        {
            List<GCodeLine> optimizedLines = new List<GCodeLine>();

            GCodeLine currentPosition = new GCodeLine(0, 0, 0); // Starting position

            while (lines.Count > 0)
            {
                double minDistance = double.MaxValue;
                int minIndex = -1;

                for (int i = 0; i < lines.Count; i++)
                {
                    double distance = CalculateDistance(currentPosition, lines[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minIndex = i;
                    }
                }

                if (minIndex != -1)
                {
                    GCodeLine nextLine = lines[minIndex];
                    optimizedLines.Add(nextLine);
                    currentPosition = nextLine;
                    lines.RemoveAt(minIndex);
                }
            }

            return optimizedLines;
        }

        private static double CalculateDistance(GCodeLine point1, GCodeLine point2)
        {
            // Simple Euclidean distance calculation
            double dx = point2.X - point1.X;
            double dy = point2.Y - point1.Y;
            double dz = point2.Z - point1.Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
 
    public class SvgPathOptimizer
    {
        //public static SvgPathSegmentList OptimizePath(SvgPathSegmentList segments)
        //{
        //    SvgPathSegmentList optimizedSegments = new SvgPathSegmentList();

        //    List<PointF> points = new List<PointF>();

        //    // 每个段的开始点和结束点转换为点的列表
        //    foreach (var segment in segments)
        //    {
        //        points.Add(segment.Start);
        //        points.Add(segment.End);
        //    }

        //    // 找点的凸包
        //    List<PointF> convexHull = ChristofidesTSP.FindTSPPath(points);

        //    // 重组优化的部分
        //    for (int i = 0; i < convexHull.Count - 1; i++)
        //    {
        //        optimizedSegments.Add(new SvgLineSegment(new PointF(convexHull[i].X, convexHull[i].Y), new PointF(convexHull[i + 1].X, convexHull[i + 1].Y)));
        //    }

        //    // 连最后一点与第一点关闭路径
        //    optimizedSegments.Add(new SvgLineSegment(new PointF(convexHull[convexHull.Count - 1].X, convexHull[convexHull.Count - 1].Y), new PointF(convexHull[0].X, convexHull[0].Y)));

        //    return optimizedSegments;
        //}

        //凸包算法——格雷厄姆扫描
        private static List<PointF> GrahamScan(List<PointF> points)
        {
            // 找到最低的点坐标(如果系和左边的)
            PointF pivot = points[0];
            foreach (var point in points)
            {
                if (point.Y < pivot.Y || (point.Y == pivot.Y && point.X < pivot.X))
                {
                    pivot = point;
                }
            }

            // 对点的极角排序轴心点
            points.Sort((a, b) => CompareByPolarAngle(pivot, a, b));

            // 创建一个堆栈跟踪点的凸包
            Stack<PointF> stack = new Stack<PointF>();
            stack.Push(points[0]);
            stack.Push(points[1]);

            // Build the convex hull
            for (int i = 2; i < points.Count; i++)
            {
                while (stack.Count >= 2 && Orientation(stack.SecondFromTop(), stack.Peek(), points[i]) != 2)
                {
                    stack.Pop();
                }
                stack.Push(points[i]);
            }

            return new List<PointF>(stack);
        }
      
        // Compare points by polar angle from the pivot point
        private static int CompareByPolarAngle(PointF pivot, PointF a, PointF b)
        {
            int orientation = Orientation(pivot, a, b);
            if (orientation == 0)
            {
                // If points are collinear, choose the one closer to the pivot point
                double distA = DistanceSquared(pivot, a);
                double distB = DistanceSquared(pivot, b);
                return distA.CompareTo(distB);
            }
            return orientation;
        }

        // Calculate the orientation of three points (clockwise, counterclockwise, or collinear)
        private static int Orientation(PointF p, PointF q, PointF r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (Math.Abs(val) < 1e-6)
            {
                return 0; // Collinear
            }
            return (val > 0) ? 1 : 2; // Clockwise or Counterclockwise
        }

        // Calculate the squared distance between two points
        private static double DistanceSquared(PointF p1, PointF p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return dx * dx + dy * dy;
        }
		public static SvgPathSegmentList OptimizePath(SvgPathSegmentList segments, int optimizeSVGPercent)
		{
			SvgPathSegmentList optimizedSegments = new SvgPathSegmentList();
            SvgPathSegment currentPosition = new SvgMoveToSegment(new PointF(0, 0)); // Starting position
            List<SvgPathSegmentList> optimizedSegmentsList = new List<SvgPathSegmentList>();
			SvgPathSegmentList tempLi = new SvgPathSegmentList();
            for (int i = 0; i < segments.Count; i++)
			{
				if (segments[i] is SvgMoveToSegment&& tempLi.Count > 0)
				{
					//if( CalculateDistance(tempLi.Last, segments[i]) > 1000) { 
                    optimizedSegmentsList.Add(tempLi);
					tempLi = new SvgPathSegmentList() { segments[i] };
                    //}
                }
				else {
                    tempLi.Add(segments[i]);
                }

            }
            optimizedSegmentsList.Add(tempLi);
            while (optimizedSegmentsList.Count > 0)
			{
				double minDistance = double.MaxValue;
				int minIndex = -1;
				int seed = 1000;//(int)(segments.Count * (optimizeSVGPercent/100f))+1;

                int fNum = (segments.Count > seed ? seed : segments.Count);

                for (int i = 0; i < optimizedSegmentsList.Count; i++)
				{
					double distance = CalculateDistance(currentPosition, optimizedSegmentsList[i]);
					if (distance < minDistance)
					{
						minDistance = distance;
						minIndex = i;
					}
				}

				if (minIndex != -1)
				{
                    SvgPathSegmentList nextSegment = optimizedSegmentsList[minIndex];
					for (int i = 0; i < nextSegment.Count; i++)
					{
                        optimizedSegments.Add(nextSegment[i]);
                    }
					currentPosition = nextSegment.Last;
                    optimizedSegmentsList.RemoveAt(minIndex);
				}
			}

			return optimizedSegments;
		}

		private static double CalculateDistance(SvgPathSegment segment1, SvgPathSegmentList segment2)
        {
            // 简单的终点之间的欧氏距离计算第一段和第二段的起点
            double dx = segment2.FirstOrDefault().Start.X - segment1.End.X;
            double dy = segment2.FirstOrDefault().Start.Y - segment1.End.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }
        private static double CalculateDistance(SvgPathSegment segment1, SvgPathSegment segment2)
        {
            // 简单的终点之间的欧氏距离计算第一段和第二段的起点
            double dx = segment2.Start.X - segment1.End.X;
            double dy = segment2.Start.Y - segment1.End.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class Edge
    {
        public int Start { get; set; }
        public int End { get; set; }
        public double Weight { get; set; }

        public Edge(int start, int end, double weight)
        {
            Start = start;
            End = end;
            Weight = weight;
        }
    }

    public class ChristofidesTSP
    {
        public static List<PointF> FindTSPPath(List<PointF> points)
        {
            int numPoints = points.Count;
            double[][] distanceMatrix = new double[numPoints][];
            for (int i = 0; i < numPoints; i++)
            {
                distanceMatrix[i] = new double[numPoints];
                for (int j = 0; j < numPoints; j++)
                {
                    distanceMatrix[i][j] = Distance(points[i], points[j]);
                }
            }

            // 构建完全图
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < numPoints; i++)
            {
                for (int j = i + 1; j < numPoints; j++)
                {
                    edges.Add(new Edge(i, j, distanceMatrix[i][j]));
                }
            }

            // 1. 找到最小生成树
            List<Edge> mst = FindMinimumSpanningTree(edges, numPoints);

            // 2. 找到最小权重完备匹配
            List<Edge> mwpm = FindMinimumWeightPerfectMatching(points,mst, numPoints);

            // 合并最小生成树和最小权重完备匹配
            List<Edge> mergedEdges = mst.Concat(mwpm).ToList();

            // 3. 找到欧拉回路
            List<int> eulerianPath = FindEulerianPath(mergedEdges, numPoints);

            // 4. 从欧拉回路得到TSP路径
            List<PointF> tspPath = new List<PointF>();
            foreach (int node in eulerianPath)
            {
                tspPath.Add(points[node]);
            }

            return tspPath;
        }

        private static double Distance(PointF p1, PointF p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // 1. 找到最小生成树
        private static List<Edge> FindMinimumSpanningTree(List<Edge> edges, int numPoints)
        {
            List<Edge> mst = new List<Edge>();
            edges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            int[] parents = new int[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                parents[i] = i;
            }

            int numEdgesAdded = 0;
            foreach (Edge edge in edges)
            {
                int rootStart = FindRoot(parents, edge.Start);
                int rootEnd = FindRoot(parents, edge.End);
                if (rootStart != rootEnd)
                {
                    mst.Add(edge);
                    parents[rootStart] = rootEnd;
                    numEdgesAdded++;
                    if (numEdgesAdded == numPoints - 1)
                    {
                        break;
                    }
                }
            }

            return mst;
        }

        private static int FindRoot(int[] parents, int node)
        {
            if (parents[node] != node)
            {
                parents[node] = FindRoot(parents, parents[node]);
            }
            return parents[node];
        }

        // 2. 找到最小权重完备匹配
        private static List<Edge> FindMinimumWeightPerfectMatching(List<PointF> points, List<Edge> mst, int numPoints)
        {
            List<Edge> mwpm = new List<Edge>();
            List<int> oddDegreeNodes = new List<int>();
            int[] degreeCounts = new int[numPoints];

            // 计算每个节点的度数
            foreach (Edge edge in mst)
            {
                degreeCounts[edge.Start]++;
                degreeCounts[edge.End]++;
            }

            // 找到所有奇数度节点
            for (int i = 0; i < numPoints; i++)
            {
                if (degreeCounts[i] % 2 != 0)
                {
                    oddDegreeNodes.Add(i);
                }
            }

            // 通过两两匹配，构建最小权重完备匹配
            for (int i = 0; i < oddDegreeNodes.Count; i++)
            {
                int node1 = oddDegreeNodes[i];
                double minWeight = double.MaxValue;
                int minNode = -1;
                for (int j = i + 1; j < oddDegreeNodes.Count; j++)
                {
                    int node2 = oddDegreeNodes[j];
                    double weight = Distance(points[node1], points[node2]);
                    if (weight < minWeight)
                    {
                        minWeight = weight;
                        minNode = node2;
                    }
                }
                mwpm.Add(new Edge(node1, minNode, minWeight));
            }

            return mwpm;
        }

        // 3. 找到欧拉回路
        private static List<int> FindEulerianPath(List<Edge> edges, int numPoints)
        {
            Dictionary<int, List<int>> graph = new Dictionary<int, List<int>>();
            foreach (Edge edge in edges)
            {
                if (!graph.ContainsKey(edge.Start))
                {
                    graph[edge.Start] = new List<int>();
                }
                graph[edge.Start].Add(edge.End);

                if (!graph.ContainsKey(edge.End))
                {
                    graph[edge.End] = new List<int>();
                }
                graph[edge.End].Add(edge.Start);
            }

            List<int> eulerianPath = new List<int>();
            Stack<int> stack = new Stack<int>();
            stack.Push(0);

            while (stack.Count > 0)
            {
                int node = stack.Peek();
                if (graph[node].Count > 0)
                {
                    int nextNode = graph[node][0];
                    stack.Push(nextNode);
                    graph[node].Remove(nextNode);
                    graph[nextNode].Remove(node);
                }
                else
                {
                    eulerianPath.Insert(0, stack.Pop());
                }
            }

            return eulerianPath;
        }
    }
    public static class StackExtensions
    {
        public static T SecondFromTop<T>(this Stack<T> stack)
        {
            if (stack.Count < 2)
            {
                throw new InvalidOperationException("Stack has less than two elements.");
            }

            T top = stack.Pop();
            T secondTop = stack.Peek();
            stack.Push(top);

            return secondTop;
        }
    }

    public class SvgOptimizer
    {
        public static string OptimizeSvg(string svgText, int optimizeSVGPercent)
        {
            SvgDocument svgDocument = SvgDocument.FromSvg<SvgDocument>(svgText);

            if (svgDocument != null)
            {
                // 从SVG文档中提取所有路径
                var paths = svgDocument.Children.OfType<SvgPath>().ToList();

                // SVG路径转换为自定义路径段
                SvgPathSegmentList segments = ConvertToSvgPathSegments(paths);

                // 优化路径片段
                SvgPathSegmentList optimizedSegments = SvgPathOptimizer.OptimizePath(segments,  optimizeSVGPercent);

                // 重组优化SVG路径
                svgDocument.Children.Clear(); 
				svgDocument.Children.Add(new SvgPath()
                {
                    PathData = optimizedSegments,
                    Fill = SvgPaintServer.None,
                    Stroke = new SvgColourServer(System.Drawing.Color.Black),
                    StrokeWidth = 1
                });

                // Serialize the optimized SVG document back to SVG text
                using (var stream = new MemoryStream())
                {
                    svgDocument.Write(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return string.Empty;
        }

        private static SvgPathSegmentList ConvertToSvgPathSegments(List<SvgPath> paths)
        {
            SvgPathSegmentList segments = new SvgPathSegmentList();

            foreach (var path in paths)
            {
                SvgPathSegmentList d = path.Attributes["d"] as SvgPathSegmentList;
				if (segments == null || segments.Count == 0) {
					segments = d;
				}
				else {
					foreach (var segment in d) {
						segments.Add(segment);

                    }
				}
     //           if (d.Count > 0)
     //           {
                    
     //               int objCount = 0;
					//foreach (SvgPathSegment svgPath in d)
					//{
     //                   segments.Add(svgPath);
     // //                  var command = svgPath.Take(1).Single();
     // //                  char cmd = char.ToUpper(command);
     // //                  bool absolute = (cmd == command);
     // //                  string remainingargs = svgPath.Substring(1);
     // //                  string argSeparators = @"[\s,]|(?=(?<!e)-)";// @"[\s,]|(?=-)|(-{,2})";        // support also -1.2e-3 orig. @"[\s,]|(?=-)"; 
     // //                  var splitArgs = Regex
     // //                      .Split(remainingargs, argSeparators)
     // //                      .Where(t => !string.IsNullOrEmpty(t));
     // //                  // get command coordinates
     // //                  float[] floatArgs = splitArgs.Select(arg => ConvertToPixel(arg)).ToArray();
					//	//for (int i = 0; i < floatArgs.Length; i += 2)
					//	//{
					//	//	var currentX = floatArgs[i]; 
					//	//	var currentY = floatArgs[i + 1];
					//	//}
     //               }
     //           }
                //PointF start = path.PathData?.FirstOrDefault()?.Start ?? new PointF(0, 0);
                //PointF end = path.PathData?.LastOrDefault()?.End ?? new PointF(0, 0);

                
            }

            return segments;
        }
        private static float factor_In2Px = 96;
        private static float factor_Mm2Px = 96f / 25.4f;
        private static float factor_Cm2Px = 96f / 2.54f;
        private static float factor_Pt2Px = 96f / 72f;
        private static float factor_Pc2Px = 12 * 96f / 72f;
        private static float factor_Em2Px = 150;
        private static float ConvertToPixel(string str, float ext = 1)        // return value in px
        {       // https://www.w3.org/TR/SVG/coords.html#Units          // in=90 or 96 ???
            bool percent = false;
            //       Logger.Trace( "convert to pixel in {0}", str);
            float factor = 1;   // no unit = px
            if (str.IndexOf("mm") > 0) { factor = factor_Mm2Px; }               // Millimeter
            else if (str.IndexOf("cm") > 0) { factor = factor_Cm2Px; }          // Centimeter
            else if (str.IndexOf("in") > 0) { factor = factor_In2Px; }          // Inch    72, 90 or 96?
            else if (str.IndexOf("pt") > 0) { factor = factor_Pt2Px; }          // Point
            else if (str.IndexOf("pc") > 0) { factor = factor_Pc2Px; }          // Pica
            else if (str.IndexOf("em") > 0) { factor = factor_Em2Px; }          // Font size
            else if (str.IndexOf("%") > 0) { percent = true; }
            str = str.Replace("pt", "").Replace("pc", "").Replace("mm", "").Replace("cm", "").Replace("in", "").Replace("em ", "").Replace("%", "").Replace("px", "");
            double test;
            if (str.Length > 0)
            {
                if (percent)
                {
                    if (double.TryParse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out test))
                    { return ((float)test * ext / 100); }
                }
                else
                {
                    if (double.TryParse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out test))
                    { return ((float)test * factor); }
                }
            }

            return 0f;
        }
    }
}

/*
Gnnn	Standard GCode command, such as move to a point
Mnnn	RepRap-defined command, such as turn on a cooling fan
Tnnn	Select tool nnn. In RepRap, a tool is typically associated with a nozzle, which may be fed by one or more extruders.
Snnn	Command parameter, such as time in seconds; temperatures; voltage to send to a motor
Pnnn	Command parameter, such as time in milliseconds; proportional (Kp) in PID Tuning
Xnnn	A X coordinate, usually to move to. This can be an Integer or Fractional number.
Ynnn	A Y coordinate, usually to move to. This can be an Integer or Fractional number.
Znnn	A Z coordinate, usually to move to. This can be an Integer or Fractional number.
U,V,W	Additional axis coordinates (RepRapFirmware)
Innn	Parameter - X-offset in arc move; integral (Ki) in PID Tuning
Jnnn	Parameter - Y-offset in arc move
Dnnn	Parameter - used for diameter; derivative (Kd) in PID Tuning
Hnnn	Parameter - used for heater number in PID Tuning
Fnnn	Feedrate in mm per minute. (Speed of print head movement)
Rnnn	Parameter - used for temperatures
Qnnn	Parameter - not currently used
Ennn	Length of extrudate. This is exactly like X, Y and Z, but for the length of filament to consume.
Nnnn	Line number. Used to request repeat transmission in the case of communications errors.
;		Gcode comments begin at a semicolon
*/

/*
Supported G-Codes in v0.9i
G38.3, G38.4, G38.5: Probing
G40: Cutter Radius Compensation Modes
G61: Path Control Modes
G91.1: Arc IJK Distance Modes
Supported G-Codes in v0.9h
G38.2: Probing
G43.1, G49: Dynamic Tool Length Offsets
Supported G-Codes in v0.8 (and v0.9)
G0, G1: Linear Motions (G0 Fast, G1 Controlled)
G2, G3: Arc and Helical Motions
G4: Dwell
G10 L2, G10 L20: Set Work Coordinate Offsets
G17, G18, G19: Plane Selection
G20, G21: Units
G28, G30: Go to Pre-Defined Position
G28.1, G30.1: Set Pre-Defined Position
G53: Move in Absolute Coordinates
G54, G55, G56, G57, G58, G59: Work Coordinate Systems
G80: Motion Mode Cancel
G90, G91: Distance Modes
G92: Coordinate Offset
G92.1: Clear Coordinate System Offsets
G93, G94: Feedrate Modes
M0, M2, M30: Program Pause and End
M3, M4, M5: Spindle Control
M8, M9: Coolant Control
*/
