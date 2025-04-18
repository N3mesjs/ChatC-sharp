using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerTCP
{
    class TCPServer
    {
        static async Task Main(string[] args)
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Any, 13);
            TcpListener listener = new(ipEndPoint);

            try
            {
                listener.Start();
                Console.WriteLine($"Server started on {ipEndPoint.Address}:{ipEndPoint.Port}");

                using TcpClient handler = await listener.AcceptTcpClientAsync();
                await using NetworkStream stream = handler.GetStream();

                // Legge il messaggio inviato dal client
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Received message from client: \"{clientMessage}\"");

                // Invia la risposta al client
                var message = $"?? {DateTime.Now} ??";
                var dateTimeBytes = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(dateTimeBytes);

                Console.WriteLine($"Sent message: \"{message}\"");
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
