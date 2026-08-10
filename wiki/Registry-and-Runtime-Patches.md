# 注册表与运行时注入

> 适用于 PCL N Plugin SDK `0.2.5+` 与 PCL.Plugin **`v0.20.0+`**。

PCL N 的扩展注册表是可组合注册系统，不是 Windows 系统注册表。它用于让插件发布结构化扩展点、观察变化，并在获得目标 ACL 授权后临时覆写其他插件的注册值。所有改变宿主状态的操作都会返回注册句柄；插件停止时宿主按生命周期自动撤销。

## 权限与命名空间

- `registry.read`：读取公开或 ACL 允许的节点；
- `registry.write-own`：在 `plugins.<自己的插件 ID>` 下注册节点；
- `registry.cross-plugin`：尝试写入其他插件命名空间，仍必须通过目标节点 ACL；
- `registry.manage-acl`：管理自己节点的 ACL；
- `runtime.patch.host`：修改 PCL N 主程序方法；
- `runtime.patch.plugins`：修改其他第三方插件方法；
- `runtime.patch.transpiler`：使用 IL Transpiler。

`pcl.plugin`、`pcl.security`、签名验证和权限判定属于永久保护范围。上述任何权限都不能通过官方 API 修改、删除、覆盖或拦截这些目标。

## 注册节点

```csharp
using System.Text.Json;
using PCL.N.Plugin;

IPluginRegistryService registry = context.Services.Require<IPluginRegistryService>();
using JsonDocument initial = JsonDocument.Parse("""{"enabled":true,"port":25565}""");

IPluginRegistryRegistration registration = registry.Register(
    new PluginRegistryNodeDescriptor(
        "plugins.dev.example.terracotta.sessions.local",
        initial.RootElement.Clone(),
        [new PluginRegistryAccessRule("dev.example.overlay", PluginRegistryRights.Read)]));

context.Lifetime.Track(registration);
```

动态变化使用 `Update`，不要删除后重新注册：

```csharp
using JsonDocument next = JsonDocument.Parse("""{"enabled":true,"port":25566}""");
registration.Update(next.RootElement.Clone());
```

目标所有者可以授予其他插件 `OverrideValue`。调用 `Override` 只增加一个可撤销的合成层，不会夺走节点所有权：

```csharp
using JsonDocument value = JsonDocument.Parse("""{"enabled":false}""");
IPluginRegistration layer = registry.Override(
    "plugins.dev.example.target.feature",
    value.RootElement.Clone(),
    priority: 200);
context.Lifetime.Track(layer);
```

## DirectInject（类 Mixin）与 IndirectInject（API 修改）

| 能力 | 基类 | 范例命名 | 服务 |
|------|------|----------|------|
| 类 Mixin（Harmony Prefix/Postfix/…） | `DirectInjector` | `ExampleDI` | `pcl.direct-inject` / `IPluginDirectInjectService` |
| API 修改（启动参数等） | `IndirectInjector` | `ExampleII` | `pcl.indirect-inject` / `IPluginIndirectInjectService` |

旧 API 已弃用并桥接到新表面：

- `IPluginRuntimePatchService` → DirectInject
- `IPluginLaunchModificationService` → IndirectInject

### DirectInject 范例（`ExampleDI`）

```csharp
using PCL.N.Plugin;

public sealed class ExampleDI : DirectInjector
{
    public override string InjectorId => "example.di";

    protected override void Configure(DirectInjectBuilder builder)
    {
        // 产品包不附带 host 符号表：请用 assembly/type/method。
        builder
            .Target(
                "PCL.Application",
                "PCL.Application.Launching.AuthlibInjectorService",
                "NormalizeAuthServer",
                ["System.String"])
            .Postfix(typeof(ExampleDI), nameof(AfterNormalize))
            .Done();
    }

    private static void AfterNormalize(ref string __result)
    {
        // 只记录必要且脱敏的信息。
    }
}

// 在 IPclNPlugin.InitializeAsync 中：
IPluginDirectInjectService di = context.Services.Require<IPluginDirectInjectService>();
foreach (IPluginRegistration reg in new ExampleDI().Apply(di))
    context.Lifetime.Track(reg);
```

#### 符号表与混淆（PCL.Plugin v0.20+）

| 策略 | 说明 |
|------|------|
| **不附带 host 符号表** | 产品不嵌入 `host-symbols.json`；`TargetSymbol` 在默认空表下会失败 |
| **不附带 PDB** | Release 构建 `DebugType=none` |
| **Sidecar 混淆** | Release publish 对 `PCL.Plugin*.dll` 运行 Obfuscar（公开 `PCL.N.Plugin.*` ABI 保留） |
| **本地调试符号表** | 可选环境变量 `PCLN_HOST_SYMBOLS_PATH` 指向私有 JSON（仅开发） |

AOT 产品下插件在 Sidecar（CoreCLR）中运行；DirectInject 可修补 Sidecar 已加载的托管宿主程序集。无法改写桌面 AOT 进程内已编译为 native 的代码。

### IndirectInject 范例（`ExampleII`）

```csharp
public sealed class ExampleII : IndirectInjector
{
    public override string InjectorId => "example.ii";

    public override void Apply(IIndirectInjectContext context)
    {
        context.ModifyLaunch(
            "example-ii-flag",
            request => request with
            {
                GameArguments = request.GameArguments.Append("--example-ii").ToArray()
            });
    }
}

IPluginIndirectInjectService ii = context.Services.Require<IPluginIndirectInjectService>();
foreach (IPluginRegistration reg in new ExampleII().ApplyTo(ii, context))
    context.Lifetime.Track(reg);
```

补丁必须使用静态方法（DirectInject），不得持有宿主内部对象到插件生命周期之外。目标签名必须尽量精确；重载方法应提供 `ParameterTypeNames`。注册失败时插件必须降级，而不是扫描或猜测其他私有方法。

## 安全边界

插件仍在 PCL N 进程内运行，权限不是操作系统沙箱。运行时补丁属于最高审核等级，必须经过网站签名版本审核、逐项用户授权和崩溃归责。直接捆绑补丁引擎、绕过服务或访问保护目标的包会被市场拒绝。
