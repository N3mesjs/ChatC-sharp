using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;

namespace WpfApp1
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
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

            // Aggiungi un messaggio di esempio
            Messages.Add(new Message("Alice", "Ciao, come stai?"));
            Messages.Add(new Message("Bob", "Tutto bene, grazie!"));
        }

        private void AddMessage(string author, string body)
        {
            Messages.Add(new Message(author, body));

            MessagesScrollViewer.ScrollToEnd(); // Scorri fino in fondo alla lista dei messaggi
        }


        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(ChatTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ChatTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(ChatTextBox.Text))
            {
                AddMessage("You", ChatTextBox.Text);
                ChatTextBox.Clear();
            }
        }

    }
}