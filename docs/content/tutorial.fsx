(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Hopac.Extra"

(**
Example Usage
=============

*)
#r "Hopac.Core.dll"
#r "Hopac.dll"
#r "Hopac.Extra.dll"
open Hopac
open Hopac.Extra.Collections

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
*)
