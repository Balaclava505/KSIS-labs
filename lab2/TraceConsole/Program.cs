using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TraceConsole
{
    class Program
    {
        private static MyTraceroute traceroute;
        private static string lastHopIP;
        private static int hopsCount;
        private const int maxHopsCount = 20;

        static void Main(string[] args)
        {
            string IPOrDomain = Console.ReadLine();
            traceroute = new MyTraceroute(IPOrDomain);

            hopsCount = 0;
            lastHopIP = "";

            for (int rowCount = 1; rowCount <= maxHopsCount; rowCount++)
            {
                traceroute.InitTTL();

                Console.WriteLine($"{rowCount}");
                for (int i = 1; i <= 3; i++)
                {
                    traceroute.InitToSend();
                    Console.WriteLine($"{traceroute.sendAndReceive()}");
                }

                if (lastHopIP == traceroute.hopIP)
                    Console.WriteLine("Превышен интервал ожидания для запроса.");
                else
                {
                    Console.WriteLine($"{traceroute.hopIP}");
                    lastHopIP = traceroute.hopIP;
                }

                Console.WriteLine("\n");

                hopsCount++;
                if (traceroute.hopIP == traceroute.ip.ToString())
                {
                    Console.WriteLine("Трассировка завершена.");
                    Console.ReadKey();
                }
                else if (hopsCount >= maxHopsCount)
                {
                    Console.WriteLine("Максимальное количество прыжков было достигнуто."); 
                }
            }
        }
    }

   
}
