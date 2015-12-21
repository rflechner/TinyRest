module public Routing

open System
open System.Text
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection

open Http

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
        let c = !x.Buffer |> Seq.skip skip |> Seq.toArray
        new String(c)
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

type IRouteHandler =
    abstract member TryHandle<'t> : string -> IHttpRequest -> IHttpResponse -> IHttpReply option
    abstract member Match : string -> IHttpRequest -> obj option

//type HttpHandler = IHttpRequest -> IHttpResponse -> IHttpReply

type HttpRoute = 
    { Verb: HttpVerb
      Handler: IRouteHandler }
    static member From v h =
        { Verb = v; Handler = h }

type HttpServerConfig =
    { Schema: HttpSchema
      Port: int
      BasePath: string option
      Routes: HttpRoute list
      Logger: ILogger option }

type RouteFormatHandler<'t> = 
    { Format : RouteFormat
      Fun : IHttpRequest -> IHttpResponse -> 't -> IHttpReply }
    static member New f h =
        { Format = f; Fun = h }
    interface IRouteHandler with
        member x.Match path _ =
            let isPrimitive (ty:Type) =
                let primitives =
                    [ typeof<byte>; typeof<char>; typeof<string>; typeof<string>; typeof<bool>
                      typeof<int8>; typeof<int16>; typeof<int32>; typeof<int64>
                      typeof<uint8>; typeof<uint16>; typeof<uint32>; typeof<uint64> ]
                primitives |> List.contains ty
            let castType ty o =
                match ty with
                | t when t = typeof<int32> ->
                    int32(unbox<int64> o) |> box
                | t when t = typeof<uint32> ->
                    uint32(unbox<int64> o) |> box
                | _ -> o
            let rec skipNextConstant (text:string) (parts:FormatPart list) (acc:string list) =
                match parts with
                | Constant s :: t ->
                    let i = text.ToLowerInvariant().IndexOf(s.ToLowerInvariant())
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
            match skipNextConstant path x.Format.Parts [] with
            | None -> None
            | Some values when values.Length <> parsed.Length -> None
            | Some values ->
                if FSharpType.IsTuple x.Format.Type then
                    let types = FSharpType.GetTupleElements(x.Format.Type)
                    let results = parsed
                                    |> List.zip values
                                    |> List.choose (fun (v,p) -> p.Match v)
                                    |> Seq.zip types
                                    |> Seq.map (fun (ty,(o, _)) -> castType ty o)
                                    |> Seq.toArray
                    Some (FSharpValue.MakeTuple(results, x.Format.Type))
                elif isPrimitive x.Format.Type && values.Length = 1 && parsed.Length = 1 then
                    let v = values |> List.head
                    match parsed.Head.Match v with
                    | Some (o,_) -> 
                        let ty = x.Format.Type
                        let r = castType ty o
                        Some r
                    | None -> None
                else None
        member x.TryHandle path rq rs = 
            let m = (x :> IRouteHandler).Match path rq
            match m with
            | Some tuple -> Some (x.Fun rq rs (tuple :?> 't))
            | None -> None

let skipStart (start:string) (str:string) =
    if str.StartsWith start |> not
    then str
    else str.Substring(start.Length)

let (|SkipStart|_|) (start:string) (stro:string option) =
    match stro with
    | None      -> None
    | Some str  -> Some (skipStart start str)

let ensureEndsWith e (s:string) = if s.EndsWith e then s else s + e
let ensureStartsWith e (s:string) = if s.StartsWith e then s else e + s

let (|EnsureStartsWith|_|) (start:string) (stro:string option) =
    match stro with
    | None      -> None
    | Some str  -> Some (ensureStartsWith start str)

let buildPrefix (c:HttpServerConfig) =
    let b = new StringBuilder()
    match c.Schema with
    | Http  -> b.Append "http" |> ignore
    | Https -> b.Append "https" |> ignore

    b.Append "://*:" |> ignore
    b.Append c.Port |> ignore
    b.Append "/" |> ignore

    match c.BasePath with
    | None            -> ()
    | SkipStart "/" p
    | Some p          -> p |> ensureEndsWith "/" |> b.Append |> ignore

    b.ToString()

type RoutePathHandler = 
    { Path : string
      Fun : RestRequest<string> -> IHttpReply }
    static member New f h =
        { Path = f
          Fun = h }
    interface IRouteHandler with
        member x.Match path rq =
            if x.Path = path
            then Some (x.Path :> obj)
            else None
        member x.TryHandle path rq rs = 
            let m = (x :> IRouteHandler).Match path rq
            match m with
            | Some _ -> 
                let r = RestRequest<string>.New rq rs rq.RawUrl
                Some (x.Fun r)
            | None -> None

type RouteRegexHandler = 
    { Regex : Regex
      Fun : RestRequest<string> -> IHttpReply }
    static member New f h =
        { Regex = f
          Fun = h }
    interface IRouteHandler with
        member x.Match path rq = 
            if  path |> x.Regex.IsMatch
            then Some (path :> obj)
            else None
        member x.TryHandle path rq rs = 
            let m = (x :> IRouteHandler).Match path rq
            match m with
            | Some _ -> 
                let r = RestRequest<string>.New rq rs rq.RawUrl
                Some (x.Fun r)
            | None -> None



