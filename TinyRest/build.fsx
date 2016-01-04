#r "packages/FAKE/tools/FakeLib.dll"
#r "packages/Newtonsoft.Json.8.0.1/lib/portable-net40+sl5+wp80+win8+wpa81/Newtonsoft.Json.dll"

open Fake
open System
open System.Text

let buildPclDir = "./buildPcl/"
let buildLibDir = "./buildLib/"
let buildDroidDir = "./buildDroid/"
let buildIosDir = "./buildIos/"

let packagingDir = "./packaging/"
let packagingRoot = "./packagingRoot/"

buildPclDir |> ensureDirectory
buildLibDir |> ensureDirectory
buildDroidDir |> ensureDirectory
buildIosDir |> ensureDirectory

packagingDir |> ensureDirectory
packagingRoot |> ensureDirectory

module NugetVersion =
    open System
    open System.Net
    open Newtonsoft.Json

    type NugetSearchItemResult =
        { Version:string
          Published:DateTime }
    type NugetSearchResult = 
        { results:NugetSearchItemResult list }
    type NugetSearchResponse = 
        { d:NugetSearchResult }
    type NugetVersionIncrement = string -> Version
    
    let positive i = Math.Max(0, i)

    let IncBuild:NugetVersionIncrement = 
        fun (version:string) ->
            let v = Version version
            sprintf "%d.%d.%d" (positive v.Major) (positive v.Minor) (positive v.Build+1)
            |> Version
    let IncMinor:NugetVersionIncrement = 
        fun (version:string) ->
            let v = Version version
            printfn "version obj: %A" v
            let n = sprintf "%d.%d.%d" (positive v.Major) (positive v.Minor+1) (positive v.Build)
            printfn "next version: %s" n
            Version n
    let IncMajor:NugetVersionIncrement = 
        fun (version:string) ->
            let v = Version version
            sprintf "%d.%d.%d" (positive v.Major+1) (positive v.Minor) (positive v.Build)
            |> Version
    
    type NugetVersionArg =
        { Server:string
          PackageName:string
          Increment:NugetVersionIncrement }
        static member Default() =
            { Server="https://www.nuget.org/api/v2"
              PackageName=""
              Increment=IncMinor }

    let getlastNugetVersion server (packageName:string) = 
        let escape = Uri.EscapeDataString
        let url = 
            sprintf "%s/Packages()?$filter=%s%s%s&$orderby=%s"
                server
                (escape "Id eq '")
                packageName
                (escape "'")
                (escape "IsLatestVersion desc")
        let client = new WebClient()
        client.Headers.Add("Accept", "application/json")
        let text = client.DownloadString url
        let json = JsonConvert.DeserializeObject<NugetSearchResponse>(text)
        json.d.results
        |> Seq.sortByDescending (fun i -> i.Published)
        |> Seq.tryHead
        |> fun i -> match i with | Some v -> Some v.Version | None -> None
    
    let nextVersion (f : NugetVersionArg -> NugetVersionArg) =
        let arg = f (NugetVersionArg.Default())
        match getlastNugetVersion arg.Server arg.PackageName with
        | Some v -> 
            printfn "Version: %s" v
            (arg.Increment v).ToString()
        | None -> "1.0"

let keyFile = @"C:\keys\nuget-romcyber.txt"
let nugetApiKey = if keyFile |> fileExists then keyFile |> ReadFileAsString else ""
tracefn "Nuget API key is '%s'" nugetApiKey
let publishNuget = false
let pclVersion = NugetVersion.nextVersion (fun arg -> { arg with PackageName="TinyRest-PCL" })

Target "Clean" (fun _ ->
    CleanDir buildPclDir
    CleanDir buildLibDir
    CleanDir buildDroidDir
    CleanDir buildIosDir

    CleanDir packagingDir
    CleanDir packagingRoot
)

Target "CreatePclPackage" (fun _ ->
    CleanDir packagingDir
    let packFiles = buildPclDir |> directoryInfo |> filesInDir |> Seq.map (fun f -> f.FullName) |> Seq.filter (fun f -> f.Contains "TinyRest_PCL.dll")
    CopyFiles packagingDir packFiles

    NuGet (fun p -> 
            {p with
                Authors = ["rflechner"]
                Title = "TinyRest-PCL"
                Project = "TinyRest-PCL"
                Description = "TinyRest portable library"
                OutputPath = packagingRoot
                Summary = "Write your service implementation in PCL to run it in multiple platforms"
                WorkingDir = packagingDir
                Version = pclVersion
                Tags = "Tiny Rest Http Server PCL"
                AccessKey = keyFile
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\portable-net45+win+wp80+MonoAndroid10+MonoTouch10", None) ]
                Dependencies = []
             }) 
            "TinyRest.nuspec"
)

