namespace Tabularius.ViewModels

open CommunityToolkit.Mvvm.ComponentModel

type MainWindowViewModel() =
    inherit ObservableObject()
    member this.Greeting = "Welcome to Tabularius!"
