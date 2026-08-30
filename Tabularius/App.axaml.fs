// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Tabularius

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Markup.Xaml
open Avalonia.Threading
open JetBrains.Collections.Viewable
open JetBrains.Lifetimes
open Serilog
open Tabularius.Interop
open Tabularius.ViewModels
open Tabularius.Views

type App() =
    inherit Application()

    static let mutable errorCollector: ErrorCollector option = None
    static let mutable configuration: Configuration.TabulariusConfiguration option = None

    let toLifetime(lifetime: IControlledApplicationLifetime) =
        let ld = new LifetimeDefinition()
        lifetime.Exit.Subscribe(fun _ -> ld.Terminate()) |> ignore
        ld.Lifetime

    static member SetErrorCollector(collector: ErrorCollector) = errorCollector <- Some collector
    static member SetConfiguration(config: Configuration.TabulariusConfiguration) = configuration <- Some config

    override this.Initialize() =
        AvaloniaXamlLoader.Load(this)

    override this.OnFrameworkInitializationCompleted() =

        Dispatcher.UIThread.UnhandledException.Subscribe(fun e ->
            Log.Logger.Error(e.Exception, "Unhandled exception")
            e.Handled <- true
        ) |> ignore

        let mutable viewModel: MainViewModel option = None

        ApplicationLifetime.Desktop() |> Option.iter(fun desktop ->
            let collector =
                errorCollector
                |> Option.defaultWith(fun () -> ErrorCollector(Lifetime.Eternal, SynchronousScheduler.Instance))

            let config =
                configuration
                |> Option.defaultValue Configuration.TabulariusConfiguration.Default

            let mainWindow = MainWindow()
            let windowService = WindowService(mainWindow, config.StatePath)
            let activityHost = BackgroundActivityHost(AvaloniaScheduler())
            let applicationLifetime = desktop |> toLifetime
            let hledger = Hledger.Initialize(applicationLifetime)

            let mainViewModel = MainViewModel(collector, config, windowService, activityHost, hledger)
            mainWindow.DataContext <- mainViewModel
            desktop.MainWindow <- mainWindow
            viewModel <- Some mainViewModel
        )

        base.OnFrameworkInitializationCompleted()

        viewModel |> Option.iter _.ReloadLastOpenedFile()
