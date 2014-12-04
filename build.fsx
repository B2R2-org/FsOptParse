// include Fake lib
#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.FscHelper

let projDesc = "A tiny, but powerful option parsing library written in a single F# file"
let releaseNotes =
  ReadFile "ReleaseNotes.md"
  |> ReleaseNotesHelper.parseReleaseNotes

let buildDir = "./build/"
let packagingRoot = "./packaging/"
let packagingDir = packagingRoot @@ "OptParse"

Target "Clean" (fun _ ->
  CleanDir buildDir
)

Target "Default" (fun _ ->
  ["src/optparse.fsi"; "src/optparse.fs"]
  |> Fsc (fun p ->
           {p with Output = buildDir @@ "OptParse.dll"
                   FscTarget = Library
                   OtherParams = ["--warnaserror:76";
                                  "--warn:3";
                                  "--checked+";
                                  "--optimize+"]
           }
         )
)

Target "CreatePackage" (fun _ ->
  let net45Dir = packagingDir @@ "lib/net45/"
  CleanDir net45Dir
  CopyFile net45Dir (buildDir @@ "OptParse.dll")
  CopyFiles packagingDir ["LICENSE"; "README.md"; "ReleaseNotes.md"]
  NuGet
    (fun p ->
      {p with
        Description = projDesc
        Version = releaseNotes.AssemblyVersion
        ReleaseNotes = toLines releaseNotes.Notes
        WorkingDir = packagingDir
        OutputPath = packagingRoot
      }
    )
    "OptParse.nuspec"
)

Target "Pack" DoNothing

// Dependencies
"Clean"
  ==> "Default"

"CreatePackage"
  ==> "Pack"

// start build
RunTargetOrDefault "Default"
