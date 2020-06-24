namespace Andromeda.Core.FSharp

// Stolen from:
// https://github.com/MoiraeSoftware/myriad/blob/71a7994744d6651c291427d7fa6b8a39c607b1f3/test/Myriad.IntegrationPluginTests/Lenses.fs
module Lenses =
    type Lens<'r, 't> = Lens of (('r -> 't) * ('r -> 't -> 'r))

    [<AutoOpen>]
    module Lens =
        let (<<) (Lens (get1, set1)) (Lens (get2, set2)) =
            let set outer value =
                let inner = get1 outer
                let updatedInner = set2 inner value
                let updatedOuter = set1 outer updatedInner
                updatedOuter

            Lens(get1 >> get2, set)

        let getl (Lens (get, _)) source = get source

        let setl (Lens (_, set)) value source = set source value
