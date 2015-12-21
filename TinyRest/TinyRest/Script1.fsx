#r "System.Xml.Linq.dll"
#r "..\packages\Newtonsoft.Json.7.0.1\lib\portable-net40+sl5+wp80+win8+wpa81\Newtonsoft.Json.dll"

#load "..\TinyRest-PCL\Http.fs"
#load "..\TinyRest-PCL\Routing.fs"
#load "..\TinyRest-PCL\TinyRestPcl.fs"

open System
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection

open Http
open Routing
open TinyRestServerPCL

type Url = {Absolute:string; Port:int}
type Reply = 
    { Content:string }
    member x.LoadUrl (u:Url) =
        printfn "url is %s" u.Absolute

module Seq =
    let ofType<'t> (s:'t seq) = 
        let ty = typeof<'t>
        s |> Seq.filter (
            fun i -> 
                match box i with
                | null -> false
                | o -> ty.IsAssignableFrom(i.GetType())
        )



format "%s" <| fun a -> text "dzdz" // a is a string
format "%d" <| fun a -> text "dzdz" // a is an int

format "%s_%d" <| fun a -> text "dzdz" // a is string * int

format "%s_%d" <| fun r -> text "dzdz" // a is string and b is an int
format "%s_%d_%s_%d_%d_%s_%s-%s_%d_%s_%d_%d_%s_%s_" <| fun tuple -> text "dzdz"

let h1 = format "/users/auth?login=%s&password=%d" 
            <| fun r -> 
                text <| sprintf "Executing %A" r.Arguments
h1.TryHandle "/users/auth?login=popopo&password=87"


//let expected = ("popopo", 87)
//let f = "/users/auth?login=%s&password=%d" |> FormatParser.Create |> fun p -> p.Parse(expected.GetType())
//let url = "/users/auth?login=popopo&password=87"
//let h1 = RouteHandler.New f
//let m1 = h1.Match url


FormatPart.Parsed(IntPart).Match "popo"
FormatPart.Parsed(IntPart).Match "8787"


