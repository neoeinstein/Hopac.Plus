module Hopac.Plus.Tests.Collections.SerializedMap

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

[<Property>]
let ``SerializedMap.add is equivalent to Map.add`` k v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.add k v m
  let sm = SerializedMap.ofMap m
  do! SerializedMap.add k v sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.remove is equivalent to Map.remove`` k (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.remove k m
  let sm = SerializedMap.ofMap m
  do! SerializedMap.remove k sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.remove after SerializedMap.add is equivalent to Map.remove after Map.add`` k v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.remove k ^ Map.add k v m
  let sm = SerializedMap.ofMap m
  do! SerializedMap.add k v sm
  do! SerializedMap.remove k sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.filter is equivalent to Map.filter`` k2v2b (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.filter k2v2b m
  let sm = SerializedMap.ofMap m
  do! SerializedMap.filter k2v2b sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.map is equivalent to Map.map`` k2v2v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.map k2v2v m
  let sm = SerializedMap.ofMap m
  do! SerializedMap.map k2v2v sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.mutate is equivalent to Map.mutate`` k v2v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.mutate k v2v m
  let sm = SerializedMap.ofMap m
  do! SerializedMap.mutate k v2v sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.exchange is equivalent to Map.exchange`` k v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.exchange k v m
  let sm = SerializedMap.ofMap m
  let! vO = SerializedMap.exchange k v sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = (actual, vO) @>
}

[<Property>]
let ``SerializedMap.mutateAndExchange is equivalent to Map.mutateAndExchange`` k v2v (m:Map<uint64,uint64>) = run ^ job {
  let expected = Map.mutateAndExchange k v2v m
  let sm = SerializedMap.ofMap m
  let! vO = SerializedMap.mutateAndExchange k v2v sm
  let! actual = SerializedMap.freeze sm

  test <@ expected = (actual, vO) @>
}

[<Property>]
let ``SerializedMap.freeze has no effect`` (m:Map<uint64,uint64>) = run ^ job {
  let sm = SerializedMap.ofMap m
  let! m1 = SerializedMap.freeze sm
  let! m2 = SerializedMap.freeze sm

  test <@ m = m1 @>
  test <@ m1 = m2 @>
}

[<Property>]
let ``SerializedMap.fork has no effect`` (m:Map<uint64,uint64>) = run ^ job {
  let sm = SerializedMap.ofMap m
  let! _ = SerializedMap.fork sm
  let! m1 = SerializedMap.freeze sm
  let! _ = SerializedMap.fork sm
  let! m2 = SerializedMap.freeze sm

  test <@ m = m1 @>
  test <@ m1 = m2 @>
}

[<Property>]
let ``SerializedMap.fork creates a child equivalent to the parent`` (m:Map<uint64,uint64>) = run ^ job {
  let smO = SerializedMap.ofMap m
  let! smN = SerializedMap.fork smO
  let! expected = SerializedMap.freeze smN
  let! actual = SerializedMap.freeze smN

  test <@ expected = actual @>
}

[<Property>]
let ``Operations on forked SerializedMap don't change parent`` k v (m:Map<uint64,uint64>) = run ^ job {
  let smO = SerializedMap.ofMap m
  let! smN = SerializedMap.fork smO
  let! expected = SerializedMap.freeze smO
  do! SerializedMap.add k v smN
  let! actual = SerializedMap.freeze smO

  test <@ expected = actual @>
}

[<Property>]
let ``Operations on forked SerializedMap don't change children`` k v (m:Map<uint64,uint64>) = run ^ job {
  let smO = SerializedMap.ofMap m
  let! smN = SerializedMap.fork smO
  let! expected = SerializedMap.freeze smN
  do! SerializedMap.add k v smO
  let! actual = SerializedMap.freeze smN

  test <@ expected = actual @>
}

[<Property>]
let ``SerializedMap.ofMap is equivalent to the passed in map`` (m:Map<uint64,uint64>) = run ^ job {
  let sm = SerializedMap.ofMap m
  let! actual = SerializedMap.freeze sm

  test <@ m = actual @>
}

[<Property>]
let ``SerializedMap.ofMap creates equvialent maps for same parameter`` (m:Map<uint64,uint64>) = run ^ job {
  let sm1 = SerializedMap.ofMap m
  let sm2 = SerializedMap.ofMap m
  let! m1 = SerializedMap.freeze sm1
  let! m2 = SerializedMap.freeze sm2

  test <@ m1 = m2 @>
}

[<Fact>]
let ``SerializedMap.create () is equivalent to Map.empty`` () = run ^ job {
  let sm : SerializedMap<uint64,uint64> = SerializedMap.create ()
  let! actual = SerializedMap.freeze sm

  test <@ Map.empty = actual @>
}
