#r "System.Xml.Linq.dll"

open System
open System.Net
open System.Text
open System.IO
open System.Xml.Linq
open System.Collections.Generic
    
#load "TinyRestServer.fs"

open TinyRestServer

let combine p1 p2 = Path.Combine(p1, p2)
let dir p = Directory.EnumerateFileSystemEntries p
let element (name:string) (attrs:XAttribute list) (text:string) = 
    let a = attrs |> Seq.cast<obj> |> List.ofSeq
    new XElement(XName.Get(name), [(text :> obj)] |> List.append a)

let container (name:string) (attrs:XAttribute list) (children:XElement list) = 
    let a = attrs |> Seq.cast<obj> |> List.ofSeq
    new XElement(XName.Get(name), children |> Seq.cast<obj> |> List.ofSeq |> List.append a)

let attr name text = new XAttribute(XName.Get(name), text)

let listFiles (q:HttpListenerRequest) (r:HttpListenerResponse) =
    let usr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let homeDir = combine usr "Downloads"
    let p = if q.QueryString.["p"] |> String.IsNullOrWhiteSpace |> not then 
                combine homeDir (q.QueryString.["p"] |> skipStart "\\" |> skipStart "/")
            else homeDir
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

let download (q:HttpListenerRequest) (r:HttpListenerResponse) =
    let usr = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let homeDir = combine usr "Downloads"
    let p = if q.QueryString.["p"] |> String.IsNullOrWhiteSpace |> not then 
                combine homeDir (q.QueryString.["p"] |> skipStart "\\" |> skipStart "/")
            else
                homeDir
    if File.Exists p then
        r.AddHeader("Content-Disposition", "Attachment;filename=" + Path.GetFileName(p))
        new StaticFileReply(p) :> IHttpReply
    else
        new ErrorHttpReply("Cannot find file") :> IHttpReply

let routes = [
                GET (Path("/")) <| fun q r -> text "coucou"
                get "/bye" <| fun q r -> text "bye bye\n@++"
                getPattern "/haha/(.*)" <| fun q r -> text "ha ha"
                GET (Path("/files")) <| listFiles
                get "/download" <| download
             ]

let conf = { Schema=Http; Port=8009; BasePath=Some "/TinyRest1"; Routes=routes; }

listen conf




Console.Read () |> ignore

