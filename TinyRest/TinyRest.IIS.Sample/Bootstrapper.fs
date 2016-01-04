namespace TinyRest.IIS.Sample

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

type Bootstrapper () = 
    interface IAppBootstrapper with
        member x.Routes() =
            GET [
                path "/" <| fun _ -> text "coucou"
                path "/bye" <| fun _ -> text "bye bye\n@++"
                regex "/haha/(.*)" <| fun _ -> text "ha ha"
                format "/car:%d_%s" 
                    <| fun rq rs (a1,a2) ->
                        rs.ContentEncoding <- Encoding.UTF8
                        json { Name=(sprintf "Peugeot %d féline" a1); Factory="Peugeot"; Color=a2 }
             ]
             :> System.Collections.Generic.IEnumerable<HttpRoute>


