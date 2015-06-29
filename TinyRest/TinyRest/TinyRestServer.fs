module TinyRestServer

    open System
    open System.Net
    open TinyRestServerPCL

    type File = System.IO.File

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
