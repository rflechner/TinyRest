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

    let skipStart (start:string) (str:string) =
        if str.StartsWith start |> not then
                str
            else
                str.Substring(start.Length)

    let (|SkipStart|_|) (start:string) (stro:string option) =
        match stro with
        | None      -> None
        | Some str  -> Some (skipStart start str)

    let ensureEndsWith e (s:string) = if s.EndsWith e then s else s + e
    let ensureStartsWith e (s:string) = if s.StartsWith e then s else e + s

    let (|EnsureStartsWith|_|) (start:string) (stro:string option) =
        match stro with
        | None      -> None
        | Some str  -> Some (ensureStartsWith start str)

    let buildPrefix (c:HttpServerConfig) =
        let b = new StringBuilder()
        match c.Schema with
        | Http  -> b.Append "http" |> ignore
        | Https -> b.Append "https" |> ignore

        b.Append "://*:" |> ignore
        b.Append c.Port |> ignore
        b.Append "/" |> ignore

        match c.BasePath with
        | None            -> ()
        | SkipStart "/" p -> p |> ensureEndsWith "/" |> b.Append |> ignore
        | Some p          -> p |> ensureEndsWith "/" |> b.Append |> ignore

        b.ToString()

    let GET pattern handler = { Verb=Get; Pattern=pattern; Handler=handler; }
    let POST pattern handler = { Verb=Post; Pattern=pattern; Handler=handler; }
    let PUT pattern handler = { Verb=Put; Pattern=pattern; Handler=handler; }
    let DELETE pattern handler = { Verb=Delete; Pattern=pattern; Handler=handler; }

    let compiledRoutes = new System.Collections.Generic.Dictionary<string, Regex>()

    let routeMatch (pattern:RoutePattern) (path:string) =
        match pattern with
        | Path p    -> path = (p |> ensureStartsWith "/")
        | Regex p   -> 
            if compiledRoutes.ContainsKey p |> not then
                compiledRoutes.Add (p, new Regex (p))
            let regex = compiledRoutes.[p]
            regex.IsMatch path
        | Format f  ->
            

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
                    let route = conf.Routes |> Seq.tryFind (fun r -> r.Verb = verb && routeMatch r.Pattern path)
                    match route with
                    | None   -> 
                        log (conf.Logger) (sprintf "Invalid route: %s" path)
                        let out = Text.Encoding.UTF8.GetBytes ("Invalid route: " + path)
                        resp.OutputStream.Write(out,0,out.Length)
                        resp.OutputStream.Flush()
                        resp.OutputStream.Dispose()
                    | Some r -> 
                        let reply = r.Handler req resp
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
    
    let get p f = GET (Path(p)) <| f
    let getPattern p f = GET (Regex(p)) <| f
    
    let post p f = POST (Path(p)) <| f
    let postPattern p f = POST (Regex(p)) <| f
    
    let put p f = PUT (Path(p)) <| f
    let putPattern p f = PUT (Regex(p)) <| f
    
    let delete p f = DELETE (Path(p)) <| f
    let deletePattern p f = DELETE (Regex(p)) <| f


    type FluentHttpReply(f:Action<IHttpRequest, IHttpResponse>) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    f.Invoke(q,r)
                    close r None
                }

    type RoutesBuilder () =
        let mutable routes:HttpRoute seq = Seq.empty

        member x.OnGetPattern (p, a:Action<IHttpRequest, IHttpResponse>) =
            let r = GET (Regex(p)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
            routes <- routes |> Seq.append [r]
            x

        member x.OnPostPattern (p, a:Action<IHttpRequest, IHttpResponse>) =
            let r = POST (Regex(p)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
            routes <- routes |> Seq.append [r]
            x

        member x.OnGetPath (p, a:Action<IHttpRequest, IHttpResponse>) =
            let r = GET (Path(p)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
            routes <- routes |> Seq.append [r]
            x

        member x.OnPostPath (p, a:Action<IHttpRequest, IHttpResponse>) =
            let r = POST (Path(p)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
            routes <- routes |> Seq.append [r]
            x

        member x.AddPath (verb, path, a:Action<IHttpRequest, IHttpResponse>) =
            let r = match verb with
                    | HttpVerb.Get -> GET (Path(path)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
                    | HttpVerb.Post -> POST (Path(path)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
                    | HttpVerb.Put -> PUT (Path(path)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
                    | HttpVerb.Delete -> DELETE (Path(path)) <| fun q r -> new FluentHttpReply(a) :> IHttpReply
            routes <- routes |> Seq.append [r]
            x



