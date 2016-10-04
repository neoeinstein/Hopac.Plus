namespace Hopac.Plus

[<AutoOpen>]
module internal Prelude =
  let always x _ = x
  let inline (^) f x = f x

  module Option =
    let orDefault (def:'x) (xO:'x option) : 'x =
      match xO with
      | Some x -> x
      | None -> def

    let condition (pred:'x -> bool) (x:'x) : 'x option =
      if pred x then Some x else None

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Hopac.Plus.Tests")>]
()
