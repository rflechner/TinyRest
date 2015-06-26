# TinyRest
A tiny FSharp and CSharp Rest server written in F#

# Install

A nuget package exists hehe: https://www.nuget.org/packages/TinyRest/

	PM> Install-Package TinyRest

# Usage in FSharp

There an example of a simple file server here: 
  https://github.com/rflechner/TinyRest/blob/master/TinyRest/TinyRest/TinyRestServer-sample.fsx



    let routes = [
                GET (Path("/")) <| fun q r -> text "coucou"
                get "/bye" <| fun q r -> text "bye bye\n@++"
                getPattern "/haha/(.*)" <| fun q r -> text "ha ha"
                GET (Path("/files")) <| listFiles
                get "/download" <| download
             ]

    let conf = { Schema=Http; Port=8009; BasePath=Some "/TinyRest1"; Routes=routes; }
	listen conf
	Console.Read () |> ignore

# Usage in CSharp

    class Program
    {
        private static int count = 0;

        private static void Main(string[] args)
        {
            TinyRestServerCSharp.TinyRest.Server()
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

