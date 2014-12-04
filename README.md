OptParse: An F# Command Line Parsing Library
===============================================

OptParse library (OptParse.dll) implements command-line parsing APIs that are
succinct and clean. It is completely written in a single F# file (fs). It is
very intuitive to use, and also provides lots of convenient command-line parsing
features.

OptParse exposes just two functions including `opt_parse` and `usage_exit`.  The
`opt_parse` function takes in a specification of command line options, a program
name, and a list of arguments from a user as input. It then parses the input
arguments and calls corresponding callback functions registered through the
specification as per interpreting each encountered option. Finally, it returns a
list of unmatched arguments. The `usage_exit` function prints out a well-formed
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
open Optparse

let x = ref 0
let y = ref false
let z = ref 0

(*
  An example command line specification, which is a list of Options.
  Each Option describes a command line option (switch) that is specified with
  either a short (a single-dash option) or long option (a double-dash option).
*)
let spec =
  [
    (* this option is specified with -x <NUM>. There is an extra argument to
       specify a number. *)
    Option (descr="this is a testing param X", (* description of an option *)
            extra=1, (* how many extra argument must be provided by a user? *)
            callback=(fun arg -> x := (int) arg.[0]), (* remember the option *)
            short="-x" (* just use a short option -x *) );

    (* this option is specified with -y. There is no extra argument. This option
       just turns on a flag, which is the global variable y in this case. *)
    Option (descr="this is a testing param Y",
            callback=(fun _ -> y := true), (* set the option to be true *)
            short="-y",
            long="--yoohoo" (* use both short and long option *) );

    (* this option is a required option. In other words, this option must be
       given to pass the option parsing. This option takes in an additional
       integer argument, and set it to the global variable z. *)
    Option (descr="required parameter <NUM> with an integer option",
            callback=(fun arg -> z := (int) arg.[0]),
            required=true, (* this is a required option *)
            extra=1, (* one additional argument to specify an integer value *)
            long="--req" (* use only a long option *) );
  ]

[<EntryPoint>]
let main (args:string[]) =
  let prog = "opttest.exe" in
  try
    let left = opt_parse spec prog args in
    printfn "Rest args: %A, x: %d, y: %b, z: %d" left !x !y !z
    0
  with
    | SpecErr msg ->
        eprintfn "invalid spec: %s" msg
        exit 1
    | RuntimeErr msg ->
        eprintfn "invalid args given by user: %s" msg
        usage_exit spec prog
```
