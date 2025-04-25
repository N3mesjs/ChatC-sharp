using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Numerics;
using System.IO;

namespace WpfApp1
{
    public partial class LogIn : Window
    {
        private string _username;
        private string _password;

        public LogIn()
        {
            InitializeComponent();
        }

        private async void SendForm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _username = userName.Text;
                _password = passWord.Password;

                TcpClient client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 13);
                using (NetworkStream stream = client.GetStream())
                {
                    // Riceve la chiave pubblica RSA dal server
                    byte[] publicKeyBytes = new byte[1024];
                    int bytesRead = await stream.ReadAsync(publicKeyBytes, 0, publicKeyBytes.Length);
                    string publicKeyString = Encoding.UTF8.GetString(publicKeyBytes, 0, bytesRead);
                    var publicKeyParts = publicKeyString.Split(',');
                    BigInteger publicKey = BigInteger.Parse(publicKeyParts[0]);
                    BigInteger modulus = BigInteger.Parse(publicKeyParts[1]);

                    // Hash della password
                    string hashedPassword = HashPassword(_password);

                    // Prepara il messaggio da inviare in formato JSON
                    var credentials = new { Type = "LogIn", Username = _username, Password = hashedPassword };
                    string jsonMessage = JsonConvert.SerializeObject(credentials);

                    // Genera una chiave AES casuale
                    using (Aes aes = Aes.Create())
                    {
                        aes.GenerateKey();
                        aes.GenerateIV();

                        // Invia la chiave AES crittografata con RSA
                        byte[] aesKeyWithIV = new byte[aes.Key.Length + aes.IV.Length];
                        Buffer.BlockCopy(aes.Key, 0, aesKeyWithIV, 0, aes.Key.Length);
                        Buffer.BlockCopy(aes.IV, 0, aesKeyWithIV, aes.Key.Length, aes.IV.Length);

                        byte[] encryptedAesKey = RSA.EncryptWithRSA(aesKeyWithIV, publicKey, modulus);
                        await stream.WriteAsync(encryptedAesKey, 0, encryptedAesKey.Length);

                        // Crittografa il messaggio JSON con AES, vedi https://learn.microsoft.com/it-it/dotnet/api/system.security.cryptography.aes?view=net-8.0
                        byte[] encryptedMessage;
                        using (MemoryStream msEncrypt = new MemoryStream())
                        {
                            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(jsonMessage);
                            }
                            encryptedMessage = msEncrypt.ToArray();
                        }

                        // Invia il messaggio crittografato
                        await stream.WriteAsync(encryptedMessage, 0, encryptedMessage.Length);

                        //Riceve la risposta crittografata dal server
                        byte[] responseBuffer = new byte[1024];
                        bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                        byte[] trimmedResponseBuffer = new byte[bytesRead];
                        Array.Copy(responseBuffer, trimmedResponseBuffer, bytesRead);

                        //Decifra la risposta con AES
                        string serverResponse;
                        using (MemoryStream msDecrypt = new MemoryStream(trimmedResponseBuffer))
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            serverResponse = srDecrypt.ReadToEnd();
                        }

                        if (serverResponse.Split(' ')[0] == "SUCCESS")
                        {
                            this.Hide();
                            MainWindow mainWindow = new MainWindow(_username);
                            mainWindow.Show();
                        }
                        else
                        {
                            MessageBox.Show($"Risposta dal server: {serverResponse}", "Risposta", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private void GoToSignUp_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
        }
    }
}
