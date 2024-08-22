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
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Buffers.Text;

namespace Monitoring_Server
{
    public partial class MonitoringServer : Form
    {
        // Networking
        private TcpListener? _server;
        private CancellationTokenSource? _cancellationTokenSource;

        // Server running status
        private bool _isRunning = false;

        // Database
        private static readonly string ConnectionString = "Data Source=terminals.db;";

        // Default Encryption Keys - Here for testing will move to a another location such as registry/config file
        private static readonly string defaultKey = "nA3WcZy/RTeVhhxMSn0mzPU32S2x9oof1fOZekHwTfQ=";
        private static readonly string defaultIV = "gzGFRb/FUf7awGZ3oSAjEw==";

        //Heartbeats
        private readonly Dictionary<string, DateTime> _lastHeartbeatReceived = new Dictionary<string, DateTime>();

        public MonitoringServer()
        {
            Batteries.Init(); // Initialize SQLitePCL
            InitializeComponent();
            InitializeDatabase();
        }

        private void MonitoringServer_Load(object sender, EventArgs e)
        {
            // Reset Server Status Label
            lblServerStatus.Text = string.Empty;

            //var fileList = CreateFileList(@"T:\Git Repositries");

            // Start Hearbeat monitor
            //MonitorHeartbeat();

            // Start the server
            StartServer();
        }

        #region Monitoring, Terminal Listener / Heartbeat Monitor

