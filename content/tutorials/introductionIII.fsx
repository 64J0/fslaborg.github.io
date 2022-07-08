(**
# F# Introduction III: Library Setup

This guide shows an example setup for a library. This is not the only way on how to do this, but merely a possibility. As always, this guide is meant as a starting point to be expanded upon.
For example, unit tests and full buildchains with automatic releases can be added to this template.
The installation of .NET 5.0 or dotnet SDK 3.1 LTS is required. It is also recommended to use [GitHub](https://github.com/) when following this example.

## Initializing the repository

An easy way to initialize a repository is by creating a new one using GitHub and cloning it.
You can automatically add a readme, a .gitignore with many entries for Visual Studio already added and a license of choice.
![](https://fslab.org/images/InitRepo.png)
After you cloned the initialized repository, it should look like this:
![](https://fslab.org/images/Lib1.png)
## Initializing the library




```
### 0.0.1 - 28/7/2021
```


## Initializing the buildchain with FAKE

Initialize a local tool manifest that will keep track of the usable local dotnet tools in this project.
In the project root: `dotnet new tool-manifest`
In the project root: Install the fake cli as local tool: `dotnet tool install fake-cli`
In the project root: Install paket as local tool: `dotnet tool install paket`
In the project root: Create a new empty `build.fsx` file
Your folder should now look like this:
![](https://fslab.org/images/Lib3.png)
Open the `build.fsx` file (intellisense will not work right after creating it) and add the following content.
First, lets reference the dependencies of the build script. In fake they are loaded via the `paket` manager:

```fsharp
#r "paket:
nuget BlackFox.Fake.BuildTask
nuget Fake.Core.Target
nuget Fake.Core.Process
nuget Fake.Core.ReleaseNotes
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.DotNet.FSFormatting
nuget Fake.DotNet.Fsi
nuget Fake.DotNet.NuGet
nuget Fake.Api.Github
nuget Fake.DotNet.Testing.Expecto 
nuget Fake.Tools.Git //"
```

Then, we open the dependencies. Note that for getting intellisense, you will have to run the script once with the fake runner (see [here](#Running-the-build-script)).

*)
#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/dotnet/fsharp/issues/5216
#endif

open BlackFox.Fake
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools

[<AutoOpen>]
/// user interaction prompts for critical build tasks where you may want to interrupt when you see wrong inputs.
module MessagePrompts =

    let prompt (msg:string) =
        System.Console.Write(msg)
        System.Console.ReadLine().Trim()
        |> function | "" -> None | s -> Some s
        |> Option.map (fun s -> s.Replace ("\"","\\\""))

    let rec promptYesNo msg =
        match prompt (sprintf "%s [Yn]: " msg) with
        | Some "Y" | Some "y" -> true
        | Some "N" | Some "n" -> false
        | _ -> System.Console.WriteLine("Sorry, invalid answer"); promptYesNo msg

    let releaseMsg = """This will stage all uncommitted changes, push them to the origin and bump the release version to the latest number in the RELEASE_NOTES.md file. 
        Do you want to continue?"""

    let releaseDocsMsg = """This will push the docs to gh-pages. Remember building the docs prior to this. Do you want to continue?"""

/// Executes a dotnet command in the given working directory
let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir
(**
Note: This `build.fsx` will be gradually epxanded



*)
/// Metadata about the project
module ProjectInfo = 

    let project = "LibraryExample"

    let summary = "An example Library"

    let configuration = "Release"

    // Git configuration (used for publishing documentation in gh-pages branch)
    // The profile where the project is posted
    let gitOwner = "YourGitProfile"
    let gitName = "YourNameHere"

    let gitHome = sprintf "%s/%s" "https://github.com" gitOwner

    let projectRepo = sprintf "%s/%s/%s" "https://github.com" gitOwner gitName

    let website = "/YourNameHere"

    let pkgDir = "pkg"

    let release = ReleaseNotes.load "RELEASE_NOTES.md"

    let stableVersion = SemVer.parse release.NugetVersion

    let stableVersionTag = (sprintf "%i.%i.%i" stableVersion.Major stableVersion.Minor stableVersion.Patch )

    let mutable prereleaseSuffix = ""

    let mutable prereleaseTag = ""

    let mutable isPrerelease = false
(**

*)
/// Barebones, minimal build tasks
module BasicTasks = 

    open ProjectInfo

    let setPrereleaseTag = BuildTask.create "SetPrereleaseTag" [] {
        printfn "Please enter pre-release package suffix"
        let suffix = System.Console.ReadLine()
        prereleaseSuffix <- suffix
        prereleaseTag <- (sprintf "%s-%s" release.NugetVersion suffix)
        isPrerelease <- true
    }

    let clean = BuildTask.create "Clean" [] {
        !! "src/**/bin"
        ++ "src/**/obj"
        ++ "pkg"
        ++ "bin"
        |> Shell.cleanDirs 
    }

    let build = BuildTask.create "Build" [clean] {
        !! "src/**/*.*proj"
        |> Seq.iter (DotNet.build id)
    }

    let copyBinaries = BuildTask.create "CopyBinaries" [clean; build] {
        let targets = 
            !! "src/**/*.??proj"
            -- "src/**/*.shproj"
            |>  Seq.map (fun f -> ((Path.getDirectory f) </> "bin" </> configuration, "bin" </> (Path.GetFileNameWithoutExtension f)))
        for i in targets do printfn "%A" i
        targets
        |>  Seq.iter (fun (fromDir, toDir) -> Shell.copyDir toDir fromDir (fun _ -> true))
    }
(**

*)
open BasicTasks
BuildTask.runOrDefault copyBinaries
(**

### build.cmd

```shell
dotnet tool restore
dotnet fake build %*
```

### build.sh

```shell
#!/usr/bin/env bash

set -eu
set -o pipefail

dotnet tool restore
dotnet fake build "$@"
```

## Running the build script





## Packing a nuget package


```
<PropertyGroup>
    <Authors>YourName</Authors>
    <Description>Your description here</Description>
    <Summary>Your summary here</Summary>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://fslab.org/projectName/</PackageProjectUrl>
    <PackageIconUrl>https://fslab.org/projectName/img/logo.png</PackageIconUrl>
    <PackageTags>documentation fsharp csharp dotnet</PackageTags>
    <RepositoryUrl>https://github.com/fslaborg/projectName</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <FsDocsLicenseLink>https://github.com/fslaborg/projectName/blob/master/LICENSE</FsDocsLicenseLink>
    <FsDocsReleaseNotesLink>https://github.com/fslaborg/projectName/blob/master/RELEASE_NOTES.md</FsDocsReleaseNotesLink>
</PropertyGroup>
```


*)
/// Package creation
module PackageTasks = 

    open ProjectInfo

    open BasicTasks

    let pack = BuildTask.create "Pack" [clean; build; copyBinaries] {
        if promptYesNo (sprintf "creating stable package with version %s OK?" stableVersionTag ) 
            then
                !! "src/**/*.*proj"
                |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                    let msBuildParams =
                        {p.MSBuildParams with 
                            Properties = ([
                                "Version",stableVersionTag
                                "PackageReleaseNotes",  (release.Notes |> String.concat "\r\n")
                            ] @ p.MSBuildParams.Properties)
                        }
                    {
                        p with 
                            MSBuildParams = msBuildParams
                            OutputPath = Some pkgDir
                    }
                ))
        else failwith "aborted"
    }

    let packPrerelease = BuildTask.create "PackPrerelease" [setPrereleaseTag; clean; build; copyBinaries] {
        if promptYesNo (sprintf "package tag will be %s OK?" prereleaseTag )
            then 
                !! "src/**/*.*proj"
                //-- "src/**/Plotly.NET.Interactive.fsproj"
                |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->
                            let msBuildParams =
                                {p.MSBuildParams with 
                                    Properties = ([
                                        "Version", prereleaseTag
                                        "PackageReleaseNotes",  (release.Notes |> String.toLines )
                                    ] @ p.MSBuildParams.Properties)
                                }
                            {
                                p with 
                                    VersionSuffix = Some prereleaseSuffix
                                    OutputPath = Some pkgDir
                                    MSBuildParams = msBuildParams
                            }
                ))
        else
            failwith "aborted"
    }
(**




## Documentation





*)
/// Build tasks for documentation setup and development
module DocumentationTasks =

    open ProjectInfo

    open BasicTasks

    let buildDocs = BuildTask.create "BuildDocs" [build; copyBinaries] {
        printfn "building docs with stable version %s" stableVersionTag
        runDotNet 
            (sprintf "fsdocs build --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" stableVersionTag)
            "./"
    }

    let buildDocsPrerelease = BuildTask.create "BuildDocsPrerelease" [setPrereleaseTag; build; copyBinaries] {
        printfn "building docs with prerelease version %s" prereleaseTag
        runDotNet 
            (sprintf "fsdocs build --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" prereleaseTag)
            "./"
    }

    let watchDocs = BuildTask.create "WatchDocs" [build; copyBinaries] {
        printfn "watching docs with stable version %s" stableVersionTag
        runDotNet 
            (sprintf "fsdocs watch --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" stableVersionTag)
            "./"
    }

    let watchDocsPrerelease = BuildTask.create "WatchDocsPrerelease" [setPrereleaseTag; build; copyBinaries] {
        printfn "watching docs with prerelease version %s" prereleaseTag
        runDotNet 
            (sprintf "fsdocs watch --eval --clean --property Configuration=Release --parameters fsdocs-package-version %s" prereleaseTag)
            "./"
    }
(**




## Adding nuget packages



```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
</configuration>
```


```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="Package source 1" value="./lib" />
  </packageSources>
</configuration>
```


*)

