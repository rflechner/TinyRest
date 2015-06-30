module public ServiceImplementation

open TinyRestServerPCL

let routes = [
                GET (Path("/")) <| fun q r -> text "coucou"
                get "/bye" <| fun q r -> text "bye bye\n@++"
                getPattern "/haha/(.*)" <| fun q r -> text "ha ha"
             ]

let startServer (listener:IHttpListener) (logger:ILogger) =
    let conf = { Schema=Http; Port=8009; BasePath=Some "/TinyRest1"; Routes=routes; Logger=Some(logger :> ILogger); }
    listener |> listen conf |> ignore