        //Terminal Listener
        private async Task ListenForTerminals(CancellationToken cancellationToken)
        {
            LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Listening on: {IPAddress.Any}");

            // Loop check while cancel request is false
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // While listener is not null action the handle Terminal
                    var terminal = await _server.AcceptTcpClientAsync().ConfigureAwait(false);
                    if (terminal != null)
                    {
                        LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Terminal Connected");
                        // Run handle terminal
                        await Task.Run(() => HandleTerminal(terminal, cancellationToken));

                    }
                }
                // Catch disposed connection error
                catch (ObjectDisposedException ex)
                {
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Terminal Connection Terminated: " + ex.Message);
                    break;
                }
                // Catch remaining exceptions
                catch (Exception ex)
                {
                    // If the cancel token isnt true, a error occured in connecting terminal
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error Accepting Terminal: " + ex.Message);
                    }
                }
            }
        }


        //Heartbeat Monitor
        private void MonitorHeartbeat()
        {
            Task.Run(async () =>
            {
                try
                {
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: MonitorHeartbeat started.");

                    while (_isRunning)
                    {
                        var now = DateTime.Now;
                        LogMessage("[ " + now.ToString("G") + " ] :: Checking heartbeats...");

                        foreach (var terminal in _lastHeartbeatReceived.Keys.ToList())
                        {
                            try
                            {
                                //LogMessage($"[ {now:G} ] :: Checking terminal {terminal}, last heartbeat at {_lastHeartbeatReceived[terminal]}.");
                                if (now - _lastHeartbeatReceived[terminal] > TimeSpan.FromMinutes(5))
                                {
                                    LogMessage($"[ {now:G} ] :: [ WARNING ] :: Communication lost to {terminal}. Last heartbeat received at {_lastHeartbeatReceived[terminal]}.");
                                    // Handle lost communication, e.g., send an alert, remove from active terminals, etc.
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"[ {now:G} ] :: Error while processing terminal {terminal}: {ex.Message}");
                                // Optionally rethrow or handle the exception
                            }
                        }

                        //LogHeartbeatDictionary(); // Log the dictionary contents

                        await Task.Delay(TimeSpan.FromMinutes(60)); // Check every minute
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"[ {DateTime.Now:G} ] :: Error in MonitorHeartbeat loop: {ex.Message}");
                    // Optionally handle the exception (e.g., restart the monitoring, alert, etc.)
                }
                finally
                {
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: MonitorHeartbeat stopped.");
                }
            });
        }
        #endregion

        #region Terminal Handler For Non/Default Encrypted Requests
        private async Task HandleTerminal(TcpClient terminal, CancellationToken cancellationToken)
        {
            var key = string.Empty;
            var iv = string.Empty;
            string decryptedRequest;
            bool defaultKeysRequired;

            try
            {
                using (terminal)
                {
                    var stream = terminal.GetStream();
                    var buffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);

                    if (bytesRead > 0)
                    {
                        var initialRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var requestParts = initialRequest.Split(',');

                        if (requestParts.Length < 3)
                        {
                            await SendErrorResponse(stream, "Invalid Message format", true, defaultKey, defaultIV, cancellationToken);
                            return;
                        }

                        var dateTime = requestParts[0];
                        var terminalName = requestParts[1];
                        var encryptedMessage = requestParts[2];

                        if (string.IsNullOrEmpty(encryptedMessage) || string.IsNullOrEmpty(terminalName) || !DateTime.TryParse(dateTime, out DateTime parsedDateTime))
                        {
                            await SendErrorResponse(stream, "Invalid request data", true, defaultKey, defaultIV, cancellationToken);
                            return;
                        }

                        var terminalID = GetTerminalById(terminalName);

                        if (terminalID == null)
                        {
                            key = defaultKey;
                            iv = defaultIV;
                            defaultKeysRequired = true;
                        }
                        else
                        {
                            key = terminalID.EncryptionKey;
                            iv = terminalID.EncryptionIV;
                            defaultKeysRequired = false;
                        }

                        try
                        {
                            decryptedRequest = DecryptString(encryptedMessage, key, iv);
                        }
                        catch (Exception ex)
                        {
                            LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Decryption failed: " + ex.Message);
                            await SendErrorResponse(stream, "Decryption failed", defaultKeysRequired, key, iv, cancellationToken);
                            return;
                        }

                        switch (decryptedRequest)
                        {
                            case "HEARTBEAT":
                                var responseMessage = string.Empty;
                                var heartbeatResponse = await TerminalHeartbeatRequest(terminalName);

                                if (heartbeatResponse.Contains("Terminal not registered"))
                                {
                                    responseMessage = heartbeatResponse;
                                    defaultKeysRequired = true;
                                    key = defaultKey;
                                    iv = defaultIV;

                                }
                                else if (heartbeatResponse.Contains("Heartbeat received"))
                                {
                                    responseMessage = heartbeatResponse;
                                    defaultKeysRequired = false;
                                }

                                var encryptedResponse = defaultKeysRequired + "," + EncryptString(responseMessage, key, iv);
                                var responseBytes = Encoding.UTF8.GetBytes(encryptedResponse);
                                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                                break;

                            case "REGISTER_TERMINAL":
                                await RegisterTerminalRequest(terminal, stream, terminalName, (IPEndPoint)terminal.Client.RemoteEndPoint, cancellationToken);
                                break;
                            default:
                                if (terminalID != null)
                                {
                                    await ProcessEncryptedRequest(stream, encryptedMessage, terminalID, cancellationToken);
                                }
                                else
                                {
                                    await SendErrorResponse(stream, "Terminal not registered", true, defaultKey, defaultIV, cancellationToken);
                                }
                                break;
                        }
                    }
                    else // Bytes were less than 0 log message to server console
                    {
                        LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Received empty request.");
                        await SendErrorResponse(stream, "Server Error - ", true, defaultKey, defaultIV, cancellationToken);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                // pass error back using default encryption
                var stream = terminal.GetStream();
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Terminal Connection Terminated: " + ex.Message);
                await SendErrorResponse(stream, "Server Error - Exception Error", true, defaultKey, defaultIV, cancellationToken);

            }
            catch (Exception ex)
            {
                // pass error back using default encryption
                var stream = terminal.GetStream();
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Exception in HandleTerminal: " + ex.Message);
                await SendErrorResponse(stream, "Server Error - Exception in HandleTerminal: " + ex.Message, true, defaultKey, defaultIV, cancellationToken);
            }
        }
        #endregion

        #region Heartbeat
        private async Task<string> TerminalHeartbeatRequest(string terminalComputerName)
        {
            var terminalID = GetTerminalById(terminalComputerName);

            if (terminalID == null)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Terminal {terminalComputerName} is not registered. Heartbeat ignored.");
                return "Terminal not registered";
            }

            if (_lastHeartbeatReceived.ContainsKey(terminalComputerName))
            {
                _lastHeartbeatReceived[terminalComputerName] = DateTime.Now;
                //LogHeartbeatDictionary();
            }
            else
            {
                _lastHeartbeatReceived.Add(terminalComputerName, DateTime.Now);
                //LogHeartbeatDictionary();
            }

            LogHeartbeat("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Heartbeat received from {terminalComputerName}");
            return "Heartbeat received";
        }
        #endregion

        #region Terminal Registration
        private async Task RegisterTerminalRequest(TcpClient terminal, NetworkStream stream, string terminalComputerName, IPEndPoint? remoteTerminal, CancellationToken cancellationToken)
        {
            if (terminalComputerName == null)
            {
                await SendErrorResponse(stream, "Invalid Terminal Name", true, defaultKey, defaultIV, cancellationToken);
                return;
            }

            LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Request to register terminal: " + terminalComputerName);

            var terminalID = GetTerminalById(terminalComputerName);
            LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Checking if terminal exists");
            if (terminalID == null)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Terminal does not exist, Prompt to add terminal");
                // Prompt the user to add the terminal
                var result = MessageBox.Show($"Do you wish to add this terminal: {terminalComputerName}?", "New Terminal Detected", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "User selected \"Yes\" to prompt");

                    // Create new encryption key and IV for the terminal
                    using (Aes aes = Aes.Create())
                    {
                        aes.GenerateKey();
                        aes.GenerateIV();
                        var newKey = Convert.ToBase64String(aes.Key);
                        var newIV = Convert.ToBase64String(aes.IV);

                        // Add the terminal to the database
                        AddTerminalToDatabase(terminalComputerName, (remoteTerminal).Address.ToString(), newKey, newIV);
                        LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Registered {terminalComputerName} Successfully");

                        // Acknowledge the connection and send the new Key and IV
                        var message = $"Terminal registered successfully,Key={newKey},IV={newIV}";
                        var encryptedMessage = true + "," + EncryptString(message, defaultKey, defaultIV);
                        var responseBytes = Encoding.UTF8.GetBytes(encryptedMessage);
                        LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Sending success response & new Encryption keys to {terminalComputerName}");
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Close the connection if the user chooses not to add the terminal
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "User selected \"No\" to prompt");
                    terminal.Close();
                    stream.Close();
                    return;
                }
            }
            else
            {
                // Acknowledge that the terminal is already registered
                var message = "Terminal already registered";
                var encryptedMessage = EncryptString(message, string.Empty, string.Empty);
                var responseBytes = Encoding.UTF8.GetBytes(encryptedMessage);
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + $"Sending terminal already registered response to {terminalComputerName}");
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
            }
        }
        #endregion

        #region Process Encrypted Requests
        private async Task ProcessEncryptedRequest(NetworkStream stream, string encryptedMessage, Terminal terminalID, CancellationToken cancellationToken)
        {
            if (terminalID == null)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: [ ERROR ]" + "ProcessEncryptedRequest - terminalID is null");
                return;
            }


            var decryptedMessage = string.Empty;
            try
            {
                var buffer = new byte[256];
                decryptedMessage = DecryptString(encryptedMessage, terminalID.EncryptionKey, terminalID.EncryptionIV);

                var splitDecryptedMessage = decryptedMessage.Split(',');

                switch (splitDecryptedMessage[0])
                {
                    case "KeyVerificationTest":
                        var responseMessage = "KeysMatch";
                        var encryptedResponse = EncryptString(responseMessage, terminalID.EncryptionKey, terminalID.EncryptionIV);
                        var responseBytes = Encoding.UTF8.GetBytes(encryptedResponse);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                        break;
                    case "GET_FILE_LIST":
                        await SendFileListAsync(stream, terminalID.EncryptionKey, terminalID.EncryptionIV, cancellationToken);
                        break;
                    case "RETRIEVE_FILE":
                        var fileName = splitDecryptedMessage[1];
                        await SendFileAsync(stream, fileName, cancellationToken);
                        break;
                    case "PING_SERVER":
                        await PingServerRequest(stream, terminalID.EncryptionKey, terminalID.EncryptionIV, cancellationToken);
                        break;
                    case "UPDATE_DATABASE":
                        await UpdateDatabaseRequest(stream, decryptedMessage, terminalID.EncryptionKey, terminalID.EncryptionIV, cancellationToken);
                        break;
                    default:
                        await HandleUnknownRequest(stream, terminalID.EncryptionKey, terminalID.EncryptionIV, cancellationToken);
                        break;
                }

            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error processing encrypted request: " + ex.Message);
                await SendErrorResponse(stream, "Error processing encrypted request", false, terminalID.EncryptionKey, terminalID.EncryptionIV, cancellationToken);
            }
        }
        #endregion

        #region Ping Request
        private async Task PingServerRequest(NetworkStream stream, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var response = EncryptString("Ping Received", key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Ping request handled.");
            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error handling PING_SERVER request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling PING_SERVER request", false, key, iv, cancellationToken);
            }
        }
        #endregion

        #region Update Database Request
        private async Task UpdateDatabaseRequest(NetworkStream stream, string data, string key, string iv, CancellationToken cancellationToken)
        {
            var splitData = data.Split(',');

            string tableToUpdate = splitData[1];
            string columnToUpdate = splitData[2];
            string columnUpdateValue = splitData[3];
            string recordID = splitData[4];
            string recordValue = splitData[5];

            try
            {
                if (UpdateTerminalDatabase(tableToUpdate, columnToUpdate, columnUpdateValue, recordID, recordValue))
                {
                    var response = EncryptString("Update database was Successful", key, iv);
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Update Database handled.");
                }
                else
                {
                    var response = EncryptString("Unable to update the database", key, iv);
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                    LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Update Database handled.");
                }


            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error handling PING_SERVER request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling UPDATE_DATABASE request", false, key, iv, cancellationToken);
            }
        }
        #endregion

        #region File Transfer

        private async Task SendFileListAsync(NetworkStream stream, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var directory = @"T:\Git Repositries";  // Update with your directory path
                var fileHashes = CreateFileList(directory);

                var fileListJson = JsonConvert.SerializeObject(fileHashes);
                var encryptedFileList = EncryptString(fileListJson, key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(encryptedFileList);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);
            }

        }

        private async Task SendFileAsync(NetworkStream stream, string filePath, CancellationToken cancellationToken)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalBytesSent = 0;
                        LogMessage("Starting file transfer...");

                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            totalBytesSent += bytesRead;
                            LogMessage($"Sent {bytesRead} bytes. Total bytes sent: {totalBytesSent}");
                        }

                        // Signal the end of the file transfer
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("EOF"), 0, 3, cancellationToken);
                        LogMessage("EOF sent, file transfer completed successfully.");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error during file transfer: {ex.Message}");
                    throw;
                }
            }
            else
            {
                LogMessage("File not found.");
                await SendErrorResponse(stream, "File not found", true, string.Empty, string.Empty, cancellationToken);
            }
        }

        private Dictionary<string, string> CreateFileList(string directory)
        {
            var fileHashes = new Dictionary<string, string>();
            var allFiles = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            int totalFiles = allFiles.Length;

            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    pbFiles.Visible = true;
                    pbFiles.Maximum = totalFiles;
                    pbFiles.Value = 0;
                }));
            }
            else
            {
                pbFiles.Visible = true;
                pbFiles.Maximum = totalFiles;
                pbFiles.Value = 0;
            }

            foreach (var filePath in allFiles)
            {
                // Get the relative path of the file
                var relativePath = Path.GetRelativePath(directory, filePath);

                UpdateProgressBar(pbFiles.Value + 1, $"Hashing File: {relativePath}");

                // Hash the file
                using (var sha256 = SHA256.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hash = sha256.ComputeHash(stream);
                        // Store the hash with the relative path as the key
                        fileHashes[relativePath] = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }

            UpdateProgressBar(0, "Hashing of files complete");
            return fileHashes;
        }

        private void UpdateProgressBar(int progress, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgressBar(progress, message)));
            }
            else
            {
                if (progress == 0)
                {
                    pbFiles.Visible = false;
                    lblHashingFile.Text = message;
                }
                else
                {
                    pbFiles.Value = progress;
                    lblHashingFile.Text = message;
                }
            }
        }

        #endregion


        #region Unknown Request Handler
        private async Task HandleUnknownRequest(NetworkStream stream, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var response = EncryptString("Unknown request", key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Unknown request received and handled.");
            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error handling unknown request: " + ex.Message);
                await SendErrorResponse(stream, "Error handling unknown request", true, defaultKey, defaultIV, cancellationToken);
            }
        }
        #endregion

        #region Error Response
        private async Task SendErrorResponse(NetworkStream stream, string errorMessage, bool defaultKeysRequired, string key, string iv, CancellationToken cancellationToken)
        {
            try
            {
                var response = defaultKeysRequired + "," + EncryptString(errorMessage, key, iv);
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error sending error response: " + ex.Message);
            }
        }
        #endregion

        #region Additional Functions
        private Terminal? GetTerminalById(string terminalId)
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
                        connection.Close();
                    }
                }
            }
            return null;
        }

        #endregion

        #region Event Triggers & Functions
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
        #endregion

        #region Start and Stop Server
        private void StartServer()
        {
            if (_isRunning)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "An instance of the server is already running");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _server = new TcpListener(IPAddress.Any, 8080);
                _server.Start();
                _isRunning = true;

                Task.Run(() => ListenForTerminals(_cancellationTokenSource.Token));
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Started Server Successfully...");
            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error starting server: " + ex.Message);
            }
        }

        private void StopServer()
        {
            if (!_isRunning)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "No running server to stop.");
                return;
            }

            try
            {
                _cancellationTokenSource.Cancel();
                _server.Stop();
                _isRunning = false;
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Server stopped successfully.");
            }
            catch (Exception ex)
            {
                LogMessage("[ " + DateTime.Now.ToString("G") + " ] :: " + "Error stopping server: " + ex.Message);
            }
        }
        #endregion

        #region Encryption Functions

        public static string EncryptString(string message, string key, string iv)
        {
            // Error out if message is null or empty
            if (string.IsNullOrEmpty(message)) { throw new ArgumentNullException(nameof(message)); }

            try
            {
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
                                swEncrypt.Write(message);
                            }
                            encrypted = msEncrypt.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(encrypted);
            }
            catch (FormatException)
            {
                throw new ArgumentException();
            }
        }

        public static string DecryptString(string encryptedMessage, string key, string iv)
        {
            if (string.IsNullOrEmpty(encryptedMessage)) { throw new ArgumentNullException(nameof(encryptedMessage)); }

            Debug.Write($"Enc Key DecryptString\n{key}\n{iv}");
            try
            {
                byte[] keyBytes = Convert.FromBase64String(key);
                byte[] ivBytes = Convert.FromBase64String(iv);
                byte[] buffer = Convert.FromBase64String(encryptedMessage);
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
            catch (FormatException)
            {
                throw new ArgumentException();
            }

        }

        #endregion

        #region Database Functions
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
                connection.Close();
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
                connection.Close();
            }
        }

        private bool UpdateTerminalDatabase(string tableToUpdate, string columnToUpdate, string columnUpdateValue, string recordID, string recordValue)
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    string updateQuery = @$"UPDATE {tableToUpdate} SET {columnToUpdate} = @columnUpdateValue WHERE {recordID} = @recordValue";

                    using (var command = new SqliteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@columnUpdateValue", columnUpdateValue);
                        command.Parameters.AddWithValue("@recordValue", recordValue);
                        command.ExecuteNonQuery();
                        connection.Close();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        #endregion

        #region Logging Functions
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
        }

        private void LogHeartbeat(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(LogHeartbeat), message);
                return;
            }

            if (string.IsNullOrEmpty(txtHeartbeats.Text))
            {
                txtHeartbeats.AppendText(message);
            }
            else
            {
                txtHeartbeats.AppendText(Environment.NewLine + message);
            }
            txtHeartbeats.SelectionStart = txtMonitoringServer.Text.Length;
            txtHeartbeats.ScrollToCaret();
        }

        private void LogHeartbeatDictionary()
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("[ " + DateTime.Now.ToString("G") + " ] :: _lastHeartbeatReceived contents:");

            foreach (var kvp in _lastHeartbeatReceived)
            {
                logBuilder.AppendLine($"Terminal: {kvp.Key}, Last Heartbeat: {kvp.Value}");
            }

            LogMessage(logBuilder.ToString());
        }
        #endregion

        #region Classes
        private class Terminal
        {
            public string? TerminalId { get; set; }
            public string? IPAddress { get; set; }
            public string? EncryptionKey { get; set; }
            public string? EncryptionIV { get; set; }
        }
        #endregion

    }
}
