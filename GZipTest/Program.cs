using System;
using VeeamTaskLib;

namespace GZipTest {
    class Program {
        public static int Main(string[] args) {
            var veeamTask = new VeeamTask();
            var result = veeamTask.Run(args);
            if (result.IsFailure) {
                Console.WriteLine(result.Error);
                return 1;
            }
            var message = result.IsSuccess ? "Ok" : result.Error;
            Console.WriteLine($"Process finished with result: {message}");
            return result.IsSuccess ? 0 : 1;
        }
    }
}
