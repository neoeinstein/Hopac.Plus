namespace Hopac.Plus.Extensions

open Hopac
open Hopac.Infixes

[<AutoOpen>]
module Operators =
  let inline (^) x = x

type StopwatchTicks = int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StopwatchTicks =
  let inline getTimestamp () : StopwatchTicks = System.Diagnostics.Stopwatch.GetTimestamp()
  let swTicksPerMicrosecond = System.Diagnostics.Stopwatch.Frequency / 1000000L
  let swTicksPerMillisecond = System.Diagnostics.Stopwatch.Frequency / 1000L

  let toTimeSpanTicks t = t * System.TimeSpan.TicksPerSecond / System.Diagnostics.Stopwatch.Frequency
  let toMicroseconds t = t / swTicksPerMicrosecond
  let toMilliseconds t = t / swTicksPerMillisecond

  let toTimeSpan =
    System.TimeSpan.FromTicks << toTimeSpanTicks

type SecondsSinceEpoch = int64
type TimeSpanTicksSinceEpoch = int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Clock =
  let ticksPerSecond = System.TimeSpan.TicksPerSecond
  let dtoEpoch = System.DateTimeOffset(1970, 1, 1, 0, 0, 0, System.TimeSpan.Zero).UtcTicks

  let getSecondsSinceEpoch = Job.delay ^ fun () -> Job.result ^ (System.DateTimeOffset.UtcNow.UtcTicks - dtoEpoch) / ticksPerSecond
  let getTimeSpanTicksSinceEpoch = Job.delay ^ fun () -> Job.result ^ System.DateTimeOffset.UtcNow.UtcTicks - dtoEpoch