Target "CreateLibPackage" (fun _ ->
    CleanDir packagingDir
    let packFiles = buildLibDir 
                        |> directoryInfo 
                        |> filesInDir 
                        |> Seq.map (fun f -> f.FullName) 
                        |> Seq.filter (fun f -> f.Contains "TinyRest.dll")
    CopyFiles packagingDir packFiles

    NuGet (fun p -> 
            {p with
                Authors = ["rflechner"]
                Project = "TinyRest"
                Title = "TinyRest"
                Description = "A tiny FSharp and CSharp Rest server"
                OutputPath = packagingRoot
                Summary = "A tiny FSharp and CSharp Rest server"
                WorkingDir = packagingDir
                Version = NugetVersion.nextVersion (fun arg -> { arg with PackageName="TinyRest" })
                Tags = "Tiny Rest Http Server"
                AccessKey = keyFile
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\net45", None) ]
                Dependencies = [
                                    "Newtonsoft.Json", GetPackageVersion "./packages/" "Newtonsoft.Json"
                                    "TinyRest-PCL", pclVersion
                                    "FSharp.Core", GetPackageVersion "./packages/" "FSharp.Core"
                               ]
             })
            "TinyRest.nuspec"
)

Target "CreateDroidPackage" (fun _ ->
    CleanDir packagingDir
    let packFiles = buildDroidDir
                        |> directoryInfo 
                        |> filesInDir 
                        |> Seq.map (fun f -> f.FullName) 
                        |> Seq.filter (fun f -> (f.Contains "TinyRest.Droid"))
    CopyFiles packagingDir packFiles

    NuGet (fun p -> 
            {p with
                Authors = ["rflechner"]
                Project = "TinyRest-Android"
                Title = "TinyRest.Android"
                Description = "A tiny FSharp and CSharp Rest server"
                OutputPath = packagingRoot
                Summary = "A tiny FSharp and CSharp Rest server"
                WorkingDir = packagingDir
                Version = NugetVersion.nextVersion (fun arg -> { arg with PackageName="TinyRest-Android" })
                Tags = "Tiny Rest Http Server xamarin android"
                AccessKey = keyFile
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\MonoAndroid", None) ]
                Dependencies = [
                                    "TinyRest-PCL", pclVersion
                                    "FSharp.Core", GetPackageVersion "./packages/" "FSharp.Core"
                               ]
             })
            "TinyRest.nuspec"
)

Target "CreateIosPackage" (fun _ ->
    CleanDir packagingDir
    let packFiles = buildIosDir
                        |> directoryInfo 
                        |> filesInDir 
                        |> Seq.map (fun f -> f.FullName) 
                        |> Seq.filter (fun f -> (f.Contains "Newtonsoft" |> not) && (f.Contains  "TinyRest_PCL" |> not))
    CopyFiles packagingDir packFiles

    NuGet (fun p -> 
            {p with
                Authors = ["rflechner"]
                Project = "TinyRest-IOS"
                Title = "TinyRest.IOS"
                Description = "A tiny FSharp and CSharp Rest server"
                OutputPath = packagingRoot
                Summary = "A tiny FSharp and CSharp Rest server"
                WorkingDir = packagingDir
                Version = NugetVersion.nextVersion (fun arg -> { arg with PackageName="TinyRest-IOS" })
                Tags = "Tiny Rest Http Server xamarin IOS"
                AccessKey = keyFile
                Publish = publishNuget
                Files = [ 
                            ("*.*", Some @"lib\XamariniOS10", None)
                            ("*.*", Some @"lib\MonoTouch10", None)
                        ]
                Dependencies = [
                                    "TinyRest-PCL", pclVersion
                               ]
             })
            "TinyRest.nuspec"
)

Target "BuildPCL" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest-PCL.fsproj" |> MSBuildRelease buildPclDir "Build" |> Log "BuildLib-Output: "
)

Target "BuildLib" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest.fsproj" |> MSBuildRelease buildLibDir "Build" |> Log "BuildLib-Output: "
)

Target "BuildDroid" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest.Droid.fsproj" |> MSBuildRelease buildDroidDir "Build" |> Log "BuildLib-Output: "
)

Target "BuildIos" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest.iOS.fsproj" |> MSBuildRelease buildIosDir "Build" |> Log "BuildLib-Output: "
)

"Clean"
    ==> "BuildPCL"
    ==> "CreatePclPackage"
    ==> "BuildLib"
    ==> "CreateLibPackage"
    ==> "BuildDroid"
    ==> "CreateDroidPackage"
    ==> "BuildIos"
    ==> "CreateIosPackage"

//RunTargetOrDefault "CreateIosPackage"
RunTargetOrDefault "CreateLibPackage"


