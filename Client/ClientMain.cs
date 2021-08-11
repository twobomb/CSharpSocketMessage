using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreCS;

namespace Client
{
    class ClientMain{
        static void Main(string[] args){

            TcpClient client = new TcpClient("127.0.0.1",2255);//Коннект к серверу
            
            for (int i = 0; i < 100; i++){//Шлём 100 сообщений
                using (var stream = new NetworkStream(client.Client)){//Берем поток сокета
                    BinaryFormatter bf = new BinaryFormatter();
                    var msg = new Message(){//Создаем сообщение которое хотим послать на сервер
                        id = i,
                        message = "Hello world",
                        title = "My Title"
                    };
                    using (MemoryStream ms = new MemoryStream()){
                        bf.Serialize(ms, msg);//сериализуем и пишем в поток
                        var arr = ms.GetBuffer();

                        stream.Write(BitConverter.GetBytes(arr.Length),0,4);//Записываем длинну буфера,естественно длинна буфер не должна быть больше Int32.MaxValue (2147483647) это ~ 2гига
                        stream.Write(arr,0,arr.Length);//Записываем сам буффер
                    }
                }
                //Thread.Sleep(100); //Если добавить задержку то всё ок
            }

            Console.WriteLine("end");
            Console.ReadKey();
        }
    }
}
