using System;


namespace ConsoleApp2
{
    class Program
    {

        static void Main(string[] args)
        {
            Chat main = new Chat();
            main.ClientConnect();
            Console.WriteLine("Write your message or '/diconnect' below");
            while (true)
            {
                string message = Console.ReadLine();
                if (message != "/disconnect")
                    main.ClientSendMessage(message);
                else break;
            }
            main.ClientDisconnect();
        }
    }
}
