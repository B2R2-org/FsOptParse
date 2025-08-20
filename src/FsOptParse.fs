(*
  B2R2.FsOptParse - FSharp-based Command Line Argument Parsing

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

namespace B2R2.FsOptParse

open System
open System.Collections.Generic
open System.Text.RegularExpressions

/// Invalid spec definition is found.
exception SpecError of string

/// User's input does not follow the spec.
exception RuntimeError of string

/// Represents command-line arguments.
type private Args = string[]

/// Represents a command-line option.
type CmdOpt<'Ctx>(descr,
                  ?callback,
                  ?required,
                  ?extra,
                  ?help,
                  ?short,
                  ?long,
                  ?dummy,
                  ?descrColor) =

  let cbDefault ctx _args = ctx

  let sanitizeExtra (n: int) =
    if n < 0 then raise <| SpecError "Extra field should be positive"
    else n

  let rec removeDashes (s: string) =
    if s.[0] = '-' then removeDashes s.[1..] else s

  let sanitizeShort (opt: string) =
    if opt.Length = 0 then opt
    else
      let opt = removeDashes opt
      if opt.Length = 1 then "-" + opt
      else raise <| SpecError $"Invalid short option {opt} is given"

  let sanitizeLong (opt: string) =
    if opt.Length = 0 then opt
    else
      let opt = "--" + (removeDashes opt)
      if opt.Length > 2 then opt
      else raise <| SpecError $"Invalid long option {opt} is given"

  /// Description of the option.
  member _.Descr with get(): string = descr

  /// Color of the description when printing the usage.
  member _.DescrColor with get(): ConsoleColor option = descrColor

  /// Callback function to be called when the option is matched.
  member _.Callback with get(): 'Ctx -> Args -> 'Ctx =
    defaultArg callback cbDefault

  /// Whether the option is required.
  member _.Required with get(): bool = defaultArg required false

  /// Number of extra arguments that the option can take.
  member _.Extra with get(): int = defaultArg extra 0 |> sanitizeExtra

  /// Whether the option should print the usage message.
  member _.Help with get(): bool = defaultArg help false

  /// Short option string.
  member _.Short with get(): string = defaultArg short "" |> sanitizeShort

  /// Long option string.
  member _.Long with get(): string = defaultArg long "" |> sanitizeLong

  /// Whether the option is a dummy option (not a real option) for usage
  /// printing.
  member _.Dummy with get(): bool = defaultArg dummy false

  interface IComparable<CmdOpt<'Ctx>> with
    member this.CompareTo obj =
      compare (this.Short, this.Long) (obj.Short, obj.Long)

  interface IComparable with
    member this.CompareTo obj =
      match obj with
      | :? CmdOpt<'Ctx> as obj -> (this :> IComparable<_>).CompareTo obj
      | _ -> raise <| SpecError "Not an option"

  interface IEquatable<CmdOpt<'Ctx>> with
    member this.Equals obj =
      this.Short = obj.Short && this.Long = obj.Long

  override this.Equals obj =
    match obj with
    | :? CmdOpt<'Ctx> as obj -> (this :> IEquatable<_>).Equals obj
    | _ -> raise <| SpecError "Not an option"

  override this.GetHashCode() =
    hash (this.Short, this.Long)

