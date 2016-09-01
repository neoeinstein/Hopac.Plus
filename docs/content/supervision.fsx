(*** hide ***)
#I "../../bin/Hopac.Plus"

(**
Supervision example
=============

*)
(*** do-not-eval ***)
#r "Hopac.Core"
#r "Hopac"
#r "Hopac.Platform"
#r "Hopac.Plus"

open Hopac
open Hopac.Infixes
open Hopac.Plus.Supervision
open Hopac.Plus.Extensions

let testMinion name failIf shutdown locker : Job<unit> =
  let rec loop state =
    Alt.choose [
      OneTimeSignal.awaitSignal shutdown ^=> SignalAck.ack
      timeOutMillis 10
        |> Alt.afterFun (fun () -> if failIf state then failwith "boom" else printfn "test [%O - %d]" name state)
        |> Alt.afterJob (fun () -> let newState = state + 1 in WillLocker.updateWill locker newState >>=. loop newState)
    ] |> asJob
  WillLocker.getLastWill locker
  |> OptionJob.bindJob loop
  |> OptionJob.orDefaultJob (loop 0)

let shutdown = OneTimeSignalSource.create ()

let sup = run <| Supervisor.create (OneTimeSignalSource.signal shutdown) (printfn "%s")

let rand = System.Random()

let job1 = JobHandle.createAnonymous ()
let job2 = JobHandle.createAnonymous ()

queue <| Supervisor.start sup job1 Restart (testMinion "test1" (fun _ -> false))
queue <| Supervisor.start sup job2 (Delayed <| timeOutMillis 20) (testMinion "delayedTest" (fun _ -> rand.Next(0, 2) = 1))

queue <| Supervisor.stop sup job1
queue <| Supervisor.stop sup job2

queue <| (timeOutMillis 1000 >>=. OneTimeSignalSource.triggerAndAwaitAck shutdown >>- fun () -> printfn "All done...")

