// include Fake lib
#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.FscHelper

let buildDir = "./build/"

Target "Clean" (fun _ ->
    CleanDir buildDir
)

Target "Default" (fun _ ->
  ["src/optparse.fsi"; "src/optparse.fs"]
  |> Fsc (fun p ->
           {p with Output = buildDir + "OptParse.dll"
                   FscTarget = Library
                   OtherParams = ["--warnaserror:76";
                                  "--warn:3";
                                  "--checked+";
                                  "--optimize+"]
           }
         )
)

// Dependencies
"Clean"
  ==> "Default"

// start build
RunTargetOrDefault "Default"
