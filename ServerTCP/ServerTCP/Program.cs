using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerTCP
{
    public class TCPServer
    {
        public static void Main(string[] args)
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Any, 13);
            TcpListener listener = new TcpListener(ipEndPoint);

            try
            {
                listener.Start();
                Console.WriteLine("Server started. Waiting for a connection...");

                while (true)
                {
                    // Accetta una connessione in modo sincrono
                    using (TcpClient client = listener.AcceptTcpClient())
                    {
                        Console.WriteLine("Client connected.");

                        // Ottieni il NetworkStream per comunicare con il client
                        using (NetworkStream stream = client.GetStream())
                        {
                            // Prepara il messaggio da inviare
                            var message = $"📅 {DateTime.Now} 🕛";
                            var dateTimeBytes = Encoding.UTF8.GetBytes(message);

                            // Invia il messaggio al client
                            stream.Write(dateTimeBytes, 0, dateTimeBytes.Length);
                            Console.WriteLine($"Sent message: \"{message}\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                listener.Stop();
                Console.WriteLine("Server stopped.");
            }
        }
    }
}
