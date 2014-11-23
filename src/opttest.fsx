(*
  Optparse - FSharp-based Command Line Argument Parsing

  Author: Sang Kil Cha <sangkil.cha@gmail.com>

  Copyright (c) 2014 Sang Kil Cha

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*)

#load "optparse.fs"
open OptParse

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

let _ =
  let args = System.Environment.GetCommandLineArgs () in
  try
    let left = opt_parse spec "opttest.fsx" args in
    printfn "Rest args: %A" left
    printfn "%d, %b, %d" !x !y !z
    0
  with OptError err ->
    printfn "option parsing error: %s" err
    1

// vim: set tw=80 sts=2 sw=2:
