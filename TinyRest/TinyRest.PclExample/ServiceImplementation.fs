module public ServiceImplementation

open System
open System.Net
open System.Text

open TinyRestServerPCL
open Routing
open Http

type Car = 
    { Name:string
      Factory:string
      Color:string }

let routes = GET [
                path "/" <| fun _ -> text "coucou"
                path "/bye" <| fun _ -> text "bye bye\n@++"
                regex "/haha/(.*)" <| fun _ -> text "ha ha"
                format "/car:%d_%s" 
                    <| fun rq rs (a1,a2) ->
                        rs.ContentEncoding <- Encoding.UTF8
                        json { Name=(sprintf "Peugeot %d féline" a1); Factory="Peugeot"; Color=a2 }
             ]

let startServer (listener:IHttpListener) (logger:ILogger) =
    let conf = { Schema=Http; Port=8009; BasePath=Some "/TinyRest1"; Routes=routes; Logger=Some(logger :> ILogger); }
    listener |> listen conf |> ignore

