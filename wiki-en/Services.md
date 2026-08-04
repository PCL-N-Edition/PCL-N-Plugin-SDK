# Services

> Applies to PCL N Plugin SDK 0.2.5 · Product host: **PCL.Plugin.Sidecar** (CoreCLR) + AOT desktop.

Plugins resolve host services from `context.Services`. Put hard requirements in Manifest `services.required`; put optional enhancements in `services.optional`.

## Runtime note

Services below are provided **inside the Sidecar process** after the host starts `PCL.Plugin.Sidecar` over IPC. The AOT desktop shell does not load third-party plugin IL. See [Architecture and Boundaries](Architecture-and-Boundaries).

## Core host services

| ID | C# interface | Purpose |
|---|---|---|
| `pcl.logging` | `IPluginLogger` | Structured plugin logs (`context.Logger`) |
| `pcl.dispatcher` | `IPluginDispatcher` | UI/main-thread work (`context.Dispatcher`) |
| `pcl.notifications` | `IPluginNotificationService` | Info / warning toasts |
| `pcl.settings` | `IPluginSettingsStore` | Per-plugin key/value settings |
| `pcl.commands` | `IPluginCommandService` | Register and invoke commands |
| `pcl.tasks` | `IPluginTaskService` | Lifetime-tracked background work |
| `pcl.instances.read` | `IPluginInstanceReadService` | Read-only Minecraft instance list |
| `pcl.localization` | `IPluginLocalizationService` | Host culture + plugin strings |
| `pcl.secure-storage` | `IPluginSecureStorage` | Isolated OS credential storage |
| `pcl.uri-launcher` | `IPluginUriLauncher` | Open http(s) via host |
| `pcl.background-tasks` | `IPluginBackgroundTaskService` | Task-manager progress UI |
| `pcl.package-assets` | `IPluginPackageAssetService` | Read-only files from the **installed signed package** |

### Package assets vs private files

- `pcl.files` (`IPluginFileService`) — read/write the plugin **private data directory**.  
- `pcl.package-assets` (`IPluginPackageAssetService`) — resolve paths listed in `META-INF/pnp.files.json`, verify size + SHA-256, return absolute path.

```csharp
IPluginPackageAssetService packages = context.Services.Require<IPluginPackageAssetService>();
PluginPackageAssetResult result = await packages.ResolveAsync("assets/template.json", cancellationToken);
if (result.IsSuccess)
    context.Logger.Info(result.Asset!.FullPath);
```

## Game, process, files, accounts

| ID | C# interface | Notes |
|---|---|---|
| `pcl.game.sessions` | `IPluginGameSessionService` | Requires `launch.observe` |
| `pcl.game.output` | `IPluginGameOutputService` | Requires `game.output.read` |
| `pcl.launch.events` | `IPluginLaunchEventService` | Requires `launch.observe` |
| `pcl.process` | `IPluginProcessService` | Requires `process.start` (+ `process.output` for streams) |
| `pcl.clipboard` | `IPluginClipboardService` | `clipboard.read` / `clipboard.write` |
| `pcl.files` | `IPluginFileService` | Private data directory only |
| `pcl.accounts.read` | `IPluginAccountReadService` | Requires `account.read` |
| `pcl.downloads` | `IPluginDownloadService` | Source catalog (read-only) |
| `pcl.launch.modify` | `IPluginLaunchModificationService` | Requires `launch.modify` |

## UI and navigation

| ID | C# interface | Package |
|---|---|---|
| `pcl.ui` | `IPluginUiSurfaceRegistry` | Abstractions |
| `pcl.ui.patch` | `IPluginUiPatchService` | Abstractions |
| `pcl.navigation` | `IPluginNavigationService` | `PCLN.Plugin.UI` |
| `pcl.ui.avalonia` | `IAvaloniaUiAccessService` | `PCLN.Plugin.UI.Avalonia` |
| `pcl.ui.avalonia.pages` | `IAvaloniaPluginPageService` | `PCLN.Plugin.UI.Avalonia` |
| `pcl.ui.avalonia.windows` | `IAvaloniaPluginWindowService` | `PCLN.Plugin.UI.Avalonia` |

IDs for UI adapters live in `PluginUiServiceIds`. Prefer declarative surfaces/slots; treat raw Avalonia as opt-in with `ui.raw-access`.

## Registry, runtime patches, exports, market

| ID | C# interface | Purpose |
|---|---|---|
| `pcl.registry` | `IPluginRegistryService` | ACL-protected extension registry |
| `pcl.runtime-patches` | `IPluginRuntimePatchService` | Trusted method patches |
| `pcl.exports` | `IPluginExportRegistry` | Cross-plugin stable contracts |
| `pcl.market` | `IPluginMarketClient` (host-internal) | Online market HTTP client |

**Market:** Sidecar uses `IPluginMarketClient` internally for browse/download/verify. The stable ID `pcl.market` is reserved; **third-party plugins must not assume** a remote market client is injected into `context.Services` today. See [Registry and Runtime Patches](Registry-and-Runtime-Patches) for registry/patch permissions and examples.

## Capability negotiation

Presence of a NuGet interface does not mean every host build exposes it. Always negotiate:

```csharp
PluginApiVersionRange range = PluginApiVersionRange.Parse(">=0.1 <1.0");
if (context.Services.Supports(PluginServiceIds.PackageAssets, range) &&
    context.Services.TryGet<IPluginPackageAssetService>(out var packages))
{
    // ...
}
```

For longer Chinese examples and cookbook snippets, see the Chinese Services guide; contracts are identical.
