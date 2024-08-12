using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Configuration;
using System.Diagnostics;

namespace Client
{
    public partial class Client : Form
    {
        private static readonly string defaultKey = "nA3WcZy/RTeVhhxMSn0mzPU32S2x9oof1fOZekHwTfQ=";
        private static readonly string defaultIV = "gzGFRb/FUf7awGZ3oSAjEw==";

        private static string EncryptionKey = string.Empty;
        private static string EncryptionIV= string.Empty;

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
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Connected to Server");

                    var stream = client.GetStream();
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Initiating Data Stream");

                    // Send DateTime/Terminal Name and Encrypted Request
                    var request = DateTime.Now.ToString("G") + "," + Environment.MachineName + "," + EncryptString(true, "REGISTER_TERMINAL", string.Empty, string.Empty);
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Encrypting 'REGISTER_TERMINAL' Message To Server");

                    // Convert to a byte array
                    var requestBytes = Encoding.UTF8.GetBytes(request);
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Encoding Message To Server");

                    // Send byte array to the server
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Sending Message To The Server");

                    // Server response for terminal registration/state
                    var ackBuffer = new byte[256];
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Creating Buffer For Server Response");

                    // Read the server response byte array
                    var bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length);
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Recieveing Message From Server");

                    // Convert the byte array back into a string
                    var encryptedResponse = Encoding.UTF8.GetString(ackBuffer, 0, bytesRead);
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Decode Message From Server");

                    //Decrypte the string
                    var decrypedResponse = DecryptString(encryptedResponse, defaultKey, defaultIV);
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Decrypting Message From Server");;

                    // Read Server Message for contents and handle appropriatley
                    if (decrypedResponse.Contains("Terminal registered successfully"))
                    {
                        Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] Terminal registration successful.");
                        var parts = decrypedResponse.Split(',');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("Key="))
                            {
                                EncryptionKey = part.Substring(4);
                                Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] New Encryption Key Sent");
                            }
                            else if (part.StartsWith("IV="))
                            {
                                EncryptionIV = part.Substring(3);
                                Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] New Encryption IV Sent");
                            }
                        }
                        
                        // Save provided keys to the configuration file
                        Properties.Settings.Default.Key = EncryptionKey;
                        Properties.Settings.Default.IV = EncryptionIV;
                        Properties.Settings.Default.Save();

                        Log("[ " + DateTime.Now.ToString("G") + " ]  " + "[ " + Environment.MachineName + " ] " + "Encryption Key And IV Saved to Configuration");
                    }
                    else if (decrypedResponse.Contains("Terminal already registered"))
                    {
                        Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] Terminal is already registered");
                    }

                    stream.Close();
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Closing Data Stream");
                    client.Close();
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Terminating Connection To Server");
                }
            }
            catch (Exception ex)
            {
                LogError("RegisterTerminalAsync - " + ex.Message);
            }
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
                    var encryptedRequest = Environment.MachineName + "," + EncryptString(true, "PING_SERVER", EncryptionKey, EncryptionIV);
                    var requestBytes = Encoding.UTF8.GetBytes(encryptedRequest);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                    Log("[ " + DateTime.Now.ToString("G") + " ]  " + "PING_SERVER request sent.");

                    // Handle Ping request response from the server
                    var responseBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    if (bytesRead > 0)
                    {
                        var response = DecryptString(Encoding.UTF8.GetString(responseBuffer, 0, bytesRead), EncryptionKey, EncryptionIV);
                        Log("[ " + DateTime.Now.ToString("G") + " ]  " + response);
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

        private static string EncryptString(bool defaultEncryption, string message, string key, string iv)
        {

            // Get Set Key
            if (defaultEncryption)
            {
                key = defaultKey;
                iv = defaultIV;
            }

            if (string.IsNullOrEmpty(message)) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrEmpty(key)) { throw new ArgumentNullException(nameof(key)); }
            if (string.IsNullOrEmpty(iv)) { throw new ArgumentNullException(nameof(iv)); }

            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);
            byte[] encryptedText;

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var msEncrypt = new System.IO.MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(message);
                        }
                        encryptedText = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encryptedText);
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

            Debug.WriteLine(message);
        }

        private void LogError(string message)
        {
            Log("[ " + DateTime.Now.ToString("G") + " ] " + "ERROR: " + message);
        }

        private void btnClearText_Click(object sender, EventArgs e)
        {
            txtConsole.Text = string.Empty;
        }

        #endregion

        private void Client_Load(object sender, EventArgs e)
        {
            EncryptionKey = Properties.Settings.Default.Key;
            EncryptionIV = Properties.Settings.Default.IV;
        }
    
    }
}
