# Architecture and Boundaries

> Applies to PCL N Plugin SDK 0.2.5.

> SDK `0.2.5` · Product runtime: PCL N Desktop (AOT) + **PCL.Plugin.Sidecar** (CoreCLR)

```text
PCL N Desktop (AOT host — no third-party plugin IL)
  └─ PluginSidecarSupervisor / length-prefixed JSON IPC
        └─ PCL.Plugin.Sidecar (CoreCLR process)
              ├─ Validate and install .pnp packages
              ├─ Stable services + UI data-chain
              ├─ Dependencies, lifecycle, safe mode, uninstall
              └─ Shared PCL.N.Plugin.Abstractions ABI
                    └─ Third-party plugins (unloadable ALC inside Sidecar)
```

## Layers

| Layer | Responsibility | Third-party reference? |
|---|---|---:|
| PCL N Desktop | Launcher UI, Minecraft features, generic UI render, sidecar supervision | No |
| PCL.Plugin / Sidecar | Load, security, market, recovery, host bridges, UI manifest/trees | No |
| PCLN.Plugin SDK | Public ABI, tooling, analyzers, tests, packaging | Yes |

`PCL.Plugin` is private. Plugins only depend on public NuGet packages (`PCLN.Plugin.Abstractions`, …).

## Product runtime (required reading)

1. **Host is AOT by default** — release builds of `PCL-N-Edition` do not embed third-party plugin IL.  
2. **Plugin platform runs in Sidecar** — `PCL.Plugin.Sidecar` is supervised by the host over IPC (catalog, install, UI data-chain, feedback, …).  
3. **Plugin ABI is unchanged** — implement `IPclNPlugin` and use `context.Services`; never reference `PCL.Desktop` or `PCL.Plugin`.  
4. **UI** — settings/plugin pages are driven by Sidecar **data-chain** (manifest / page tree / actions); the host injects navigation and renders generically.  
5. **Legacy in-process overlay** (`PclWithPlugin`) is for debug/special builds only, not store distribution.

Host implementation notes live in the main repository:

- `docs/architecture/plugin-sidecar-ipc.md`
- `docs/architecture/native-aot-desktop.md`

## Forbidden dependencies

- `PCL.Application`
- `PCL.Desktop`
- `PCL.Plugin`
- Launcher private DI containers
- Private CLR namespaces or reflection into host internals

Analyzers PNPSDK001–003 and PNPSDK006 enforce this at compile time.

## Allowed interaction

- `IPluginContext` and stable `IPluginService` IDs  
- Public DTOs such as `PluginInstanceInfo`  
- Capabilities (for example settings pages)  
- Manifest-declared services, permissions, dependencies, and UI  
- Host-published surfaces/slots  
- Plugin private directories from `context.Directories`  
- `IPluginPackageAssetService` for read-only files from the installed signed package (file-table + SHA-256)

## Load isolation

Inside the **Sidecar process**, each plugin uses a collectible `AssemblyLoadContext`:

- Abstractions are shared from the default context so ABI type identity matches  
- Private managed dependencies resolve from the plugin package  
- Native libraries probe `runtimes/<rid>/native/`  
- Stop releases registrations and attempts unload  

ALC isolation is not a security sandbox. Signing, permissions, review, and user trust remain mandatory.

## Why declarative UI

Direct Avalonia visual-tree mutation couples plugins to control internals. Surfaces/slots and the Sidecar data-chain keep contracts as stable IDs, version ranges, and data nodes so the host can enforce permissions, resolve conflicts, apply safe mode, and revoke contributions on unload.
