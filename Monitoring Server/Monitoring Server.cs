using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Monitoring_Server
{
    public partial class MonitoringServer : Form
    {
        private TcpListener _server;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;

        private static readonly string EncryptionKey = Properties.Settings.Default.Key;
        private static readonly string EncryptionIV = Properties.Settings.Default.IV;

        public MonitoringServer()
        {
            InitializeComponent();
        }

        private void MonitoringServer_Load(object sender, EventArgs e)
        {
            lblServerStatus.Text = string.Empty;
        }

        #region Encryption Functions

        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            byte[] key = Convert.FromBase64String(EncryptionKey);
            byte[] iv = Convert.FromBase64String(EncryptionIV);
            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            byte[] key = Convert.FromBase64String(EncryptionKey);
            byte[] iv = Convert.FromBase64String(EncryptionIV);
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        #endregion

        private async Task ListenForClients(CancellationToken cancellationToken)
        {
            LogMessage($"Listening server: {IPAddress.Any}");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
                    if (client != null)
                    {
                        await Task.Run(() => HandleClient(client, cancellationToken));
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Server has been stopped, exit the loop.
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        LogMessage("Error accepting client: " + ex.Message);
                    }
                    //else
                    //{
                    //    // Log only if not due to cancellation
                    //    LogMessage("Server stopped accepting clients.");
                    //}
                }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var buffer = new byte[256];

                    // Read the client's computer name
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        var clientComputerName = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LogMessage("Terminal connected: " + clientComputerName);

                        // Acknowledge the connection
                        var ackMessage = Encoding.UTF8.GetBytes("Connection acknowledged");
                        await stream.WriteAsync(ackMessage, 0, ackMessage.Length, cancellationToken).ConfigureAwait(false);
                    }

                    // Read the client's encrypted message
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        var encryptedRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var decryptedRequest = DecryptString(encryptedRequest);
                        var requestType = decryptedRequest.Split(' ')[0];

                        switch (requestType)
                        {
                            case "PING_SERVER":
                                await PingServerRequest(stream, cancellationToken);
                                break;
                            default:
                                await HandleUnknownRequest(stream, cancellationToken);
                                break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                LogMessage("Client connection was closed.");
            }
            catch (IOException ex)
            {
                LogMessage("IOException in HandleClient: " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("Exception in HandleClient: " + ex.Message);
            }
        }

        private async Task PingServerRequest(NetworkStream stream, CancellationToken cancellationToken)
        {
            try
            {
                var response = Encoding.UTF8.GetBytes("Ping Received");
                await stream.WriteAsync(response, 0, response.Length, cancellationToken).ConfigureAwait(false);
                LogMessage("Ping request handled.");
            }
            catch (Exception ex)
            {
                LogMessage("Error handling PING_SERVER request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling PING_SERVER request", cancellationToken);
            }
        }

        private async Task HandleUnknownRequest(NetworkStream stream, CancellationToken cancellationToken)
        {
            try
            {
                var response = Encoding.UTF8.GetBytes("Unknown request");
                await stream.WriteAsync(response, 0, response.Length, cancellationToken).ConfigureAwait(false);
                LogMessage("Unknown request received and handled.");
            }
            catch (Exception ex)
            {
                LogMessage("Error handling unknown request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling unknown request", cancellationToken);
            }
        }

        private async Task SendErrorResponse(NetworkStream stream, string errorMessage, CancellationToken cancellationToken)
        {
            try
            {
                var response = Encoding.UTF8.GetBytes(errorMessage);
                await stream.WriteAsync(response, 0, response.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage("Error sending error response: " + ex.Message);
            }
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogMessage), message);
                return;
            }

            if (string.IsNullOrEmpty(txtMonitoringServer.Text))
            {
                txtMonitoringServer.AppendText(message);
            }
            else
            {
                txtMonitoringServer.AppendText(Environment.NewLine + message);
            }
            txtMonitoringServer.SelectionStart = txtMonitoringServer.Text.Length;
            txtMonitoringServer.ScrollToCaret();

            Console.WriteLine(message);
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (!_isRunning)
            {
                StartServer();
                menuStartServer.Text = "Stop Server";
                lblServerStatus.Text = "Server Running...";
            }
            else
            {
                StopServer();
                menuStartServer.Text = "Start Server";
                lblServerStatus.Text = "Server isnt";

            }

        }

        #region Start/Stop Server
        private void StartServer()
        {
            if (_isRunning)
            {
                LogMessage("An instance of the server is already running");
                return;
            }

            string ssMessage = "Started Server Successfully...";
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _server = new TcpListener(IPAddress.Any, 8080);
                _server.Start();
                _isRunning = true;

                Task.Run(() => ListenForClients(_cancellationTokenSource.Token));
                LogMessage(ssMessage);
            }
            catch (Exception ex)
            {
                LogMessage("Error starting server: " + ex.Message);
            }
        }

        private void StopServer()
        {
            if (!_isRunning)
            {
                LogMessage("No running server to stop.");
                return;
            }
            else
            {
                try
                {
                    _cancellationTokenSource.Cancel();
                    _server.Stop();
                    _isRunning = false;
                    LogMessage("Server stopped successfully.");
                }
                catch (Exception ex)
                {
                    LogMessage("Error stopping server: " + ex.Message);
                }
            }
        }
        #endregion

    }
}
