using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ServerTCP
{
    class TCPServer
    {
        static async Task Main(string[] args)
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Any, 13);
            TcpListener listener = new(ipEndPoint);

            // Genera le chiavi RSA

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

            // Legge il messaggio cifrato inviato dal client
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            BigInteger encryptedMessage = new BigInteger(buffer[..bytesRead]);
            Console.WriteLine($"Received encrypted message: {encryptedMessage}");

            // Decifra il messaggio
            BigInteger decryptedMessage = RSA.Decrypt(encryptedMessage, rsaKeys.PrivateKey, rsaKeys.Modulus);
            string clientMessage = Encoding.UTF8.GetString(decryptedMessage.ToByteArray());
            Console.WriteLine($"Decrypted message from client: \"{clientMessage}\"");

            // Invia una risposta cifrata al client
            string response = $"📅 {DateTime.Now} 🕛";
            BigInteger responseMessage = new BigInteger(Encoding.UTF8.GetBytes(response));
            BigInteger encryptedResponse = RSA.Encrypt(responseMessage, rsaKeys.PublicKey, rsaKeys.Modulus);
            byte[] encryptedResponseBytes = encryptedResponse.ToByteArray();
            await stream.WriteAsync(encryptedResponseBytes, 0, encryptedResponseBytes.Length);
            Console.WriteLine($"Sent encrypted response: {encryptedResponse}");
        }
    }
}
