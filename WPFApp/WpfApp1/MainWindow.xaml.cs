using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Firebase.Database;
using Firebase.Database.Query;
using System.Linq;
using System.Threading.Tasks;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Message> Messages;
        private string _username;
        private FirebaseClient _firebaseClient;

        public MainWindow(string username)
        {
            InitializeComponent();

            // Inizializza Firebase
            _firebaseClient = new FirebaseClient("https://choco-d86c6-default-rtdb.europe-west1.firebasedatabase.app");

            // Inizializza la lista dei messaggi
            _username = username;
            Messages = new ObservableCollection<Message>();
            MessagesContainer.ItemsSource = Messages;

            // Carica i messaggi dal database
            LoadMessagesFromDatabase();
        }

        private async void LoadMessagesFromDatabase()
        {
            try
            {
                var messages = await _firebaseClient
                    .Child("messages")
                    .OrderByKey()
                    .OnceAsync<Message>();

                foreach (var message in messages)
                {
                    Messages.Add(new Message(message.Object.Author, message.Object.Body));
                }

                MessagesScrollViewer.ScrollToEnd(); // Scorri fino in fondo alla lista dei messaggi
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
                // Aggiungi il messaggio alla lista
                Messages.Add(new Message(author, body));

                // Salva il messaggio nel database
                await _firebaseClient
                    .Child("messages")
                    .PostAsync(new Message(author, body));

                MessagesScrollViewer.ScrollToEnd(); // Scorri fino in fondo alla lista dei messaggi
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il salvataggio del messaggio: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
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
                AddMessage(_username, ChatTextBox.Text);
                ChatTextBox.Clear();
            }
        }
    }
}

