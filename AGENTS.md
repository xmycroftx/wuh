# AGENTS.md — wuh modernization

**Project:** WUH (Windows Update Helper) — a Windows CLI wrapping the WUApiLib COM
(Windows Update Agent) API. The single compiled source is `wuh/wuh.cs`
(`wuh/Program.cs` is NOT in the build — ignore it). Builds only on Windows.

## Goal: bring it up to date — .NET 8 + System.CommandLine 2.0 + System.Text.Json

Preserve **all existing CLI behavior and output**. Do not change what commands do,
only how the code is structured/parsed.

### 1. Project → SDK-style .NET 8
- Rewrite `wuh/wuh.csproj` as an **SDK-style** project: `<Project Sdk="Microsoft.NET.Sdk">`.
- `<TargetFramework>net8.0-windows</TargetFramework>`, `<OutputType>Exe</OutputType>`,
  `<Nullable>enable</Nullable>`, `<UseWindowsForms>true</UseWindowsForms>` (code uses
  WinForms `NotifyIcon`/`Form`/`Label` and `System.Drawing`).
- Keep the WUApiLib COM interop via `<COMReference Include="WUApiLib">` (same GUID
  `{B596CC9F-56E5-419E-A622-E01BB457431E}`, v2.0, `EmbedInteropTypes=true`).
- Replace `packages.config` with `<PackageReference>`s. Delete the old MSBuild
  imports, signing/manifest/ClickOnce cruft, and `App.config` (net8 doesn't need it).
- `System.CommandLine` as a `<PackageReference>` (latest stable **2.0** — NOT the
  2022 `beta3`). Drop the `System.Buffers`/`System.Memory`/etc. shims (net8 built-in).

### 2. Argument parsing → System.CommandLine 2.0
- Replace the hand-rolled `foreach (Object obj in args)` + `obj.ToString().Contains(...)`
  parsing in `Main` (it's buggy — substring matching cross-triggers options).
- Model it with a `RootCommand` + subcommands **install / show-available /
  show-updated / show-pending** and boolean `Option`s: `--download`, `--all`,
  `--enable-hidden`, `--enable-previews`, `--enable-cumulative`, `--json`,
  `--enable-optional`, `--enable-assigned`, `--security-only`. `help` is built in.
- Keep the same guard: cannot combine a show-* action with install/download.
- Use the **current** System.CommandLine API (`SetAction`/`ParseResult.GetValue`,
  `rootCommand.Parse(args).Invoke()`), not the old `beta3` `Handler.SetHandler` API.

### 3. JSON → System.Text.Json
- The `showUpdates` path hand-concatenates a `jsonAllUpdates` string (fragile, no
  escaping). Replace with `System.Text.Json` (`JsonSerializer` over a small DTO, or
  `JsonObject`/`Utf8JsonWriter`). Keep the same shape: `{"windowsUpdates": { "<id>":
  {"Result":..,"Title":..,"Date":..}, ... }}`.

### 4. Fix the broken history / `show-updated` path (`showUpdates`)
- Line ~106 `if (history[i].HResult == 0 || true)` — the `|| true` makes it always
  true, disabling the HResult filter. Remove the dead `|| true`.
- The comma logic `if (i != count - 1) jsonAllUpdates += ","` emits **invalid JSON**
  when entries are skipped (the KB2267602 filter). The System.Text.Json rewrite
  (section 3) fixes this structurally — don't hand-place commas.
- Line ~127 `txtPendingUpdates += txtPendingUpdates += "\t" + ...` is a
  **double-assignment bug** (appends the buffer to itself). Should be a single
  `txtPendingUpdates += "\t" + history[i].Title + "\n";`.
- The extra `++afterFilter;` after the loop (line ~141) is an off-by-one; drop it.

### 5. Audit the nested loops
- `installMatching` and `installDownloaded` contain **near-duplicate** update-
  classification foreach loops. Extract the shared "should this update be included?"
  logic into one helper (e.g. `bool ShouldInclude(IUpdate u, ...flags)`), and one
  shared download/install routine. Keep behavior identical.
- `installDownloaded` references **undeclared** `uSearcher`, `updatesToInstall`,
  `uSession` — it won't compile. Give it its own locals (mirror `installMatching`).

### Conventions
- C#: 4-space indent, `PascalCase` methods, braces on their own lines.
- Don't invent new behavior, flags, or output text. Fix bugs only where they block
  the migration (e.g. undeclared fields in `installMatching`/`installDownloaded` —
  make them proper params/locals; don't rewrite the update logic).
- **You can't build here (no Windows/COM).** Prefer correct, minimal, compiling-shaped
  changes; call out anything you're unsure compiles.
