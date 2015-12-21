module public TinyRestServerPCL

    open System
    open System.Text
    open System.Text.RegularExpressions
    open System.Net
    open System.IO

    open Http
    open Routing

    let log (logger:ILogger option) (text:string) =
        match logger with
        | Some l -> l.Log(text)
        | None   -> ()

    let close (resp:IHttpResponse) (logger:ILogger option) =
        try 
            resp.OutputStream.Flush()
            resp.OutputStream.Dispose() 
        with e -> log logger (sprintf "Error: %s" e.Message)

    type TextHttpReply(txt:string, ?logger:ILogger) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    let out = Text.Encoding.UTF8.GetBytes txt
                    r.OutputStream.Write(out,0,out.Length)
                    close r logger
                }
    
    type HtmlHttpReply(txt:string, ?logger:ILogger) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    r.ContentType <- "text/html"
                    let out = Text.Encoding.UTF8.GetBytes txt
                    r.OutputStream.Write(out,0,out.Length)
                    close r logger
                }

    type ErrorHttpReply(txt:string, ?logger:ILogger) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    log logger (sprintf "Http error: %s" txt)
                    r.StatusCode <- 500
                    let out = Text.Encoding.UTF8.GetBytes txt
                    r.OutputStream.Write(out,0,out.Length)
                    close r logger
                }

    let handler :(IHttpRequest -> IHttpResponse -> HttpServerConfig -> Async<unit>) = 
        fun req resp conf -> 
            async {
                try
                    log (conf.Logger) (sprintf "RawUrl: %s" req.RawUrl)
                    log (conf.Logger) (sprintf "AbsolutePath: %s" req.Url.AbsolutePath)
                    let bp = match conf.BasePath with | Some s -> s | None -> "/"
                    let path = skipStart bp req.Url.AbsolutePath |> ensureStartsWith "/"
                    log (conf.Logger) (sprintf "path: %s" path)

                    let verb = req.HttpMethod |> toVerb
                    let route = conf.Routes 
                                |> Seq.filter (fun r -> r.Verb = verb)
                                |> Seq.choose (fun r -> r.Handler.TryHandle path req resp)
                                |> Seq.tryHead
                    match route with
                    | None   -> 
                        log (conf.Logger) (sprintf "Invalid route: %s" path)
                        let out = Text.Encoding.UTF8.GetBytes ("Invalid route: " + path)
                        resp.OutputStream.Write(out,0,out.Length)
                        resp.OutputStream.Flush()
                        resp.OutputStream.Dispose()
                    | Some reply -> 
                        reply.Send req resp |> Async.StartImmediate
                with e -> log (conf.Logger) (sprintf "Error: %s" e.Message)
                          close resp (conf.Logger)
            }

    let listen (conf:HttpServerConfig) (listener:IHttpListener) = 
        let prefix = conf |> buildPrefix 
        log (conf.Logger) (sprintf "listen prefix: %s" prefix)
        prefix |> listener.AddPrefix
        listener.Start()
        let asynctask = Async.FromBeginEnd(listener.BeginGetContext,listener.EndGetContext)
        async {
            while true do 
                let! context = asynctask
                Async.Start (handler context.Request context.Response conf)
        } |> Async.Start
        listener

    let text s = new TextHttpReply(s) :> IHttpReply
    let html s = new TextHttpReply(s) :> IHttpReply

    type IOjectModelSerializer =
        abstract member Serialize<'t> : 't * target:Stream -> unit

    type ObjectModelHttpReply<'t>(model:'t, serializer:IOjectModelSerializer, contentType:string, ?logger:ILogger) =
       interface IHttpReply with
            member x.Send _ r =
                async {
                    r.ContentType <- contentType
                    serializer.Serialize(model, r.OutputStream)
                    close r logger
                }

    type JsonModelSerializer() =
        interface IOjectModelSerializer with
            member x.Serialize<'t> (m:'t,target:Stream) =
                let out = m |> Newtonsoft.Json.JsonConvert.SerializeObject |> Text.Encoding.UTF8.GetBytes
                target.Write(out,0,out.Length)
                target.Flush()

    let sendModel<'t, 's when 's :> IOjectModelSerializer> (m:'t) contentType (s:unit -> 's) = 
        ObjectModelHttpReply(m, s(), contentType) :> IHttpReply

    let json m =
        sendModel m "application/json" <| fun _ -> JsonModelSerializer() :> IOjectModelSerializer

    let path p h =
        RoutePathHandler.New p h :> IRouteHandler

    let regex p h =
        let r = new Regex(p, RegexOptions.IgnorePatternWhitespace ||| RegexOptions.IgnoreCase)
        RouteRegexHandler.New r h  :> IRouteHandler

    let format (pf : PrintfFormat<_,_,_,_,'t>) (h : RestRequest<'t> -> IHttpReply) =
        let f = pf.Value |> FormatParser.Create |> fun p -> p.Parse(typeof<'t>)
        RouteFormatHandler<'t>.New f h  :> IRouteHandler

    let buildRoutes v (handlers:#IRouteHandler list) = 
        handlers
        |> List.map (fun h -> HttpRoute.From v h)

    let GET (handlers:#IRouteHandler list) = 
        buildRoutes HttpVerb.Get handlers
    
    let POST (handlers:#IRouteHandler list) = 
        buildRoutes HttpVerb.Post handlers

    let PUT (handlers:#IRouteHandler list) = 
        buildRoutes HttpVerb.Put handlers

    let DELETE (handlers:#IRouteHandler list) = 
        buildRoutes HttpVerb.Delete handlers

    let rr = 
        GET [
            path "/test" <| fun r -> failwith ""
            path "/sasa" <| fun r -> json "touotu"
        ]


    type FluentHttpReply(f:Action<IHttpRequest, IHttpResponse>) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    f.Invoke(q,r)
                    close r None
                }

    type RoutesBuilder () =
        let mutable routes:HttpRoute list = List.empty

        let add r = routes <- r :: routes
        let toHttpReply (o:obj) = 
            match o with
            | null               -> text ""
            | :? IHttpReply as r -> r
            | :? string as r     -> text r
            | _                  -> text (o.ToString())

        member x.OnGetPattern (p, a:Action<IHttpRequest, IHttpResponse>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Get r |> add
            x

        member x.OnPutPattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Put r |> add
            x

        member x.OnDeletePattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Delete r |> add
            x

        member x.OnPostPattern (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = regex p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Post r |> add
            x

        member x.OnGetPath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Get r |> add
            x

        member x.OnPostPath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Post r |> add
            x

        member x.OnPutPath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Put r |> add
            x

        member x.OnDeletePath (p, a:System.Func<IHttpRequest, IHttpResponse, obj>) =
            let r = path p <| fun r -> a.Invoke(r.Request, r.Response) |> toHttpReply
            HttpRoute.From HttpVerb.Delete r |> add
            x




