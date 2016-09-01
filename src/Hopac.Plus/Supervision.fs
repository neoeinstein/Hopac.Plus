namespace Hopac.Plus.Supervision

open Hopac
open Hopac.Infixes
open Hopac.Plus
open Hopac.Plus.Extensions

type Policy =
  | Restart
  | Terminate
  | Delayed of restartDelay:Alt<unit>

type SignalAck = SignalAck of IVar<unit>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SignalAck =
  let ack (SignalAck uI) : Job<unit> =
    uI *<= ()

type OneTimeSignal = OneTimeSignal of IVar<IVar<unit>>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OneTimeSignal =
  let awaitSignal (OneTimeSignal uIC) : Alt<SignalAck> =
    asAlt ^ uIC
    |> Alt.afterFun ^ SignalAck

type OneTimeSignalSource = OneTimeSignalSource of IVar<IVar<unit>>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OneTimeSignalSource =
  let create () : OneTimeSignalSource =
    OneTimeSignalSource ^ IVar ()

  let trigger (OneTimeSignalSource uIC) : Job<unit> =
    uIC *<= IVar ()

  let triggerAndAwaitAck (OneTimeSignalSource uIC) : Alt<unit> =
    Alt.withNackJob ^ fun nack ->
      Alt.choose
        [ nack ^=>. Alt.never
          IVar.read uIC
        ]

  let signal (OneTimeSignalSource uIC) : OneTimeSignal =
    OneTimeSignal uIC

