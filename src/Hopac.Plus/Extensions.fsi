namespace Hopac.Plus.Extensions

open Hopac

type StopwatchTicks = int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StopwatchTicks =
  val toTimeSpanTicks : StopwatchTicks -> int64
  val toMicroseconds : StopwatchTicks -> int64
  val toMilliseconds : StopwatchTicks -> int64
  val toTimeSpan : StopwatchTicks -> System.TimeSpan

type SecondsSinceEpoch = int64
type TimeSpanTicksSinceEpoch = int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Clock =
  val getSecondsSinceEpoch : Job<SecondsSinceEpoch>
  val getTimeSpanTicksSinceEpoch : Job<TimeSpanTicksSinceEpoch>

module Promise =
  val nothing<'x> : Promise<'x>
  val inline value : x:'x -> Promise<'x>
  val inline ofJob : xJ:Job<'x> -> Promise<'x>
  val unit : Promise<unit>
  val inline raises : ex:exn -> Promise<'x>

module Alt =
  val never<'x> : Alt<'x>
  val unit : Alt<unit>
  val inline value : x:'x -> Alt<'x>

  val timeFun : onComplete  : (StopwatchTicks ->      unit ) -> onNack  : (StopwatchTicks ->      unit ) -> xA : Alt<'x> -> Alt<'x>
  val timeJob : onCompleteJ : (StopwatchTicks -> #Job<unit>) -> onNackJ : (StopwatchTicks -> #Job<unit>) -> xA : Alt<'x> -> Alt<'x>

module Job =
  val inline value : x:'x -> Job<'x>
  val unit : Job<unit>
  val eternal<'x> : Job<'x>

  val timeFun : onComplete  : (StopwatchTicks ->      unit ) -> xJ : Job<'x> -> Job<'x>
  val timeJob : onCompleteJ : (StopwatchTicks -> #Job<unit>) -> xJ : Job<'x> -> Job<'x>

module OptionJob =
  val inline create : x:'x -> Job<'x option>
  val none<'x> : Job<'x option>
  val inline ofOption : xO:'x option -> Job<'x option>
  val ofJob : xJ:Job<'x> -> Job<'x option>

  val bind : x2yOJ:('x -> #Job<'y option>) -> xOJ:Job<'x option> -> Job<'y option>
  val bindOpt : x2yO:('x -> 'y option) -> xOJ:Job<'x option> -> Job<'y option>
  val bindJob : x2yJ:('x -> #Job<'y>) -> xOJ:Job<'x option> -> Job<'y option>

  val map : x2y:('x -> 'y) -> xOJ:Job<'x option> -> Job<'y option>

  val orDefault : x:'x -> xOJ:Job<'x option> -> Job<'x>
  val orDefaultJob : xJ:Job<'x> -> xOJ:Job<'x option> -> Job<'x>

module ChoiceJob =
  val inline create : x:'x -> Job<Choice<'x,'y>>
  val inline createLeft : y:'y -> Job<Choice<'x,'y>>
  val inline ofChoice : xyC:Choice<'x,'y> -> Job<Choice<'x,'y>>
  val ofJob : xJ:Job<'x> -> Job<Choice<'x,'y>>
  val ofLeftJob : yJ:Job<'y> -> Job<Choice<'x,'y>>

  val bind : x2ayCJ:('x -> #Job<Choice<'a,'y>>) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'a,'y>>
  val bindLeft : y2xbCJ:('y -> #Job<Choice<'x,'b>>) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'x,'b>>
  val bindJob : x2aJ:('x -> #Job<'a>) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'a,'y>>
  val bindLeftJob : y2bJ:('y -> #Job<'b>) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'x,'b>>
  val bindChoice : x2ayC:('x -> Choice<'a,'y>) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'a,'y>>
  val bindLeftChoice : y2xbC:('y -> Choice<'x,'b>) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'x,'b>>

  val map : x2a:('x -> 'a) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'a,'y>>
  val mapLeft : y2b:('y -> 'b) -> xyCJ:Job<Choice<'x,'y>> -> Job<Choice<'x,'b>>

  val fold : x2t:('x -> 't) -> y2t:('y -> 't) -> xyCJ:Job<Choice<'x,'y>> -> Job<'t>
  val foldJob : x2t:('x -> #Job<'t>) -> y2t:('y -> #Job<'t>) -> xyCJ:Job<Choice<'x,'y>> -> Job<'t>
