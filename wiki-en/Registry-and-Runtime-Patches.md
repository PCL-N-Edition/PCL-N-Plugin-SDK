# Registry and Runtime Patches

> Applies to PCL N Plugin SDK `0.2.5+` and PCL.Plugin **`v0.20.0+`**.

The PCL registry is a composable extension registry, not the Windows system Registry. Plugins publish immutable JSON nodes, observe changes, and may add reversible overrides to another plugin only when both the effective permission and the target ACL allow it. Every mutation returns a lifetime-owned registration.

## Permissions and namespaces

- `registry.read`: read visible nodes;
- `registry.write-own`: register below `plugins.<own-plugin-id>`;
- `registry.cross-plugin`: request a cross-plugin write, still subject to the target ACL;
- `registry.manage-acl`: manage ACLs on owned nodes;
- `runtime.patch.host`: patch PCL N host methods (DirectInject);
- `runtime.patch.plugins`: patch another third-party plugin;
- `runtime.patch.transpiler`: register an IL transpiler;
- `launch.modify`: IndirectInject launch-arg transforms.

`pcl.plugin`, `pcl.security`, signature verification, and permission enforcement are permanently protected targets. No public permission grants write, remove, override, or patch access to them.

## Registering a node

```csharp
using System.Text.Json;
using PCL.N.Plugin;

IPluginRegistryService registry = context.Services.Require<IPluginRegistryService>();
using JsonDocument json = JsonDocument.Parse("""{"enabled":true,"port":25565}""");
IPluginRegistryRegistration node = registry.Register(
    new PluginRegistryNodeDescriptor(
        "plugins.dev.example.terracotta.sessions.local",
        json.RootElement.Clone(),
        [new PluginRegistryAccessRule("dev.example.overlay", PluginRegistryRights.Read)]));
context.Lifetime.Track(node);
```

Use `node.Update(nextValue)` for live state. `Override(path, value, priority)` adds a reversible composition layer and never transfers ownership of the target node.

## DirectInject (class Mixin) and IndirectInject (API modification)

| Capability | Base class | Example name | Service |
|------------|------------|--------------|---------|
| Class Mixin (Harmony) | `DirectInjector` | `ExampleDI` | `pcl.direct-inject` / `IPluginDirectInjectService` |
| API modification (launch args, …) | `IndirectInjector` | `ExampleII` | `pcl.indirect-inject` / `IPluginIndirectInjectService` |

**Obsolete (bridged):**

- `IPluginRuntimePatchService` → DirectInject
- `IPluginLaunchModificationService` → IndirectInject

### ExampleDI

```csharp
public sealed class ExampleDI : DirectInjector
{
    public override string InjectorId => "example.di";

    protected override void Configure(DirectInjectBuilder builder)
    {
        // Product builds do NOT ship a host symbol table — use Target(...).
        builder
            .Target("PCL.Application", "PCL.Application.Launching.AuthlibInjectorService",
                "NormalizeAuthServer", ["System.String"])
            .Postfix(typeof(ExampleDI), nameof(AfterNormalize))
            .Done();
    }

    private static void AfterNormalize(ref string __result) { }
}
```

### ExampleII

```csharp
public sealed class ExampleII : IndirectInjector
{
    public override string InjectorId => "example.ii";

    public override void Apply(IIndirectInjectContext context)
    {
        context.ModifyLaunch("example-ii", request => request with
        {
            GameArguments = request.GameArguments.Append("--example-ii").ToArray()
        });
    }
}
```

### Product packaging (PCL.Plugin v0.20+)

| Policy | Detail |
|--------|--------|
| No host symbol table | Not embedded; do not rely on `TargetSymbol` in production |
| No PDBs | Release `DebugType=none` |
| Sidecar obfuscation | Release publish runs Obfuscar on `PCL.Plugin*.dll` (public `PCL.N.Plugin.*` kept) |
| Local symbols only | Optional `PCLN_HOST_SYMBOLS_PATH` for private debug JSON |

Under AOT product layout, plugins run in the CoreCLR sidecar. DirectInject can patch host-managed assemblies loaded there; it cannot rewrite native AOT code in the desktop process.

Runtime patch permissions are the highest review tier. Packages must use the host service; bundling an independent patch engine or targeting protected platform code is rejected by market review.
