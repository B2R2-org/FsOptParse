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

let sanitizeExtra (n: int) =
  if n < 0 then specerr "Extra field should be positive"
  else n

let rec removeDashes (s: string) =
  if s.[0] = '-' then removeDashes s.[1..] else s

let sanitizeShort (opt: string) =
  if opt.Length = 0 then opt
  else
    let opt = removeDashes opt
    if opt.Length = 1 then "-" + opt
    else specerr (sprintf "Invalid short option %s is given" opt)

let sanitizeLong (opt: string) =
  if opt.Length = 0 then opt
  else
    let opt = "--" + (removeDashes opt) in
    if opt.Length > 2 then opt
    else specerr (sprintf "Invalid long option %s is given" opt)

(** command line option *)
type 'a Option (descr, ?callback, ?required, ?extra, ?short, ?long, ?dummy) =
  let defaultCB opts (_args:args) = opts in

  member this.descr : string = descr
  member this.callback : ('a -> args -> 'a) = defaultArg callback defaultCB
  member this.required : bool = defaultArg required false
  member this.extra : int = defaultArg extra 0 |> sanitizeExtra
  member this.short : string = defaultArg short "" |> sanitizeShort
  member this.long : string = defaultArg long "" |> sanitizeLong
  member this.dummy : bool = defaultArg dummy false

  interface IComparable<'a Option> with
    member this.CompareTo obj =
      compare (this.short, this.long) (obj.short, obj.long)

  interface IComparable with
    member this.CompareTo obj =
      match obj with
        | :? ('a Option) as obj -> (this :> IComparable<_>).CompareTo obj
        | _ -> specerr "Not an option"

  interface IEquatable<'a Option> with
    member this.Equals obj =
      this.short = obj.short && this.long = obj.long

  override this.Equals obj =
    match obj with
      | :? ('a Option) as obj -> (this :> IEquatable<_>).Equals obj
      | _ -> specerr "Not an option"

  override this.GetHashCode () =
    hash (this.short, this.long)

(* The specification of command line options *)
type 'a spec = 'a Option list

let rec rep acc ch n =
  if n <= 0 then acc else rep (ch::acc) ch (n-1)

let getExtra extraCnt descr =
  let pattern = @"<([a-zA-Z0-9]+)>"
  let m = Regex.Matches(descr, pattern)
  if m.Count > 0 && m.Count <= extraCnt then
    Seq.fold (fun (acc:string) (m:Match) ->
      acc + " <" + m.Groups.[1].Value + ">"
    ) "" (Seq.cast m)
  else
    " <OPT>"

let extraString extraCnt descr =
  if extraCnt > 0 then getExtra extraCnt descr
  else ""

let optStringCheck short long =
  if short = "" && long = "" then specerr "Optstring not given"
  else short, long

let fullOptStr (opt:'a Option) =
  let l = opt.long.Length in
  let s = opt.short.Length in
  if l > 0 && s > 0 then
    opt.short + "," + opt.long + (extraString opt.extra opt.descr)
  else if l > 0 then
    opt.long + (extraString opt.extra opt.descr)
  else
    opt.short + (extraString opt.extra opt.descr)

(** show usage and exit *)
let usageExitInt prog (spec: 'a spec) maxwidth reqset =
  let spaceFill (str: string) =
    let margin = 5 in
    let space = maxwidth - str.Length + margin in
    String.concat "" (rep [] " " space)
  in
  (* printing a simple usage *)
  printf "Usage: %s " prog
  (* required option must be presented in the usage *)
  Set.iter (fun (reqopt: 'a Option) ->
    let short, long = optStringCheck reqopt.short reqopt.long in
    if short.Length = 0 then
      printf "%s%s " long (extraString reqopt.extra reqopt.descr)
    else
      printf "%s%s " short (extraString reqopt.extra reqopt.descr)
  ) reqset
  printfn "[opts...]\n"
  (* printing a list of options *)
  List.iter (fun (optarg: 'a Option) ->
    if optarg.dummy then
      printfn "%s" optarg.descr
    else
      let _short, _long = optStringCheck optarg.short optarg.long in
      let optstr = fullOptStr optarg in
      printfn "%s%s: %s" optstr (spaceFill optstr) optarg.descr
  ) spec
  printfn ""; exit 1

let setUpdate (opt: string) optset =
  if opt.Length > 0 then
    if Set.exists (fun s -> s = opt) optset then
      specerr (sprintf "Duplicated opt: %s" opt)
    else
      Set.add opt optset
  else
    optset

let checkSpec (spec: 'a spec) =
  let optset : Set<string> = Set.empty |> Set.add "-h" |> Set.add "--help" in
  let _ =
    List.fold (fun optset (opt: 'a Option) ->
      if opt.dummy then
        optset
      else
        let short, long = optStringCheck opt.short opt.long in
        setUpdate short optset |> setUpdate long
    ) optset spec
  in
  spec

let getSpecInfo (spec: 'a spec) =
  List.fold (fun (width, (reqset: Set<'a Option>)) (optarg: 'a Option) ->
    let w =
      let opt = fullOptStr optarg in
      let newwidth = opt.Length in
      if newwidth > width then newwidth else width
    in
    let r =
      if optarg.required && not optarg.dummy then Set.add optarg reqset
      else reqset
    in
    w, r
  ) (0, Set.empty) spec (* maxwidth, required opts *)

let rec parse left (spec: 'a spec) (args: args) reqset state =
  if args.Length <= 0 then
    if Set.isEmpty reqset then List.rev left, state
    else rterr "Required arguments not provided"
  else
    let args, left, reqset, state = specLoop args reqset left state spec in
    parse left spec args reqset state
and specLoop args reqset left state = function
  | [] ->
      args.[1..], (args.[0] :: left), reqset, state
  | (optarg: 'a Option)::rest ->
      let m, args, reqset, state =
        if optarg.dummy then false, args, reqset, state
        else argMatch optarg args reqset state
      in
      if m then args, left, reqset, state
      else specLoop args reqset left state rest
and argMatch (optarg: 'a Option) args reqset state =
  let argNoMatch = (false, args, reqset, state) in
  let s, l = optStringCheck optarg.short optarg.long in
  let extra = optarg.extra in
  if s = args.[0] || l = args.[0] then
    argMatchRet optarg args reqset extra state
  else if args.[0].Contains("=") then
    let splittedArg = args.[0].Split([|'='|], 2) in
    if s = splittedArg.[0] || l = splittedArg.[0] then
      let args = Array.concat [splittedArg; args.[1..]] in
      argMatchRet optarg args reqset extra state
    else
      argNoMatch
  else
    argNoMatch
and argMatchRet (optarg: 'a Option) args reqset extra state =
  if (args.Length - extra) < 1 then
    rterr (sprintf "Extra arg not given for %s" args.[0])
  else
    let state': 'a =
      try optarg.callback state args.[1..extra]
      with e -> (eprintfn "Callback failure for %s" args.[0]); rterr e.Message
    in
    (true, args.[(1+extra)..], Set.remove optarg reqset, state')

(** Parse command line arguments and return a list of unmatched arguments *)
let optParse (spec: 'a spec) prog (args: args) (state: 'a) =
  let maxwidth, reqset = checkSpec spec |> getSpecInfo in
  if args.Length < 0 then
    usageExitInt prog spec maxwidth reqset
  else if Array.exists (fun a -> a = "-h" || a = "--help") args then
    usageExitInt prog spec maxwidth reqset
  else
    parse [] spec args reqset state

(** Show usage and exit *)
let usageExit spec prog =
  let maxwidth, reqset = checkSpec spec |> getSpecInfo in
  usageExitInt prog spec maxwidth reqset

// vim: set tw=80 sts=2 sw=2:
