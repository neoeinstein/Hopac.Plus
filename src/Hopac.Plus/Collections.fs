namespace Hopac.Plus.Collections

open Hopac
open Hopac.Infixes

[<AutoOpen>]
module InternalOperators =
  let inline (^) x = x

module Map =
  let mutate k v2v m =
    match Map.tryFind k m with
    | Some v -> Map.add k (v2v v) m
    | None   -> m

  let exchange k v m =
    Map.add k v m, Map.tryFind k m

  let mutateAndExchange k v2v m =
    let vO = Map.tryFind k m
    let mN =
      match vO with
      | Some v -> Map.add k (v2v v) m
      | None   -> m
    mN, vO

type SerializedMap<'k, 'v when 'k : comparison> =
  SM of MVar<Map<'k,'v>>

module SerializedMap =
  let ofMap m = SM ^ MVar m
  let create () = ofMap Map.empty

  let add    k v (SM mM) = mM |> MVar.mutateFun ^ Map.add    k v
  let remove k   (SM mM) = mM |> MVar.mutateFun ^ Map.remove k

  let filter k2v2b (SM mM) = mM |> MVar.tryMutateFun ^ Map.filter k2v2b
  let map    k2v2v (SM mM) = mM |> MVar.tryMutateFun ^ Map.map    k2v2v
  let mutate k v2v (SM mM) = mM |> MVar.tryMutateFun ^ Map.mutate k v2v

  let exchange k v (SM mM) = mM |> MVar.modifyFun ^ Map.exchange k v
  let mutateAndExchange k v2v (SM mM) =
    mM |> MVar.tryModifyFun ^ Map.mutateAndExchange k v2v

  let freeze (SM mM) = MVar.read mM
  let fork sm = freeze sm ^-> ofMap

type SharedMap<'k,'v when 'k : comparison> =
  { addCh : Ch<'k * 'v * IVar<unit>>
    mapCh : Ch<('k -> 'v -> 'v) * IVar<unit>>
    filterCh : Ch<('k -> 'v -> bool) * IVar<unit>>
    removeCh : Ch<'k * IVar<unit>>
    mutateCh : Ch<'k * ('v -> 'v) * IVar<unit>>
    mutateOutCh : Ch<'k * ('v -> 'v) * IVar<'v option>>
    exchangeCh : Ch<'k * 'v * IVar<'v option>>
    map : Map<'k,'v> ref }

module SharedMap =
  let ofMap init =
    let o =
      { addCh = Ch (); mapCh = Ch (); filterCh = Ch (); removeCh = Ch ()
        mutateCh = Ch (); mutateOutCh = Ch (); exchangeCh = Ch (); map = ref init }
    Job.foreverServer
      ( Alt.choose
          [ o.addCh ^=> fun (k,v,ivar) ->
              o.map := Map.add k v !o.map
              ivar *<= ()
            o.removeCh ^=> fun (k,ivar) ->
              o.map := Map.remove k !o.map
              ivar *<= ()
            o.filterCh ^=> fun (k2v2b,ivar) ->
              o.map := Map.filter k2v2b !o.map
              ivar *<= ()
            o.mapCh ^=> fun (k2v2v,ivar) ->
              o.map := Map.map k2v2v !o.map
              ivar *<= ()
            o.mutateCh ^=> fun (k,v2v,ivar) ->
              match Map.tryFind k !o.map with
              | Some v -> o.map := Map.add k (v2v v) !o.map
              | None -> ()
              ivar *<= ()
            o.mutateOutCh ^=> fun (k,v2v,ivar) ->
              let m = !o.map
              let oldVOpt = Map.tryFind k m
              match oldVOpt with
              | Some oldV -> o.map := Map.add k (v2v oldV) m
              | None -> ()
              ivar *<= oldVOpt
            o.exchangeCh ^=> fun (k,v,ivar) ->
              let m = !o.map
              let oldVOpt = Map.tryFind k m
              o.map := Map.add k v m
              ivar *<= oldVOpt
          ]
      )
    >>-. o

  let create () = ofMap Map.empty

  let add k v sm =
    sm.addCh    *<-=>- fun ivar -> (k,v,ivar)

  let remove k sm =
    sm.removeCh *<-=>- fun ivar -> (k,  ivar)

  let filter k2v2b sm =
    sm.filterCh *<-=>- fun ivar -> (k2v2b,ivar)

  let map k2v2v sm =
    sm.mapCh    *<-=>- fun ivar -> (k2v2v,ivar)

  let mutate k v2v sm =
    sm.mutateCh *<-=>- fun ivar -> (k,v2v,ivar)

  let exchange k v sm =
    sm.exchangeCh  *<-=>- fun ivar -> (k,  v,ivar)

  let mutateAndExchange k v2v sm =
    sm.mutateOutCh *<-=>- fun ivar -> (k,v2v,ivar)

  let freeze sm =
    Alt.always ^ System.Threading.Volatile.Read sm.map

  let fork sm =
    ofMap ^ System.Threading.Volatile.Read sm.map