type WillLocker = WillLocker of MVar<obj option>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module WillLocker =
  let create initial : WillLocker =
    WillLocker ^ MVar ^ Some ^ box initial
  let createEmpty () : WillLocker =
    WillLocker ^ MVar None
  let updateWill (WillLocker aM) (a:'a) : Job<unit> =
    asJob ^ MVar.mutateFun (always ^ Some ^ box a) aM
  let getLastWill (WillLocker aM) : Job<'a option> =
    OptionJob.map unbox ^ MVar.read aM

type JobInit = OneTimeSignal -> WillLocker -> Job<unit>

[<StructuredFormatDisplay("JobHandle {Value}")>]
type JobHandle =
  JobHandle of string with
  override x.ToString() =
    match x with JobHandle str -> str
  member x.Value = string x

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module JobHandle =
  let create str = JobHandle str
  let createAnonymous () = JobHandle ^ string ^ System.Guid.NewGuid()

type MinionInfo =
  {
    handle : JobHandle
    policy : Policy
    job    : JobInit
  }

type MinionState =
  {
    info : MinionInfo
    locker : WillLocker
    shutdown : OneTimeSignalSource
  }

[<StructuredFormatDisplay("JobId {Value}")>]
type JobId =
  JobId of uint64 with
  override x.ToString() =
    match x with JobId str -> string str
  member x.Value = string x


type SupervisorState =
  {
    ident     : uint64
    minions   : Map<JobId, MinionState>
    processes : Map<JobId, Alt<JobId>>
    delayed   : Map<JobHandle, Alt<JobHandle * (SupervisorState -> Job<SupervisorState>)>>
    version   : uint64
  }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SupervisorState =
  let initial =
    { ident = 0UL
      minions = Map.empty
      processes = Map.empty
      delayed = Map.empty
      version = 0UL }

  let removeMinion jobId state =
    let minionStateO = state.minions |> Map.tryFind jobId
    { state with
        version   = state.version + 1UL
        processes = Map.remove jobId state.processes
        minions   = Map.remove jobId state.minions
        delayed   = match minionStateO with Some { info = { handle = jh } } -> Map.remove jh state.delayed | None -> state.delayed  }

  let addMinion jobId minionState (p : Proc) state =
    { state with
        version = state.version + 1UL
        ident   = state.ident + 1UL
        processes =
          Map.add jobId (p ^-> (fun () -> jobId)) state.processes
        minions   =
          Map.add jobId minionState state.minions }

  let addDelayed handle promise state =
    { state with
        version = state.version + 1UL
        delayed = Map.add handle promise state.delayed }

  let removeDelayed handle state =
    { state with
        version = state.version + 1UL
        delayed = Map.remove handle state.delayed }

  let jobNames state =
    let running =
      state.minions
      |> Map.toList
      |> List.map (fun (_, { info = { handle = h } }) -> h)
    let delayed =
      state.delayed
      |> Map.toList
      |> List.map fst
    List.concat [running;delayed]

  let jobState state handle =
    state.minions
    |> Map.tryPick ^ fun jid s -> if s.info.handle = handle then Some (jid,s) else None

type Supervisor =
  {
    register   : Ch<MinionInfo>
    unregister : Ch<JobHandle>
  }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Supervisor =
  let startMinion logger minionInfo willLocker state =
    logger <| sprintf "Starting minion %O" minionInfo.handle
    if SupervisorState.jobNames state |> List.contains minionInfo.handle then
      logger <| sprintf "New minion not started; %O already supervised" minionInfo.handle
      Job.result state
    else
      let jobId = JobId state.ident
      let minionState =
        { info = minionInfo
          locker = willLocker |> Option.orDefault ^ WillLocker.createEmpty ()
          shutdown = OneTimeSignalSource.create () }
      Proc.start (minionInfo.job (OneTimeSignalSource.signal minionState.shutdown) minionState.locker)
      >>- fun p ->
            logger <| sprintf "Minion %O (%A) started" minionInfo.handle jobId
            SupervisorState.addMinion jobId minionState p state

  let unregisterMinion logger state handle =
    logger <| sprintf "Unregistering %A started" handle
    match SupervisorState.jobState state handle with
    | Some (jobId, minionState) ->
      logger <| sprintf "Shutting down %A (%A)" handle jobId
      OneTimeSignalSource.triggerAndAwaitAck minionState.shutdown
      >>-. SupervisorState.removeMinion jobId state

    | None ->
      logger <| sprintf "Received request to unregister unknown job %A" handle
      Job.result (SupervisorState.removeDelayed handle state)

  let handleTermination logger state jobId =
    let minionState = Map.find jobId state.minions
    match minionState.info.policy with
    | Terminate ->
      logger <| sprintf "%A (%A) terminated; removing from supervision" minionState.info.handle jobId
      Job.result ^ SupervisorState.removeMinion jobId state

    | Restart ->
      logger <| sprintf "%A (%A) terminated; restarting" minionState.info.handle jobId
      SupervisorState.removeMinion jobId state
      |> startMinion logger minionState.info (Some minionState.locker)

    | Delayed delay ->
      logger <| sprintf "%A (%A) terminated; restarting in %A" minionState.info.handle jobId delay
      let promise =
        delay
        >>-. (minionState.info.handle, startMinion logger minionState.info (Some minionState.locker))
        |> memo
      state
      |> SupervisorState.removeMinion jobId
      |> SupervisorState.addDelayed minionState.info.handle (Promise.read promise)
      |> Job.result

  let executeShutdown logger state ack =
    let shutdownMinion (minionState : MinionState) =
      OneTimeSignalSource.triggerAndAwaitAck minionState.shutdown
    let shutdownAll =
      Job.seqIgnore (state.minions |> Map.toSeq |> Seq.map (snd >> shutdownMinion))
      |> Job.map (fun () -> logger "All minions shutdown")
      |> memo
    logger "Shutting down minions!"
    Alt.choose [
      shutdownAll |> Promise.read
      timeOutMillis 1000 |> Alt.afterFun (fun () -> logger "Minion shutdown timed out without all minions shutting down cleanly")
    ] >>=. SignalAck.ack ack

  let create shutdown logger =
    let registerCh   = Ch()
    let unregisterCh = Ch()

    let rec loop state =
      // Sanity check; should fail hard if these fail...
      // These should be removed after testing.
      let processIds = state.processes |> Map.toList |> List.map fst
      if processIds <> (state.minions |> Map.toList |> List.map fst) then
        failwithf "Unmatched process and minion maps\nprocesses: %A\n minions: %A"
          processIds
          (state.minions |> Map.toList |> List.map fst)
      if processIds <> List.distinct processIds then
        failwithf "duplicate process ids?"
      let minionNames = state.minions |> Map.toList |> List.map (snd >> fun ms -> ms.info.handle)
      if minionNames <> List.distinct minionNames then
        failwithf "duplicate minion handles detected\n%A" minionNames
      logger <| "Current state version: " + string state.version

      Alt.choose [
        // shutdown
        OneTimeSignal.awaitSignal shutdown ^=> executeShutdown logger state

        // anything else will create a new state and then recurse into the loop
        Alt.choose [
          // process delayed restarts
          state.delayed
          |> Map.toSeq
          |> Seq.map snd
          |> Alt.choose
          |> Alt.afterJob
              (fun (delayName, restart) ->
                state
                |> SupervisorState.removeDelayed delayName
                |> restart)

          // register new minion
          registerCh ^=>
            fun minionInfo ->
              startMinion logger minionInfo None state

          // unregister minion
          unregisterCh ^=> unregisterMinion logger state

          // handle termination
          state.processes
          |> Map.toSeq
          |> Seq.map snd
          |> Alt.choose
          |> Alt.afterJob ^ handleTermination logger state
        ] |> Alt.afterJob loop
      ]

    Job.start ^ loop SupervisorState.initial
    >>-. { register = registerCh; unregister = unregisterCh }

  let start s h p init =
    s.register *<- { handle = h; policy = p; job = init }

  let stop s h =
    s.unregister *<- h
