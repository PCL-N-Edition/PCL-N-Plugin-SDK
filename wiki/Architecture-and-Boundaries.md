# 架构与边界

> Applies to PCL N Plugin SDK 0.2.5.

> SDK `0.2.5` · 产品运行时：PCL N 桌面（AOT）+ **PCL.Plugin.Sidecar**（CoreCLR）

```text
PCL N 桌面应用（AOT 宿主，不加载第三方插件 IL）
  └─ PluginSidecarSupervisor / 长度前缀 JSON IPC
        └─ PCL.Plugin.Sidecar（CoreCLR 进程）
              ├─ 验证并安装 .pnp
              ├─ 提供稳定服务与 UI data-chain
              ├─ 管理依赖、生命周期、安全模式和卸载
              └─ 共享 PCL.N.Plugin.Abstractions ABI
                    └─ 第三方插件（Sidecar 内可卸载 AssemblyLoadContext）
```

## 三层职责

| 层 | 职责 | 第三方能否引用 |
|---|---|---:|
| PCL N Desktop | 启动器核心 UI、Minecraft 功能、通用 UI 渲染、Sidecar 监督 | 否 |
| PCL.Plugin / Sidecar | 插件加载、安全、市场、恢复、宿主桥接、UI 清单/页面树 | 否 |
| PCLN.Plugin SDK | 公共 ABI、开发工具、Analyzer、测试与打包 | 是 |

`PCL.Plugin` 私有不影响开发：它实现公开接口，但内部类型、目录结构和服务容器可以自由重构。插件只面对稳定契约（`PCLN.Plugin.Abstractions` 等 NuGet）。

## 产品运行时形态（必读）

1. **宿主默认 AOT**：发布包中的 `PCL-N-Edition` 不嵌入第三方插件 IL。  
2. **插件平台在 Sidecar**：`PCL.Plugin.Sidecar` 由宿主启动，经 IPC 提供目录/安装/UI 数据链/反馈等。  
3. **插件 ABI 不变**：你仍实现 `IPclNPlugin`、调用 `context.Services`；**不要**引用 `PCL.Desktop` 或 `PCL.Plugin`。  
4. **UI**：设置/插件页由 Sidecar 推送 **data-chain**（manifest / page tree / actions），宿主只做通用渲染与导航注入，而不是在宿主源码中写死插件页面。  
5. **遗留进程内源码叠加**（`PclWithPlugin`）仅用于调试/特殊构建，**不是**商店分发形态。  
6. **PCL.Plugin 产品包（v0.20+）**：Sidecar Release **混淆**、**无 PDB**、**不附带 host 符号表**；DirectInject 用 `Target(assembly,type,method)`，勿依赖嵌入符号表。

主机仓库架构说明（实现细节）：

- `docs/architecture/plugin-sidecar-ipc.md`
- `docs/architecture/native-aot-desktop.md`

## 禁止的依赖

- `PCL.Application`
- `PCL.Desktop`
- `PCL.Plugin`
- 启动器内部 DI/服务容器
- 私有 CLR 命名空间或反射访问

对应 Analyzer PNPSDK001–003 和 PNPSDK006 会在编译期提示。

## 允许的交互方式

- `IPluginContext` 与稳定 `IPluginService`；
- 公开 DTO，例如 `PluginInstanceInfo`；
- Capability，例如设置页注册；
- Manifest 声明的服务、权限、依赖和 UI；
- 宿主发布的 Surface/Slot；
- `context.Directories` 提供的插件私有目录；
- `IPluginPackageAssetService` 解析已安装签名包内的只读资源（对照文件表校验）。

## 加载隔离

在 **Sidecar 进程内**，每个插件使用可回收的 AssemblyLoadContext：

- Abstractions 从默认上下文共享，保证 ABI 类型身份一致；
- 插件私有托管依赖从自身包解析；
- 原生依赖从 `runtimes/<rid>/native/` 探测；
- 停用时释放注册、清理引用并尝试卸载。

隔离加载上下文不是安全沙箱。插件仍能调用 .NET 和操作系统 API（在 Sidecar 进程权限范围内），因此签名、权限、审核和用户信任都不可省略。

## 为什么使用声明式 UI

直接操作 Avalonia Visual Tree 会把插件绑定到控件实现。Surface/Slot 与 Sidecar data-chain 把契约缩小为稳定 ID、版本范围与数据节点；AXAML 只描述视觉内容，行为通过命令连接。这样宿主可以验证权限、解决冲突、应用安全模式并在卸载时撤销贡献。
