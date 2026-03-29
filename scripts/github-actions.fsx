let licenseHeader = """
# SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>
#
# SPDX-License-Identifier: MIT

# This file is auto-generated.""".Trim()

#r "nuget: Generaptor.Library, 1.11.0"

open System
open TruePath
open System.Xml.Linq
open Generaptor
open Generaptor.GitHubActions
open type Generaptor.GitHubActions.Commands

let haskellProjectDirectory = LocalPath "HledgerInterop"
let haskellProjectFile = haskellProjectDirectory / "HledgerInterop.proj"

let getProjectItemIncludes itemName =
    let project = XDocument.Load haskellProjectFile.Value
    project.Descendants(XName.Get itemName)
    |> Seq.choose (fun item ->
        match item.Attribute(XName.Get "Include") with
        | null -> None
        | includeAttribute -> Some includeAttribute.Value
    )
    |> Seq.collect (fun value ->
        value.Split(';', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries)
    )
    |> Seq.map (fun pattern -> $"{haskellProjectDirectory.Value}/{pattern.Replace('\\', '/')}")
    |> Seq.distinct
    |> Seq.sort
    |> List.ofSeq

let haskellCacheInputs = [
    haskellProjectFile.Value
    yield! getProjectItemIncludes "HaskellSource"
    yield! getProjectItemIncludes "HaskellConfig"
]

let haskellCacheInputHash =
    haskellCacheInputs
    |> Seq.map(_.Replace("\\", "/"))
    |> Seq.map (sprintf "'%s'")
    |> String.concat ", "
    |> sprintf "${{ hashFiles(%s) }}"

