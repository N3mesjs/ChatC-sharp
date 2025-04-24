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

namespace ServerTCP
{
    class TCPServer
    {
        static async Task Main(string[] args)
        {
            // Inizializza Firebase Admin SDK
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("")
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

            // Deserializza il JSON per ottenere username e password
            try
            {
                var credentials = JsonConvert.DeserializeObject<dynamic>(clientMessage);
                string type = credentials.Type;
                string username = credentials.Username;
                string password = credentials.Password;
                Console.WriteLine($"Tipo: {type}, Username: {username}, Password: {password}");

                // Inizializza Firebase client
                var firebaseClient = new FirebaseClient("");
                string response = "";

                // Salva i dati nel database Firebase
                if (type == "SignUp")
                {
                    // Verifica se l'username esiste già
                    bool usernameExists = false;

                    // Ottieni tutti gli utenti dal database
                    var users = await firebaseClient
                        .Child("users")
                        .OnceAsync<dynamic>();

                    // Controlla se esiste già un utente con lo stesso username
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
                        // Salva i dati nel database
                        await firebaseClient
                            .Child("users")
                            .PostAsync(new { Username = username, Password = password });

                        //response = $"SUCCESS: Registrazione completata con successo! 📅 {DateTime.Now} 🕛";
                        response = "SUCCESS";
                        Console.WriteLine("User data saved to Firebase.");
                    }
                }
                else if (type == "Login")
                {
                    // Gestione login (puoi aggiungere questa parte se necessario)
                    bool loginSuccess = false;

                    // Ottieni tutti gli utenti dal database
                    var users = await firebaseClient
                        .Child("users")
                        .OnceAsync<dynamic>();

                    // Controlla se le credenziali sono corrette
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
                        //response = $"SUCCESS: Login effettuato con successo! 📅 {DateTime.Now} 🕛";
                        response = "SUCCESS";
                        Console.WriteLine($"User '{username}' logged in successfully.");
                    }
                    else
                    {
                        response = "ERROR: Username o password non validi.";
                        Console.WriteLine("Login failed: Invalid username or password.");
                    }
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
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");

                // Invia una risposta di errore cifrata
                string errorResponse = $"ERROR: Si è verificato un errore sul server. {ex.Message}";
                byte[] encryptedErrorResponse;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = aesIV;

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(errorResponse);
                        }
                        encryptedErrorResponse = msEncrypt.ToArray();
                    }
                }

                await stream.WriteAsync(encryptedErrorResponse, 0, encryptedErrorResponse.Length);
                Console.WriteLine($"Sent error response: \"{errorResponse}\"");
            }
        }

    }
}
