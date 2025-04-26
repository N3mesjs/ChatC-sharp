using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Input;
using System.IO;
using System.Security.Cryptography;
using System.Numerics;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;

/*
 *  TODO: Add private chats
 *  Also add moderation so admins have the ability to delete messages or warn users or ban them
 */

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Message> Messages;
        private string _username;

        public MainWindow(string username)
        {
            InitializeComponent();

            // Inizializza la lista dei messaggi
            _username = username;
            Messages = new ObservableCollection<Message>();
            MessagesContainer.ItemsSource = Messages;

            // Carica i messaggi iniziali dal server
            LoadGlobalMessagesFromServer();

            // Configura il timer per il polling
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2); // Esegui ogni 2 secondi
            timer.Tick += (s, e) => LoadGlobalMessagesFromServer();
            timer.Start();
        }

        private HashSet<string> _messageTracker = new HashSet<string>(); // Tiene traccia dei messaggi univoci

        private async void LoadGlobalMessagesFromServer()
        {
            try
            {
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

                        // Prepara la richiesta per ottenere i messaggi
                        var request = new { Type = "messages" };
                        string jsonRequest = JsonConvert.SerializeObject(request);

                        // Crittografa la richiesta con AES
                        byte[] encryptedRequest;
                        using (MemoryStream msEncrypt = new MemoryStream())
                        {
                            using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(jsonRequest);
                            }
                            encryptedRequest = msEncrypt.ToArray();
                        }

                        // Invia la richiesta crittografata
                        await stream.WriteAsync(encryptedRequest, 0, encryptedRequest.Length);

                        // Riceve la risposta crittografata dal server
                        byte[] responseBuffer = new byte[4096];
                        bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                        byte[] trimmedResponseBuffer = new byte[bytesRead];
                        Array.Copy(responseBuffer, trimmedResponseBuffer, bytesRead);

                        // Decifra la risposta con AES
                        string jsonResponse;
                        using (MemoryStream msDecrypt = new MemoryStream(trimmedResponseBuffer))
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            jsonResponse = srDecrypt.ReadToEnd();
                        }

                        // Deserializza i messaggi e aggiungili all'ObservableCollection
                        var messages = JsonConvert.DeserializeObject<Message[]>(jsonResponse);
                        foreach (var message in messages)
                        {
                            // Crea un identificatore unico per il messaggio
                            string messageId = $"{message.Author}:{message.Body}";

                            // Aggiungi il messaggio solo se non è già stato aggiunto
                            if (!_messageTracker.Contains(messageId)) // TODO: provarlo
                            {
                                _messageTracker.Add(messageId);
                                Messages.Add(message);
                            }
                        }

                        MessagesScrollViewer.ScrollToEnd(); // Scorri fino in fondo alla lista dei messaggi
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il caricamento dei messaggi: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddMessage(string author, string body)
        {
            try
            {
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

                        // Prepara il messaggio da inviare
                        var message = new { Type = "AddMessage", Author = author, Body = body };
                        string jsonMessage = JsonConvert.SerializeObject(message);

                        // Crittografa il messaggio con AES
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
                    }

                    // Non aggiungere manualmente il messaggio qui
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'invio del messaggio: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(ChatTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ChatTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(ChatTextBox.Text))
            {
                AddMessage(_username, ChatTextBox.Text); // Usa _username come autore
                ChatTextBox.Clear();
            }
        }

        private void globalChat_Click(object sender, RoutedEventArgs e)
        {
            LoadGlobalMessagesFromServer();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ChatTextBox.Text))
            {
                AddMessage(_username, ChatTextBox.Text); // Usa _username come autore
                ChatTextBox.Clear();
            }

        }
    }
}
