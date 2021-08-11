using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class ServerMain
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 2255);
            listener.Start(100);//Запускаем сервер, слушаем порт 2255
            Console.WriteLine("Listen 2255");
            while (true){
                new ClientSocket(listener.AcceptSocket());//Ждем подключения клиента, при подключении создаем новый класс ClientSocket
                Console.WriteLine("Socket connect");//Сокет подключен, ждем других клиентов
            }
        }


    }
}
