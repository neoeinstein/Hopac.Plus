namespace Hopac.Extra.Collections

open Hopac
open Hopac.Infixes

type SharedMap<'key,'value when 'key : comparison> =
  { addCh : Ch<'key * 'value * IVar<unit>>
    mapCh : Ch<('key -> 'value -> 'value) * IVar<unit>>
    filterCh : Ch<('key -> 'value -> bool) * IVar<unit>>
    removeCh : Ch<'key * IVar<unit>>
    mutateCh : Ch<'key * ('value -> 'value) * IVar<unit>>
    mutateOutCh : Ch<'key * ('value -> 'value) * IVar<'value option>>
    exchangeCh : Ch<'key * 'value * IVar<'value option>>
    map : Map<'key,'value> ref }

module SharedMap =
  let ofMap init =
    let o = { addCh = Ch (); mapCh = Ch (); filterCh = Ch (); removeCh = Ch (); mutateCh = Ch (); mutateOutCh = Ch (); exchangeCh = Ch (); map = ref init }
    Job.foreverServer
      ( Alt.choose
          [ o.addCh ^=> fun (k,v,ivar) ->
              o.map := Map.add k v !o.map
              ivar *<= ()
            o.removeCh ^=> fun (k,ivar) ->
              o.map := Map.remove k !o.map
              ivar *<= ()
            o.mapCh ^=> fun (f,ivar) ->
              o.map := Map.map f !o.map
              ivar *<= ()
            o.filterCh ^=> fun (f,ivar) ->
              o.map := Map.filter f !o.map
              ivar *<= ()
            o.mutateCh ^=> fun (k,f,ivar) ->
              match Map.tryFind k !o.map with
              | Some v -> o.map := Map.add k (f v) !o.map
              | None -> ()
              ivar *<= ()
            o.mutateOutCh ^=> fun (k,f,ivar) ->
              let m = !o.map
              let oldVOpt = Map.tryFind k m
              match oldVOpt with
              | Some oldV -> o.map := Map.add k (f oldV) m
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

  let create () =
    ofMap Map.empty

  let add k v smap =
    smap.addCh *<-=>- fun ivar -> (k,v,ivar)

  let map f smap =
    smap.mapCh  *<-=>- fun ivar -> (f,ivar)

  let filter f smap =
    smap.filterCh  *<-=>- fun ivar -> (f,ivar)

  let remove k smap =
    smap.removeCh *<-=>- fun ivar -> (k,ivar)

  let freeze smap =
    Job.result <| System.Threading.Volatile.Read smap.map

  let internal getNow smap =
    System.Threading.Volatile.Read smap.map

  let fork smap =
    let v = System.Threading.Volatile.Read smap.map
    ofMap v

  let mutate k f smap =
    smap.mutateCh *<-=>- fun ivar -> (k,f,ivar)

  let mutateAndTryGetPrior k f smap =
    smap.mutateOutCh *<-=>- fun ivar -> k,f,ivar

  let tryExchange k v smap =
    smap.exchangeCh *<-=>- fun ivar -> k,v,ivar