let workflows = [

    let workflow name steps =
        workflow name [
            header licenseHeader
            yield! steps
        ]

    let checkOut = step(
        name = "Check out the sources",
        usesSpec = Auto "actions/checkout"
    )

    let dotNetJob id steps =
        job id [
            setEnv "DOTNET_CLI_TELEMETRY_OPTOUT" "1"
            setEnv "DOTNET_NOLOGO" "1"
            setEnv "NUGET_PACKAGES" "${{ github.workspace }}/.github/nuget-packages"

            checkOut
            step(
                name = "Set up .NET SDK",
                usesSpec = Auto "actions/setup-dotnet"
            )
            step(
                name = "Cache NuGet packages",
                usesSpec = Auto "actions/cache",
                options = Map.ofList [
                    "key", "${{ runner.os }}.nuget.${{ hashFiles('**/*.*proj', '**/*.props') }}"
                    "path", "${{ env.NUGET_PACKAGES }}"
                ]
            )

            yield! steps
        ]

    let cacheHaskellBuildOutput name =
        step(
            id = "hledger-interop-bin",
            name = "Cache HledgerInterop build output",
            usesSpec = Auto "actions/cache",
            options = Map.ofList [
                "key", name + ".${{ matrix.image }}.hledger-bin." + haskellCacheInputHash
                "path", "HledgerInterop/bin"
            ]
        )

    let refreshHaskellBuildOutputTimestamps =
        step(
            name = "Refresh cached HledgerInterop output timestamps",
            condition = "steps.hledger-interop-bin.outputs.cache-hit == 'true'",
            shell = "pwsh",
            run = """Get-ChildItem -Path HledgerInterop/bin -Recurse -File |
    ForEach-Object { $_.LastWriteTimeUtc = [DateTime]::UtcNow }"""
        )

    let setUpHaskellEnvironment name = [
        step(
            condition = "${{ steps.hledger-interop-bin.outputs.cache-hit != 'true' }}",
            name = "Set up Haskell Stack",
            usesSpec = Auto "haskell-actions/setup",
            options = Map.ofList [
                "enable-stack", "true"
                "stack-version", "3.9.1"
            ]
        )

        step(
            condition = "${{ steps.hledger-interop-bin.outputs.cache-hit != 'true' }}",
            name = "Determine the Stack root directory",
            id = "stack",
            shell = "pwsh",
            run = """$stackRoot = stack path --stack-root
if (!$?) { throw "Stack exit code: $LASTEXITCODE" }
else {
    Write-Host "stack-root=$stackRoot"
    "stack-root=$stackRoot" >> $env:GITHUB_OUTPUT
}"""
        )

        step(
            condition = "${{ steps.hledger-interop-bin.outputs.cache-hit != 'true' }}",
            name = "Cache Stack dependencies",
            usesSpec = Auto "actions/cache",
            options = Map.ofList [
                "key", name + ".${{ matrix.image }}.stack.${{ hashFiles('HledgerInterop/stack.yaml', 'HledgerInterop/stack.yaml.lock', 'HledgerInterop/*.cabal') }}"
                "path", "${{ steps.stack.outputs.stack-root }}"
            ]
        )
    ]

    // TODO[#40]: Remove this later, after some future upgrade of Svg.Skia.Converter incorporating an update of SkiaSharp.
    //            See https://github.com/mono/SkiaSharp/issues/3272
    let setUpSkiaSharpWorkarounds = [
        step(
            name = "Work around a SkiaSharp issue on ARM64 Linux",
            condition = "runner.os == 'Linux' && runner.arch == 'ARM64'",
            run = "echo \"LD_PRELOAD=/lib/aarch64-linux-gnu/libuuid.so.1:/lib/aarch64-linux-gnu/libfreetype.so.6\" >> $GITHUB_ENV"
        )
    ]

    let images = [
        "macos-15"
        "ubuntu-24.04"
        "ubuntu-24.04-arm"
        "windows-2025"
    ]

    workflow "main" [
        name "Main"
        onPushTo "main"
        onPushTo "renovate/**"
        onPullRequestTo "main"
        onSchedule "0 0 * * 6"
        onWorkflowDispatch

        dotNetJob "verify-workflows" [
            runsOn "ubuntu-24.04"
            step(run = "dotnet fsi ./scripts/github-actions.fsx verify")
        ]

        dotNetJob "check" [
            strategy(failFast = false, matrix = [
                "image", images
            ])
            runsOn "${{ matrix.image }}"

            cacheHaskellBuildOutput "main"
            refreshHaskellBuildOutputTimestamps
            yield! setUpHaskellEnvironment "main"

            step(
                condition = "steps.hledger-interop-bin.outputs.cache-hit != 'true'",
                name = "Test HledgerInterop",
                shell = "pwsh",
                workingDirectory = "HledgerInterop",
                run = "stack test"
            )

            yield! setUpSkiaSharpWorkarounds

            step(
                name = "Build",
                run = "dotnet build"
            )
            step(
                name = "Test",
                run = "dotnet test",
                timeoutMin = 10
            )
        ]

        job "licenses" [
            runsOn "ubuntu-24.04"
            checkOut
            step(
                name = "REUSE license check",
                usesSpec = Auto "fsfe/reuse-action"
            )
        ]

        job "encoding" [
            runsOn "ubuntu-24.04"
            checkOut
            step(
                name = "Verify encoding",
                shell = "pwsh",
                run = "Install-Module VerifyEncoding -Repository PSGallery -RequiredVersion 2.3.0 -Force && Test-Encoding"
            )
        ]

        job "todos" [
            runsOn "ubuntu-24.04"
            checkOut
            step(
                name = "Check TODOs",
                usesSpec = Auto "ForNeVeR/Todosaurus/action",
                options = Map.ofList [
                    "strict", "true"
                    "github-token", "${{ secrets.GITHUB_TOKEN }}"
                ]
            )
        ]
    ]

    workflow "release" [
        name "Release"
        onPushTo "main"
        onPushTo "renovate/**"
        onPushTags "v*"
        onPullRequestTo "main"
        onSchedule "0 0 * * 6"
        onWorkflowDispatch
        dotNetJob "publish" [
            strategy(failFast = false, matrix = [
                "image", images
            ])
            runsOn "${{ matrix.image }}"

            cacheHaskellBuildOutput "release"
            refreshHaskellBuildOutputTimestamps
            yield! setUpHaskellEnvironment "release"

            step(
                id = "version",
                name = "Get version",
                shell = "pwsh",
                run = "echo \"version=$(scripts/Get-Version.ps1 -RefName $env:GITHUB_REF)\" >> $env:GITHUB_OUTPUT"
            )
            yield! setUpSkiaSharpWorkarounds

            step(
                name = "Build the project",
                shell = "pwsh",
                run = "dotnet build --configuration Release -p:Version=${{ steps.version.outputs.version }}"
            )
            step(
                name = "Publish the project",
                shell = "pwsh",
                run = "dotnet publish --configuration Release --self-contained -p:Version=${{ steps.version.outputs.version }}"
            )
            step(
                name = "Pack the publication result",
                shell = "pwsh",
                run = "Compress-Archive -Path Tabularius/bin/publish/* -DestinationPath tabularius-${{ steps.version.outputs.version }}-${{ matrix.image }}.zip"
            )
            step(
                name = "Upload the publication result",
                usesSpec = Auto "actions/upload-artifact",
                options = Map.ofList [
                    "name", "tabularius-${{ matrix.image }}"
                    "path", "./tabularius-${{ steps.version.outputs.version }}-${{ matrix.image }}.zip"
                ]
            )
        ]

        dotNetJob "release" [
            needs "publish"
            jobPermission(PermissionKind.Contents, AccessKind.Write)
            runsOn "ubuntu-24.04"
            step(
                id = "version",
                name = "Get version",
                shell = "pwsh",
                run = "echo \"version=$(scripts/Get-Version.ps1 -RefName $env:GITHUB_REF)\" >> $env:GITHUB_OUTPUT"
            )
            step(
                name = "Download artifacts",
                usesSpec = Auto "actions/download-artifact",
                options = Map.ofList [
                    "merge-multiple", "true"
                    "pattern", "tabularius-*"
                ]
            )
            step(
                name = "Read changelog",
                usesSpec = Auto "ForNeVeR/ChangelogAutomation.action",
                options = Map.ofList [
                    "output", "./release-notes.md"
                ]
            )
            step(
                name = "Upload artifacts",
                usesSpec = Auto "actions/upload-artifact",
                options = Map.ofList [
                    "path", "./release-notes.md\n./tabularius-*.zip"
                ]
            )
            step(
                condition = "startsWith(github.ref, 'refs/tags/v')",
                name = "Create a release",
                usesSpec = Auto "softprops/action-gh-release",
                options = Map.ofList [
                    "body_path", "./release-notes.md"
                    "files", "./tabularius-*.zip"
                    "name", "Tabularius v${{ steps.version.outputs.version }}"
                ]
            )
        ]
    ]
]
exit <| EntryPoint.Process fsi.CommandLineArgs workflows
