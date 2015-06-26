#r "packages/FAKE.3.35.4/tools/FakeLib.dll"

open Fake

let buildDir = "./build/"
let packagingDir = "./packaging/"
let packagingRoot = "./packagingRoot/"
let buildVersion = "1.0"

buildDir |> ensureDirectory
packagingDir |> ensureDirectory
packagingRoot |> ensureDirectory

Target "Clean" (fun _ ->
    CleanDir buildDir
    CleanDir packagingDir
)

Target "CreatePackage" (fun _ ->
    let allPackageFiles = buildDir |> directoryInfo |> filesInDir |> Seq.map (fun f -> f.FullName)
    CopyFiles packagingDir allPackageFiles

    NuGet (fun p -> 
        {p with
            Authors = ["rflechner"]
            Project = "TinyRest"
            Description = "A tiny FSharp and CSharp Rest server"
            OutputPath = packagingRoot
            Summary = "A tiny FSharp and CSharp Rest server"
            WorkingDir = packagingDir
            Version = buildVersion
            AccessKey = ""
            Publish = false
            //Files = [(@"tools\**\*.*", None, None)]
            Files = (allPackageFiles |> Seq.map (fun f -> (f,None,None)) |> Seq.toList)
             }) 
            "TinyRest.nuspec"
)

Target "BuildLib" (fun _ ->
    trace "Building default target"
    RestorePackages()
    !! "**/TinyRest.fsproj"
        |> MSBuildRelease buildDir "Build"
        |> Log "BuildLib-Output: "
)

"Clean"
    ==> "BuildLib"
    ==> "CreatePackage"


RunTargetOrDefault "CreatePackage"
