module Hopac.Plus.Tests.Collections.SharedMap

open Hopac
open Hopac.Plus.Collections
open NUnit.Framework

[<Test>]
let ``Add-freeze-remove-check is correct`` () =
  run <| job {
    let! smap = SharedMap.create ()
    let k,v = "test", 10
    do! SharedMap.add k v smap
    Assert.AreEqual(Some v, Map.tryFind k (SharedMap.getNow smap))

    let! frozen1 = SharedMap.freeze smap
    Assert.AreEqual(frozen1, SharedMap.getNow smap)

    do! SharedMap.add k 20 smap
    Assert.AreNotEqual(frozen1, SharedMap.getNow smap)

    let! frozen2 = SharedMap.freeze smap
    Assert.AreEqual(Some 20, Map.tryFind k frozen2)

    do! SharedMap.remove k smap
    Assert.IsTrue (Map.isEmpty <| SharedMap.getNow smap)
  }
