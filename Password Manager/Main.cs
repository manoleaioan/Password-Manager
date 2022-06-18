﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        }

        private void EncryptData()
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
                        row.Cells["password"].Value = Convert.ToBase64String(
                            Aes.Encrypt(row.Cells["password"].Value.ToString(), secretKey)
                        );
                    }
                }

                DECRYPTED = false;
                UpdateFooter(!DECRYPTED);
            }
            catch { }
        }

        private void ChangeSecret()
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
                    UpdateFooter(!DECRYPTED);
                }
            );

            Controls.Add(enterPwModal);
            enterPwModal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            enterPwModal.Dock = DockStyle.Bottom;
            enterPwModal.BringToFront();
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
            if (secretKey == null)
            {
                ChangeSecret();
                MessageBox.Show("First you need to create a secret key");
                return;
            }
            EncryptData();
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
                        encryptBtn.Location = decryptBtn.Location;
                        UpdateFooter(!DECRYPTED);
                        foreach (DataGridViewRow row in grid.Rows)
                        {
                            row.Height = grid.RowTemplate.Height;
                        }
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
                    EncryptData();
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
                MessageBox.Show("Data saved sucessfully!");
            }
            catch { }
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
            DECRYPTED = grid.Visible;
        }
    }
}