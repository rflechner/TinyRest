namespace TinyRestServerCSharp

    open TinyRestServer
    open System.Collections.Generic
    open System.Net
    open System.Runtime.CompilerServices

    type HttpRestServer (schema, port, basePath, routes) =
        member x.Listen () =
            let conf = { Schema=schema; Port=port; BasePath=Some basePath; Routes=routes |> Seq.toList; }
            listen conf

    type ServerBuilder() =
        let mutable port:int = 8001
        let mutable basePath:string = "/"
        let mutable schema = Http
        let routes = new List<HttpRoute>()

        let toHttpReply (o:obj) = 
            match o with
            | null               -> text ""
            | :? IHttpReply as r -> r
            | :? string as r     -> text r
            | _                  -> text (o.ToString())

        member x.WithPort p = 
            port <- p
            x
        member x.WithBasePath p = 
            basePath <- p
            x
        member x.WithHttp () = 
            schema <- Http
            x
        member x.WithHttps () = 
            schema <- Https
            x

        member x.OnGetPattern (p, a:System.Func<HttpListenerRequest, HttpListenerResponse, obj>) =
            let r = GET (Regex(p)) <| fun q r -> a.Invoke(q,r) |> toHttpReply
            routes.Add r
            x

        member x.OnPostPattern (p, a:System.Func<HttpListenerRequest, HttpListenerResponse, obj>) =
            let r = POST (Regex(p)) <| fun q r -> a.Invoke(q,r) |> toHttpReply
            routes.Add r
            x

        member x.OnGetPath (p, a:System.Func<HttpListenerRequest, HttpListenerResponse, obj>) =
            let r = GET (Path(p)) <| fun q r -> a.Invoke(q,r) |> toHttpReply
            routes.Add r
            x

        member x.OnPostPath (p, a:System.Func<HttpListenerRequest, HttpListenerResponse, obj>) =
            let r = POST (Path(p)) <| fun q r -> a.Invoke(q,r) |> toHttpReply
            routes.Add r
            x

        member x.Create () =
            new HttpRestServer(schema, port, basePath, routes)

    type TinyRest () =
        static member Server() =
            new ServerBuilder()

    [<Extension>]
    type HttpListenerResponseExtensions () =
         [<Extension>]
         static member inline Json (r:System.Net.HttpListenerResponse, o:obj) = Newtonsoft.Json.JsonConvert.SerializeObject(o) |> text

//    module Extensions =
//        type System.Net.HttpListenerResponse with
//            member x.Json (o) = Newtonsoft.Json.JsonConvert.SerializeObject(o)

