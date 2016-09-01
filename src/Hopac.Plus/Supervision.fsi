namespace Hopac.Plus.Supervision

open Hopac
open Hopac.Infixes

type Policy =
  | Restart
  | Terminate
  | Delayed of restartDelay:Alt<unit>

type JobHandle
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JobHandle =
  val create : string -> JobHandle
  val createAnonymous : unit -> JobHandle

type Supervisor

type OneTimeSignalSource
type OneTimeSignal
type SignalAck

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OneTimeSignalSource =
  val create : unit -> OneTimeSignalSource
  val trigger : OneTimeSignalSource -> Job<unit>
  val triggerAndAwaitAck : OneTimeSignalSource -> Alt<unit>
  val signal : OneTimeSignalSource -> OneTimeSignal

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OneTimeSignal =
  val awaitSignal : OneTimeSignal -> Alt<SignalAck>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SignalAck =
  val ack : SignalAck -> Job<unit>

type WillLocker

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WillLocker =
  val create : 'a -> WillLocker
  val createEmpty : unit -> WillLocker
  val updateWill : WillLocker -> 'a -> Job<unit>
  val getLastWill : WillLocker -> Job<'a option>

type JobInit = OneTimeSignal -> WillLocker -> Job<unit>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Supervisor =
  val create : shutdown:OneTimeSignal -> logger:(string -> unit) -> Job<Supervisor>
  val start : supervisor:Supervisor -> handle:JobHandle -> policy:Policy -> init:JobInit -> Alt<unit>
  val stop : supervisor:Supervisor -> handle:JobHandle -> Alt<unit>
