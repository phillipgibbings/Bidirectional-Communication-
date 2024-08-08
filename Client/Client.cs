using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace Client
{
    public partial class Client : Form
    {

        public Client()
        {
            InitializeComponent();
        }

        #region Ping Request
        private async void RequestPingButton_Click(object sender, EventArgs e)
        {
            
            await RequestPingAsync();

            //Button doesnt need to return any errors as the async method returns them
            //try
            //{
                
            //}
            //catch (Exception ex)
            //{
            //    LogError("RequestPingButton_Click - " + ex.Message);
            //}
        }

        private async Task RequestPingAsync()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Open server connection
                    await client.ConnectAsync("127.0.0.1", 8080);
                    var stream = client.GetStream();

                    // Send computer name
                    var computerName = Environment.MachineName;
                    var computerNameBytes = Encoding.UTF8.GetBytes(computerName);
                    await stream.WriteAsync(computerNameBytes, 0, computerNameBytes.Length);

                    // Handle server response for computer name
                    var ackBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length);
                    var ackResponse = Encoding.UTF8.GetString(ackBuffer, 0, bytesRead);
                    Log("Server response: " + ackResponse);

                    // Sent Ping Request
                    var request = Encoding.UTF8.GetBytes("PING_SERVER");
                    await stream.WriteAsync(request, 0, request.Length);
                    Log("PING_SERVER request sent.");

                    // Handle Ping request response from the server
                    var responseBuffer = new byte[256];
                    bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    var response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
                    Log(response);

                    // Close the stream and client connection
                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogError("RequestPingAsync - " + ex.Message);
                throw;
            }
        }
        #endregion

        private string[] DecryptStrings(byte[] encryptedData, int dataLength, byte[] key, byte[] iv)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    using (var decryptor = aes.CreateDecryptor(key, iv))
                    using (var ms = new System.IO.MemoryStream(encryptedData, 0, dataLength))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new System.IO.StreamReader(cs))
                    {
                        var decryptedString = reader.ReadToEnd();
                        return decryptedString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error decrypting strings: " + ex.Message);
                throw;
            }
        }

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
    }
}
