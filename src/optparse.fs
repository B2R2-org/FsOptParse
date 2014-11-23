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

module OptParse

open System
open System.Text.RegularExpressions

(** Option parsing error *)
exception SpecErr of string
exception RuntimeErr of string

let specerr msg = raise (SpecErr msg)
let rterr msg = raise (RuntimeErr msg)

type args = string array

let sanitize_extra (n: int) =
  if n < 0 then specerr "Extra field should be positive"
  else n

let rec remove_dashes (s: string) =
  if s.[0] = '-' then remove_dashes s.[1..] else s

let sanitize_short (opt: string) =
  if opt.Length = 0 then opt
  else
    let opt = remove_dashes opt in
    if opt.Length = 1 then "-" + opt
    else specerr (sprintf "Invalid short option %s is given" opt)

let sanitize_long (opt: string) =
  if opt.Length = 0 then opt
  else
    let opt = "--" + (remove_dashes opt) in
    if opt.Length > 2 then opt
    else specerr (sprintf "Invalid long option %s is given" opt)

(** command line option *)
type Option (descr, callback, ?required, ?extra, ?short, ?long) =
  member this.descr : string = descr
  member this.callback = callback
  member this.required : bool = defaultArg required false
  member this.extra : int = defaultArg extra 0 |> sanitize_extra
  member this.short : string = defaultArg short "" |> sanitize_short
  member this.long : string = defaultArg long "" |> sanitize_long

  interface IComparable<Option> with
    member this.CompareTo obj =
      compare (this.short, this.long) (obj.short, obj.long)

  interface IComparable with
    member this.CompareTo obj =
      match obj with
        | :? Option as obj -> (this :> IComparable<_>).CompareTo obj
        | _ -> specerr "Not an option"

  interface IEquatable<Option> with
    member this.Equals obj =
      this.short = obj.short && this.long = obj.long

  override this.Equals obj =
    match obj with
      | :? Option as obj -> (this :> IEquatable<_>).Equals obj
      | _ -> specerr "Not an option"

  override this.GetHashCode () =
    hash (this.short, this.long)

(* The specification of command line options *)
type spec = Option list

let rec rep acc ch n =
  if n <= 0 then acc else rep (ch::acc) ch (n-1)

let get_extra extra_cnt descr =
  let pattern = @"<([a-zA-Z0-9]+)>" in
  let m = Regex.Matches(descr, pattern) in
  if m.Count > 0 && m.Count <= extra_cnt then
    Seq.fold (fun (acc:string) (m:Match) ->
      acc + " <" + m.Groups.[1].Value + ">"
    ) "" (Seq.cast m)
  else
    " <OPT>"

let extra_string extra_cnt descr =
  if extra_cnt > 0 then get_extra extra_cnt descr
  else ""

let opt_string_check short long =
  if short = "" && long = "" then specerr "Optstring not given"
  else short, long

let full_optstr (opt:Option) =
  let l = opt.long.Length in
  let s = opt.short.Length in
  if l > 0 && s > 0 then
    opt.short + "," + opt.long + (extra_string opt.extra opt.descr)
  else if l > 0 then
    opt.long + (extra_string opt.extra opt.descr)
  else
    opt.short + (extra_string opt.extra opt.descr)

(** show usage and exit *)
let usage_exit_int prog (spec: spec) maxwidth reqset =
  let space_fill (str: string) =
    let margin = 5 in
    let space = maxwidth - str.Length + margin in
    String.concat "" (rep [] " " space)
  in
  (* printing a simple usage *)
  printf "Usage: %s " prog
  (* required option must be presented in the usage *)
  Set.iter (fun (reqopt: Option) ->
    let short, long = opt_string_check reqopt.short reqopt.long in
    if short.Length = 0 then
      printf "%s%s " long (extra_string reqopt.extra reqopt.descr)
    else
      printf "%s%s " short (extra_string reqopt.extra reqopt.descr)
  ) reqset
  printfn "[opts...]\n"
  (* printing a list of options *)
  List.iter (fun (optarg: Option) ->
    let short, long = opt_string_check optarg.short optarg.long in
    let optstr = full_optstr optarg in
    printfn "%s%s: %s" optstr (space_fill optstr) optarg.descr
  ) spec
  printfn ""; exit 1

let set_update optset opt =
  if Set.exists (fun s -> s = opt) optset then
    specerr (sprintf "Duplicated opt: %s" opt)
  else
    Set.add opt optset

let check_spec (spec: spec) =
  let optset : Set<string> = Set.empty |> Set.add "-h" |> Set.add "--help" in
  let _ =
    List.fold (fun optset (opt: Option) ->
      let short, long = opt_string_check opt.short opt.long in
      let optset =
        if short.Length > 0 then set_update optset short else optset
      in
      if long.Length > 0 then set_update optset long else optset
    ) optset spec
  in
  spec

let get_spec_info spec =
  List.fold (fun (width, (reqset: Set<Option>)) (optarg: Option) ->
    let w =
      let opt = full_optstr optarg in
      let newwidth = opt.Length in
      if newwidth > width then newwidth else width
    in
    let r = if optarg.required then Set.add optarg reqset else reqset in
    w, r
  ) (0, Set.empty) spec (* maxwidth, required opts *)

let rec parse left (spec: spec) (args: args) reqset =
  if args.Length <= 0 then
    if Set.isEmpty reqset then List.rev left
    else rterr "Required arguments not provided"
  else
    let args, left, reqset = spec_loop args reqset left spec in
    parse left spec args reqset
and spec_loop args reqset left = function
    | [] ->
        args.[1..], (args.[0] :: left), reqset
    | optarg::rest ->
        let matching, args, reqset = arg_match optarg args reqset in
        if matching then args, left, reqset
        else spec_loop args reqset left rest
and arg_match optarg args reqset =
  let arg_no_match = (false, args, reqset) in
  let s, l = opt_string_check optarg.short optarg.long in
  let extra = optarg.extra in
  if s = args.[0] || l = args.[0] then
    arg_match_ret optarg args reqset extra
  else if args.[0].Contains("=") then
    let splitted_arg = args.[0].Split([|'='|], 2) in
    if s = splitted_arg.[0] || l = splitted_arg.[0] then
      let args = Array.concat [splitted_arg; args.[1..]] in
      arg_match_ret optarg args reqset extra
    else
      arg_no_match
  else
    arg_no_match
and arg_match_ret optarg args reqset extra =
  if (args.Length - extra) < 1 then
    rterr (sprintf "Extra arg not given for %s" args.[0])
  else
    try optarg.callback args.[1..extra]
    with e -> (eprintfn "Callback failure for %s" args.[0]); rterr e.Message
    (true, args.[(1+extra)..], Set.remove optarg reqset)

(** Parse command line arguments and return a list of unmatched arguments *)
let opt_parse (spec: spec) prog (args: args) =
  let maxwidth, reqset = check_spec spec |> get_spec_info in
  if args.Length <= 0 then
    usage_exit_int prog spec maxwidth reqset
  else if Array.exists (fun a -> a = "-h" || a = "--help") args then
    usage_exit_int prog spec maxwidth reqset
  else
    parse [] spec args reqset

(** Show usage and exit *)
let usage_exit spec prog =
  let maxwidth, reqset = check_spec spec |> get_spec_info in
  usage_exit_int prog spec maxwidth reqset

// vim: set tw=80 sts=2 sw=2:
