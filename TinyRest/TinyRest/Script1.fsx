open System
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection


type Url = {Absolute:string; Port:int}
type Reply = 
    { Content:string }
    member x.LoadUrl (u:Url) =
        printfn "url is %s" u.Absolute

/// Verify that f x, and then return x, otherwise fail witha 'format failure' message
//let private check f x = if f x then x else failwithf "format failure \"%s\"" x
//
//let private parseDecimal x = Decimal.Parse(x, System.Globalization.CultureInfo.InvariantCulture)
//
///// The supported characters for the formatter
//let parsers =
//  dict [
//    'b', Boolean.Parse >> box
//    'd', int64 >> box
//    'i', int64 >> box
//    's', box
//    'u', uint32 >> int64 >> box
//    'x', check (String.forall Char.IsLower) >> ((+) "0x") >> int64 >> box
//    'X', check (String.forall Char.IsUpper) >> ((+) "0x") >> int64 >> box
//    'o', ((+) "0o") >> int64 >> box
//    'e', float >> box // no check for correct format for floats
//    'E', float >> box
//    'f', float >> box
//    'F', float >> box
//    'g', float >> box
//    'G', float >> box
//    'M', parseDecimal >> box
//    'c', char >> box
//  ]
//
//// array of all possible formatters, i.e. [|"%b"; "%d"; ...|]
//let separators =
//  parsers.Keys
//  |> Seq.map (fun c -> "%" + c.ToString())
//  |> Seq.toArray
//
//// Creates a list of formatter characters from a format string,
//// for example "(%s,%d)" -> ['s', 'd']
//let rec getFormatters xs =
//  match xs with
//  | '%' :: '%' :: xr -> getFormatters xr
//  | '%' :: x :: xr   ->
//    if parsers.ContainsKey x then x :: getFormatters xr
//    else failwithf "Unknown formatter %%%c" x
//  | x :: xr          -> getFormatters xr
//  | []               -> []
//
//// Coerce integer types from int64
//let coerce o = function
//  | v when v = typeof<int32> ->
//    int32(unbox<int64> o) |> box
//  | v when v = typeof<uint32> ->
//    uint32(unbox<int64> o) |> box
//  | _ -> o
//
///// Parse the format in 'pf' from the string 's', failing and raising an exception
///// otherwise
//let sscanf (pf:PrintfFormat<_,_,_,_,'t>) s : 't =
//  let formatStr  = pf.Value
//  let constants  = formatStr.Split([|"%%"|], StringSplitOptions.None) 
//                   |> Array.map (fun x -> x.Split(separators, StringSplitOptions.None))
//  let regexStr   = constants 
//                   |> Array.map (fun c -> c |> Array.map Regex.Escape |> String.concat "(.*?)")
//                   |> String.concat "%"
//  let regex      = Regex("^" + regexStr + "$")
//  let formatters = formatStr.ToCharArray() // need original string here (possibly with "%%"s)
//                   |> Array.toList |> getFormatters
//  let groups =
//    regex.Match(s).Groups
//    |> Seq.cast<Group>
//    |> Seq.skip 1
//
//  let matches =
//    (groups, formatters)
//    ||> Seq.map2 (fun g f -> g.Value |> parsers.[f])
//    |> Seq.toArray
//
//  if matches.Length = 1 then
//    coerce matches.[0] typeof<'t> :?> 't
//  else
//    let tupleTypes = FSharpType.GetTupleElements(typeof<'t>)
//    let matches =
//      (matches,tupleTypes)
//      ||> Array.map2 ( fun a b -> coerce a b)
//    FSharpValue.MakeTuple(matches, typeof<'t>) :?> 't
//
//let inline succeed x = async.Return (Some x)
//
//let fail = async.Return None
//
//let pathScan (pf : PrintfFormat<_,_,_,_,'t>) (h : 't ->  Reply) =
//  let scan url =
//    try 
//        let r = sscanf pf url
//        Some r
//    with _ -> None
//
//  let F (r:Url) =
//    match scan r.Absolute with
//    | Some p ->
//        let part = h p
//        part.LoadUrl r |> succeed
//    | None -> 
//        fail
//  F
//
//// some basic testing
//let (a,b)           = sscanf "(%%%s,%M)" "(%hello, 4.53)"
//let aaa : int32     = sscanf "aaaa%d" "aaaa4"
//let bbb : int64     = sscanf "aaaa%d" "aaaa4"
//let (x,y,z)         = sscanf "%s-%s-%s" "test-this-string"
//let (c,d,e : uint32,f,g,h,i) = sscanf "%b-%d-%i,%u,%x,%X,%o" "false-42--31,13,ff,FF,42"
//let (j,k,l,m,n,o,p) = sscanf "%f %F %g %G %e %E %c" "1 2.1 3.4 .3 43.2e32 0 f"
//
//let aa              = sscanf "(%s)" "(45.33)"
//
//let sasa = sscanf "%d" "6454"
//
//
//pathScan "%s" <| fun s -> failwith ""
//pathScan "%s_%d" <| fun s d -> failwith ""

module Seq =
    let ofType<'t> (s:'t seq) = 
        let ty = typeof<'t>
        s |> Seq.filter (
            fun i -> 
                match box i with
                | null -> false
                | o -> ty.IsAssignableFrom(i.GetType())
        )

type FormatParsed =
    | StringPart
    | CharPart
    | BoolPart
    | IntPart
    | DecimalPart
    | HexaPart
    member x.Parse (s:string) : obj =
        match x with
        | StringPart    -> s |> box
        | CharPart      -> s.Chars 0 |> box
        | BoolPart      -> s |> Boolean.Parse |> box
        | IntPart       -> s |> int64 |> box
        | DecimalPart   -> s |> float |> box
        | HexaPart      -> 
            let str = s.ToLower() 
            if str.StartsWith "0x"
            then s |> int64 |> box
            else ("0x" + s) |> int64 |> box

type FormatPart =
    | Constant  of string
    | Parsed    of FormatParsed
    member x.Match text : (obj*Type) option=
        let parseInt s = 
            let r:int64 ref = ref 0L
            if Int64.TryParse(s, r) then Some (box !r, typeof<Int64>) else None
        match x with
        | Constant s -> if s = text then Some (box s, typeof<string>) else None
        | Parsed p ->
            match p with
            | StringPart    -> Some (box text, typeof<string>)
            | CharPart      -> 
                if text = null || text.Length > 1 
                then None
                else Some (text.Chars 0 |> box, typeof<char>)
            | BoolPart      -> 
                let r:bool ref = ref false
                if bool.TryParse(text, r)
                then Some (box !r, typeof<bool>)
                else None
            | IntPart       -> parseInt text
            | DecimalPart   -> 
                let r:decimal ref = ref 0m
                if Decimal.TryParse(text, r)
                then Some (box !r, typeof<Decimal>)
                else None
            | HexaPart      -> 
                match text.ToLower() with
                | str when str.StartsWith "0x" |> not -> "0x" + str
                | str -> str
                |> parseInt

type RouteFormat = 
    { Parts:FormatPart list 
      Type:Type }

type FormatParser = 
    { Parts:FormatPart list ref
      Buffer:char list ref
      Format:string
      Position:int ref }
    static member Create f =
        { Parts = ref List.empty
          Buffer = ref List.empty
          Format = f
          Position = ref 0 }
    member x.Acc (s:string) =
        x.Buffer := !x.Buffer @ (s.ToCharArray() |> Seq.toList)
    member x.Acc (c:char) =
        x.Buffer := !x.Buffer @ [c]
    member private x.Finished () =
        !x.Position >= x.Format.Length
    member x.Next() =
        if x.Finished() |> not then
            x.Format.Chars !x.Position |> x.Acc
            x.Position := !x.Position + 1
    member x.PreviewNext() =
        if !x.Position >= x.Format.Length - 1
        then None
        else Some (x.Format.Chars (!x.Position))
    member x.Push t =
        x.Parts := !x.Parts @ t
        x.Buffer := List.empty
    member x.StringBuffer skip =
        !x.Buffer |> Seq.skip skip |> Seq.toArray |> String
    member x.Parse (ty:Type) =
        while x.Finished() |> not do
            x.Next()
            match !x.Buffer with
            | '%' :: '%' :: _ -> x.Push [Constant (x.StringBuffer 1)]
            | '%' :: 'b' :: _ -> x.Push [Parsed BoolPart]
            | '%' :: 'i' :: _
            | '%' :: 'u' :: _
            | '%' :: 'd' :: _ -> x.Push [Parsed IntPart]
            | '%' :: 'c' :: _ -> x.Push [Parsed StringPart]
            | '%' :: 's' :: _ -> x.Push [Parsed StringPart]
            | '%' :: 'e' :: _
            | '%' :: 'E' :: _
            | '%' :: 'f' :: _
            | '%' :: 'F' :: _
            | '%' :: 'g' :: _
            | '%' :: 'G' :: _ -> x.Push [Parsed DecimalPart]
            | '%' :: 'x' :: _
            | '%' :: 'X' :: _ -> x.Push [Parsed HexaPart]
            | c :: _ -> 
                let n = x.PreviewNext()
                match n with
                | Some '%' -> x.Push [Constant (x.StringBuffer 0)]
                | _ -> ()
            | _ -> ()
        if !x.Buffer |> Seq.isEmpty |> not then x.Push [Constant (x.StringBuffer 0)]
        { Parts = !x.Parts; Type = ty }

