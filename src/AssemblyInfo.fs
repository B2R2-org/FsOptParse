namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("OptParse.Runtime")>]
[<assembly: AssemblyProductAttribute("OptParse.Runtime")>]
[<assembly: AssemblyDescriptionAttribute("A tiny, but powerful option parsing library written in a single F# file")>]
[<assembly: AssemblyVersionAttribute("0.2.0")>]
[<assembly: AssemblyFileVersionAttribute("0.2.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.0"
