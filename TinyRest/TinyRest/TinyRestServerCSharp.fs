namespace TinyRestServerCSharp

    open System.Collections.Generic
    open System.Net
    open System.Runtime.CompilerServices

    open Http
    open Routing
    open TinyRestServerPCL
    open TinyRestServer

    type HttpRestServer (schema, port, basePath, routes, logger) =
        member x.Listen () =
            let listener = new Listener()
            let conf = { Schema=schema; Port=port; BasePath=Some basePath; Routes=routes |> Seq.toList; Logger=logger }
            listener |> listen conf

    type ServerBuilder() =
        let mutable port:int = 8001
        let mutable basePath:string = "/"
        let mutable schema = Http
        let mutable logger:ILogger option = None

        let routes = new List<HttpRoute>()

        let toHttpReply (o:obj) = 
            match o with
            | null               -> text ""
            | :? IHttpReply as r -> r
            | :? string as r     -> text r
            | _                  -> text (o.ToString())

        member x.WithLogger p = 
            logger <- Some(p)
            x
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

        member x.OnGetPattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Get r |> routes.Add
            x

        member x.OnPutPattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Put r |> routes.Add
            x

        member x.OnDeletePattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Delete r |> routes.Add
            x

        member x.OnPostPattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Post r |> routes.Add
            x

        member x.OnGetPath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Get r |> routes.Add
            x

        member x.OnPostPath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Post r |> routes.Add
            x

        member x.OnPutPath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Put r |> routes.Add
            x

        member x.OnDeletePath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Delete r |> routes.Add
            x

        member x.Create () =
            new HttpRestServer(schema, port, basePath, routes, logger)

    type TinyRest () =
        static member Server() =
            new ServerBuilder()

    [<Extension>]
    type HttpListenerResponseExtensions () =
         [<Extension>]
         static member inline Json (r:IHttpResponse, o:obj) = Newtonsoft.Json.JsonConvert.SerializeObject(o) |> text

//    module Extensions =
//        type System.Net.HttpListenerResponse with
//            member x.Json (o) = Newtonsoft.Json.JsonConvert.SerializeObject(o)