module Promise =
  let nothing<'x> : Promise<'x> = Promise ()
  let inline value<'x> (x : 'x) = Promise x
  let inline ofJob (xJ : Job<'x>) = Promise<'x> xJ
  let unit = ofJob ^ Job.unit ()
  let inline raises (ex : exn) = Promise ex

module Alt =
  let never<'x> : Alt<'x> = Alt.never ()
  let unit = asAlt ^ Promise.unit
  let inline value x = Alt.always x

  let timeFun onComplete onNack xA =
    Alt.withNackJob ^ fun nack ->
      let ts = StopwatchTicks.getTimestamp()
      Job.start
        <| nack ^-> fun () -> onNack     ^ StopwatchTicks.getTimestamp() - ts
        >>-. xA ^-> fun  x -> onComplete ^ StopwatchTicks.getTimestamp() - ts; x

  let timeJob onCompleteJ onNackJ xA =
    Alt.withNackJob ^ fun nack ->
      let ts = StopwatchTicks.getTimestamp()
      Job.start
        <| nack ^=> fun () -> onNackJ     ^ StopwatchTicks.getTimestamp() - ts
        >>-. xA ^=> fun  x -> onCompleteJ ^ StopwatchTicks.getTimestamp() - ts; x

module Job =
  let inline value x = Job.result x
  let unit = asJob Promise.unit
  let eternal<'x> = asJob Alt.never<'x>

  let timeFun onComplete xJ =
    Job.delay ^ fun () ->
      let ts = StopwatchTicks.getTimestamp()
      xJ >>- fun x -> onComplete (StopwatchTicks.getTimestamp() - ts); x

  let timeJob onCompleteJ xJ =
    Job.delay ^ fun () ->
      let ts = StopwatchTicks.getTimestamp()
      xJ >>= fun x -> onCompleteJ (StopwatchTicks.getTimestamp() - ts); x

module OptionJob =
  let inline create x = Job.result ^ Some x
  let none<'x> : Job<'x option> = Job.value None
  let inline ofOption (xO: 'x option) = Job.result xO
  let ofJob xJ = Job.map Some xJ

  let bind x2yOJ xOJ =
    xOJ |> Job.bind ^ function
      | Some x -> asJob ^ x2yOJ x
      | None -> none

  let bindOpt x2yO xOJ =
    xOJ |> Job.map ^ function
      | Some x -> x2yO x
      | None -> None

  let bindJob x2yJ xOJ =
    xOJ |> Job.bind ^ function
      | Some x -> ofJob ^ x2yJ x
      | None -> none

  let map x2y xOJ =
    xOJ |> Job.map ^ Option.map x2y

  let orDefault x xOJ =
    xOJ |>Job.map ^ fun xO -> defaultArg xO x
  let orDefaultJob xJ xOJ =
    xOJ |> Job.bind ^ function
      | Some x -> Job.result x
      | None -> xJ

  let traverse xJ2yJ (xJO : #Job<'x> option) : Job<'y option> =
    match xJO with
    | Some xJ -> ofJob ^ xJ2yJ xJ
    | None -> none

  let sequence (xJO : #Job<'x> option) : Job<'x option> =
    traverse id xJO

module ChoiceJob =
  let inline create x = Job.result ^ Choice1Of2 x
  let inline createLeft y = Job.result ^ Choice2Of2 y
  let inline ofChoice (xyC : Choice<'x,'y>) = Job.result xyC
  let ofJob xJ = Job.map Choice1Of2 xJ
  let ofLeftJob yJ = Job.map Choice2Of2 yJ

  let bind x2ayCJ xyCJ =
    xyCJ |> Job.bind ^ function
      | Choice1Of2 x -> asJob ^ x2ayCJ x
      | Choice2Of2 y -> createLeft y

  let bindLeft y2xbCJ xyCJ =
    xyCJ |> Job.bind ^ function
      | Choice1Of2 x -> create x
      | Choice2Of2 y -> asJob ^ y2xbCJ y

  let bindJob x2aJ xyCJ =
    xyCJ |> Job.bind ^ function
      | Choice1Of2 x -> ofJob ^ x2aJ x
      | Choice2Of2 y -> createLeft y

  let bindLeftJob y2bJ xyCJ =
    xyCJ |> Job.bind ^ function
      | Choice1Of2 x -> create x
      | Choice2Of2 y -> ofLeftJob ^ y2bJ y

  let bindChoice x2ayC xyCJ =
    xyCJ |> Job.map ^ function
      | Choice1Of2 x -> x2ayC x
      | Choice2Of2 y -> Choice2Of2 y

  let bindLeftChoice y2xbC xyCJ =
    xyCJ |> Job.map ^ function
      | Choice1Of2 x -> Choice1Of2 x
      | Choice2Of2 y -> y2xbC y

  let map x2a xyCJ =
    xyCJ |> Job.map ^ function
      | Choice1Of2 x -> Choice1Of2 ^ x2a x
      | Choice2Of2 y -> Choice2Of2 y

  let mapLeft y2b xyCJ =
    xyCJ |> Job.map ^ function
      | Choice1Of2 x -> Choice1Of2 x
      | Choice2Of2 y -> Choice2Of2 ^ y2b y

  let fold x2t y2t xyCJ =
    xyCJ |> Job.map ^ function
      | Choice1Of2 x -> x2t x
      | Choice2Of2 y -> y2t y

  let foldJob x2tJ y2tJ xyCJ =
    xyCJ |> Job.bind ^ function
      | Choice1Of2 x -> asJob ^ x2tJ x
      | Choice2Of2 y -> asJob ^ y2tJ y

  let traverse xJ2aJ (xJyC : Choice<Job<'x>,'y>) : Job<Choice<'a,'y>> =
    match xJyC with
    | Choice1Of2 xJ -> ofJob ^ xJ2aJ xJ
    | Choice2Of2 y -> createLeft y

  let traverseLeft yJ2bJ (xyJC : Choice<'x,Job<'y>>) : Job<Choice<'x,'b>> =
    match xyJC with
    | Choice1Of2 x -> create x
    | Choice2Of2 yJ -> ofLeftJob ^ yJ2bJ yJ

  let traverseBoth xJ2aJ yJ2bJ (xJyJC : Choice<Job<'x>,Job<'y>>) : Job<Choice<'a,'b>> =
    match xJyJC with
    | Choice1Of2 xJ -> ofJob ^ xJ2aJ xJ
    | Choice2Of2 yJ -> ofLeftJob ^ yJ2bJ yJ

  let sequence (xJyC : Choice<Job<'x>,'y>) : Job<Choice<'x,'y>> =
    traverse id xJyC

  let sequenceLeft (xyJC : Choice<'x,Job<'y>>) : Job<Choice<'x,'y>> =
    traverseLeft id xyJC

  let sequenceBoth (xJyJC : Choice<Job<'x>,Job<'y>>) : Job<Choice<'x,'y>> =
    traverseBoth id id xJyJC
