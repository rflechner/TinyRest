module HttpMapings

    open System
    open System.Net
    open System.Web

    open Http
    open Routing
    open TinyRestServerPCL

    let flatten (nvc:Collections.Specialized.NameValueCollection) =
        nvc.AllKeys |> Seq.map (fun k -> new System.Collections.Generic.KeyValuePair<string, string>(k, nvc.[k]))

    type HttpRequest(r:System.Web.HttpRequest) =
        interface IHttpRequest with
            member x.AcceptTypes = r.AcceptTypes
            member x.ClientCertificateError = 0
            member x.ContentEncoding = r.ContentEncoding
            member x.ContentLength64 = int64 r.ContentLength
            member x.ContentType = r.ContentType
            member x.Cookies = 
                let cookies = new CookieCollection()
                for k in r.Cookies.AllKeys do
                    let c = r.Cookies.Item k
                    let co = Cookie(c.Name, c.Value, c.Path, c.Domain)
                    cookies.Add co
                cookies
            member x.HasEntityBody = r.ReadEntityBodyMode <> ReadEntityBodyMode.None
            member x.Headers = r.Headers |> flatten
            member x.HttpMethod = r.HttpMethod
            member x.InputStream = r.InputStream
            member x.IsAuthenticated = r.IsAuthenticated
            member x.IsLocal = r.IsLocal
            member x.IsSecureConnection = r.IsSecureConnection
            member x.IsWebSocketRequest = 
                r.Headers.AllKeys |> Seq.exists (fun h -> h.Contains "WebSocket")
            member x.KeepAlive =
                let c = r.Item "Connection"
                c = "keep-alive"
            member x.LocalEndPoint = 
                new IPEndPoint(IpAddress="127.0.0.1", Port=r.Url.Port)
            member x.ProtocolVersion = Version(1,1)
            member x.QueryString = r.QueryString |> flatten
            member x.RawUrl = r.RawUrl
            member x.RemoteEndPoint = new IPEndPoint(IpAddress=r.UserHostAddress, Port=r.Url.Port)
            member x.RequestTraceIdentifier = Guid.NewGuid()
            member x.Url = r.Url
            member x.UrlReferrer = r.UrlReferrer
            member x.UserAgent = r.UserAgent
            member x.UserHostAddress = r.UserHostAddress
            member x.UserHostName = r.UserHostName
            member x.UserLanguages = r.UserLanguages

    type HttpResponse(r:System.Web.HttpResponse) =
        interface IHttpResponse with
            member x.Abort () = r.Clear()
            member x.AddHeader (name:string,value:string) = r.AddHeader(name, value)
            member x.AppendCookie (c:Cookie) = 
                r.AppendCookie(HttpCookie(c.Name, c.Value))
            member x.AppendHeader (name:string, value:string) = r.AppendHeader(name, value)
            member x.Close () = r.Close()
            member x.Close (responseEntity:byte [],willBlock:bool) = r.Close ()
            member x.ContentEncoding 
                with get() = r.ContentEncoding 
                and set(v) = r.ContentEncoding <- v
            member x.ContentLength64
                with get() = int64 r.OutputStream.Length
                and set(v) = r.AddHeader("Content-Length", v.ToString())
            member x.ContentType
                with get() = r.ContentType
                and set(v) = r.ContentType <- v
            member x.Cookies
                with get() =
                    let cookies = new CookieCollection()
                    for k in r.Cookies.AllKeys do
                        let c = r.Cookies.Item k
                        let co = Cookie(c.Name, c.Value, c.Path, c.Domain)
                        cookies.Add co
                    cookies
                and set(cookies) =
                    r.Cookies.Clear()
                    for c in cookies do
                        r.Cookies.Add(HttpCookie(c.Name, c.Value))
            member x.Headers
                with get() = 
                    let headers = WebHeaderCollection()
                    for k in r.Headers.AllKeys do
                        let h = r.Headers.Item k
                        headers.Add (k, h)
                    headers
                and set(headers) = 
                    r.Headers.Clear()
                    for k in headers.AllKeys do
                        r.Headers.Add (k, r.Headers.Item k)
            member x.KeepAlive
                with get() = false
                and set(v) = ()
            member x.OutputStream = r.OutputStream
            member x.ProtocolVersion
                with get() = Version(1,1)
                and set(v) = ()
            member x.Redirect(url:string) = r.Redirect(url)
            member x.RedirectLocation
                with get() = r.RedirectLocation
                and set(v) = r.RedirectLocation <- v
            member x.SendChunked 
                with get() = false
                and set(v) = ()
            member x.SetCookie(c:Cookie) = r.SetCookie(HttpCookie(c.Name, c.Value))
            member x.StatusCode
                with get() = r.StatusCode
                and set(v) = r.StatusCode <- v
            member x.StatusDescription
                with get() = r.StatusDescription
                and set(v) = r.StatusDescription <- v
        interface IDisposable with
            member x.Dispose() = r.Close()
