module Hopac.Plus.Tests.Collections.SharedMap

open Hopac
open Hopac.Infixes
open Hopac.Extensions
open Hopac.Plus.Collections
open Fuchu
open Swensen.Unquote

let inline private (^) x = x

[<Tests>]
let tests =
  testList "SharedMap" [
    testJob "Add-freeze-remove check is correct" ^ job {
      let! smap = SharedMap.create ()
      let k,v = "test", 10
      do! SharedMap.add k v smap
      let! step1 = SharedMap.freeze smap

      do! SharedMap.add k 20 smap
      let! step2 = SharedMap.freeze smap

      do! SharedMap.remove k smap
      let! step3 = SharedMap.freeze smap

      test <@ Some v = Map.tryFind k step1 @>
      test <@ step1 <> step2 @>
      test <@ Some 20 = Map.tryFind k step2 @>
      test <@ Map.isEmpty step3 @>
    }
    MapTests.tests
      SharedMap.create
      SharedMap.ofMap
      SharedMap.add
      SharedMap.remove
      SharedMap.filter
      SharedMap.map
      SharedMap.mutate
      SharedMap.exchange
      SharedMap.mutateAndExchange
      SharedMap.freeze
      SharedMap.fork
]

