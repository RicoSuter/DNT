using System;

using ClassLibrary10;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World from ConsoleApp1!");

            var cl10 = new ClassLibrary10.Class10_1();
            cl10.Hello();

            Console.WriteLine("Press <Enter> to continue");
            Console.ReadLine();
        }
    }
}
