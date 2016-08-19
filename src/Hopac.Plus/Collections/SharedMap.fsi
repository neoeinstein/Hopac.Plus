namespace Hopac.Plus.Collections

type SharedMap<'key,'value when 'key : comparison>

module SharedMap =
  val create : unit -> Hopac.Job<SharedMap<'key,'value>>
  val ofMap : map:Map<'key,'value> -> Hopac.Job<SharedMap<'key,'value>>
  val add : key:'key -> value:'value -> smap:SharedMap<'key,'value> -> Hopac.Alt<unit>
  val remove : key:'key -> smap:SharedMap<'key,'value> -> Hopac.Alt<unit>
  val filter : predicate:('key -> 'value -> bool) -> smap:SharedMap<'key,'value> -> Hopac.Alt<unit>
  val map : f:('key -> 'value -> 'value) -> smap:SharedMap<'key,'value> -> Hopac.Alt<unit>
  val mutate : key:'key -> f:('value -> 'value) -> smap:SharedMap<'key,'value> -> Hopac.Alt<unit>
  val tryExchange : key:'key -> newValue:'value -> smap:SharedMap<'key,'value> -> Hopac.Alt<'value option>
  val mutateAndTryGetPrior : key:'key -> f:('value -> 'value) -> smap:SharedMap<'key,'value> -> Hopac.Alt<'value option>
  val freeze : smap:SharedMap<'key,'value> -> Hopac.Job<Map<'key,'value>>
  val fork : smap:SharedMap<'key,'value> -> Hopac.Job<SharedMap<'key,'value>>
  val internal getNow : smap:SharedMap<'key,'value> -> Map<'key,'value>
