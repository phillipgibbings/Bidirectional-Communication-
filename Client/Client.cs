using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Configuration;

namespace Client
{
    public partial class Client : Form
    {
        private static string EncryptionKey = Properties.Settings.Default.Key;
        private static string EncryptionIV = Properties.Settings.Default.IV;

        public Client()
        {
            InitializeComponent();
        }

        #region Event Handlers

        private async void RegisterTerminalButton_Click(object sender, EventArgs e)
        {
            await RegisterTerminalAsync();
        }

        private async void RequestPingButton_Click(object sender, EventArgs e)
        {
            await RequestPingAsync();
        }

        #endregion

        #region Terminal Registration

        private async Task RegisterTerminalAsync()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", 8080);
                    var stream = client.GetStream();

                    // Send terminal name request
                    var request = "REGISTER_TERMINAL " + Environment.MachineName;
                    var requestBytes = Encoding.UTF8.GetBytes(request);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                    // Handle server response for terminal name
                    var ackBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length);
                    var ackResponse = Encoding.UTF8.GetString(ackBuffer, 0, bytesRead);
                    Log("Server response: " + ackResponse);

                    if (ackResponse.StartsWith("Terminal registered successfully"))
                    {
                        Log("Terminal registration successful.");
                        var parts = ackResponse.Split(';');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("Key="))
                            {
                                EncryptionKey = part.Substring(4);
                            }
                            else if (part.StartsWith("IV="))
                            {
                                EncryptionIV = part.Substring(3);
                            }
                        }

                        UpdateAppConfig("Key", EncryptionKey);
                        UpdateAppConfig("IV", EncryptionIV);
                        Log("Encryption key and IV updated.");
                    }
                    else if (ackResponse.Contains("Terminal already registered"))
                    {
                        Log("Terminal is already registered.");
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogError("RegisterTerminalAsync - " + ex.Message);
            }
        }

        private void UpdateAppConfig(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        #endregion

        #region Ping Request

        private async Task RequestPingAsync()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync("127.0.0.1", 8080);
                    var stream = client.GetStream();

                    // Send Ping Request encrypted
                    var encryptedRequest = Environment.MachineName + EncryptString("PING_SERVER", Environment.MachineName, EncryptionKey, EncryptionIV);
                    var requestBytes = Encoding.UTF8.GetBytes(encryptedRequest);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                    Log("PING_SERVER request sent.");

                    // Handle Ping request response from the server
                    var responseBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    if (bytesRead > 0)
                    {
                        var response = DecryptString(Encoding.UTF8.GetString(responseBuffer, 0, bytesRead), EncryptionKey, EncryptionIV);
                        Log(response);
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogError("RequestPingAsync - " + ex.Message);
            }
        }

        #endregion

        #region Encryption Functions

        private static string EncryptString(string plainText, string terminalID, string key, string iv)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);
            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        private static string DecryptString(string cipherText, string key, string iv)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new System.IO.MemoryStream(buffer))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }

            if (string.IsNullOrEmpty(txtConsole.Text))
            {
                txtConsole.AppendText(message);
            }
            else
            {
                txtConsole.AppendText(Environment.NewLine + message);
            }
            txtConsole.SelectionStart = txtConsole.Text.Length;
            txtConsole.ScrollToCaret();

            Console.WriteLine(message);
        }

        private void LogError(string message)
        {
            Log("ERROR: " + message);
        }

        private void btnClearText_Click(object sender, EventArgs e)
        {
            txtConsole.Text = string.Empty;
        }

        #endregion
    }
}
