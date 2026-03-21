namespace Tabularius

open Avalonia.Threading
open JetBrains.Collections.Viewable

type AvaloniaScheduler() =
    interface IScheduler with
        member this.Queue(action) = Dispatcher.UIThread.Post action
        member this.IsActive = Dispatcher.UIThread.CheckAccess()
        member this.OutOfOrderExecution = false
