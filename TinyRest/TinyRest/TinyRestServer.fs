module TinyRestServer

    open System
    open System.Text
    open System.Text.RegularExpressions
    open System.Net

    open TinyRestServerPCL


    type File = System.IO.File
//
//    type HttpSchema = 
//        | Http
//        | Https
//
//    type HttpVerb = 
//        | Get
//        | Post
//        | Put
//        | Delete
//
//
//    type IHttpReply =
//       abstract member Send : HttpListenerRequest -> HttpListenerResponse -> Async<unit>
//
//    type HttpHandler = HttpListenerRequest -> HttpListenerResponse -> IHttpReply
//
//    type RoutePattern = 
//        | Path      of string
//        | Regex     of string
//        //| Format    of string
//
//    type HttpRoute = {
//        Verb: HttpVerb
//        Pattern: RoutePattern
//        Handler: HttpHandler
//    }
//
//    type HttpServerConfig = {
//        Schema: HttpSchema
//        Port: int
//        BasePath: string option
//        Routes: HttpRoute list
//    }
//
//    let close (resp:HttpListenerResponse) =
//        try 
//            resp.OutputStream.Flush()
//            resp.OutputStream.Close() 
//        with e -> printfn "Error: %s" e.Message
//
//    type TextHttpReply(txt:string) =
//       interface IHttpReply with
//            member x.Send q r =
//                async {
//                    let out = Text.Encoding.ASCII.GetBytes txt
//                    r.OutputStream.Write(out,0,out.Length)
//                    close r
//                }
//    
//    type HtmlHttpReply(txt:string) =
//       interface IHttpReply with
//            member x.Send q r =
//                async {
//                    r.ContentType <- "text/html"
//                    let out = Text.Encoding.UTF8.GetBytes txt
//                    r.OutputStream.Write(out,0,out.Length)
//                    close r
//                }
//
//    type ErrorHttpReply(txt:string) =
//       interface IHttpReply with
//            member x.Send q r =
//                async {
//                    printfn "Http error: %s" txt
//                    r.StatusCode <- 500
//                    let out = Text.Encoding.ASCII.GetBytes txt
//                    r.OutputStream.Write(out,0,out.Length)
//                    close r
//                }

    type ConsoleLogger () =
        interface ILogger with
            member x.Log (text:string) =
                printfn "%s" text

    type StaticFileReply (path:string, ?logger:ILogger) =
        interface IHttpReply with
            member x.Send q r =
                async {
                    use fs = File.OpenRead(path)
                    fs.CopyTo(r.OutputStream)
                    close r logger
                }

    let flatten (nvc:Collections.Specialized.NameValueCollection) =
        nvc.AllKeys |> Seq.map (fun k -> new System.Collections.Generic.KeyValuePair<string, string>(k, nvc.[k]))

    type HttpRequest(r:HttpListenerRequest) =
        interface IHttpRequest with
            member x.AcceptTypes = r.AcceptTypes
            member x.ClientCertificateError = r.ClientCertificateError
            member x.ContentEncoding = r.ContentEncoding
            member x.ContentLength64 = r.ContentLength64
            member x.ContentType = r.ContentType
            member x.Cookies = r.Cookies
            member x.HasEntityBody = r.HasEntityBody
            member x.Headers = r.Headers |> flatten
            member x.HttpMethod = r.HttpMethod
            member x.InputStream = r.InputStream
            member x.IsAuthenticated = r.IsAuthenticated
            member x.IsLocal = r.IsLocal
            member x.IsSecureConnection = r.IsSecureConnection
            member x.IsWebSocketRequest = r.IsWebSocketRequest
            member x.KeepAlive = r.KeepAlive
            member x.LocalEndPoint = new IPEndPoint(IpAddress=r.LocalEndPoint.Address.ToString(), Port=r.LocalEndPoint.Port)
            member x.ProtocolVersion = r.ProtocolVersion
            member x.QueryString = r.QueryString |> flatten
            member x.RawUrl = r.RawUrl
            member x.RemoteEndPoint = new IPEndPoint(IpAddress=r.RemoteEndPoint.Address.ToString(), Port=r.RemoteEndPoint.Port)
            member x.RequestTraceIdentifier = r.RequestTraceIdentifier
            member x.TransportContext = r.TransportContext
            member x.Url = r.Url
            member x.UrlReferrer = r.UrlReferrer
            member x.UserAgent = r.UserAgent
            member x.UserHostAddress = r.UserHostAddress
            member x.UserHostName = r.UserHostName
            member x.UserLanguages = r.UserLanguages

    type HttpResponse(r:HttpListenerResponse) =
        interface IHttpResponse with
            member x.Abort () = r.Abort()
            member x.AddHeader (name:string,value:string) = r.AddHeader(name, value)
            member x.AppendCookie (cookie:Cookie) = r.AppendCookie(cookie)
            member x.AppendHeader (name:string, value:string) = r.AppendHeader(name, value)
            member x.Close () = r.Close()
            member x.Close (responseEntity:byte [],willBlock:bool) = r.Close(responseEntity, willBlock)
            member x.ContentEncoding 
                with get() = r.ContentEncoding 
                and set(v) = r.ContentEncoding <- v
            member x.ContentLength64
                with get() = r.ContentLength64
                and set(v) = r.ContentLength64 <- v
            member x.ContentType
                with get() = r.ContentType
                and set(v) = r.ContentType <- v
            member x.Cookies
                with get() = r.Cookies
                and set(v) = r.Cookies <- v
            member x.Headers
                with get() = r.Headers
                and set(v) = r.Headers <- v
            member x.KeepAlive
                with get() = r.KeepAlive
                and set(v) = r.KeepAlive <- v
            member x.OutputStream = r.OutputStream
            member x.ProtocolVersion 
                with get() = r.ProtocolVersion
                and set(v) = r.ProtocolVersion <- v
            member x.Redirect(url:string) = r.Redirect(url)
            member x.RedirectLocation
                with get() = r.RedirectLocation
                and set(v) = r.RedirectLocation <- v
            member x.SendChunked 
                with get() = r.SendChunked
                and set(v) = r.SendChunked <- v
            member x.SetCookie(cookie:Cookie) = r.SetCookie(cookie)
            member x.StatusCode
                with get() = r.StatusCode
                and set(v) = r.StatusCode <- v
            member x.StatusDescription
                with get() = r.StatusDescription
                and set(v) = r.StatusDescription <- v
        interface IDisposable with
            member x.Dispose() = r.Close()

    type ListnerContext(ctx:HttpListenerContext) =
        interface IHttpListenerContext with
            member x.Request : IHttpRequest = new HttpRequest(ctx.Request) :> IHttpRequest
            member x.Response : IHttpResponse = new HttpResponse(ctx.Response) :> IHttpResponse

    type Listener () =
        let listener = new HttpListener()
        interface IHttpListener with
            member x.AddPrefix (p:string) =
                listener.Prefixes.Add p

            member x.Start() =
                listener.Start()

            member x.BeginGetContext (callback:AsyncCallback, state:obj) =
                listener.BeginGetContext(callback, state)
            
            member x.Close() =
                listener.Close()

            member x.EndGetContext (asyncResult:IAsyncResult) =
                new ListnerContext(listener.EndGetContext(asyncResult)) :> IHttpListenerContext

            member x.GetContext () =
                new ListnerContext(listener.GetContext()) :> IHttpListenerContext

            member x.GetContextAsync () : Threading.Tasks.Task<IHttpListenerContext> =
                async {
                    let! c = listener.GetContextAsync() |> Async.AwaitTask
                    return (new ListnerContext(c) :> IHttpListenerContext)
                } |> Async.StartAsTask

    let getVal key (nvc:NameValueCollection) =
        nvc |> Seq.tryPick(fun kv -> if kv.Key = key then Some(kv.Value) else None)

