using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyRest.CSharpPCLExample
{
    public class Host
    {
        int count = 0;

        public void Run(TinyRestServerPCL.IHttpListener listener, TinyRestServerPCL.ILogger logger)
        {

            /*
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
             */

            listener.AddPrefix("http://+:8001");

            var routes = new List<TinyRestServerPCL.HttpRoute>();
            
            new TinyRestServerPCL.RoutesBuilder()
                .OnGetPath("/", (request, response) =>
                    {
                        var writer = new System.IO.StreamWriter(response.OutputStream);
                        writer.WriteLine("coucou");
                    }
                );
            
        }

    }

    //public class TextHttpReply : TinyRestServerPCL.IHttpReply
    //{

    //    public Microsoft.FSharp.Control.FSharpAsync<Microsoft.FSharp.Core.Unit> Send(TinyRestServerPCL.IHttpRequest value, TinyRestServerPCL.IHttpResponse value)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
