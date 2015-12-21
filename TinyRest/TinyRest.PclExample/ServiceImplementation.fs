module public ServiceImplementation

open TinyRestServerPCL
open Routing
open Http

let routes = GET [
                path "/" <| fun _ -> text "coucou"
                path "/bye" <| fun _ -> text "bye bye\n@++"
                regex "/haha/(.*)" <| fun _ -> text "ha ha"
             ]

let startServer (listener:IHttpListener) (logger:ILogger) =
    let conf = { Schema=Http; Port=8009; BasePath=Some "/TinyRest1"; Routes=routes; Logger=Some(logger :> ILogger); }
    listener |> listen conf |> ignore

