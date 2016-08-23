module Hopac.Plus.Tests.Collections.SerializedMap

open Hopac
open Hopac.Infixes
open Hopac.Extensions
open Hopac.Plus.Collections
open Fuchu
open Swensen.Unquote

let inline private (^) x = x

[<Tests>]
let tests =
  testList "SerializedMap" [
    testJob "Add-freeze-remove check is correct" ^ job {
      let smap = SerializedMap.create ()
      let k,v = "test", 10
      do! SerializedMap.add k v smap
      let! step1 = SerializedMap.freeze smap

      do! SerializedMap.add k 20 smap
      let! step2 = SerializedMap.freeze smap

      do! SerializedMap.remove k smap
      let! step3 = SerializedMap.freeze smap

      test <@ Some v = Map.tryFind k step1 @>
      test <@ step1 <> step2 @>
      test <@ Some 20 = Map.tryFind k step2 @>
      test <@ Map.isEmpty step3 @>
    }
    MapTests.tests
      (SerializedMap.create >> Job.result)
      (SerializedMap.ofMap >> Job.result)
      SerializedMap.add
      SerializedMap.remove
      SerializedMap.filter
      SerializedMap.map
      SerializedMap.mutate
      SerializedMap.exchange
      SerializedMap.mutateAndExchange
      SerializedMap.freeze
      SerializedMap.fork
]

