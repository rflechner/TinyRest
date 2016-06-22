#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open System
open System.Text
open NuGetVersion
open ProcessHelper
open XamarinHelper
open EnvironmentHelper

let buildPclDir = "./buildPcl/"
let buildLibDir = "./buildLib/"
let buildDroidDir = "./buildDroid/"
let buildIosDir = "./buildIos/"
let buildIISDir = "./buildIIS/"

let packagingDir = "./packaging/"
let packagingRoot = "./packagingRoot/"

ensureDirectory buildPclDir
ensureDirectory buildLibDir
ensureDirectory buildDroidDir
ensureDirectory buildIosDir
ensureDirectory buildIISDir
ensureDirectory packagingDir
ensureDirectory packagingRoot

let trim (s:string) = s.Trim()

//let keyFile = __SOURCE_DIRECTORY__ @@ "../../nuget_key.txt"
let keyFile = @"C:\keys\nuget-romcyber.txt"
let nugetApiKey = trim <| if keyFile |> fileExists then keyFile |> ReadFileAsString else ""
tracefn "Nuget API key is '%s'" nugetApiKey
//let mutable publishNuget = false
let pclVersion = nextVersion (fun arg -> { arg with PackageName="TinyRest-PCL" })

let createPclPackage publishNuget =
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
                AccessKey = nugetApiKey
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\portable-net45+win+wp80+MonoAndroid10+MonoTouch10", None) ]
                Dependencies = []
             }) 
            "TinyRest.nuspec"

let createLibPackage publishNuget =
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
                Version = nextVersion (fun arg -> { arg with PackageName="TinyRest"; Increment=IncMinor })
                Tags = "Tiny Rest Http Server"
                AccessKey = nugetApiKey
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\net45", None) ]
                Dependencies = [
                                    "Newtonsoft.Json", GetPackageVersion "./packages/" "Newtonsoft.Json"
                                    "TinyRest-PCL", pclVersion
                                    "FSharp.Core", GetPackageVersion "./packages/" "FSharp.Core"
                               ]
             })
            "TinyRest.nuspec"

let createIISPackage publishNuget =
    CleanDir packagingDir
    let packFiles = buildIISDir 
                        |> directoryInfo 
                        |> filesInDir 
                        |> Seq.map (fun f -> f.FullName) 
                        |> Seq.filter (fun f -> f.Contains "TinyRest.IIS.dll")
    CopyFiles packagingDir packFiles

    NuGet (fun p -> 
            {p with
                Authors = ["rflechner"]
                Project = "TinyRest.IIS"
                Title = "TinyRest.IIS"
                Description = "A tiny FSharp and CSharp Rest server hosted on IIS"
                OutputPath = packagingRoot
                Summary = "A tiny FSharp and CSharp Rest server hosted on IIS"
                WorkingDir = packagingDir
                Version = nextVersion (fun arg -> { arg with PackageName="TinyRest.IIS" })
                Tags = "Tiny Rest Http Server IIS"
                AccessKey = nugetApiKey
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\net45", None) ]
                Dependencies = [
                                    "Newtonsoft.Json", GetPackageVersion "./packages/" "Newtonsoft.Json"
                                    "FSharp.Core", GetPackageVersion "./packages/" "FSharp.Core"
                               ]
             })
            "TinyRest.nuspec"

let createDroidPackage publishNuget =
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
                Version = nextVersion (fun arg -> { arg with PackageName="TinyRest-Android" })
                Tags = "Tiny Rest Http Server xamarin android"
                AccessKey = nugetApiKey
                Publish = publishNuget
                Files = [ ("*.*", Some @"lib\MonoAndroid", None) ]
                Dependencies = [
                                    "TinyRest-PCL", pclVersion
                                    "FSharp.Core", GetPackageVersion "./packages/" "FSharp.Core"
                               ]
             })
            "TinyRest.nuspec"

let createIosPackage publishNuget =
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
                Version = nextVersion (fun arg -> { arg with PackageName="TinyRest-IOS" })
                Tags = "Tiny Rest Http Server xamarin IOS"
                AccessKey = nugetApiKey
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
let packNugets publish =
    createPclPackage publish
    createLibPackage publish
    createIISPackage publish
    createDroidPackage publish
    createIosPackage publish

Target "Clean" (fun _ ->
    CleanDir buildPclDir
    CleanDir buildLibDir
    CleanDir buildDroidDir
    CleanDir buildIosDir
    CleanDir buildIISDir

    CleanDir packagingDir
    CleanDir packagingRoot
)

Target "PackNugets" (fun _ ->
    packNugets false
)

Target "PublishNugets" (fun _ ->
    packNugets true
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
    !! "**/TinyRest.iOS.Core.fsproj" |> MSBuildRelease buildIosDir "Build" |> Log "BuildLib-Output: "
)

Target "BuildIIS" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest.IIS.fsproj" |> MSBuildRelease buildIISDir "Build" |> Log "BuildIIS-Output: "
)

"Clean"
    ==> "BuildPCL"
    ==> "BuildLib"
    ==> "BuildIIS"
    ==> "BuildDroid"
    ==> "BuildIos"
    ==> "PackNugets"
    ==> "PublishNugets"

RunTargetOrDefault "PackNugets"


