(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Hopac.Plus"
#r "Hopac.Core.dll"
#r "Hopac.dll"
#r "Hopac.Plus.dll"

(**
Hopac.Plus
======================

A collection of utilities that encapsulate common concurrency patterns and constructs for use with the Hopac concurrency library.

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Hopac.Plus library can be <a href="https://nuget.org/packages/Hopac.Plus">installed from NuGet</a>:
      <pre>PM> Install-Package Hopac.Plus</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

Extensions
---------

Example using the functions in the `Hopac.Plus.Extensions` namespace:

*)
(*** define-output: extensions ***)
open Hopac
open Hopac.Plus.Extensions

OptionJob.create 42L
|> OptionJob.bindOpt (fun i -> if i % 2L = 0L then Some i else None)
|> Job.map (function Some x -> Choice1Of2 x | None -> Choice2Of2 "Not divisible by 2")
|> ChoiceJob.map (fun i -> i / 6L)
|> ChoiceJob.fold Some (fun _ -> None)
|> OptionJob.orDefaultJob Clock.getSecondsSinceEpoch
|> run

(**
Result:
*)
(*** include-it: extensions ***)
(**

SharedMap
-------

Example using the `SharedMap`:

*)
open Hopac
open Hopac.Plus.Collections

let smap = run <| SharedMap.create ()

let printMap smap =
  SharedMap.freeze smap
  |> Job.map (printfn "%A")

let example smap = job {
  let key = "Example"
  do! SharedMap.add key 10 smap
  do! printMap smap

  do! SharedMap.add key 20 smap
  do! printMap smap

  do! SharedMap.mutate key ((*) 5) smap
  do! printMap smap

  do! SharedMap.remove key smap
  do! printMap smap
}

run <| example smap

(**

Samples & documentation
-----------------------

The library comes with comprehensible documentation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.

Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork
the project and submit pull requests. If you're adding a new public API, please also
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Apache 2.0 license, which allows modification and
redistribution for both commercial and non-commercial purposes. For more information see the
[License file][license] in the GitHub repository.

  [content]: https://github.com/fsprojects/Hopac.Plus/tree/master/docs/content
  [gh]: https://github.com/fsprojects/Hopac.Plus
  [issues]: https://github.com/fsprojects/Hopac.Plus/issues
  [readme]: https://github.com/fsprojects/Hopac.Plus/blob/master/README.md
  [license]: https://github.com/fsprojects/Hopac.Plus/blob/master/LICENSE.txt
*)
