using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LaserGRBL
{
    public partial class SetLineLenForm : Form
    {
        public SetLineLenForm()
        {
            InitializeComponent();
        }
        GrblCore Core;
        private void Btn_SetLineLen_Click(object sender, EventArgs e)
        {
            int len;
            if (int.TryParse(textBox_LineLength.Text, out len))
            {
                Core.SetLineLength(len);
                this.Dispose();
            }
            else {
                MessageBox.Show("输入错误！");
            }

        }
      

        public void ShowDialog(Form parent, GrblCore core)
        {
            Core = core;
            base.ShowDialog(parent);
        }
    }
}
