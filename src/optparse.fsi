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

(** Option parsing error *)
exception SpecErr of string
exception RuntimeErr of string

(** Arguments *)
type args = string array

(** A command line option *)
type Option =
  class
    interface System.IEquatable<Option>
    interface System.IComparable
    interface System.IComparable<Option>

    new : descr     : string
        * callback  : (args -> unit)
        * ?required : bool
        * ?extra    : int
        * ?short    : string
        * ?long     : string
       -> Option
  end

(* The specification of command line options *)
type spec = Option list

(** Parse command line arguments and return a list of unmatched arguments *)
val opt_parse : spec -> string -> args -> string list

(** Show usage and exit *)
val usage_exit : spec -> string -> 'a

// vim: set tw=80 sts=2 sw=2:
