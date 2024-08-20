using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Configuration;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Net;
using System.Diagnostics.Eventing.Reader;

namespace Client
{
    public partial class Client : Form
    {
        // Default Encryption Keys - Here for testing will move to a another location such as registry/config file
        private static readonly string defaultKey = "nA3WcZy/RTeVhhxMSn0mzPU32S2x9oof1fOZekHwTfQ=";
        private static readonly string defaultIV = "gzGFRb/FUf7awGZ3oSAjEw==";

        private static string EncryptionKey = string.Empty;
        private static string EncryptionIV = string.Empty;

        private System.Threading.Timer _heartbeatTimer;

        public Client()
        {
            InitializeComponent();
        }

        private void Client_Load(object sender, EventArgs e)
        {
            // If the configuration contains Keys and IV's set the EncryptionKey and EncryptionIV strings
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Key))
            {
                EncryptionKey = Properties.Settings.Default.Key;
            }
            else
            {
                EncryptionKey = defaultKey;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.IV))
            {
                EncryptionIV = Properties.Settings.Default.IV;
            }
            else
            {
                EncryptionIV = defaultIV;
            }

            // Start the heard beat timer
            InitializeHeartbeatTimer();

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

        private async void btnUpdateDatabase_Click(object sender, EventArgs e)
        {
            await RequestUpdateDatabase();
        }

        private void InitializeHeartbeatTimer()
        {
            _heartbeatTimer = new System.Threading.Timer(HeartbeatTimerCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); //TimeSpan.FromHours(1));
        }
        private async void HeartbeatTimerCallback(object state)
        {
            await SendHeartbeatAsync();
        }
        #endregion

        #region Heartbeats
        private async Task SendHeartbeatAsync()
        {
            bool defaultKeysRequired;
            if (string.IsNullOrEmpty(Properties.Settings.Default.Key) && string.IsNullOrEmpty(Properties.Settings.Default.IV))
            {
                EncryptionKey = defaultKey;
                EncryptionIV = defaultIV;
                defaultKeysRequired = true;
            }
            else
            {
                EncryptionKey = Properties.Settings.Default.Key;
                EncryptionIV = Properties.Settings.Default.IV;
                defaultKeysRequired = false;
            }

            try
            {
                using (var client = new TcpClient())
                {
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Sending Heartbeat Message To The Server");
                    await client.ConnectAsync("127.0.0.1", 8080);
                    var stream = client.GetStream();

                    var request = DateTime.Now.ToString("G") + "," + Environment.MachineName + "," + EncryptString(defaultKeysRequired, "HEARTBEAT", EncryptionKey, EncryptionIV);
                    var requestBytes = Encoding.UTF8.GetBytes(request);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                    // Read the server response
                    var responseBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                    if (bytesRead > 0)
                    {
                        byte[] response = new byte[bytesRead];
                        Array.Copy(responseBuffer, response, bytesRead);

                        var encryptedResponse = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                        if (!string.IsNullOrEmpty(encryptedResponse))
                        {
                            var responseParts = encryptedResponse.Split(',');
                            var decryptedResponse = string.Empty;

                            if (responseParts.Length > 0 && Convert.ToBoolean(responseParts[0]) == true)
                            {
                                decryptedResponse = DecryptString(responseParts[1], defaultKey, defaultIV);
                            }
                            else
                            {
                                decryptedResponse = DecryptString(responseParts[1], EncryptionKey, EncryptionIV);
                            }

                            if (decryptedResponse.Contains("Terminal not registered"))
                            {
                                Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] Terminal is not registered. Stopping heartbeat timer.");
                                _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite); // Stop the timer
                            }
                            else if (decryptedResponse.Contains("Heartbeat received"))
                            {
                                Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] Heartbeat acknowledged.");
                            }
                        }
                        else
                        {
                            Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] Empty response received.");
                        }
                    }
                    else
                    {
                        Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] No response received.");
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogError("SendHeartbeatAsync - " + ex.Message);
            }
        }
        #endregion

        #region Terminal Registration (DONE)
        private async Task RegisterTerminalAsync()
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.Key) && !string.IsNullOrEmpty(Properties.Settings.Default.IV))
            {
                Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Terminal already registered with the Server");
                return; 
            }

            try
            {
                using (var client = new TcpClient())
                {
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Sending Registery Terminal Message");
                    // Create Connection to the Server
                    await client.ConnectAsync("127.0.0.1", 8080);
                    // Open Datastream
                    var stream = client.GetStream();

                    // Send DateTime/Terminal Name and Encrypted Request
                    var request = DateTime.Now.ToString("G") + "," + Environment.MachineName + "," + EncryptString(true, "REGISTER_TERMINAL", string.Empty, string.Empty);
                    var requestBytes = Encoding.UTF8.GetBytes(request);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);


                    var ackBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length);
                    var encryptedResponse = Encoding.UTF8.GetString(ackBuffer, 0, bytesRead);
                    var responseParts = encryptedResponse.Split(',');
                    var decrypedResponse = DecryptString(responseParts[1], defaultKey, defaultIV);

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
                        Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Encryption Key And IV Saved to Configuration");

                        // Start the heard beat timer
                        Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Starting Heartbeat Service");
                        InitializeHeartbeatTimer();
                    }
                    else if (decrypedResponse.Contains("Terminal already registered"))
                    {
                        Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ SERVER ] Terminal is already registered");
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

        #endregion

        #region Ping Request (DONE)

        private async Task RequestPingAsync()
        {
            EncryptionKey = Properties.Settings.Default.Key;
            EncryptionIV = Properties.Settings.Default.IV;

            try
            {
                using (var client = new TcpClient())
                {
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Sending Ping Message");
                    await client.ConnectAsync("127.0.0.1", 8080);
                    var stream = client.GetStream();

                    // Send Ping Request encrypted
                    var request = DateTime.Now.ToString("G") + "," + Environment.MachineName + "," + EncryptString(false, "PING_SERVER", EncryptionKey, EncryptionIV);
                    var requestBytes = Encoding.UTF8.GetBytes(request);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                    // Handle Ping request response from the server
                    var responseBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                    if (bytesRead > 0)
                    {
                        //Create a new byte array for the response removing any Padded bytes
                        byte[] response = new byte[bytesRead];
                        Array.Copy(responseBuffer, response, bytesRead);
                        var encryptedResponse = Encoding.UTF8.GetString(response);
                        var decryptedResponse = DecryptString(encryptedResponse, EncryptionKey, EncryptionIV);

                        Log("[ " + DateTime.Now.ToString("G") + " ] :: [ SERVER ] " + decryptedResponse);
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogError("RequestPingAsync :: " + ex.Message);
            }
        }

        #endregion

        #region Update Database
        private async Task RequestUpdateDatabase()
        {
            EncryptionKey = Properties.Settings.Default.Key;
            EncryptionIV = Properties.Settings.Default.IV;

            try
            {
                using (var client = new TcpClient())
                {
                    Log("[ " + DateTime.Now.ToString("G") + " ] :: " + "[ " + Environment.MachineName + " ] " + "Sending Database Update");
                    await client.ConnectAsync("127.0.0.1", 8080);
                    var stream = client.GetStream();

                    // Send Ping Request encrypted
                    //@$"UPDATE {1} SET {2} = @{3} WHERE {4} = @{5}";
                    //
                    // {1} :: Table to update
                    // {2} :: Column Name of value to be changed 
                    // {3} :: New Value
                    // {4} :: Where Record ID = {5}
                    // {5} :: Value of Record ID

                    // DB Values
                    string tableToUpdate = "Terminals";
                    string columnToUpdate = "RandomUpdateHere";
                    string columnUpdateValue = "Im Just a Test Column and Value";
                    string recordID = "TerminalID";
                    string recordValue = "NMUK-TT4-L";

                    // DB Values to formatted string
                    string databaseData = tableToUpdate + "," + columnToUpdate + "," + columnUpdateValue + "," + recordID + "," + recordValue;

                    var request = DateTime.Now.ToString("G") + "," + Environment.MachineName + "," + EncryptString(false, $"UPDATE_DATABASE,{databaseData}", EncryptionKey, EncryptionIV);
                    var requestBytes = Encoding.UTF8.GetBytes(request);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                    // Handle Ping request response from the server
                    var responseBuffer = new byte[256];
                    var bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

                    if (bytesRead > 0)
                    {
                        //Create a new byte array for the response removing any Padded bytes
                        byte[] response = new byte[bytesRead];
                        Array.Copy(responseBuffer, response, bytesRead);
                        var encryptedResponse = Encoding.UTF8.GetString(response);
                        var decryptedResponse = DecryptString(encryptedResponse, EncryptionKey, EncryptionIV);

                        Log("[ " + DateTime.Now.ToString("G") + " ] :: [ SERVER ] " + decryptedResponse);
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogError("RequestPingAsync :: " + ex.Message);
            }
        }

        #endregion





        #region Encryption Functions

        private static string EncryptString(bool defaultEncryption, string message, string key, string iv)
        {
            // If defaultEncryption is true, use default key and IV values
            if (defaultEncryption)
            {
                key = defaultKey;
                iv = defaultIV;
            }

            // Validate that message, key, and IV are not null or empty
            if (string.IsNullOrEmpty(message)) { throw new ArgumentNullException(nameof(message)); }
            if (string.IsNullOrEmpty(key)) { throw new ArgumentNullException(nameof(key)); }
            if (string.IsNullOrEmpty(iv)) { throw new ArgumentNullException(nameof(iv)); }

            // Convert the key and IV from Base64 strings to byte arrays
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);
            byte[] encryptedText;

            // Create an AES encryption object
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;   // Set the AES key -- Default or Passed in
                aes.IV = ivBytes;     // Set the AES IV -- Default or Passed in

                // Create an encryptor to perform the stream transformation
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                // Create a memory stream to hold the encrypted data
                using (var msEncrypt = new MemoryStream())
                {
                    // Create a crypto stream that links data to the encryptor
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        // Create a stream writer to write the plain text data to the crypto stream
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(message);  // Write the message to be encrypted
                        }
                        // After writing, the memory stream contains the encrypted data
                        encryptedText = msEncrypt.ToArray();
                    }
                }
            }

            // Convert the encrypted byte array to a Base64 string and return it
            return Convert.ToBase64String(encryptedText);
        }


        private static string DecryptString(string cipherText, string key, string iv)
        {
            // Validate that the cipherText is not null or empty
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            // Convert the key and IV from Base64 strings to byte arrays
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);
            // Convert the cipherText from a Base64 string to a byte array
            byte[] buffer = Convert.FromBase64String(cipherText);

            // Create an AES decryption object
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;   // Key passed in from properties Unique to the terminal 
                aes.IV = ivBytes;     // IV passed in from properties Unique to the terminal

                // Create a decryptor to perform the stream transformation
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                // Create a memory stream to hold the encrypted data
                using (var msDecrypt = new System.IO.MemoryStream(buffer))
                {
                    // Create a crypto stream that links data to the decryptor
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        // Create a stream reader to read the decrypted data from the crypto stream
                        using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                        {
                            // Read the decrypted data and return it as a string
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
        }

        private void LogError(string message)
        {
            Log("[ " + DateTime.Now.ToString("G") + " ] " + "[ ERROR ] :: " + message);
        }

        private void btnClearText_Click(object sender, EventArgs e)
        {
            txtConsole.Text = string.Empty;
        }

        #endregion


    }
}
