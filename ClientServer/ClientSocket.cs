using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreCS;

namespace Server{
    public class ClientSocket{
        private Socket socket;
        private MemoryStream recieveData = new MemoryStream();
        public const int BufferSize = 512; //Размер буфера
        byte[] buffer = new byte[BufferSize];

        public ClientSocket(Socket s){
            this.socket = s;
            socket.BeginReceive(this.buffer, 0, ClientSocket.BufferSize, 0, new AsyncCallback(ReceiveCallback), this);
        }

        private void RecieveMessage(Message msg) {//Вызывается когда получили запрос от клиента
            Console.WriteLine("Recieve: {0} {1} {2}", msg.id, msg.title, msg.message);//выводим на экран
            msg.title = "ответ";
            msg.message = "hi";
            this.SendMessage(msg);//отправим ответ клиенту
        }
        private static void ReceiveCallback(IAsyncResult ar){
            ClientSocket state = (ClientSocket)ar.AsyncState;
            Socket client = state.socket;
            int bytesRead = 0;
            try{
                bytesRead = client.EndReceive(ar);//количество байт записанных в буффер
            }
            catch (SocketException se){
                if (se.ErrorCode == 10054){
                    Console.WriteLine("Клиент отключился");
                    return;
                }
                else
                    throw se;
            }
            if (bytesRead > 0){
                state.recieveData.Write(state.buffer, 0, bytesRead);//пишет в поток принятых байт
                Console.WriteLine("read {0} byte, available {1}", bytesRead, client.Available);
            }
            // ОБРАБОТКА ПОТОКА
                while(state.recieveData.Length > 4) {//обрабатываем поток пока там его длинна больше нашего заголовка, длинна 4 потому-что int занимает 4 байта, это заголовок 
                    state.recieveData.Seek(0, SeekOrigin.Begin);//Ставим указатель в начало поток, потому что read читает от указателя, это тупо 
                    byte[] head = new byte[4];//Буфер для заголовка
                    state.recieveData.Read(head, 0, 4);//записываем заголовок (размер сообщения)  в буффер
                    int sizeContent = BitConverter.ToInt32(head, 0);//Заголовок содержит длину Message в байтах, которую Message занимает после заголовка
                    if (state.recieveData.Length  >= sizeContent + 4) { //Если мы приняли данных уже на длинну сообщений + длинну заголовка, значит можно достать данные
                        IFormatter formatter = new BinaryFormatter();
                        byte[] dataMessage = new byte[sizeContent];//Создаем буффер для сообщения
                        state.recieveData.Seek(4, SeekOrigin.Begin);//Ставим указатель в потоке на начало нашего сообщения ( 1байт после заголовка)
                        state.recieveData.Read(dataMessage, 0, sizeContent);//Пишем наше сообщение в буфер
                        using (MemoryStream msTemp = new MemoryStream(dataMessage)) //Создаем временный поток и пишем в  него сообщения для десереализации
                            state.RecieveMessage((Message)formatter.Deserialize(msTemp));//десереализируем сообщения и передаем методу recievemesssage
                        //Далее на нужно удалить из потока наш заголовок + данные
                        if (state.recieveData.Length - sizeContent - 4 > 0) {//Если у нас в потоке еще остались данные кроме тех что мы прочитали
                            byte[] dataOther = new byte[state.recieveData.Length - sizeContent - 4];//Создаем буффер для оставшихся
                            state.recieveData.Seek(sizeContent + 4, SeekOrigin.Begin);//ставим указатель в потоке на начало оставшихся данных
                            state.recieveData.Read(dataOther, 0, dataOther.Length);//записываем оставшиеся данные в буффер
                            state.recieveData.SetLength(0);//сбрасываем поток...
                            state.recieveData.Write(dataOther,0,dataOther.Length);//записываем в поток оставшиеся данные
                            state.recieveData.Seek(0, SeekOrigin.End);//ставим указатель в конец потока на всякий случай
                    }
                        else//Если в потоке больше нет данных
                            state.recieveData.SetLength(0);//сбрасываем поток...

                    }
                    else { //Иначе если мы приняли данных меньше чем длинна сообщений + длинна заголовка, значит выходим из цикла, нам нужно еще чуть чуть принять
                        state.recieveData.Seek(0, SeekOrigin.End);//ставим указатель в конец потока на всякий случай
                        break;
                    }
                }

            client.BeginReceive(state.buffer, 0, ClientSocket.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);//ждем еще данные
        }

        public void SendMessage(Message msg) {//Послать сообщение клиенту
            using (MemoryStream ms = new MemoryStream()) {
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                ms.WriteByte(0);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, msg);
                ms.Seek(0, SeekOrigin.Begin);
                ms.Write(BitConverter.GetBytes(ms.Length-4),0,4);
                ms.Seek(0, SeekOrigin.End);
                socket.BeginSend(ms.GetBuffer(), 0, (int) ms.Length, 0, ar => {
                    (ar.AsyncState as ClientSocket).socket.EndSend(ar);
                    Console.WriteLine("Сообщение отправлено клиенту");
                }, this);
            }
        }
        ~ClientSocket()
        {
            if (recieveData != null)
            {
                recieveData.Close();
                recieveData.Dispose();
            }
        }
    }
}
