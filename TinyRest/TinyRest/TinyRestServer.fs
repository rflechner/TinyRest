module TinyRestServer

    open System
    open System.Text
    open System.Text.RegularExpressions
    open System.Net

    type File = System.IO.File

    type HttpSchema = 
        | Http
        | Https

    type HttpVerb = 
        | Get
        | Post
        | Put
        | Delete


    type IHttpReply =
       abstract member Send : HttpListenerRequest -> HttpListenerResponse -> Async<unit>

    type HttpHandler = HttpListenerRequest -> HttpListenerResponse -> IHttpReply

    type RoutePattern = 
        | Path      of string
        | Regex     of string
        //| Format    of string

    type HttpRoute = {
        Verb: HttpVerb
        Pattern: RoutePattern
        Handler: HttpHandler
    }

    type HttpServerConfig = {
        Schema: HttpSchema
        Port: int
        BasePath: string option
        Routes: HttpRoute list
    }

    let close (resp:HttpListenerResponse) =
        try 
            resp.OutputStream.Flush()
            resp.OutputStream.Close() 
        with e -> printfn "Error: %s" e.Message

    type TextHttpReply(txt:string) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    let out = Text.Encoding.ASCII.GetBytes txt
                    r.OutputStream.Write(out,0,out.Length)
                    close r
                }
    
    type HtmlHttpReply(txt:string) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    r.ContentType <- "text/html"
                    let out = Text.Encoding.UTF8.GetBytes txt
                    r.OutputStream.Write(out,0,out.Length)
                    close r
                }

    type ErrorHttpReply(txt:string) =
       interface IHttpReply with
            member x.Send q r =
                async {
                    printfn "Http error: %s" txt
                    r.StatusCode <- 500
                    let out = Text.Encoding.ASCII.GetBytes txt
                    r.OutputStream.Write(out,0,out.Length)
                    close r
                }

    type StaticFileReply(path:string) =
        interface IHttpReply with
            member x.Send q r =
                async {
                    use fs = File.OpenRead(path)
                    fs.CopyTo(r.OutputStream)
                    close r
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
                compiledRoutes.Add (p, new Regex (p, RegexOptions.Compiled))
            let regex = compiledRoutes.[p]
            regex.IsMatch path

    let handler :(HttpListenerRequest -> HttpListenerResponse -> HttpServerConfig -> Async<unit>) = 
        fun req resp conf -> 
            async {
                try
                    printfn "RawUrl: %s" req.RawUrl
                    printfn "AbsolutePath: %s" req.Url.AbsolutePath
                    let bp = match conf.BasePath with | Some s -> s | None -> "/"
                    let path = skipStart bp req.Url.AbsolutePath |> ensureStartsWith "/"
                    printfn "path: %s" path

                    let verb = req.HttpMethod |> toVerb
                    let route = conf.Routes |> Seq.tryFind (fun r -> r.Verb = verb && routeMatch r.Pattern path) 
                    match route with
                    | None   -> 
                        printfn "Invalid route: %s" path
                        let out = Text.Encoding.ASCII.GetBytes ("Invalid route: " + path)
                        resp.OutputStream.Write(out,0,out.Length)
                        resp.OutputStream.Flush()
                        resp.OutputStream.Close()
                    | Some r -> 
                        let reply = r.Handler req resp
                        reply.Send req resp |> Async.StartImmediate
                with e -> printfn "Error: %s" e.Message
                          close resp
            }

    let listen (conf:HttpServerConfig) = 
            let listener = new HttpListener()
            let prefix = conf |> buildPrefix 
            printfn "listen prefix: %s" prefix
            prefix |> listener.Prefixes.Add
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

    let urlDecode (text:string) =
        let rec decode s acc = 
            match s with
            | HexaChar (c, t) -> decode t (c :: acc)
            | c :: t          -> decode t (c :: acc)
            | []              -> new string(acc |> List.rev |> Array.ofList)
        decode (text |> Seq.toList) []
    
    let get p f = GET (Path(p)) <| f
    let getPattern p f = GET (Regex(p)) <| f
    
    let post p f = POST (Path(p)) <| f
    let postPattern p f = POST (Regex(p)) <| f
    
    let put p f = PUT (Path(p)) <| f
    let putPattern p f = PUT (Regex(p)) <| f
    
    let delete p f = DELETE (Path(p)) <| f
    let deletePattern p f = DELETE (Regex(p)) <| f

    


