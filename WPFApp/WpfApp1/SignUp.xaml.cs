using System;
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

        private void sendForm_Click(object sender, RoutedEventArgs e)
        {
            _username = userName.Text;
            _password = passWord.Password;
        }
    }
}
