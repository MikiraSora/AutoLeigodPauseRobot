using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoLeigodPauseRobot.Dialog
{
    public partial class InputDialog : Form
    {
        public InputDialog(string title,bool is_password)
        {
            InitializeComponent();

            Text=title;
            textBox1.UseSystemPasswordChar=is_password;
        }

        public string InputText => textBox1.Text;

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
