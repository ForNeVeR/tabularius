namespace Tabularius.DesignTime

open Tabularius.Interop

type HledgerDesignTimeApi() =
    interface IHledgerApi with
        member this.VerifyJournal(_, _) =
            raise(System.NotImplementedException())
