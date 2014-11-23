Fs.OptParse: An F# Command Line Parsing Library
===============================================

Fs.OptParse library (OptParse.dll) implements command-line parsing APIs that are
succinct and clean. It is completely written in a single F# file (fs), and a
single signature file (fsi). It is also very intuitive to use, and provides lots
of convenient command-line parsing features.

Fs.OptParse exposes a single function `opt_parse` that takes in a specification
of command line options, a program name, and a list of arguments from a user as
input. It then parses the input arguments and calls corresponding callback
functions registered through the specification as per interpreting each
encountered option. Finally, it returns a list of unmatched arguments.

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
  try
    let left = opt_parse spec "opttest.exe" args in
    printfn "Rest args: %A" left
    printfn "%d, %b, %d" !x !y !z
    0
  with OptError err ->
    printfn "option parsing error: %s" err
    1
```
