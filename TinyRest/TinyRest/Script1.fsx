#load "..\TinyRest-PCL\Http.fs"
#load "..\TinyRest-PCL\Routing.fs"

open System
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection

open Routing

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



urlFormat "%s" <| fun a -> () // a is a string
urlFormat "%d" <| fun a -> () // a is an int

urlFormat "%s_%d" <| fun a -> () // a is string * int

urlFormat "%s_%d" <| fun r -> () // a is string and b is an int
urlFormat "%s_%d_%s_%d_%d_%s_%s-%s_%d_%s_%d_%d_%s_%s_" <| fun tuple -> ()

let h1 = urlFormat "/users/auth?login=%s&password=%d"
            <| fun (a,b) -> 
                printfn "Executing %A" (a,b)
h1.Handle "/users/auth?login=popopo&password=87"

//let expected = ("popopo", 87)
//let f = "/users/auth?login=%s&password=%d" |> FormatParser.Create |> fun p -> p.Parse(expected.GetType())
//let url = "/users/auth?login=popopo&password=87"
//let h1 = RouteHandler.New f
//let m1 = h1.Match url


FormatPart.Parsed(IntPart).Match "popo"
FormatPart.Parsed(IntPart).Match "8787"


