module Hopac.Plus.Tests.Collections.SharedMap

open Hopac
open Hopac.Infixes
open Hopac.Extensions
open Hopac.Plus.Collections
open FsCheck.Xunit
open Xunit
open Swensen.Unquote
open System.Threading.Tasks

let inline private (^) x = x

[<Fact>]
let ``Add-freeze-remove-check is correct`` () =
  run ^ Task.startJob ^ job {
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

[<Property>]
let ``SharedMap.add is equivalent to Map.add`` k v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.add k v m
  let! sm = SharedMap.ofMap m
  do! SharedMap.add k v sm
  let! actual = SharedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.remove is equivalent to Map.remove`` k (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.remove k m
  let! sm = SharedMap.ofMap m
  do! SharedMap.remove k sm
  let! actual = SharedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.remove after SharedMap.add is equivalent to Map.remove after Map.add`` k v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.remove k ^ Map.add k v m
  let! sm = SharedMap.ofMap m
  do! SharedMap.add k v sm
  do! SharedMap.remove k sm
  let! actual = SharedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.filter is equivalent to Map.filter`` k2v2b (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.filter k2v2b m
  let! sm = SharedMap.ofMap m
  do! SharedMap.filter k2v2b sm
  let! actual = SharedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.map is equivalent to Map.map`` k2v2v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.map k2v2v m
  let! sm = SharedMap.ofMap m
  do! SharedMap.map k2v2v sm
  let! actual = SharedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.mutate is equivalent to Map.mutate`` k v2v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.mutate k v2v m
  let! sm = SharedMap.ofMap m
  do! SharedMap.mutate k v2v sm
  let! actual = SharedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.exchange is equivalent to Map.exchange`` k v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.exchange k v m
  let! sm = SharedMap.ofMap m
  let! vO = SharedMap.exchange k v sm
  let! actual = SharedMap.freeze sm

  test <@ expected = (actual, vO) @>
}

[<Property>]
let ``SharedMap.mutateAndExchange is equivalent to Map.mutateAndExchange`` k v2v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.mutateAndExchange k v2v m
  let! sm = SharedMap.ofMap m
  let! vO = SharedMap.mutateAndExchange k v2v sm
  let! actual = SharedMap.freeze sm

  test <@ expected = (actual, vO) @>
}

[<Property>]
let ``SharedMap.freeze has no effect`` (m:Map<uint64,uint64>) = run ^ job {
  let! sm = SharedMap.ofMap m
  let! m1 = SharedMap.freeze sm
  let! m2 = SharedMap.freeze sm

  test <@ m = m1 @>
  test <@ m1 = m2 @>
}

[<Property>]
let ``SharedMap.fork has no effect`` (m:Map<uint64,uint64>) = run ^ job {
  let! sm = SharedMap.ofMap m
  let! _ = SharedMap.fork sm
  let! m1 = SharedMap.freeze sm
  let! _ = SharedMap.fork sm
  let! m2 = SharedMap.freeze sm

  test <@ m = m1 @>
  test <@ m1 = m2 @>
}

[<Property>]
let ``SharedMap.fork creates a child equivalent to the parent`` (m:Map<uint64,uint64>) = run ^ job {
  let! smO = SharedMap.ofMap m
  let! smN = SharedMap.fork smO
  let! expected = SharedMap.freeze smN
  let! actual = SharedMap.freeze smN

  test <@ expected = actual @>
}

[<Property>]
let ``Operations on forked SharedMap don't change parent`` k v (m:Map<uint64,uint64>) = run ^ job {
  let! smO = SharedMap.ofMap m
  let! smN = SharedMap.fork smO
  let! expected = SharedMap.freeze smO
  do! SharedMap.add k v smN
  let! actual = SharedMap.freeze smO

  test <@ expected = actual @>
}

[<Property>]
let ``Operations on forked SharedMap don't change children`` k v (m:Map<uint64,uint64>) = run ^ job {
  let! smO = SharedMap.ofMap m
  let! smN = SharedMap.fork smO
  let! expected = SharedMap.freeze smN
  do! SharedMap.add k v smO
  let! actual = SharedMap.freeze smN

  test <@ expected = actual @>
}

[<Property>]
let ``SharedMap.ofMap is equivalent to the passed in map`` (m:Map<uint64,uint64>) = run ^ job {
  let! sm = SharedMap.ofMap m
  let! actual = SharedMap.freeze sm

  test <@ m = actual @>
}

[<Property>]
let ``SharedMap.ofMap creates equvialent maps for same parameter`` (m:Map<uint64,uint64>) = run ^ job {
  let! sm1 = SharedMap.ofMap m
  let! sm2 = SharedMap.ofMap m
  let! m1 = SharedMap.freeze sm1
  let! m2 = SharedMap.freeze sm2

  test <@ m1 = m2 @>
}

[<Fact>]
let ``SharedMap.create () is equivalent to Map.empty`` () = run ^ job {
  let! (sm : SharedMap<uint64,uint64>) = SharedMap.create ()
  let! actual = SharedMap.freeze sm

  test <@ Map.empty = actual @>
}
