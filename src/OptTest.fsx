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

    (* A dummy option to pretty-print the usage *)
    Option ((* description of the option *)
            descr="",
            dummy=true
           );
    Option ((* description of the option *)
            descr="[Required Options]",
            dummy=true
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

let _ =
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

// vim: set tw=80 sts=2 sw=2:
