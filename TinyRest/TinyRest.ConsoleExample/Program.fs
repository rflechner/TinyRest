open System
open System.Net
open System.Text
open System.IO
open System.Xml.Linq
open System.Collections.Generic

open Http
open Routing
open TinyRestServerPCL
open TinyRestServer

type User =
    { Login:string
      Email:string
      Birth:DateTime }

let logger = new ConsoleLogger()

let combine p1 p2 = Path.Combine(p1, p2)
let dir p = Directory.EnumerateFileSystemEntries p
let element (name:string) (attrs:XAttribute list) (text:string) = 
    let a = attrs |> Seq.cast<obj> |> List.ofSeq
    new XElement(XName.Get(name), [(text :> obj)] |> List.append a)

let container (name:string) (attrs:XAttribute list) (children:XElement list) = 
    let a = attrs |> Seq.cast<obj> |> List.ofSeq
    new XElement(XName.Get(name), children |> Seq.cast<obj> |> List.ofSeq |> List.append a)

let attr name text = new XAttribute(XName.Get(name), text)

let listFiles (r:RestRequest<string>) =
    let q = r.Request
    let usr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let homeDir = combine usr "Downloads"
    let p = match q.QueryString |> getVal "p" with
             | Some v -> combine homeDir (v |> skipStart "\\" |> skipStart "/")
             | None   -> homeDir
    let dirs = seq  {
                      for d in p |> Directory.EnumerateDirectories do
                        let rel = d.Substring homeDir.Length
                        let link = sprintf "?p=%s" (encode rel)
                        let e = element "a" [attr "href" link] rel
                        yield container "li" [] [e]
                    } |> List.ofSeq

    let files = seq { 
                      for d in p |> Directory.EnumerateFiles do
                        let rel = d.Substring homeDir.Length
                        let link = sprintf "download?p=%s" (encode rel)
                        let e = element "a" [attr "href" link] rel
                        yield container "li" [] [e]
                    } |> List.ofSeq

    let body = seq {
                    if p = homeDir |> not then
                        let pa = Directory.GetParent(p).FullName.Substring homeDir.Length
                        let link = sprintf "?p=%s" (encode pa)
                        yield element "a" [attr "href" link] ".."

                    yield element "h2" [] "Folders"
                    yield container "ul" [] dirs

                    yield element "h2" [] "Files"
                    yield container "ul" [] files

                } |> List.ofSeq

    let h = container "html" [] [ container "body" [] body ]
    h.ToString() |> html

let download (r:RestRequest<string>) =
    let q = r.Request
    let usr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let homeDir = combine usr "Downloads"
    let p = match q.QueryString |> getVal "p" with
             | Some v -> combine homeDir (v |> skipStart "\\" |> skipStart "/")
             | None   -> homeDir
    if File.Exists p then
        r.Response.AddHeader("Content-Disposition", "Attachment;filename=" + Path.GetFileName(p))
        new StaticFileReply(p,logger) :> IHttpReply
    else
        new ErrorHttpReply("Cannot find file", logger) :> IHttpReply

type Car = 
    { Name:string
      Factory:string
      Color:string }

let reply<'t> (model:'t) =
    ()

[<EntryPoint>]
let main _ = 
    let routes = 
        GET [
            path "/" <| fun _ -> text "coucou"
            regex "/[1-9]{2}_toto" <| fun _ -> text "regex works !"
            path "/ip" <| fun r -> text r.Request.RemoteEndPoint.IpAddress
            path "/bye" <| fun _ -> text "bye bye\n@++"
            regex "/haha/(.*)" <| fun _ -> text "ha ha"
            path "/files" <| listFiles
            path "/download" <| download
            path "/user" <| fun _ -> json {Login="Romain"; Email="rflechner@romcyber.com"; Birth=DateTime(1985, 02, 11)}
            format "/user:%d" <| fun rq rs args -> text <| sprintf "format works ! url: %A id is %d" rq.RawUrl args
            format "/user:%s" <| fun rq rs args -> json {Login=args; Email="rflechner@romcyber.com"; Birth=DateTime(1985, 02, 11)}
            format "/car:%d_%s" 
                <| fun rq rs (a1,a2) ->
                    rs.ContentEncoding <- Encoding.UTF8
                    json { Name=(sprintf "Peugeot %d féline" a1); Factory="Peugeot"; Color=a2 }
        ] @
        POST [
            path "/" <| fun p -> text "coucou posted !"
        ]

    let conf = { Schema=Http; Port=8009; BasePath=Some "/TinyRest1"; Routes=routes; Logger=Some(logger :> ILogger); }
    let listener = new Listener()
    listener |> listen conf |> ignore

    printfn "Press any key to kill the server ..."
    Console.ReadKey true |> ignore
    0

