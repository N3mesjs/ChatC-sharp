using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for SignUp.xaml
    /// </summary>
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
            MessageBox.Show("Metodo sendForm_Click chiamato!");
            _username = userName.Text;
            _password = passWord.Password;

            try
            {
                // Specifica l'indirizzo IP e la porta del server
                var ipAddress = IPAddress.Parse("127.0.0.1"); // Cambia con l'indirizzo del server
                var port = 13; // Cambia con la porta del server

                TcpClient client = new TcpClient();
                await client.ConnectAsync(ipAddress, port);

                using (NetworkStream stream = client.GetStream())
                {
                    // Prepara il messaggio da inviare
                    string messageToSend = $"Username: {_username}, Password: {_password}";
                    byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);

                    // Invia il messaggio
                    await stream.WriteAsync(dataToSend, 0, dataToSend.Length);

                    // Ricevi la risposta dal server
                    var buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string serverResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Mostra la risposta del server
                    MessageBox.Show($"Risposta dal server: {serverResponse}", "Risposta", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // Gestione degli errori
                MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
