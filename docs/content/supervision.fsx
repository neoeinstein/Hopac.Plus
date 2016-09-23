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

let goodDaemon name will : Job<unit> =
  let rec init () =
    printfn "%s: Starting" name
    Will.latest will
    |> OptionJob.orDefault 0
    |> Job.bind loop
  and loop state =
    printfn "%s: loop %i" name state
    timeOutMillis 10
      |> Alt.afterJob (fun () ->
        let newState = state + 1
        let nextLoop = if newState < 100 then loop newState else Alt.unit
        Will.update will newState
        >>=. nextLoop
      )
  init ()

let badDaemon name (rand : System.Random) will : Job<unit> =
  let rec init () =
    printfn "%s: Starting" name
    Will.latest will
    |> OptionJob.orDefault 0
    |> Job.bind loop
  and loop state =
    printfn "%s: loop %i" name state
    timeOutMillis 10
      |> Alt.afterJob (fun () ->
        let newState = state + 1
        let nextLoop =
          if newState < 100 then
            if rand.Next(0,5) = 0 then
              throw state
            else
              loop newState
          else
            Job.unit
        Will.update will newState
        >>=. nextLoop
      ) |> asJob
  and throw state =
    Job.delay <| fun () ->
      printfn "%s: Failed" name
      if state < 75 then
        job { raise (exn "Sadness") }
      else
        job { raise (exn "Super Sad") }
  init ()

let rand = System.Random()

let startJ xJ = Promise.start xJ |> run >>- printfn "%A" |> start

// Examples of starting supervised jobs

let retryThrice = Policy.retry 3u
let backoffTo8 = Policy.exponentialBackoff 1000u 2u 8000u 8u

startJ <| Job.superviseWithWill Policy.restart (goodDaemon "happy")
startJ <| Job.superviseWithWill Policy.terminate (badDaemon "sad" rand)
startJ <| Job.superviseWithWill retryThrice (badDaemon "retry" rand)
startJ <| Job.superviseWithWill backoffTo8 (badDaemon "retry" rand)
