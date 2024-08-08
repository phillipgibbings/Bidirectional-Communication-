using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Monitoring_Server
{
    public partial class MonitoringServer : Form
    {
        private TcpListener _server;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private static readonly string ConnectionString = "Data Source=terminals.db;";

        public MonitoringServer()
        {
            Batteries.Init(); // Initialize SQLitePCL
            InitializeComponent();
            InitializeDatabase();
        }

        private void MonitoringServer_Load(object sender, EventArgs e)
        {
            lblServerStatus.Text = string.Empty;
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Terminals (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TerminalId TEXT NOT NULL,
                        IPAddress TEXT NOT NULL,
                        EncryptionKey TEXT NOT NULL,
                        EncryptionIV TEXT NOT NULL
                    )";
                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void AddTerminalToDatabase(string terminalId, string ipAddress, string encryptionKey, string encryptionIV)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string insertQuery = @"
                    INSERT INTO Terminals (TerminalId, IPAddress, EncryptionKey, EncryptionIV) 
                    VALUES (@TerminalId, @IPAddress, @EncryptionKey, @EncryptionIV)";
                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@TerminalId", terminalId);
                    command.Parameters.AddWithValue("@IPAddress", ipAddress);
                    command.Parameters.AddWithValue("@EncryptionKey", encryptionKey);
                    command.Parameters.AddWithValue("@EncryptionIV", encryptionIV);
                    command.ExecuteNonQuery();
                }
            }
        }

        #region Encryption Functions

        public static string EncryptString(string plainText, string key, string iv)
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

        public static string DecryptString(string cipherText, string key, string iv)
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
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        LogMessage("Error accepting client: " + ex.Message);
                    }
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

                    // Read the client's initial message
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        var initialRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var parts = initialRequest.Split(' ');
                        var requestType = parts[0];
                        var clientComputerName = parts.Length > 1 ? parts[1] : null;

                        switch (requestType)
                        {
                            case "REGISTER_TERMINAL":
                                await HandleRegisterTerminalRequest(client, stream, clientComputerName, cancellationToken);
                                break;
                            default:
                                // Retrieve terminal details from the database
                                var terminal = GetTerminalById(clientComputerName);
                                if (terminal != null)
                                {
                                    // Process encrypted requests
                                    await ProcessEncryptedRequest(stream, initialRequest, terminal, cancellationToken);
                                }
                                else
                                {
                                    await SendErrorResponse(stream, "Terminal not registered", "defaultKey", "defaultIV", cancellationToken); // Use some default or secure response
                                }
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

        private async Task ProcessEncryptedRequest(NetworkStream stream, string initialRequest, Terminal terminal, CancellationToken cancellationToken)
        {
            try
            {
                var buffer = new byte[256];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    var encryptedRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var decryptedRequest = DecryptString(encryptedRequest, terminal.EncryptionKey, terminal.EncryptionIV);
                    var requestType = decryptedRequest.Split(' ')[0];

                    switch (requestType)
                    {
                        case "PING_SERVER":
                            await PingServerRequest(stream, terminal.EncryptionKey, terminal.EncryptionIV, cancellationToken);
                            break;
                        default:
                            await HandleUnknownRequest(stream, terminal.EncryptionKey, terminal.EncryptionIV, cancellationToken);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Error processing encrypted request: " + ex.Message);
                await SendErrorResponse(stream, "Error processing encrypted request", terminal.EncryptionKey, terminal.EncryptionIV, cancellationToken);
            }
        }

        private async Task HandleRegisterTerminalRequest(TcpClient client, NetworkStream stream, string clientComputerName, CancellationToken cancellationToken)
        {
            if (clientComputerName == null)
            {
                await SendErrorResponse(stream, "Invalid terminal name", "defaultKey", "defaultIV", cancellationToken);
                return;
            }

            LogMessage("Terminal registration request received: " + clientComputerName);

            var terminal = GetTerminalById(clientComputerName);
            if (terminal == null)
            {
                // Prompt the user to add the terminal
                var result = MessageBox.Show($"Do you wish to add this terminal: {clientComputerName}?", "New Terminal Detected", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    // Create new encryption key and IV for the terminal
                    using (Aes aes = Aes.Create())
                    {
                        aes.GenerateKey();
                        aes.GenerateIV();
                        var newKey = Convert.ToBase64String(aes.Key);
                        var newIV = Convert.ToBase64String(aes.IV);

                        // Add the terminal to the database
                        AddTerminalToDatabase(clientComputerName, ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(), newKey, newIV);

                        // Acknowledge the connection and send the new Key and IV
                        var ackMessage = $"Terminal registered successfully;Key={newKey};IV={newIV}";
                        var ackBytes = Encoding.UTF8.GetBytes(ackMessage);
                        await stream.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Close the connection if the user chooses not to add the terminal
                    client.Close();
                    return;
                }
            }
            else
            {
                // Acknowledge that the terminal is already registered
                var ackMessage = "Terminal already registered";
                var ackBytes = Encoding.UTF8.GetBytes(ackMessage);
                await stream.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandlePingRequest(NetworkStream stream, string clientComputerName, CancellationToken cancellationToken)
        {
            var terminal = GetTerminalById(clientComputerName);
            if (terminal != null)
            {
                try
                {
                    var buffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    if (bytesRead > 0)
                    {
                        var encryptedRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var decryptedRequest = DecryptString(encryptedRequest, terminal.EncryptionKey, terminal.EncryptionIV);
                        if (decryptedRequest == "PING_SERVER")
                        {
                            await PingServerRequest(stream, terminal.EncryptionKey, terminal.EncryptionIV, cancellationToken);
                        }
                        else
                        {
                            await HandleUnknownRequest(stream, terminal.EncryptionKey, terminal.EncryptionIV, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Error handling PING_SERVER request: " + ex.Message);
                    await SendErrorResponse(stream, "Error handling PING_SERVER request", terminal.EncryptionKey, terminal.EncryptionIV, cancellationToken);
                }
            }
            else
            {
                await SendErrorResponse(stream, "Terminal not registered", "defaultKey", "defaultIV", cancellationToken); // Use some default or secure response
            }
        }

        private async Task PingServerRequest(NetworkStream stream, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var response = EncryptString("Ping Received", key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                LogMessage("Ping request handled.");
            }
            catch (Exception ex)
            {
                LogMessage("Error handling PING_SERVER request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling PING_SERVER request", key, iv, cancellationToken);
            }
        }

        private async Task HandleUnknownRequest(NetworkStream stream, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var response = EncryptString("Unknown request", key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                LogMessage("Unknown request received and handled.");
            }
            catch (Exception ex)
            {
                LogMessage("Error handling unknown request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling unknown request", key, iv, cancellationToken);
            }
        }

        private async Task SendErrorResponse(NetworkStream stream, string errorMessage, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var response = EncryptString(errorMessage, key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage("Error sending error response: " + ex.Message);
            }
        }

        private Terminal GetTerminalById(string terminalId)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Terminals WHERE TerminalId = @TerminalId";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TerminalId", terminalId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Terminal
                            {
                                TerminalId = reader["TerminalId"].ToString(),
                                IPAddress = reader["IPAddress"].ToString(),
                                EncryptionKey = reader["EncryptionKey"].ToString(),
                                EncryptionIV = reader["EncryptionIV"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        private class Terminal
        {
            public string TerminalId { get; set; }
            public string IPAddress { get; set; }
            public string EncryptionKey { get; set; }
            public string EncryptionIV { get; set; }
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

            Debug.WriteLine(message);
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
                lblServerStatus.Text = "Server isn't running";
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

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _server = new TcpListener(IPAddress.Any, 8080);
                _server.Start();
                _isRunning = true;

                Task.Run(() => ListenForClients(_cancellationTokenSource.Token));
                LogMessage("Started Server Successfully...");
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
        #endregion
    }
}
