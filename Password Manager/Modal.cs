using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Password_Manager
{
    public partial class Modal : UserControl
    {
        private Action<string, Action, Action<string>> callback;

        public Modal(Action<string, Action, Action<string>> _callback)
        {
            callback = _callback;
            InitializeComponent();
        }

        private void Modal_Load(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                callback(textBox1.Text, Hide, displayError);
                return true;
            }
            else if (keyData == Keys.Escape)
            {
                Hide();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void displayError(string err)
        {
            wrongpw.Text = err;
            wrongpw.Visible = true;

        }
        private void cancel_Click(object sender, EventArgs e)
        {
            Hide();
        }

        public void submit_Click(object sender, EventArgs e)
        {
            wrongpw.Visible = false;
            callback(textBox1.Text, Hide, displayError);
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void cancel_MouseEnter(object sender, EventArgs e)
        {
            cancel.ForeColor = Color.SkyBlue;
        }

        private void cancel_MouseLeave(object sender, EventArgs e)
        {
            cancel.ForeColor = Color.FromArgb(141, 141, 141);
        }
    }
}