[<AutoOpen>]
module private CmdOpt =

  let [<Literal>] ExtraArgPattern = @"<([a-zA-Z0-9]+)>"

  let extractExtraArgStringFromDesc extraCnt descr =
    let ms = Regex.Matches(descr, ExtraArgPattern)
    if ms.Count > 0 && ms.Count <= extraCnt then
      let sb = Text.StringBuilder()
      for m in ms do sb.Append(" <" + m.Groups[1].Value + ">") |> ignore
      sb.ToString()
    else (* cannot extract information from the descr, so just show this. *)
      " <OPT>"

  let getExtraArgString extraCnt descr =
    if extraCnt > 0 then extractExtraArgStringFromDesc extraCnt descr
    else ""

  let getOptSummary reqSet =
    let sb = Text.StringBuilder()
    for (reqopt: CmdOpt<_>) in reqSet do
      let short, long = reqopt.Short, reqopt.Long
      if short.Length = 0 then
        sb.Append $"{long}{getExtraArgString reqopt.Extra reqopt.Descr} "
        |> ignore
      else
        sb.Append $"{short}{getExtraArgString reqopt.Extra reqopt.Descr} "
        |> ignore
    sb.Append("[opts...]").ToString().Trim()

  /// Print a single-line (simple) usage.
  let printSimpleUsage prog usageFormatGetter reqSet =
    let usgForm = usageFormatGetter ()
    let usgForm = if String.length usgForm = 0 then "Usage: %p %o" else usgForm
    let usgForm = usgForm.Replace("%p", prog)
    let usgForm = usgForm.Replace("%o", getOptSummary reqSet)
    Console.Write usgForm
    Console.WriteLine Environment.NewLine

  let setColor = function
    | None -> ()
    | Some color -> Console.ForegroundColor <- color

  let clearColor = function
    | None -> ()
    | Some _ -> Console.ResetColor()

  let getOptUsageString (opt: CmdOpt<_>) =
    let long = opt.Long
    let short = opt.Short
    if long.Length > 0 && short.Length > 0 then
      $"{short}, {long}{getExtraArgString opt.Extra opt.Descr}"
    elif long.Length > 0 then
      long + (getExtraArgString opt.Extra opt.Descr)
    else
      short + (getExtraArgString opt.Extra opt.Descr)

  let [<Literal>] Margin = 5

  let rec rep acc ch n =
    if n <= 0 then acc
    else rep (ch :: acc) ch (n - 1)

  let fillSpace maxWidth (str: string) =
    let space = maxWidth - str.Length + Margin
    String.concat "" (rep [] " " space)

  let printFullUsage spec maxWidth termFn =
    for (opt: CmdOpt<_>) in spec do
      setColor opt.DescrColor
      if opt.Dummy then
        Console.WriteLine opt.Descr
      else
        let optstr = getOptUsageString opt
        $"{optstr}{fillSpace maxWidth optstr}: {opt.Descr}"
        |> Console.WriteLine
      clearColor opt.DescrColor
    Console.WriteLine()
    termFn ()

  /// Show usage and exit.
  let showUsage prog usageFormatGetter spec maxWidth reqSet termFn =
    printSimpleUsage prog usageFormatGetter reqSet
    printFullUsage spec maxWidth termFn

  let updateOptSet (opt: string) (optSet: HashSet<string>) =
    if opt.Length > 0 then
      if optSet.Contains opt then raise <| SpecError $"Duplicate opt: {opt}"
      else optSet.Add opt |> ignore
    else
      ()

  let inline assertOptStringExistence short long =
    if String.IsNullOrEmpty short && String.IsNullOrEmpty long then
      raise <| SpecError "Optstring not given"
    else
      ()

  let checkSpec spec =
    let optSet = HashSet<string>()
    for opt: CmdOpt<_> in spec do
      if opt.Dummy then
        ()
      else
        let short, long = opt.Short, opt.Long
        assertOptStringExistence short long
        updateOptSet short optSet
        updateOptSet long optSet

  let computeMaxWidthAndRequiredOptSet spec =
    List.fold (fun (maxWidth, reqSet) opt ->
      let maxWidth =
        let optLen = getOptUsageString opt |> String.length
        if optLen > maxWidth then optLen else maxWidth
      let reqSet =
        if opt.Required && not opt.Dummy then Set.add opt reqSet
        else reqSet
      maxWidth, reqSet
    ) (0, Set.empty) spec

  let rec parse left spec args reqSet usage state =
    if Array.isEmpty args then
      if Set.isEmpty reqSet then List.rev left, state
      else raise <| RuntimeError "Required arguments not provided"
    else
      let args, left, reqSet, state = specLoop args reqSet left usage state spec
      parse left spec args reqSet usage state

  and specLoop args reqSet left usage state = function
    | [] ->
      args[1..], (args[0] :: left), reqSet, state
    | (opt: CmdOpt<_>) :: rest ->
      let m, args, reqSet, state =
        if opt.Dummy then false, args, reqSet, state
        else argMatch opt args reqSet usage state
      if m then args, left, reqSet, state
      else specLoop args reqSet left usage state rest

  and argMatch (opt: CmdOpt<_>) args reqSet usage state =
    let argNoMatch = (false, args, reqSet, state)
    let short, long = opt.Short, opt.Long
    let extra = opt.Extra
    if short = args[0] || long = args[0] then
      argMatchRet opt args reqSet extra usage state
    elif short.Length > 0 && args[0].StartsWith(short) && extra > 0 then
      (* Short options can have extra argument without having a space char. *)
      let splittedArg = [| args[0][0..1]; args[0][2..] |]
      let args = Array.concat [ splittedArg; args[1..] ]
      argMatchRet opt args reqSet extra usage state
    elif args[0].Contains("=") then
      let splittedArg = args[0].Split([| '=' |], 2)
      if short = splittedArg.[0] || long = splittedArg.[0] then
        let args = Array.concat [ splittedArg; args[1..] ]
        argMatchRet opt args reqSet extra usage state
      else
        argNoMatch
    else
      argNoMatch

  and argMatchRet opt args reqSet extra usage state =
    if (args.Length - extra) < 1 then
      raise <| RuntimeError $"Extra arg not given for {args[0]}"
    elif opt.Help then
      usage ()
      exit 0
    else
      let state' =
        try
          opt.Callback state args[1..extra]
        with e ->
          Console.Error.WriteLine $"Callback failure for {args[0]}"
          raise <| RuntimeError e.Message
      (true, args[(1 + extra)..], Set.remove opt reqSet, state')

/// Represents the command-line option parser.
type OptParse =

  /// <summary>
  /// Parses command-line arguments and returns a list of unmatched arguments.
  /// </summary>
  static member Parse(spec, usageFormatGetter, prog, args: Args, state) =
    checkSpec spec
    let maxwidth, reqSet = computeMaxWidthAndRequiredOptSet spec
    let usage () = showUsage prog usageFormatGetter spec maxwidth reqSet ignore
    if args.Length < 0 then usage (); raise <| RuntimeError "No argument given"
    else parse [] spec args reqSet usage state

  /// <summary>
  /// Parses command-line arguments and returns a list of unmatched arguments.
  /// </summary>
  static member Parse(spec, prog, args, state) =
    OptParse.Parse(spec, (fun () -> ""), prog, args, state)

  /// <summary>
  /// Prints out the usage.
  /// </summary>
  static member PrintUsage(spec, prog, usageFormatGetter, termFn) =
    checkSpec spec
    let maxwidth, reqSet = computeMaxWidthAndRequiredOptSet spec
    showUsage prog usageFormatGetter spec maxwidth reqSet termFn

  /// <summary>
  /// Prints out the usage.
  /// </summary>
  static member PrintUsage(spec, prog, usageFormatGetter) =
    OptParse.PrintUsage(spec, prog, usageFormatGetter, (fun () -> exit 1))

  /// <summary>
  /// Prints out the usage.
  /// </summary>
  static member PrintUsage(spec, prog) =
    OptParse.PrintUsage(spec, prog, (fun () -> ""), (fun () -> exit 1))
