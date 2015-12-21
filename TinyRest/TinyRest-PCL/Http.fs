module public Http

    open System
    open System.Text
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
        //abstract member TransportContext : TransportContext
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

    type RestRequest<'t> = 
        { Request: IHttpRequest
          Response: IHttpResponse
          Arguments: 't }
        static member New rq rs a =
            { Request = rq; Arguments = a; Response = rs }

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
        
    let toVerb (s:string) = 
        match s.ToUpperInvariant() with
        | "GET"     -> Get
        | "POST"    -> Post
        | "PUT"     -> Put
        | "DELETE"  -> Delete
        | _         -> Get

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
    

