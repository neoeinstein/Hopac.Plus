[<AutoOpen>]
module FsCheck.Hopac

open FsCheck
open Hopac

let inline private (^) x = x

module Prop =
  let ofJob x2j = Prop.ofTestable ^ fun x -> x2j >> run
