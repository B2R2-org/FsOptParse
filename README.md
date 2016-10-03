OptParse: An F# Command Line Parsing Library
===============================================

OptParse library (OptParse.dll) implements command-line parsing APIs that are
succinct and clean. It is completely written in a single F# file (fs). It is
very intuitive to use, and also provides lots of convenient command-line parsing
features.

OptParse exposes just two functions including `optParse` and `usageExit`.  The
`optParse` function takes in a specification of command line options, a program
name, and a list of arguments from a user as input. It then parses the input
arguments and calls corresponding callback functions registered through the
specification as per interpreting each encountered option. Finally, it returns a
list of unmatched arguments. The `usageExit` function prints out a well-formed
usage based on a given specification, and terminates the program.

Build
-----
OptParse uses FAKE for building. Simply type `make` in a terminal.

Package
-------
Available in NuGet.

[![NuGet Status](http://img.shields.io/nuget/v/OptParse.svg?style=flat)](https://www.nuget.org/packages/OptParse/)

Example
-------

The src/opttest.fsx file contains an example usage.


```fsharp
open OptParse

(** defines a state to pass to the option parser *)
type opts =
  {
    optX : int;
    optY : bool;
    optZ : string;
  }

(** default option state *)
let defaultOpts =
  {
    optX = 0;
    optY = false;
    optZ = "";
  }

(*
  An example command line specification, which is a list of Options.
  Each Option describes a command line option (switch) that is specified with
  either a short (a single-dash option) or long option (a double-dash option).
*)
let spec =
  [
    (* This option can be specified with -x <NUM>. There is an extra argument to
       specify a value in integer. *)
    Option ((* description of the option *)
            descr="this is a testing param X",
            (* how many extra argument must be provided by a user? *)
            extra=1,
            (* callback sets up the option and returns it *)
            callback=(fun opts arg -> {opts with optX=(int) arg.[0]}),
            (* use a short option style -x *)
            short="-x"
           );

    (* This option can be specified with -y. There is no extra argument. This
       option just sets a flag, optY. *)
    Option ((* description of the option *)
            descr="this is a testing param Y",
            (* set the option to be true *)
            callback=(fun opts _ -> {opts with optY=true}),
            (* use a short option style (-y) *)
            short="-y",
            (* also use a long option style (--yoohoo) *)
            long="--yoohoo"
           );

    (* The third option is a required option. In other words, option parsing
       will raise an exception if this option is not given by a user. This
       option takes in an additional integer argument, and set it to the global
       variable z. *)
    Option ((* description of the option *)
            descr="required parameter <STRING> with an integer option",
            (* callback to set the optZ value *)
            callback=(fun opts arg -> {opts with optZ=arg.[0]}),
            (* specifying this is a required option *)
            required=true,
            (* one additional argument to specify an integer value *)
            extra=1,
            (* use only a long option style *)
            long="--req"
           );
  ]

[<EntryPoint>]
let main (args:string[]) =
  let prog = "opttest.fsx"
  let args = System.Environment.GetCommandLineArgs ()
  let usageStr = "[Usage]\n  %p %o"
  try
    let left, opts = optParse spec usageStr prog args defaultOpts
    printfn "Rest args: %A, x: %d, y: %b, z: %s"
      left opts.optX opts.optY opts.optZ
    0
  with
    | SpecErr msg ->
        eprintfn "Invalid spec: %s" msg
        exit 1
    | RuntimeErr msg ->
        eprintfn "Invalid args given by user: %s" msg
        usagePrint spec prog usageStr (fun () -> exit 1)
```
