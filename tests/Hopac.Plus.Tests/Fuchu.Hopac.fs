[<AutoOpen>]
module Fuchu.Hopac

open FsCheck
open Fuchu
open Hopac

let inline private (^) x = x

let testJob name job =
  testCase name ^ fun () -> run job

let testPropertyJobWithConfig config name x2j =
  testPropertyWithConfig config name ^ Prop.ofJob x2j

let testPropertyJob name x2j =
  testProperty name ^ Prop.ofJob ^ x2j
