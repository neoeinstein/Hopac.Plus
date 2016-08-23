module Hopac.Plus.Tests.Collections.MapTests

open Hopac
open Hopac.Infixes
open Hopac.Extensions
open Hopac.Plus.Collections
open FsCheck
open Fuchu
open Swensen.Unquote

type MapT = Map<uint64,uint64>

let inline private (^) x = x

let inline tests create ofMap add remove filter map mutate exchange mutateAndExchange freeze fork =
  let testEquivalence1 k2m2m k2x2xJ k m = job {
      let expected = k2m2m k m
      let! x = asJob ^ ofMap m
      do! asJob ^ k2x2xJ k x
      let! actual = asJob ^ freeze x

      test <@ expected = actual @>
    }
  let testEquivalence2 k2v2m2m k2v2x2xJ k v m = job {
      let expected = k2v2m2m k v m
      let! x = asJob ^ ofMap m
      do! asJob ^ k2v2x2xJ k v x
      let! actual = asJob ^ freeze x

      test <@ expected = actual @>
    }
  let testEquivalenceWithOut2 k2v2m2m k2v2x2xJ k v m = job {
      let expected = k2v2m2m k v m
      let! x = asJob ^ ofMap m
      let! vO = asJob ^ k2v2x2xJ k v x
      let! actual = asJob ^ freeze x

      test <@ expected = (actual, vO) @>
    }
  testList "Common map-like tests" [
    testPropertyJob "add is equivalent to Map.add" ^ fun (k,v,m:MapT) ->
      testEquivalence2 Map.add add k v m
    testPropertyJob "remove is equivalent to Map.remove" ^ fun (k,m:MapT) ->
      testEquivalence1 Map.remove remove k m
    testPropertyJob "filter is equivalent to Map.filter" ^ fun (Fun k2v2b,m:MapT) ->
      testEquivalence1 Map.filter filter k2v2b m
    testPropertyJob "map is equivalent to Map.map" ^ fun (Fun k2v2v,m:MapT) ->
      testEquivalence1 Map.map map k2v2v m
    testPropertyJob "mutate is equivalent to Map.mutate" ^ fun (k,Fun v2v,m:MapT) ->
      testEquivalence2 Map.mutate mutate k v2v m
    testPropertyJob "exchange is equivalent to Map.exchange" ^ fun (k,v,m:MapT) ->
      testEquivalenceWithOut2 Map.exchange exchange k v m
    testPropertyJob "mutateAndExchange is equivalent to Map.mutateAndExchange" ^ fun (k,Fun v2v,m:MapT) ->
      testEquivalenceWithOut2 Map.mutateAndExchange mutateAndExchange k v2v m
    testPropertyJob "add then remove is equivalent to Map.add then Map.remove" ^ fun (k,v,m:MapT) -> job {
      let expected = Map.remove k ^ Map.add k v m
      let! x = asJob ^ ofMap m
      do! asJob ^ add k v x
      do! asJob ^ remove k x
      let! actual = asJob ^ freeze x

      test <@ expected = actual @>
    }
    testPropertyJob "freeze has no effect" ^ fun (m:MapT) -> job {
      let! x = asJob ^ ofMap m
      let! m1 = asJob ^ freeze x
      let! m2 = asJob ^ freeze x

      test <@ m = m1 @>
      test <@ m1 = m2 @>
    }
    testPropertyJob "fork has no effect" ^ fun (m:MapT) -> job {
      let! x = asJob ^ ofMap m
      let! _ = asJob ^ fork x
      let! m1 = asJob ^ freeze x
      let! _ = asJob ^ fork x
      let! m2 = asJob ^ freeze x

      test <@ m = m1 @>
      test <@ m1 = m2 @>
    }
    testPropertyJob "forked child is equivalent to parent" ^ fun (m:MapT) -> job {
      let! xP = asJob ^ ofMap m
      let! xC = asJob ^ fork xP
      let! expected = asJob ^ freeze xP
      let! actual = asJob ^ freeze xC

      test <@ expected = actual @>
    }
    testPropertyJob "add on forked child doesn't affect parent" ^ fun (k,v,m:MapT) -> job {
      let! xP = asJob ^ ofMap m
      let! xC = asJob ^ fork xP
      let! expected = asJob ^ freeze xP
      do! asJob ^ add k v xC
      let! actual = asJob ^ freeze xP

      test <@ expected = actual @>
    }
    testPropertyJob "add on forked parent doesn't affect child" ^ fun (k,v,m:MapT) -> job {
      let! xP = asJob ^ ofMap m
      let! xC = asJob ^ fork xP
      let! expected = asJob ^ freeze xC
      do! asJob ^ add k v xP
      let! actual = asJob ^ freeze xC

      test <@ expected = actual @>
    }
    testPropertyJob "ofMap then freeze is equivalent to source map" ^ fun (m:MapT) -> job {
      let! x = asJob ^ ofMap m
      let! actual = asJob ^ freeze x

      test <@ m = actual @>
    }
    testPropertyJob "ofMap results are equivalent" ^ fun (m:MapT) -> job {
      let! x1 = asJob ^ ofMap m
      let! x2 = asJob ^ ofMap m
      let! m1 = asJob ^ freeze x1
      let! m2 = asJob ^ freeze x2

      test <@ m1 = m2 @>
    }
    testJob "create is equivalent to Map.empty" ^ job {
      let! x = asJob ^ create ()
      let! actual = asJob ^ freeze x

      test <@ Map.empty = actual @>
    }
  ]
