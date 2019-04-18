using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpBroadcast
{
    class Program
    { 
        static void Main(string[] args)
        {
            try
            { 
                Console.WriteLine("enter your name");
                string name = Console.ReadLine();
                p2p chat = new p2p(name);
                chat.SendMessage();

                Thread ListenThread = new Thread(new ThreadStart(chat.Listen));
                ListenThread.Start();

                Thread ListenThread2 = new Thread(new ThreadStart(chat.TCPListen));
                ListenThread2.Start();

                Thread ListenThread3 = new Thread(new ThreadStart(chat.HistoryListen));
                ListenThread3.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

    }
}
