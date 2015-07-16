module public TinyRestServerPCL

    open System
    open System.Text
    open System.Text.RegularExpressions
    open System.Net

    type HttpSchema = 
        | Http
        | Https

    type HttpVerb = 
        | Get
        | Post
        | Put
        | Delete

    type NameValueCollection = System.Collections.Generic.KeyValuePair<string, string> seq

    type IPEndPoint() =
        let mutable ipAddress = ""
        let mutable port = 0
        member x.IpAddress with get() = ipAddress and set (v) = ipAddress <- v
        member x.Port with get() = port and set (v) = port <- v

    type IHttpRequest =
        abstract member AcceptTypes : string []
        abstract member ClientCertificateError : int
        abstract member ContentEncoding : Encoding
        abstract member ContentLength64 : int64
        abstract member ContentType : string
        abstract member Cookies : CookieCollection
        abstract member HasEntityBody : bool
        abstract member Headers : NameValueCollection
        abstract member HttpMethod : string
        abstract member InputStream : IO.Stream
        abstract member IsAuthenticated : bool
        abstract member IsLocal : bool
        abstract member IsSecureConnection : bool
        abstract member IsWebSocketRequest : bool
        abstract member KeepAlive : bool
        abstract member LocalEndPoint : IPEndPoint
        abstract member ProtocolVersion : Version
        abstract member QueryString : NameValueCollection
        abstract member RawUrl : string
        abstract member RemoteEndPoint : IPEndPoint
        abstract member RequestTraceIdentifier : Guid
        abstract member TransportContext : TransportContext
        abstract member Url : Uri
        abstract member UrlReferrer : Uri
        abstract member UserAgent : string
        abstract member UserHostAddress : string
        abstract member UserHostName : string
        abstract member UserLanguages : string []

    type IHttpResponse =
        inherit IDisposable
        abstract member Abort : unit -> unit
        abstract member AddHeader : name:string * value:string -> unit
        abstract member AppendCookie : cookie:Cookie -> unit
        abstract member AppendHeader : name:string * value:string -> unit
        abstract member Close : unit -> unit
        abstract member Close : responseEntity:byte [] * willBlock:bool -> unit
        abstract member ContentEncoding : Encoding with get, set
        abstract member ContentLength64 : int64 with get, set
        abstract member ContentType : string with get, set
        abstract member Cookies : CookieCollection with get, set
        abstract member Headers : WebHeaderCollection with get, set
        abstract member KeepAlive : bool with get, set
        abstract member OutputStream : IO.Stream
        abstract member ProtocolVersion : Version with get, set
        abstract member Redirect : url:string -> unit
        abstract member RedirectLocation : string with get, set
        abstract member SendChunked : bool with get, set
        abstract member SetCookie : cookie:Cookie -> unit
        abstract member StatusCode : int with get, set
        abstract member StatusDescription : string with get, set

    type IHttpReply =
       abstract member Send : IHttpRequest -> IHttpResponse -> Async<unit>

    type HttpHandler = IHttpRequest -> IHttpResponse -> IHttpReply

    type RoutePattern = 
        | Path      of string
        | Regex     of string
        //| Format    of string

    type HttpRoute = {
        Verb: HttpVerb
        Pattern: RoutePattern
        Handler: HttpHandler
    }

    type ILogger =
        abstract member Log : string -> unit

    type IHttpListenerContext =
        abstract member Request : IHttpRequest
        abstract member Response : IHttpResponse

    type IHttpListener =
        abstract member AddPrefix : string -> unit
        abstract member Start : unit -> unit
        abstract member BeginGetContext : callback:AsyncCallback * state:obj -> IAsyncResult
        abstract member Close : unit -> unit
        abstract member EndGetContext : asyncResult:IAsyncResult -> IHttpListenerContext
        abstract member GetContext : unit -> IHttpListenerContext
        abstract member GetContextAsync : unit -> Threading.Tasks.Task<IHttpListenerContext>

    type HttpServerConfig = {
        Schema: HttpSchema
        Port: int
        BasePath: string option
        Routes: HttpRoute list
        Logger: ILogger option
    }

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

    let toVerb (s:string) = 
        match s.ToUpperInvariant() with
        | "GET"     -> Get
        | "POST"    -> Post
        | "PUT"     -> Put
        | "DELETE"  -> Delete
        | _         -> Get

    let compiledRoutes = new System.Collections.Generic.Dictionary<string, Regex>()

    let routeMatch (pattern:RoutePattern) (path:string) =
        match pattern with
        | Path p    -> path = (p |> ensureStartsWith "/")
        | Regex p   -> 
            if compiledRoutes.ContainsKey p |> not then
                compiledRoutes.Add (p, new Regex (p))
            let regex = compiledRoutes.[p]
            regex.IsMatch path

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

    let toHex (c:char) = Convert.ToInt32(c) |> fun i -> i.ToString("X")
    let encode s = 
        let parts = s |> Seq.map (fun c -> "%" + (c |> toHex))
        String.Join("", parts)

    let (|HexaChar|_|) (s:char list) =
        if s.Length > 0 && s.Head = '%' then
            let chars = s |> Seq.skip 1 |> Seq.take 2 |> Array.ofSeq
            let h = new String(chars)
            let num = Convert.ToInt32(h, 16)
            let tail = s |> Seq.skip (chars.Length+1) |> List.ofSeq
            Some ((Convert.ToChar num), tail)
        else
            None

    let toChars (s:string) =
        [for i in [0..s.Length] -> s.[i]]

    let urlDecode (text:string) =
        let rec decode s acc = 
            match s with
            | HexaChar (c, t) -> decode t (c :: acc)
            | c :: t          -> decode t (c :: acc)
            | []              -> new string(acc |> List.rev |> Array.ofList)
        decode (text |> toChars) []
    
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
        let toFluentReply f = new FluentHttpReply(f) :> IHttpReply

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



