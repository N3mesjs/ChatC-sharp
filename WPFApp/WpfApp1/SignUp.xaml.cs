using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Numerics;

namespace WpfApp1
{
    public partial class SignUp : Window
    {
        private string _username;
        private string _password;

        public SignUp()
        {
            InitializeComponent();
        }

        private async void SendForm_Click(object sender, RoutedEventArgs e)
        {
            _username = userName.Text;
            _password = passWord.Password;

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

                    // Prepara il messaggio da inviare
                    string messageToSend = $"Username: {_username}, Password: {_password}";
                    BigInteger message = new BigInteger(Encoding.UTF8.GetBytes(messageToSend));

                    // Crittografa il messaggio con RSA
                    BigInteger encryptedMessage = RSA.Encrypt(message, publicKey, modulus);
                    byte[] encryptedMessageBytes = encryptedMessage.ToByteArray();

                    // Invia il messaggio crittografato
                    await stream.WriteAsync(encryptedMessageBytes, 0, encryptedMessageBytes.Length);

                    // Riceve la risposta crittografata dal server
                    byte[] responseBuffer = new byte[1024];
                    bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                    byte[] trimmedResponseBuffer = new byte[bytesRead];
                    Array.Copy(responseBuffer, trimmedResponseBuffer, bytesRead);
                    BigInteger encryptedResponse = new BigInteger(trimmedResponseBuffer);


                    // Decifra la risposta
                    BigInteger decryptedResponse = RSA.Decrypt(encryptedResponse, publicKey, modulus);
                    string serverResponse = Encoding.UTF8.GetString(decryptedResponse.ToByteArray());

                    MessageBox.Show($"Risposta dal server: {serverResponse}", "Risposta", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
