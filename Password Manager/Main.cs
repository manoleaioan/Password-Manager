using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
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
            encryptBtn.Location = new Point(27, 18);
            encryptBtn.Visible = !decrypted;
            decryptBtn.Visible = decrypted;
            changeSecretBtn.Visible = !decrypted;
        }

        private void HideModalOnClickOutside()
        {
            if (enterPwModal != null && enterPwModal.Visible)
            {
                enterPwModal.Hide();
            }
        }

        private void encryptBtn_Click(object sender, EventArgs e)
        {
            EncryptData();
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

        private void decryptBtn_Click(object sender, EventArgs e)
        {
            if (DECRYPTED)
                return;

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
                        encryptBtn.Location = decryptBtn.Location;
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

        private void saveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (DECRYPTED)
                {
                    if (!EncryptData())
                    {
                        return;
                    }
                }

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
                UpdateFooter(grid.Rows.Count != 1);
                saveBtn.Text = "Saved";
                saveBtn.ForeColor = Color.SkyBlue;

                Task.Delay(500).ContinueWith(t =>
                {
                    saveBtn.Invoke((MethodInvoker)delegate
                    {
                        saveBtn.Text = "Save";
                        saveBtn.ForeColor = Color.White;
                    });
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
                g.DrawString(text, font, new SolidBrush(Color.FromArgb(100, 255, 255, 255)), (Width - size.Width) / 2, (Height - size.Height) / 3);
            }
            else
            {
                grid.ForeColor = Color.White;
                grid.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(167, 167, 167);
            }
        }
    }
}
