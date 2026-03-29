<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Contributor Guide
=================

Prerequisites
-------------
To work with the project, you'll need:
- [.NET SDK 9][dotnet-sdk] or later,
- [Haskell Stack][haskell-stack] 3.x or later,
- **(Linux only)** Clang — install via your package manager (e.g. `sudo apt install clang`).

  This is required because stack might have problems installing without clang. Last observed issue:

  > ```
  > gcc : error : unrecognized command-line option '--target=x86_64-unknown-linux'
  > ```

Build
-----
<!-- TODO[#40]: Get rid of this block in the future, after some future upgrade of Svg.Skia.Converter incorporating an update of SkiaSharp. -->
On AArch64 Linux, you might need to set up the following environment if you experience the issue [SkiaSharp#3272][skia-sharp.3272]:

```
LD_PRELOAD=/lib/aarch64-linux-gnu/libuuid.so.1:/lib/aarch64-linux-gnu/libfreetype.so.6
```

Use the following shell command:

```console
$ dotnet build
```

Test
----
### HledgerInterop
Use the following shell command:
```console
$ cd HledgerInterop && stack test
```

### Tabularius
Use the following shell command:
```console
$ dotnet test
```

Publish
-------
First, build the application (see the previous section). Then, run the following shell command:
```console
$ dotnet publish --configuration Release --self-contained
```

This will generate the package for the current OS in the `Tabularius/bin/publish` folder.

License Automation
------------------
If the CI asks you to update the file licenses, follow one of these:
1. Update the headers manually (look at the existing files), something like this:
   ```fsharp
   // SPDX-FileCopyrightText: %year% %your name% <%your contact info, e.g. email%>
   //
   // SPDX-License-Identifier: MIT
   ```
   (accommodate to the file's comment style if required).
2. Alternately, use [REUSE][reuse] tool:
   ```console
   $ reuse annotate --license MIT --copyright '%your name% <%your contact info, e.g. email%>' %file names to annotate%
   ```

(Feel free to attribute the changes to "Tabularius contributors <https://github.com/ForNeVeR/Tabularius>" instead of your name in a multi-author file, or if you don't want your name to be mentioned in the project's source: this doesn't mean you'll lose the copyright.)

File Encoding Changes
---------------------
If the automation asks you to update the file encoding (line endings or UTF-8 BOM) in certain files, run the following PowerShell script ([PowerShell Core][powershell] is recommended to run this script):
```console
$ pwsh -c "Install-Module VerifyEncoding -Repository PSGallery -RequiredVersion 2.3.0 -Force && Test-Encoding -ExcludePatterns '*.Designer.cs' -AutoFix"
```

The `-AutoFix` switch will automatically fix the encoding issues, and you'll only need to commit and push the changes.

GitHub Actions
--------------
If you want to update the GitHub Actions used in the project, edit the file that generated them: `scripts/github-actions.fsx`.

Then run the following shell command:
```console
$ dotnet fsi scripts/github-actions.fsx
```

[dotnet-sdk]: https://dotnet.microsoft.com/en-us/download
[haskell-stack]: https://docs.haskellstack.org/en/stable/install_and_upgrade/
[powershell]: https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell
[reuse]: https://reuse.software/
[skia-sharp.3272]: https://github.com/mono/SkiaSharp/issues/3272
