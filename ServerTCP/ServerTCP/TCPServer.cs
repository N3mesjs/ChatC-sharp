using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Firebase.Database;
using Firebase.Database.Query;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace ServerTCP
{
    class TCPServer
    {
        static async Task Main(string[] args)
        {
            // Inizializza Firebase Admin SDK
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("C:\\Users\\Nymes\\Documents\\GitHub\\ChatC-sharp\\ServerTCP\\ServerTCP\\choco-d86c6-firebase-adminsdk-282m1-005daefe68.json") // Sostituisci con il percorso del tuo file JSON
            });

            var ipEndPoint = new IPEndPoint(IPAddress.Any, 13);
            TcpListener listener = new(ipEndPoint);

            try
            {
                listener.Start();
                Console.WriteLine($"Server started on {ipEndPoint.Address}:{ipEndPoint.Port}");

                while (true)
                {
                    TcpClient handler = await listener.AcceptTcpClientAsync();
                    Console.WriteLine($"Client connected: {((IPEndPoint)handler.Client.RemoteEndPoint).Address}");

                    // Gestisci ogni client in un task separato
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await HandleClientAsync(handler);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error handling client: {ex.Message}");
                        }
                        finally
                        {
                            handler.Close();
                            Console.WriteLine("Client disconnected.");
                        }
                    });
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            await using NetworkStream stream = client.GetStream();
            var rsaKeys = RSA.GenerateKeys(512);

            // Invia la chiave pubblica al client
            string publicKey = $"{rsaKeys.PublicKey},{rsaKeys.Modulus}";
            byte[] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
            await stream.WriteAsync(publicKeyBytes, 0, publicKeyBytes.Length);
            Console.WriteLine("Public key sent to client.");

            // Riceve la chiave AES cifrata
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            byte[] encryptedAesKey = new byte[bytesRead];
            Array.Copy(buffer, encryptedAesKey, bytesRead);

            // Decifra la chiave AES
            byte[] aesKeyWithIV = RSA.DecryptWithRSA(encryptedAesKey, rsaKeys.PrivateKey, rsaKeys.Modulus);
            byte[] aesKey = new byte[32]; // Presumiamo chiavi AES-256
            byte[] aesIV = new byte[16];  // IV standard per AES
            Buffer.BlockCopy(aesKeyWithIV, 0, aesKey, 0, aesKey.Length);
            Buffer.BlockCopy(aesKeyWithIV, aesKey.Length, aesIV, 0, aesIV.Length);

            // Riceve il messaggio cifrato con AES
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            byte[] encryptedMessage = new byte[bytesRead];
            Array.Copy(buffer, encryptedMessage, bytesRead);

            // Decifra il messaggio
            string clientMessage;
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIV;

                using (MemoryStream msDecrypt = new MemoryStream(encryptedMessage))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    clientMessage = srDecrypt.ReadToEnd();
                }
            }

            Console.WriteLine($"Decrypted message from client: \"{clientMessage}\"");

            // Deserializza il JSON per ottenere il tipo di richiesta
            try
            {
                var request = JsonConvert.DeserializeObject<dynamic>(clientMessage);
                string type = request.Type;

                // Inizializza Firebase client
                var firebaseClient = new FirebaseClient("https://choco-d86c6-default-rtdb.europe-west1.firebasedatabase.app");
                string response = "";

                if (type == "messages")
                {
                    // Recupera i messaggi dal database
                    var messages = await firebaseClient
                        .Child("messages")
                        .OrderByKey()
                        .OnceAsync<Message>();

                    // Serializza i messaggi in JSON
                    var messageList = messages.Select(m => new Message(m.Object.Author, m.Object.Body)).ToArray();
                    response = JsonConvert.SerializeObject(messageList);
                }
                else if (type == "AddMessage")
                {
                    string author = request.Author;
                    string body = request.Body;

                    // Salva il messaggio nel database
                    await firebaseClient
                        .Child("messages")
                        .PostAsync(new Message(author, body));

                    response = "SUCCESS: Messaggio salvato con successo.";
                    Console.WriteLine($"Message from '{author}' saved: {body}");
                }
                else if (type == "SignUp")
                {
                    string username = request.Username;
                    string password = request.Password;

                    // Verifica se l'username esiste già
                    bool usernameExists = false;

                    var users = await firebaseClient
                        .Child("users")
                        .OnceAsync<dynamic>();

                    foreach (var user in users)
                    {
                        if (user.Object.Username == username)
                        {
                            usernameExists = true;
                            break;
                        }
                    }

                    if (usernameExists)
                    {
                        response = "ERROR: Username già esistente. Scegli un altro username.";
                        Console.WriteLine($"Username '{username}' already exists.");
                    }
                    else
                    {
                        await firebaseClient
                            .Child("users")
                            .PostAsync(new { Username = username, Password = password, isAdmin = false });

                        response = "SUCCESS";
                        Console.WriteLine("User data saved to Firebase.");
                    }
                }
                else if (type == "LogIn")
                {
                    string username = request.Username;
                    string password = request.Password;

                    bool loginSuccess = false;

                    var users = await firebaseClient
                        .Child("users")
                        .OnceAsync<dynamic>();

                    foreach (var user in users)
                    {
                        if (user.Object.Username == username && user.Object.Password == password)
                        {
                            loginSuccess = true;
                            break;
                        }
                    }

                    if (loginSuccess)
                    {
                        response = "SUCCESS";
                        Console.WriteLine($"User '{username}' logged in successfully.");
                    }
                    else
                    {
                        response = "ERROR: Username o password non validi.";
                        Console.WriteLine("Login failed: Invalid username or password.");
                    }
                }
                else
                {
                    response = "ERROR: Tipo di richiesta non supportato.";
                }

                // Invia una risposta cifrata al client con AES
                byte[] encryptedResponse;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = aesIV;

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(response);
                        }
                        encryptedResponse = msEncrypt.ToArray();
                    }
                }

                await stream.WriteAsync(encryptedResponse, 0, encryptedResponse.Length);
                Console.WriteLine($"Sent encrypted response: \"{response}\"");
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
            }
        }
    }

    public class Message
    {
        public string Author { get; set; }
        public string Body { get; set; }
        public bool IsLocal { get; set; }
        public DateTime Timestamp { get; set; }

        public Message(string author, string body, bool isLocal = false)
        {
            Author = author;
            Body = body;
            IsLocal = isLocal;
            Timestamp = DateTime.UtcNow;
        }
    }



}
