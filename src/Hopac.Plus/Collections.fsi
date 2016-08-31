namespace Hopac.Plus.Collections

open Hopac

module Map =
  val mutate :            k:'k -> v2v:('v->'v) -> m:Map<'k,'v> -> Map<'k,'v>
  val exchange :          k:'k ->   v:     'v  -> m:Map<'k,'v> -> Map<'k,'v> * 'v option
  val mutateAndExchange : k:'k -> v2v:('v->'v) -> m:Map<'k,'v> -> Map<'k,'v> * 'v option

/// Mutation `Alt`s commit on the write being complete.
/// Reads via `SerializedMap.freeze` and `SerializedMap.fork` are synchronized
/// with writes.
type SerializedMap<'k, 'v when 'k : comparison>

module SerializedMap =
  val ofMap  : m:Map<'k,'v> -> SerializedMap<'k,'v>
  val create : unit         -> SerializedMap<'k,'v>

  val add :    k:'k -> v:'v -> sm:SerializedMap<'k,'v> -> Alt<unit>
  val remove : k:'k ->         sm:SerializedMap<'k,'v> -> Alt<unit>

  val filter : k2v2b:('k -> 'v -> bool) -> sm:SerializedMap<'k,'v> -> Alt<unit>
  val map :    k2v2v:('k -> 'v ->   'v) -> sm:SerializedMap<'k,'v> -> Alt<unit>
  val mutate : k:'k -> v2v:('v ->   'v) -> sm:SerializedMap<'k,'v> -> Alt<unit>

  val exchange :          k:'k ->   v:       'v  -> sm:SerializedMap<'k,'v> -> Alt<'v option>
  val mutateAndExchange : k:'k -> v2v:('v -> 'v) -> sm:SerializedMap<'k,'v> -> Alt<'v option>

  val freeze : sm:SerializedMap<'k,'v> -> Alt<Map<'k,'v>>
  val fork :   sm:SerializedMap<'k,'v> -> Alt<SerializedMap<'k,'v>>

/// Mutation `Alt`s commit on the write being complete.
/// Reads via `SharedMap.freeze` and `SharedMap.fork` are not synchronized.
type SharedMap<'k, 'v when 'k : comparison>

module SharedMap =
  val ofMap  : m:Map<'k,'v> -> Job<SharedMap<'k,'v>>
  val create : unit         -> Job<SharedMap<'k,'v>>

  val add :    k:'k -> v:'v -> sm:SharedMap<'k,'v> -> Alt<unit>
  val remove : k:'k ->         sm:SharedMap<'k,'v> -> Alt<unit>

  val filter : k2v2b:('k -> 'v -> bool) -> sm:SharedMap<'k,'v> -> Alt<unit>
  val map :    k2v2v:('k -> 'v ->   'v) -> sm:SharedMap<'k,'v> -> Alt<unit>
  val mutate : k:'k -> v2v:('v ->   'v) -> sm:SharedMap<'k,'v> -> Alt<unit>

  val exchange :          k:'k ->   v:       'v  -> sm:SharedMap<'k,'v> -> Alt<'v option>
  val mutateAndExchange : k:'k -> v2v:('v -> 'v) -> sm:SharedMap<'k,'v> -> Alt<'v option>

  val freeze : sm:SharedMap<'k,'v> -> Job<Map<'k,'v>>
  val fork :   sm:SharedMap<'k,'v> -> Job<SharedMap<'k,'v>>
