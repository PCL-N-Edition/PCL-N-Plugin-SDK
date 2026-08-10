using System.Reflection;

namespace PCL.N.Plugin;

/// <summary>
/// Stable host method identity used by DirectInject (Mixin-style launcher injection).
/// Prefer <see cref="Id"/> over raw assembly/type/method strings so AOT renames and
/// overloads can be tracked via the host symbol table shipped with PCL.Plugin.
/// </summary>
public sealed record PluginHostSymbol(
    string Id,
    string AssemblyName,
    string TypeName,
    string MethodName,
    IReadOnlyList<string> ParameterTypeNames,
    int? GenericArity = null,
    string? Kind = "method",
    string? Notes = null);

/// <summary>Read-only view of the host symbol table embedded / loaded by PCL.Plugin.</summary>
public interface IPluginHostSymbolTable
{
    /// <summary>Symbol table format version (currently 1).</summary>
    int FormatVersion { get; }

    /// <summary>Host product version stamp used when the table was generated.</summary>
    string? HostVersion { get; }

    /// <summary>All symbols known to the current runtime.</summary>
    IReadOnlyList<PluginHostSymbol> All { get; }

    bool TryGet(string symbolId, out PluginHostSymbol symbol);

    PluginHostSymbol GetRequired(string symbolId);
}

/// <summary>
/// Low-level DirectInject descriptor. Prefer subclassing <see cref="DirectInjector"/>
/// (<c>ExampleDI</c>) instead of building descriptors by hand.
/// </summary>
public sealed record PluginDirectInjectDescriptor
{
    public required string InjectId { get; init; }

    /// <summary>
    /// Stable id from the host symbol table, e.g.
    /// <c>pcl.application.downloads.version_resolver.resolve_async</c>.
    /// Required unless <see cref="RawTarget"/> is set.
    /// </summary>
    public string? SymbolId { get; init; }

    /// <summary>Optional raw target when the symbol table has no entry yet (debug only).</summary>
    public PluginRuntimePatchTarget? RawTarget { get; init; }

    public MethodInfo? Prefix { get; init; }

    public MethodInfo? Postfix { get; init; }

    public MethodInfo? Transpiler { get; init; }

    public MethodInfo? Finalizer { get; init; }

    public int Priority { get; init; }

    public IReadOnlyList<string> Before { get; init; } = [];

    public IReadOnlyList<string> After { get; init; } = [];
}

public sealed record PluginDirectInjectInfo(
    string GlobalInjectId,
    string? SymbolId,
    PluginRuntimePatchTarget Target,
    bool HasPrefix,
    bool HasPostfix,
    bool HasTranspiler,
    bool HasFinalizer,
    int Priority);
