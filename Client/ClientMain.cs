using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreCS;

namespace Client
{
    class ClientMain
    {
        private static TcpClient client;
        private static MemoryStream recieveData = new MemoryStream();
        public const int BufferSize = 512; //Размер буфера
        public static byte[] buffer = new byte[BufferSize];

        static void Main(string[] args){

            client = new TcpClient("127.0.0.1",2255);//Коннект к серверу
            client.Client.BeginReceive(buffer, 0, BufferSize, 0, new AsyncCallback(ReceiveCallback), null);//Приготовились принимать данные

            for (int i = 0; i < 10; i++)//Шлём сообщения серверу
                    SendMessage(new Message() {
                        id = i,
                        message = "Hello world",
                        title = "My Title"
                    });

            Console.ReadKey();
        }

        private static void RecieveMessage(Message msg) {//Вызывается когда получили ответ от сервера
            Console.WriteLine("Ответ сервера: {0} {1} {2}", msg.id, msg.title, msg.message);
        }
        public static void SendMessage(Message msg) {//Послать сообщение серверу
            using (MemoryStream ms = new MemoryStream()) {
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, msg);
                ms.Seek(0, SeekOrigin.Begin);
                ms.Write(BitConverter.GetBytes(ms.Length - 4), 0, 4);
                ms.Seek(0, SeekOrigin.End);
                ClientMain.client.Client.BeginSend(ms.GetBuffer(), 0, (int)ms.Length, 0, ar => {
                    ClientMain.client.Client.EndSend(ar);
                    Console.WriteLine("Сообщение отправлено серверу");
                }, null);
            }
        }


        private static void ReceiveCallback(IAsyncResult ar) {
            Socket client = ClientMain.client.Client;
            int bytesRead = 0;
            try {
                bytesRead = client.EndReceive(ar);//количество байт записанных в буффер
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10054)
                {
                    Console.WriteLine("Потеря соединения с сервером");
                    return;
                }
                else
                    throw se;
            }
            if (bytesRead > 0)
                ClientMain.recieveData.Write(ClientMain.buffer, 0, bytesRead);
            // ОБРАБОТКА ПОТОКА
            while (ClientMain.recieveData.Length > 4) {
                ClientMain.recieveData.Seek(0, SeekOrigin.Begin);
                byte[] head = new byte[4];
                ClientMain.recieveData.Read(head, 0, 4);
                int sizeContent = BitConverter.ToInt32(head, 0);
                if (ClientMain.recieveData.Length >= sizeContent + 4) {
                    IFormatter formatter = new BinaryFormatter();
                    byte[] dataMessage = new byte[sizeContent];
                    ClientMain.recieveData.Seek(4, SeekOrigin.Begin);
                    ClientMain.recieveData.Read(dataMessage, 0, sizeContent);
                    using (MemoryStream msTemp = new MemoryStream(dataMessage)) 
                        RecieveMessage((Message) formatter.Deserialize(msTemp));
                    if (ClientMain.recieveData.Length - sizeContent - 4 > 0) {
                        byte[] dataOther = new byte[ClientMain.recieveData.Length - sizeContent - 4];
                        ClientMain.recieveData.Seek(sizeContent + 4, SeekOrigin.Begin);
                        ClientMain.recieveData.Read(dataOther, 0, dataOther.Length);
                        ClientMain.recieveData.SetLength(0);
                        ClientMain.recieveData.Write(dataOther, 0, dataOther.Length);
                        ClientMain.recieveData.Seek(0, SeekOrigin.End);
                    }
                    else
                        ClientMain.recieveData.SetLength(0);

                }
                else {
                    ClientMain.recieveData.Seek(0, SeekOrigin.End);
                    break;
                }
            }

            client.BeginReceive(ClientMain.buffer, 0, ClientMain.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), null);//ждем еще данные
        }

    }
}
