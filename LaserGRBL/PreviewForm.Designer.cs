﻿/*
 * Created by SharpDevelop.
 * User: Diego
 * Date: 05/12/2016
 * Time: 23:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace LaserGRBL
{
	partial class PreviewForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private LaserGRBL.UserControls.GrblPanel Preview;
		
		/// <summary>
		/// Disposes resources used by the control.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PreviewForm));
            this.Preview = new LaserGRBL.UserControls.GrblPanel();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.CustomButtonArea = new LaserGRBL.PreviewForm.MyFlowPanel();
            this.MNAddCB = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addCustomButtonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MnLockButton = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exportCustomButtonsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importCustomButtonsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.BtnUnlock = new LaserGRBL.UserControls.ImageButton();
            this.BtnZeroing = new LaserGRBL.UserControls.ImageButton();
            this.BtnHoming = new LaserGRBL.UserControls.ImageButton();
            this.BtnResume = new LaserGRBL.UserControls.ImageButton();
            this.BtnPenUp = new LaserGRBL.UserControls.ImageButton();
            this.BtnReset = new LaserGRBL.UserControls.ImageButton();
            this.BtnStop = new LaserGRBL.UserControls.ImageButton();
            this.Btn_SetLineLen = new LaserGRBL.UserControls.ImageButton();
            this.TT = new System.Windows.Forms.ToolTip(this.components);
            this.MNRemEditCB = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.RemoveButton = new System.Windows.Forms.ToolStripMenuItem();
            this.editButtonToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel8.SuspendLayout();
            this.MNAddCB.SuspendLayout();
            this.MNRemEditCB.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Preview
            // 
            resources.ApplyResources(this.Preview, "Preview");
            this.Preview.Name = "Preview";
            this.Preview.Load += new System.EventHandler(this.Preview_Load);
            // 
            // tableLayoutPanel8
            // 
            resources.ApplyResources(this.tableLayoutPanel8, "tableLayoutPanel8");
            this.tableLayoutPanel8.Controls.Add(this.CustomButtonArea, 10, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnUnlock, 1, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnZeroing, 2, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnHoming, 0, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnResume, 6, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnPenUp, 4, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnReset, 7, 0);
            this.tableLayoutPanel8.Controls.Add(this.BtnStop, 5, 0);
            this.tableLayoutPanel8.Controls.Add(this.Btn_SetLineLen, 8, 0);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel8_Paint);
            // 
            // CustomButtonArea
            // 
            this.CustomButtonArea.AllowDrop = true;
            this.CustomButtonArea.ContextMenuStrip = this.MNAddCB;
            resources.ApplyResources(this.CustomButtonArea, "CustomButtonArea");
            this.CustomButtonArea.Name = "CustomButtonArea";
            this.CustomButtonArea.Paint += new System.Windows.Forms.PaintEventHandler(this.CustomButtonArea_Paint);
            // 
            // MNAddCB
            // 
            this.MNAddCB.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addCustomButtonToolStripMenuItem,
            this.MnLockButton,
            this.toolStripMenuItem1,
            this.exportCustomButtonsToolStripMenuItem,
            this.importCustomButtonsToolStripMenuItem});
            this.MNAddCB.Name = "CMM";
            resources.ApplyResources(this.MNAddCB, "MNAddCB");
            this.MNAddCB.Opening += new System.ComponentModel.CancelEventHandler(this.MNAddCB_Opening);
            // 
            // addCustomButtonToolStripMenuItem
            // 
            this.addCustomButtonToolStripMenuItem.Name = "addCustomButtonToolStripMenuItem";
            resources.ApplyResources(this.addCustomButtonToolStripMenuItem, "addCustomButtonToolStripMenuItem");
            this.addCustomButtonToolStripMenuItem.Click += new System.EventHandler(this.addCustomButtonToolStripMenuItem_Click);
            // 
            // MnLockButton
            // 
            this.MnLockButton.Checked = true;
            this.MnLockButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.MnLockButton.Name = "MnLockButton";
            resources.ApplyResources(this.MnLockButton, "MnLockButton");
            this.MnLockButton.Click += new System.EventHandler(this.MnUnlockButtonClick);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // exportCustomButtonsToolStripMenuItem
            // 
            this.exportCustomButtonsToolStripMenuItem.Name = "exportCustomButtonsToolStripMenuItem";
            resources.ApplyResources(this.exportCustomButtonsToolStripMenuItem, "exportCustomButtonsToolStripMenuItem");
            this.exportCustomButtonsToolStripMenuItem.Click += new System.EventHandler(this.exportCustomButtonsToolStripMenuItem_Click);
            // 
            // importCustomButtonsToolStripMenuItem
            // 
            this.importCustomButtonsToolStripMenuItem.Name = "importCustomButtonsToolStripMenuItem";
            resources.ApplyResources(this.importCustomButtonsToolStripMenuItem, "importCustomButtonsToolStripMenuItem");
            this.importCustomButtonsToolStripMenuItem.Click += new System.EventHandler(this.importCustomButtonsToolStripMenuItem_Click);
            // 
            // BtnUnlock
            // 
            this.BtnUnlock.AltImage = null;
            resources.ApplyResources(this.BtnUnlock, "BtnUnlock");
            this.BtnUnlock.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnUnlock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnUnlock.Caption = null;
            this.BtnUnlock.Coloration = System.Drawing.Color.Empty;
            this.BtnUnlock.Image = ((System.Drawing.Image)(resources.GetObject("BtnUnlock.Image")));
            this.BtnUnlock.Name = "BtnUnlock";
            this.BtnUnlock.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnUnlock, resources.GetString("BtnUnlock.ToolTip"));
            this.BtnUnlock.UseAltImage = false;
            this.BtnUnlock.Click += new System.EventHandler(this.BtnUnlockClick);
            // 
            // BtnZeroing
            // 
            this.BtnZeroing.AltImage = null;
            resources.ApplyResources(this.BtnZeroing, "BtnZeroing");
            this.BtnZeroing.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnZeroing.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnZeroing.Caption = null;
            this.BtnZeroing.Coloration = System.Drawing.Color.Empty;
            this.BtnZeroing.Image = ((System.Drawing.Image)(resources.GetObject("BtnZeroing.Image")));
            this.BtnZeroing.Name = "BtnZeroing";
            this.BtnZeroing.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnZeroing, resources.GetString("BtnZeroing.ToolTip"));
            this.BtnZeroing.UseAltImage = false;
            this.BtnZeroing.Click += new System.EventHandler(this.BtnZeroing_Click);
            // 
            // BtnHoming
            // 
            this.BtnHoming.AltImage = null;
            resources.ApplyResources(this.BtnHoming, "BtnHoming");
            this.BtnHoming.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnHoming.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnHoming.Caption = null;
            this.BtnHoming.Coloration = System.Drawing.Color.Empty;
            this.BtnHoming.Image = ((System.Drawing.Image)(resources.GetObject("BtnHoming.Image")));
            this.BtnHoming.Name = "BtnHoming";
            this.BtnHoming.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnHoming, resources.GetString("BtnHoming.ToolTip"));
            this.BtnHoming.UseAltImage = false;
            this.BtnHoming.Click += new System.EventHandler(this.BtnGoHomeClick);
            // 
            // BtnResume
            // 
            this.BtnResume.AltImage = null;
            resources.ApplyResources(this.BtnResume, "BtnResume");
            this.BtnResume.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnResume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnResume.Caption = null;
            this.BtnResume.Coloration = System.Drawing.Color.Empty;
            this.BtnResume.Image = ((System.Drawing.Image)(resources.GetObject("BtnResume.Image")));
            this.BtnResume.Name = "BtnResume";
            this.BtnResume.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnResume, resources.GetString("BtnResume.ToolTip"));
            this.BtnResume.UseAltImage = false;
            this.BtnResume.Click += new System.EventHandler(this.BtnResumeClick);
            // 
            // BtnPenUp
            // 
            this.BtnPenUp.AltImage = null;
            resources.ApplyResources(this.BtnPenUp, "BtnPenUp");
            this.BtnPenUp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnPenUp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnPenUp.Caption = null;
            this.BtnPenUp.Coloration = System.Drawing.Color.Empty;
            this.BtnPenUp.Image = ((System.Drawing.Image)(resources.GetObject("BtnPenUp.Image")));
            this.BtnPenUp.Name = "BtnPenUp";
            this.BtnPenUp.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnPenUp, resources.GetString("BtnPenUp.ToolTip"));
            this.BtnPenUp.UseAltImage = false;
            this.BtnPenUp.Click += new System.EventHandler(this.BtnPenUp_Click);
            // 
            // BtnReset
            // 
            this.BtnReset.AltImage = null;
            resources.ApplyResources(this.BtnReset, "BtnReset");
            this.BtnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnReset.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnReset.Caption = null;
            this.BtnReset.Coloration = System.Drawing.Color.Empty;
            this.BtnReset.Image = ((System.Drawing.Image)(resources.GetObject("BtnReset.Image")));
            this.BtnReset.Name = "BtnReset";
            this.BtnReset.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnReset, resources.GetString("BtnReset.ToolTip"));
            this.BtnReset.UseAltImage = false;
            this.BtnReset.Click += new System.EventHandler(this.BtnResetClick);
            // 
            // BtnStop
            // 
            this.BtnStop.AltImage = ((System.Drawing.Image)(resources.GetObject("BtnStop.AltImage")));
            resources.ApplyResources(this.BtnStop, "BtnStop");
            this.BtnStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.BtnStop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BtnStop.Caption = null;
            this.BtnStop.Coloration = System.Drawing.Color.Empty;
            this.BtnStop.Image = ((System.Drawing.Image)(resources.GetObject("BtnStop.Image")));
            this.BtnStop.Name = "BtnStop";
            this.BtnStop.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.BtnStop, resources.GetString("BtnStop.ToolTip"));
            this.BtnStop.UseAltImage = false;
            this.BtnStop.Click += new System.EventHandler(this.BtnStopClick);
            // 
            // Btn_SetLineLen
            // 
            this.Btn_SetLineLen.AltImage = null;
            resources.ApplyResources(this.Btn_SetLineLen, "Btn_SetLineLen");
            this.Btn_SetLineLen.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.Btn_SetLineLen.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Btn_SetLineLen.Caption = null;
            this.Btn_SetLineLen.Coloration = System.Drawing.Color.Empty;
            this.Btn_SetLineLen.Image = ((System.Drawing.Image)(resources.GetObject("Btn_SetLineLen.Image")));
            this.Btn_SetLineLen.Name = "Btn_SetLineLen";
            this.Btn_SetLineLen.SizingMode = LaserGRBL.UserControls.ImageButton.SizingModes.FixedSize;
            this.TT.SetToolTip(this.Btn_SetLineLen, resources.GetString("Btn_SetLineLen.ToolTip"));
            this.Btn_SetLineLen.UseAltImage = false;
            this.Btn_SetLineLen.Click += new System.EventHandler(this.Btn_SetLineLen_Click);
            // 
            // MNRemEditCB
            // 
            this.MNRemEditCB.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RemoveButton,
            this.editButtonToolStripMenuItem});
            this.MNRemEditCB.Name = "CMM";
            resources.ApplyResources(this.MNRemEditCB, "MNRemEditCB");
            // 
            // RemoveButton
            // 
            this.RemoveButton.Name = "RemoveButton";
            resources.ApplyResources(this.RemoveButton, "RemoveButton");
            // 
            // editButtonToolStripMenuItem
            // 
            this.editButtonToolStripMenuItem.Name = "editButtonToolStripMenuItem";
            resources.ApplyResources(this.editButtonToolStripMenuItem, "editButtonToolStripMenuItem");
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel8, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.Preview, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // PreviewForm
            // 
            this.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this, "$this");
            this.Name = "PreviewForm";
            this.tableLayoutPanel8.ResumeLayout(false);
            this.MNAddCB.ResumeLayout(false);
            this.MNRemEditCB.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

		}

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
		private UserControls.ImageButton BtnReset;
		private UserControls.ImageButton BtnStop;
		private UserControls.ImageButton BtnResume;
		private System.Windows.Forms.ToolTip TT;
		private System.Windows.Forms.ContextMenuStrip MNAddCB;
		private System.Windows.Forms.ToolStripMenuItem addCustomButtonToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip MNRemEditCB;
		private System.Windows.Forms.ToolStripMenuItem RemoveButton;
		private System.Windows.Forms.ToolStripMenuItem editButtonToolStripMenuItem;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exportCustomButtonsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem importCustomButtonsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MnLockButton;
        private UserControls.ImageButton BtnHoming;
        private UserControls.ImageButton BtnZeroing;
        private UserControls.ImageButton BtnUnlock;
        private UserControls.ImageButton BtnPenUp;
        private UserControls.ImageButton Btn_SetLineLen;
        private MyFlowPanel CustomButtonArea;
    }
}
