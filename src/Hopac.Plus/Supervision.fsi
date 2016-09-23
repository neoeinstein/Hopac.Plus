namespace Hopac.Plus.Supervision

open Hopac

type Will<'a>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Will =
  val create :   unit -> Will<'a>
  val createFull : 'a -> Will<'a>
  val update :   Will<'a> -> 'a -> Alt<unit>
  val latest :   Will<'a>       -> Alt<'a option>
  val exchange : Will<'a> -> 'a -> Alt<'a option>
  val revoke :   Will<'a>       -> Alt<unit>

type Policy =
  | Always of FailureAction
  | DetermineWith of (exn -> FailureAction)
  | DetermineWithJob of (exn -> Job<FailureAction>)
and FailureAction =
  | Restart
  | RestartDelayed of restartDelayMs:uint32
  | Terminate
  | Escalate

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Policy =
  val restart : Policy
  val restartDelayed : uint32 -> Policy
  val terminate : Policy
  val escalate : Policy
  val retry : maxRetries : uint32 -> Policy
  val retryWithDelay : delay : uint32 -> maxRetries : uint32 -> Policy
  val exponentialBackoff : initialDelay : uint32 -> multiplier : uint32 -> maxDelay : uint32 -> maxRetries : uint32 -> Policy

type SupervisedJob<'a> = Job<Choice<'a,exn>>

module Job =
  val supervise :         p : Policy ->   xJ :              #Job<'x>  -> SupervisedJob<'x>
  val superviseWithWill : p : Policy -> w2xJ : (Will<'a> -> #Job<'x>) -> SupervisedJob<'x>
