#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open System
open System.Text
open NuGetVersion

getLastNuGetVersion (NuGetVersionArg.Default().Server) "FAKE"
nextVersion (fun arg -> { arg with PackageName="FAKE"; Increment=IncPatch })
nextVersion (fun arg -> { arg with PackageName="FAKE"; Increment=IncMinor })
nextVersion (fun arg -> { arg with PackageName="FAKE"; Increment=IncMajor })


let buildPclDir = "./buildPcl/"
let buildLibDir = "./buildLib/"
let buildDroidDir = "./buildDroid/"
let buildIosDir = "./buildIos/"
let buildIISDir = "./buildIIS/"

let packagingDir = "./packaging/"
let packagingRoot = "./packagingRoot/"

buildPclDir |> ensureDirectory
buildLibDir |> ensureDirectory
buildDroidDir |> ensureDirectory
buildIosDir |> ensureDirectory
buildIISDir |> ensureDirectory

packagingDir |> ensureDirectory
packagingRoot |> ensureDirectory

let keyFile = @"C:\keys\nuget-romcyber.txt"
let nugetApiKey = if keyFile |> fileExists then keyFile |> ReadFileAsString else ""
tracefn "Nuget API key is '%s'" nugetApiKey
let mutable publishNuget = false
let pclVersion = nextVersion (fun arg -> { arg with PackageName="TinyRest-PCL" })

Target "Clean" (fun _ ->
    CleanDir buildPclDir
    CleanDir buildLibDir
    CleanDir buildDroidDir
    CleanDir buildIosDir
    CleanDir buildIISDir

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
                AccessKey = nugetApiKey
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
)

Target "CreateIISPackage" (fun _ ->
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

Target "BuildIIS" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest.IIS.fsproj" |> MSBuildRelease buildIISDir "Build" |> Log "BuildIIS-Output: "
)

Target "PublishNugets" (fun _ ->
    trace "Nugets published"
)

if hasBuildParam "target" && environVar "target" = "PublishNugets"
then publishNuget <- true

"Clean"
    ==> "BuildPCL"
    ==> "CreatePclPackage"
    ==> "BuildLib"
    ==> "CreateLibPackage"
    ==> "BuildIIS"
    ==> "CreateIISPackage"
    ==> "BuildDroid"
    ==> "CreateDroidPackage"
    ==> "BuildIos"
    ==> "CreateIosPackage"
    ==> "PublishNugets"

RunTargetOrDefault "CreateIosPackage"
//RunTargetOrDefault "CreateLibPackage"


