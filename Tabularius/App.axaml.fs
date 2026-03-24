// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml
open Avalonia.Threading
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Serilog
open Tabularius.ViewModels
open Tabularius.Views

type App() =
    inherit Application()

    static let mutable errorCollector: ErrorCollector option = None
    static let mutable configuration: Configuration.TabulariusConfiguration option = None

    static member SetErrorCollector(collector: ErrorCollector) = errorCollector <- Some collector
    static member SetConfiguration(config: Configuration.TabulariusConfiguration) = configuration <- Some config

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =

        Dispatcher.UIThread.UnhandledException.Subscribe(fun e ->
            Log.Logger.Error(e.Exception, "Unhandled exception")
            e.Handled <- true
        ) |> ignore

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0)

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            let collector =
                errorCollector
                |> Option.defaultWith(fun () -> ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance))

            let config =
                configuration
                |> Option.defaultValue Configuration.TabulariusConfiguration.Default

            let windowService = WindowService()
            desktop.MainWindow <- MainWindow(DataContext = MainViewModel(collector, config, windowService))
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
