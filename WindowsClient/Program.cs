using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WindowsClient
{
    class Program
    {
        static int BUFFER_SIZE = 1024;

        static void Main(string[] args)
        {
            TcpClient socket = new TcpClient("localhost", 9092);
            NetworkStream stream = socket.GetStream();

            Thread threadReceiveData = new Thread(() => listenFunctionality(stream));
            threadReceiveData.IsBackground = true;
            threadReceiveData.Start();

            Thread threadSendData = new Thread(() => sendFunctionality(stream));
            threadSendData.IsBackground = true;
            threadSendData.Start();

            while (threadReceiveData.IsAlive && threadSendData.IsAlive)
            {
                Thread.Sleep(50);
            }

            stream.Close();
            socket.Close();
        }

        static void listenFunctionality(NetworkStream stream)
        {
            while (true)
            {
                try
                {
                    if (isReceiveText(stream))
                    {
                        int size = receiveSize(stream);
                        string text = Encoding.UTF8.GetString(receiveBytes(stream, size));
                        Console.WriteLine("Received text: " + (text.Length > 75 ? text.Substring(0, 75) + "..." : text));
                    }
                    else
                    {
                        int size = receiveSize(stream);
                        receiveFile(stream, "Received.jpg");
                        Console.WriteLine("Received file saved");
                    }
                }
                catch (Exception Err)
                {
                    Console.WriteLine(Err.StackTrace);
                    break;
                }
            }
        }

        static void sendFunctionality(NetworkStream stream)
        {
            while (true)
            {
                try
                {
                    sendFile(stream, false, "Capture.jpg");
                    Console.WriteLine("Sent binary file");
                    Thread.Sleep(new Random().Next(0, 10000));

                    sendFile(stream, true, "data.json");
                    Console.WriteLine("Sent text file");
                    Thread.Sleep(new Random().Next(0, 10000));
                }
                catch (Exception Err)
                {
                    Console.WriteLine(Err.StackTrace);
                    break;
                }
            }
        }

        static void sendBytes(NetworkStream stream, bool isText, byte[] bytes)
        {
            if (isText)
                stream.Write(Encoding.UTF8.GetBytes("s"), 0, 1);
            else
                stream.Write(Encoding.UTF8.GetBytes("b"), 0, 1);

            byte[] messageLength = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
            stream.Write(messageLength, 0, messageLength.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        static void sendText(NetworkStream stream, string content)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(content);
            byte[] messageLength = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(utf8Bytes.Length));
            byte[] messageType = Encoding.UTF8.GetBytes("s");

            stream.Write(messageType, 0, 1);
            stream.Write(messageLength, 0, messageLength.Length);
            stream.Write(utf8Bytes, 0, utf8Bytes.Length);
        }

        static void sendFile(NetworkStream stream, bool isText, string path)
        {
            FileStream fileStream = File.OpenRead(path);

            byte[] messageLength = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)fileStream.Length));
            byte[] messageType = Encoding.UTF8.GetBytes("s");

            if (!isText)
                messageType = Encoding.UTF8.GetBytes("b");

            stream.Write(messageType, 0, 1);
            stream.Write(messageLength, 0, messageLength.Length);

            byte[] buffer = new byte[BUFFER_SIZE];
            int read;

            while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                stream.Write(buffer, 0, read);
            }
        }

        static bool isReceiveText(NetworkStream stream)
        {
            byte[] symbol = new byte[1];
            stream.Read(symbol, 0, symbol.Length);
            string type = Encoding.UTF8.GetString(symbol);

            return type == "s";
        }

        static int receiveSize(NetworkStream stream)
        {
            byte[] datasize = new byte[4];
            stream.Read(datasize, 0, 4);
            return BitConverter.ToInt32(datasize, 0);
        }

        static byte[] receiveBytes(NetworkStream stream, int size)
        {
            byte[] data = new byte[size];
            int dataleft = size;
            int total = 0;

            while (total < size)
            {
                int recv = stream.Read(data, total, dataleft);
                if (recv == 0) break;
                total += recv;
                dataleft -= recv;
            }

            return data;
        }

        static void receiveFile(NetworkStream stream, string path)
        {
            if (File.Exists(path)) File.Delete(path);
            FileStream fileStream = File.OpenWrite(path);

            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                if (bytesRead < buffer.Length) break;
            }

            fileStream.Close();
        }
    }
}
