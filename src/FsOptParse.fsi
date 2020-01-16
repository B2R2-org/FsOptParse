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

module B2R2.FsOptParse

/// Invalid spec definition is found.
exception SpecErr of string

/// User's input does not follow the spec.
exception RuntimeErr of string

/// Command-line arguments.
type Args = string array

/// A command line option.
type 'a Option =
  class
    interface System.IEquatable<'a Option>
    interface System.IComparable
    interface System.IComparable<'a Option>

    new : descr       : string
        * ?callback   : ('a -> Args -> 'a)
        * ?required   : bool
        * ?extra      : int
        * ?help       : bool
        * ?short      : string
        * ?long       : string
        * ?dummy      : bool
        * ?descrColor : System.ConsoleColor
       -> 'a Option
  end

/// The specification of command line options.
type 'a Spec = 'a Option list

/// Parse command line arguments and return a list of unmatched arguments.
val optParse:
     /// Command line specification
     'a Spec
     /// The usage getter is a delayed call-back function that returns a usage
     /// string. The returned string can have two format specifiers: %p for a
     /// program name, and %o for options.
  -> usageGetter: (unit -> string)
     /// Program
  -> prog: string
     /// Command line args.
  -> Args
     /// Data storing the initial option values
  -> 'a
     /// Returns a list of unmatched (non-option) args and the final option data
  -> string list * 'a

/// Print the usage message.
val usagePrint:
     'a Spec
  -> prog: string
  -> usageGetter: (unit -> string)
   /// A callback function called at the end of usagePrint.
  -> termFn: (unit -> 'b)
  -> 'b

// vim: set tw=80 sts=2 sw=2:
