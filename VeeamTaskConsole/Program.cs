using System;
using VeeamTaskLib;

namespace VeeamTaskConsole {
    partial class Program {
        public static int Main()    {
            
            var veeamTask = new VeeamTask();
            
            while(true) {

                string[] args = new string[3];

                Console.WriteLine("Enter mode (compress/decompress):");
                args[0] = Console.ReadLine();

                Console.WriteLine("Enter source file name:");
                args[1] = Console.ReadLine();

                Console.WriteLine("Enter destination file name:");
                args[2] = Console.ReadLine();

                var result = veeamTask.Run(args);

                if (result.IsFailure) {
                    Console.WriteLine(result.Error);
                }
                else {
                    Console.WriteLine("Ok");
                }
            }

        }


    }
}
