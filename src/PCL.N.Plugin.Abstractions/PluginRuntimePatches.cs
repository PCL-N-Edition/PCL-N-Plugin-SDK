using System.ComponentModel;
using System.Reflection;

namespace PCL.N.Plugin;

/// <summary>
/// A stable string-based target so plugins do not reference PCL implementation assemblies.
/// </summary>
/// <remarks>
/// Prefer host symbol ids via <see cref="DirectInjector"/> / <see cref="IPluginDirectInjectService"/>.
/// </remarks>
public sealed record PluginRuntimePatchTarget(
    string AssemblyName,
    string TypeName,
    string MethodName,
    IReadOnlyList<string>? ParameterTypeNames = null,
    int? GenericArity = null,
    string? TargetPluginId = null);

/// <summary>
/// Harmony-compatible patch methods. Prefer <see cref="DirectInjector"/> instead.
/// </summary>
[Obsolete("Use DirectInjector (ExampleDI) with IPluginDirectInjectService. This descriptor is bridged to DirectInject.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record PluginRuntimePatchDescriptor
{
    public required string PatchId { get; init; }

    public required PluginRuntimePatchTarget Target { get; init; }

    public MethodInfo? Prefix { get; init; }

    public MethodInfo? Postfix { get; init; }

    public MethodInfo? Transpiler { get; init; }

    public MethodInfo? Finalizer { get; init; }

    public int Priority { get; init; }

    public IReadOnlyList<string> Before { get; init; } = [];

    public IReadOnlyList<string> After { get; init; } = [];

    /// <summary>Convert to the DirectInject descriptor used by the current runtime.</summary>
    public PluginDirectInjectDescriptor ToDirectInject() =>
        new()
        {
            InjectId = PatchId,
            RawTarget = Target,
            Prefix = Prefix,
            Postfix = Postfix,
            Transpiler = Transpiler,
            Finalizer = Finalizer,
            Priority = Priority,
            Before = Before,
            After = After
        };
}

/// <summary>Legacy patch info. Prefer <see cref="PluginDirectInjectInfo"/>.</summary>
[Obsolete("Use PluginDirectInjectInfo / IPluginDirectInjectService.ListOwned(). Bridged from DirectInject.")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record PluginRuntimePatchInfo(
    string GlobalPatchId,
    PluginRuntimePatchTarget Target,
    bool HasPrefix,
    bool HasPostfix,
    bool HasTranspiler,
    bool HasFinalizer,
    int Priority);

/// <summary>
/// Legacy Mixin-style runtime patching API.
/// </summary>
/// <remarks>
/// Deprecated in favor of <see cref="DirectInjector"/> + <see cref="IPluginDirectInjectService"/>.
/// Existing <see cref="Register"/> calls are bridged to DirectInject.
/// </remarks>
[Obsolete("Use DirectInjector (class Mixin / ExampleDI) with IPluginDirectInjectService (pcl.direct-inject).")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IPluginRuntimePatchService : IPluginService
{
    IPluginRegistration Register(PluginRuntimePatchDescriptor descriptor);

    IReadOnlyList<PluginRuntimePatchInfo> ListOwned();
}
