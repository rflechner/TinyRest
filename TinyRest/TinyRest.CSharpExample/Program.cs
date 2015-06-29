using System;
using TinyRestServerCSharp;

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
                .WithBasePath("/learning")
                .OnGetPath("/", (request, response) => "coucou " + (count++))
                .OnGetPath("/json", (request, response) => response.Json(new
                {
                    Text = "coucou " + (count++)
                }))
                .Create()
                .Listen();

            Console.Read();
        }

    }
}