type RouteHandler = 
    { Format: RouteFormat }
    static member New f =
        { Format = f }
    member x.Match url =
        let rec skipNextConstant (text:string) (parts:FormatPart list) (acc:string list) =
            match parts with
            | Constant s :: t ->
                let i = text.IndexOf(s, StringComparison.InvariantCultureIgnoreCase)
                if i >= 0 then
                    let start = text.Substring (0, i)
                    let j = i + s.Length
                    let rest = text.Substring (j, text.Length - j)
                    skipNextConstant rest t (if i > 0 then acc @ [start] else acc)
                else None
            | Parsed _ :: [] -> Some (acc @ [text])
            | _ :: t -> skipNextConstant text t acc
            | [] -> if text.Length = 0 then Some acc else None
        let parsed = x.Format.Parts
                     |> List.filter (fun p -> match p with | Parsed _ -> true | _ -> false)

        let pp = skipNextConstant url x.Format.Parts []
        printfn "pp: %A" pp
        match pp with
        | None -> None
        | Some values when values.Length <> parsed.Length -> 
            printfn "failing: %A" (parsed, values)
            None
        | Some values ->
            let types = FSharpType.GetTupleElements(x.Format.Type)
            let results = parsed
                          |> List.zip values
                          |> List.choose (fun (v,p) -> p.Match v)
                          |> Seq.zip types
                          |> Seq.map (
                            fun (tupleType,(o, t)) -> 
                                match tupleType with
                                | v when v = typeof<int32> ->
                                    int32(unbox<int64> o) |> box
                                | v when v = typeof<uint32> ->
                                    uint32(unbox<int64> o) |> box
                                | _ -> o
                          )
                          |> Seq.toArray
                          
            printfn "Creating tuple: %A" (results, x.Format.Type)
            Some (FSharpValue.MakeTuple(results, x.Format.Type))
        


let fecho (pf : PrintfFormat<_,_,_,_,'t>) (h : 't -> unit) =
    printfn "format: %s" pf.Value
    
    let f = pf.Value |> FormatParser.Create |> fun p -> p.Parse(typeof<'t>)
    let hm = RouteHandler.New f

    let funh url =
        let m = hm.Match url
        match m with
        | Some tuple -> h (tuple :?> 't)
        | None -> 
            printfn "invalid url"
    funh

fecho "%s" <| fun a -> () // a is a string
fecho "%d" <| fun a -> () // a is an int

fecho "%s_%d" <| fun a -> () // a is string * int

fecho "%s_%d" <| fun (a,b) -> () // a is string and b is an int
fecho "%s_%d_%s_%d_%d_%s_%s-%s_%d_%s_%d_%d_%s_%s_" <| fun tuple -> ()

let fh1 = fecho "/users/auth?login=%s&password=%d"
            <| fun (a,b) -> 
                printfn "Executing %A" (a,b)

fh1 "/users/auth?login=popopo&password=87"

let expected = ("popopo", 87)
let f = "/users/auth?login=%s&password=%d" |> FormatParser.Create |> fun p -> p.Parse(expected.GetType())
let url = "/users/auth?login=popopo&password=87"
let h1 = RouteHandler.New f
let m1 = h1.Match url


FormatPart.Parsed(IntPart).Match "popo"
FormatPart.Parsed(IntPart).Match "8787"


