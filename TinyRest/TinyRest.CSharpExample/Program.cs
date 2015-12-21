using System;

namespace TinyRest.CSharpExample
{
    class Program
    {
        private static int count = 0;

        private static void Main(string[] args)
        {
            TinyRestServerCSharp.TinyRest.Server()
                .WithLogger(new TinyRestServer.ConsoleLogger())
                .WithHttp()
                .WithPort(8001)
                .WithBasePath("/ApiMocking")
                .OnGetPath("/", (request, response) => "coucou " + (count++))
                //.OnGetPath("/json", (request, response) => response.Json(new
                //{
                //    Text = "coucou " + (count++)
                //}))
                .OnGetPath("/api/v1/accounts/forgot-passwords", (request, response) => "ça marche")
                .OnGetPath("/api/authorize", (request, response) => "ça log")
                
                .Create()
                .Listen();

            Console.WriteLine("Press any key to kill the server ...");
            Console.ReadKey(true);
        }

    }
}
