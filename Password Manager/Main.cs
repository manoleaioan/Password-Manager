using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Password_Manager
{
    public partial class Main : Form
    {
        private bool DECRYPTED = true;
        private string secretKey;
        private UserControl enterPwModal;
        private string filePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PasswordManager"
        );
        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;

        public Main()
        {
            InitializeComponent();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Delete && DECRYPTED)
            {
                try
                {
                    int rowIndex = grid.CurrentCell.RowIndex;
                    grid.Rows.RemoveAt(rowIndex);
                    return true;
                }
                catch { }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                grid.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(grid, true, null);
                string[] lines = File.ReadAllLines(filePath + "\\data.txt");
                string[] values;

                for (int i = 0; i < lines.Length; i++)
                {
                    values = lines[i].ToString().Split('|');
                    string[] row = new string[values.Length];

                    for (int j = 0; j < values.Length; j++)
                    {
                        row[j] = values[j].Trim();
                    }
                    grid.Rows.Add(row);
                }
            }
            catch { }

            if (grid.Rows.Count > 1)
            {
                grid.Enabled = false;
                DECRYPTED = false;
            }

            UpdateFooter(!DECRYPTED);
            grid.ClearSelection();
            foreach (DataGridViewRow row in grid.Rows)
            {
                row.Height = grid.RowTemplate.Height;
            }
        }

        private bool EncryptData()
        {
            if (CheckIfNoSecret())
            {
                return false;
            }

            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (
                        row.Cells["password"].Value != null
                        && row.Cells["password"].Value.ToString() != ""
                    )
                    {
                        row.Cells["password"].Value = Convert.ToBase64String(
                            Aes.Encrypt(row.Cells["password"].Value.ToString(), secretKey)
                        );
                    }
                }

                DECRYPTED = false;
                grid.Enabled = false;
                grid.ClearSelection();
                UpdateFooter(!DECRYPTED);
                save();
                cryptBtn.ForeColor = Color.SkyBlue;
                cryptBtn.Text = "Saving...";

                Task.Delay(500).ContinueWith(t =>
                {
                    cryptBtn.Invoke((MethodInvoker)delegate
                    {
                        cryptBtn.Text = "Decrypt passwords";
                        cryptBtn.ForeColor = Color.White;      
                    });
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ChangeSecret(string msg = "")
        {
            enterPwModal = new Modal(
                (string pw, Action close, Action<string> displayError) =>
                {
                    if (pw.Length == 0)
                    {
                        displayError("Password too short");
                        return;
                    }
                    close();
                    secretKey = pw;
                    changeSecretBtn.Text = "Change secret key " + pw.Substring(0, 1) + "***";
                    UpdateFooter(!DECRYPTED);
                }, msg
            );

            Controls.Add(enterPwModal);
            enterPwModal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            enterPwModal.Dock = DockStyle.Bottom;
            enterPwModal.BringToFront();
            grid.ClearSelection();
        }

        private void UpdateFooter(bool decrypted)
        {
            cryptBtn.Location = new Point(Width / 2 - cryptBtn.Width / 2, 18);
            cryptBtn.Text = (!decrypted ? "Encrypt" : "Decrypt") + " passwords";
            changeSecretBtn.Visible = !decrypted;
            if (!decrypted)
            {
                cryptBtn.Location = new Point(cryptBtn.Location.X - changeSecretBtn.Width / 2, cryptBtn.Location.Y);
                changeSecretBtn.Location = new Point(cryptBtn.Location.X + changeSecretBtn.Width, cryptBtn.Location.Y);
            }
        }

        private void HideModalOnClickOutside()
        {
            if (enterPwModal != null && enterPwModal.Visible)
            {
                enterPwModal.Hide();
            }
        }

        private bool CheckIfNoSecret()
        {
            if (secretKey == null)
            {
                ChangeSecret("First you need to create a secret key");
                return true;
            }
            return false;
        }

        private void save()
        {
            if (DECRYPTED)
            {
                if (!EncryptData())
                {
                    return;
                }
            }

            try
            {
                if (!System.IO.Directory.Exists(filePath))
                {
                    System.IO.Directory.CreateDirectory(filePath);
                }

                TextWriter writer = new StreamWriter(filePath + "\\data.txt");

                for (int i = 0; i < grid.Rows.Count - 1; i++)
                {
                    for (int j = 0; j < grid.Columns.Count; j++)
                    {
                        if (grid.Rows[i].Cells[j].Value == null)
                        {
                            grid.Rows[i].Cells[j].Value = "";
                        }

                        if (j == grid.Columns.Count - 1)
                        {
                            writer.Write("\t" + grid.Rows[i].Cells[j].Value.ToString());
                        }
                        else
                            writer.Write(
                                "\t" + grid.Rows[i].Cells[j].Value.ToString() + "\t" + "|"
                            );
                    }

                    writer.WriteLine("");
                }

                writer.Close();
                grid.Enabled = grid.Rows.Count == 1;
            }
            catch { }
        }

        private void cryptBtn_Click(object sender, EventArgs e)
        {
            if (DECRYPTED)
            {
                EncryptData();
                return;
            }

            enterPwModal = new Modal(
                (string pw, Action close, Action<string> displayError) =>
                {
                    try
                    {
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            if (
                                row.Cells["password"].Value != null
                                && row.Cells["password"].Value.ToString() != ""
                            )
                            {
                                byte[] byteArrayPw = Convert.FromBase64String(
                                    row.Cells["password"].Value.ToString()
                                );
                                row.Cells["password"].Value = Aes.Decrypt(byteArrayPw, pw);
                            }
                        }
                        close();
                        DECRYPTED = true;
                        grid.Enabled = true;
                        secretKey = pw;
                        changeSecretBtn.Text = "Change secret key " + pw.Substring(0, 1) + "***";
                        UpdateFooter(!DECRYPTED);
                    }
                    catch (Exception ex)
                    {
                        displayError("Wrong password or corrupt data");
                        Console.WriteLine(ex);
                    }
                }
            );

            Controls.Add(enterPwModal);
            enterPwModal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            enterPwModal.Dock = DockStyle.Bottom;
            enterPwModal.BringToFront();
        }

        private void changeSecretBtn_Click(object sender, EventArgs e)
        {
            ChangeSecret();
        }

        private void grid_Click(object sender, EventArgs e)
        {
            HideModalOnClickOutside();
        }

        private void Main_Click(object sender, EventArgs e)
        {
            HideModalOnClickOutside();
        }

        private void grid_EnabledChanged(object sender, EventArgs e)
        {
            DECRYPTED = grid.Enabled;
        }

        private void grid_Paint(object sender, PaintEventArgs e)
        {
            if (!DECRYPTED)
            {
                base.OnPaint(e);
                Graphics g;
                g = e.Graphics;
                grid.ForeColor = Color.FromArgb(80, 80, 80);
                grid.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
                SolidBrush bg = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                g.FillRectangle(bg, 0, 0, grid.Width, grid.Height);
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                string text = "Passwords Encrypted";
                Font font = new Font("Segoe UI", 25, FontStyle.Bold);
                SizeF size = g.MeasureString(text, font);
                grid.ScrollBars = ScrollBars.None;
                g.DrawString(text, font, new SolidBrush(Color.FromArgb(100, 255, 255, 255)), (Width - size.Width) / 2, (Height - size.Height) / 3 - 20);
            }
            else
            {
                grid.ForeColor = Color.White;
                grid.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(167, 167, 167);
                grid.ScrollBars = ScrollBars.Both;
            }
        }

        private void grid_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                if (dragBoxFromMouseDown != Rectangle.Empty &&
                    !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    DragDropEffects dropEffect = grid.DoDragDrop(
                    grid.Rows[rowIndexFromMouseDown],
                    DragDropEffects.Move);
                }
            }
        }

        private void grid_MouseDown(object sender, MouseEventArgs e)
        {
            rowIndexFromMouseDown = grid.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                Size dragSize = SystemInformation.DragSize;
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)),
                                    dragSize);
            }
            else
                dragBoxFromMouseDown = Rectangle.Empty;
        }

        private void grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void grid_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = grid.PointToClient(new Point(e.X, e.Y));

            rowIndexOfItemUnderMouseToDrop =
                grid.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (e.Effect == DragDropEffects.Move)
            {
                if (rowIndexOfItemUnderMouseToDrop < 0)
                {
                    rowIndexOfItemUnderMouseToDrop = 0;
                    return;
                }
                else if (rowIndexOfItemUnderMouseToDrop >= grid.Rows.Count - 1)
                {
                    rowIndexOfItemUnderMouseToDrop = grid.Rows.Count - 2;
                }
                DataGridViewRow rowToMove = e.Data.GetData(
                    typeof(DataGridViewRow)) as DataGridViewRow;
                grid.Rows.RemoveAt(rowIndexFromMouseDown);
                grid.Rows.Insert(rowIndexOfItemUnderMouseToDrop, rowToMove);
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (!grid.Enabled)
            {
                grid.Refresh();
            }
        }
    }
}