//    let skipStart (start:string) (str:string) =
//        if str.StartsWith start |> not then
//                str
//            else
//                str.Substring(start.Length)
//
//    let (|SkipStart|_|) (start:string) (stro:string option) =
//        match stro with
//        | None      -> None
//        | Some str  -> Some (skipStart start str)
//
//    let ensureEndsWith e (s:string) = if s.EndsWith e then s else s + e
//    let ensureStartsWith e (s:string) = if s.StartsWith e then s else e + s
//
//    let (|EnsureStartsWith|_|) (start:string) (stro:string option) =
//        match stro with
//        | None      -> None
//        | Some str  -> Some (ensureStartsWith start str)
//
//    let buildPrefix (c:HttpServerConfig) =
//        let b = new StringBuilder()
//        match c.Schema with
//        | Http  -> b.Append "http" |> ignore
//        | Https -> b.Append "https" |> ignore
//
//        b.Append "://*:" |> ignore
//        b.Append c.Port |> ignore
//        b.Append "/" |> ignore
//
//        match c.BasePath with
//        | None            -> ()
//        | SkipStart "/" p -> p |> ensureEndsWith "/" |> b.Append |> ignore
//        | Some p          -> p |> ensureEndsWith "/" |> b.Append |> ignore
//
//        b.ToString()
//
//    let GET pattern handler = { Verb=Get; Pattern=pattern; Handler=handler; }
//    let POST pattern handler = { Verb=Post; Pattern=pattern; Handler=handler; }
//    let PUT pattern handler = { Verb=Put; Pattern=pattern; Handler=handler; }
//    let DELETE pattern handler = { Verb=Delete; Pattern=pattern; Handler=handler; }
//
//    let toVerb (s:string) = 
//        match s.ToUpperInvariant() with
//        | "GET"     -> Get
//        | "POST"    -> Post
//        | "PUT"     -> Put
//        | "DELETE"  -> Delete
//        | _         -> Get
//
//    let compiledRoutes = new System.Collections.Generic.Dictionary<string, Regex>()
//
//    let routeMatch (pattern:RoutePattern) (path:string) =
//        match pattern with
//        | Path p    -> path = (p |> ensureStartsWith "/")
//        | Regex p   -> 
//            if compiledRoutes.ContainsKey p |> not then
//                compiledRoutes.Add (p, new Regex (p, RegexOptions.Compiled))
//            let regex = compiledRoutes.[p]
//            regex.IsMatch path
//
//    let handler :(HttpListenerRequest -> HttpListenerResponse -> HttpServerConfig -> Async<unit>) = 
//        fun req resp conf -> 
//            async {
//                try
//                    printfn "RawUrl: %s" req.RawUrl
//                    printfn "AbsolutePath: %s" req.Url.AbsolutePath
//                    let bp = match conf.BasePath with | Some s -> s | None -> "/"
//                    let path = skipStart bp req.Url.AbsolutePath |> ensureStartsWith "/"
//                    printfn "path: %s" path
//
//                    let verb = req.HttpMethod |> toVerb
//                    let route = conf.Routes |> Seq.tryFind (fun r -> r.Verb = verb && routeMatch r.Pattern path) 
//                    match route with
//                    | None   -> 
//                        printfn "Invalid route: %s" path
//                        let out = Text.Encoding.ASCII.GetBytes ("Invalid route: " + path)
//                        resp.OutputStream.Write(out,0,out.Length)
//                        resp.OutputStream.Flush()
//                        resp.OutputStream.Close()
//                    | Some r -> 
//                        let reply = r.Handler req resp
//                        reply.Send req resp |> Async.StartImmediate
//                with e -> printfn "Error: %s" e.Message
//                          close resp
//            }
//
//    let listen (conf:HttpServerConfig) = 
//            let listener = new HttpListener()
//            let prefix = conf |> buildPrefix 
//            printfn "listen prefix: %s" prefix
//            prefix |> listener.Prefixes.Add
//            listener.Start()
//            let asynctask = Async.FromBeginEnd(listener.BeginGetContext,listener.EndGetContext)
//            async {
//                while true do 
//                    let! context = asynctask
//                    Async.Start (handler context.Request context.Response conf)
//            } |> Async.Start 
//            listener
//
//    let text s = new TextHttpReply(s) :> IHttpReply
//    let html s = new TextHttpReply(s) :> IHttpReply
//
//    let toHex (c:char) = Convert.ToInt32(c) |> fun i -> i.ToString("X")
//    let encode s = 
//        let parts = s |> Seq.map (fun c -> "%" + (c |> toHex))
//        String.Join("", parts)
//
//    let (|HexaChar|_|) (s:char list) =
//        if s.Length > 0 && s.Head = '%' then
//            let chars = s |> Seq.skip 1 |> Seq.take 2 |> Array.ofSeq
//            let h = new String(chars)
//            let num = Convert.ToInt32(h, 16)
//            let tail = s |> Seq.skip (chars.Length+1) |> List.ofSeq
//            Some ((Convert.ToChar num), tail)
//        else
//            None
//
//    let urlDecode (text:string) =
//        let rec decode s acc = 
//            match s with
//            | HexaChar (c, t) -> decode t (c :: acc)
//            | c :: t          -> decode t (c :: acc)
//            | []              -> new string(acc |> List.rev |> Array.ofList)
//        decode (text |> Seq.toList) []
//    
//    let get p f = GET (Path(p)) <| f
//    let getPattern p f = GET (Regex(p)) <| f
//    
//    let post p f = POST (Path(p)) <| f
//    let postPattern p f = POST (Regex(p)) <| f
//    
//    let put p f = PUT (Path(p)) <| f
//    let putPattern p f = PUT (Regex(p)) <| f
//    
//    let delete p f = DELETE (Path(p)) <| f
//    let deletePattern p f = DELETE (Regex(p)) <| f
//
//    
//
//
