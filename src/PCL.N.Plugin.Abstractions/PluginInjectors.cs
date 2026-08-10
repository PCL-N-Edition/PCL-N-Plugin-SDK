using System.ComponentModel;
using System.Reflection;

namespace PCL.N.Plugin;

/// <summary>
/// Kind of Harmony-style method hook used by <see cref="DirectInjector"/>.
/// </summary>
public enum DirectInjectKind
{
    Prefix,
    Postfix,
    Transpiler,
    Finalizer
}

/// <summary>
/// Marks a static method on a <see cref="DirectInjector"/> subclass as a Mixin-style hook.
/// Prefer <see cref="SymbolId"/> (host symbol table) over raw assembly/type/method names.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class DirectInjectAttribute : Attribute
{
    /// <summary>Stable id from the host symbol table (recommended under AOT packaging).</summary>
    public string? SymbolId { get; init; }

    public string? Assembly { get; init; }

    public string? Type { get; init; }

    public string? Method { get; init; }

    /// <summary>Optional parameter type full names to disambiguate overloads.</summary>
    public string[]? ParameterTypeNames { get; init; }

    public DirectInjectKind Kind { get; init; } = DirectInjectKind.Prefix;

    public int Priority { get; init; }

    public string? TargetPluginId { get; init; }
}

/// <summary>
/// Fluent builder used by <see cref="DirectInjector.Configure"/>.
/// </summary>
public sealed class DirectInjectBuilder
{
    private readonly string _injectorId;
    private readonly int _defaultPriority;
    private readonly List<PluginDirectInjectDescriptor> _descriptors = [];
    private DirectInjectBindingBuilder? _pending;

    internal DirectInjectBuilder(string injectorId, int defaultPriority)
    {
        _injectorId = injectorId;
        _defaultPriority = defaultPriority;
    }

    internal IReadOnlyList<PluginDirectInjectDescriptor> Descriptors
    {
        get
        {
            FlushPending();
            return _descriptors;
        }
    }

    public DirectInjectBindingBuilder TargetSymbol(string symbolId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbolId);
        FlushPending();
        _pending = new DirectInjectBindingBuilder(this, symbolId.Trim(), rawTarget: null);
        return _pending;
    }

    public DirectInjectBindingBuilder Target(
        string assemblyName,
        string typeName,
        string methodName,
        IReadOnlyList<string>? parameterTypeNames = null,
        string? targetPluginId = null)
    {
        FlushPending();
        _pending = new DirectInjectBindingBuilder(
            this,
            symbolId: null,
            new PluginRuntimePatchTarget(
                assemblyName,
                typeName,
                methodName,
                parameterTypeNames,
                TargetPluginId: targetPluginId));
        return _pending;
    }

    internal void Add(PluginDirectInjectDescriptor descriptor) => _descriptors.Add(descriptor);

    internal void ClearPending(DirectInjectBindingBuilder binding)
    {
        if (ReferenceEquals(_pending, binding))
            _pending = null;
    }

    internal void FlushPending()
    {
        if (_pending is null)
            return;
        DirectInjectBindingBuilder pending = _pending;
        _pending = null;
        pending.Commit();
    }

    internal string NextId(string suffix) =>
        _injectorId + "." + suffix + "." + (_descriptors.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

    internal int DefaultPriority => _defaultPriority;
}

/// <summary>Binds Prefix / Postfix / Transpiler / Finalizer for one DirectInject target.</summary>
public sealed class DirectInjectBindingBuilder
{
    private readonly DirectInjectBuilder _owner;
    private readonly string? _symbolId;
    private readonly PluginRuntimePatchTarget? _rawTarget;
    private MethodInfo? _prefix;
    private MethodInfo? _postfix;
    private MethodInfo? _transpiler;
    private MethodInfo? _finalizer;
    private int _priority;
    private bool _prioritySet;

    internal DirectInjectBindingBuilder(
        DirectInjectBuilder owner,
        string? symbolId,
        PluginRuntimePatchTarget? rawTarget)
    {
        _owner = owner;
        _symbolId = symbolId;
        _rawTarget = rawTarget;
        _priority = owner.DefaultPriority;
    }

    public DirectInjectBindingBuilder Priority(int priority)
    {
        _priority = priority;
        _prioritySet = true;
        return this;
    }

    public DirectInjectBindingBuilder Prefix(MethodInfo method)
    {
        _prefix = method ?? throw new ArgumentNullException(nameof(method));
        return CommitIfComplete();
    }

    public DirectInjectBindingBuilder Prefix(Type declaringType, string methodName) =>
        Prefix(RequireStatic(declaringType, methodName));

    public DirectInjectBindingBuilder Postfix(MethodInfo method)
    {
        _postfix = method ?? throw new ArgumentNullException(nameof(method));
        return CommitIfComplete();
    }

    public DirectInjectBindingBuilder Postfix(Type declaringType, string methodName) =>
        Postfix(RequireStatic(declaringType, methodName));

    public DirectInjectBindingBuilder Transpiler(MethodInfo method)
    {
        _transpiler = method ?? throw new ArgumentNullException(nameof(method));
        return CommitIfComplete();
    }

    public DirectInjectBindingBuilder Transpiler(Type declaringType, string methodName) =>
        Transpiler(RequireStatic(declaringType, methodName));

    public DirectInjectBindingBuilder Finalizer(MethodInfo method)
    {
        _finalizer = method ?? throw new ArgumentNullException(nameof(method));
        return CommitIfComplete();
    }

    public DirectInjectBindingBuilder Finalizer(Type declaringType, string methodName) =>
        Finalizer(RequireStatic(declaringType, methodName));

    /// <summary>Finish the current binding and allow chaining another target.</summary>
    public DirectInjectBuilder Done()
    {
        Commit();
        return _owner;
    }

    private DirectInjectBindingBuilder CommitIfComplete() => this;

    internal void Commit()
    {
        if (_prefix is null && _postfix is null && _transpiler is null && _finalizer is null)
            throw new InvalidOperationException("DirectInject binding requires at least one hook method.");

        _owner.Add(new PluginDirectInjectDescriptor
        {
            InjectId = _owner.NextId("binding"),
            SymbolId = _symbolId,
            RawTarget = _rawTarget,
            Prefix = _prefix,
            Postfix = _postfix,
            Transpiler = _transpiler,
            Finalizer = _finalizer,
            Priority = _prioritySet ? _priority : _owner.DefaultPriority
        });
        _owner.ClearPending(this);
    }

    private static MethodInfo RequireStatic(Type declaringType, string methodName)
    {
        ArgumentNullException.ThrowIfNull(declaringType);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        MethodInfo? method = declaringType.GetMethod(
            methodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (method is null)
            throw new MissingMethodException(declaringType.FullName, methodName);
        if (!method.IsStatic)
            throw new ArgumentException($"DirectInject hook must be static: {declaringType.FullName}.{methodName}");
        return method;
    }
}

/// <summary>
/// Class Mixin injector (DirectInject). Subclass and either override
/// <see cref="Configure"/> or mark static methods with <see cref="DirectInjectAttribute"/>.
/// Example class name convention: <c>ExampleDI</c>.
/// </summary>
public abstract class DirectInjector
{
    /// <summary>Stable injector id (plugin-local unique).</summary>
    public abstract string InjectorId { get; }

    /// <summary>Default Harmony priority for bindings from this injector.</summary>
    public virtual int Priority => 0;

    /// <summary>Optional fluent configuration of targets and hooks.</summary>
    protected virtual void Configure(DirectInjectBuilder builder)
    {
    }

    /// <summary>
    /// Collect all DirectInject descriptors (fluent + attribute-based) and register them.
    /// </summary>
    public IReadOnlyList<IPluginRegistration> Apply(IPluginDirectInjectService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        DirectInjectBuilder builder = new(InjectorId, Priority);
        Configure(builder);
        CollectAttributeBindings(builder);

        List<IPluginRegistration> registrations = [];
        foreach (PluginDirectInjectDescriptor descriptor in builder.Descriptors)
            registrations.Add(service.Inject(descriptor));
        return registrations;
    }

    private void CollectAttributeBindings(DirectInjectBuilder builder)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (MethodInfo method in GetType().GetMethods(flags))
        {
            foreach (DirectInjectAttribute attr in method.GetCustomAttributes<DirectInjectAttribute>(inherit: false))
            {
                if (!method.IsStatic)
                    throw new InvalidOperationException($"DirectInject attribute methods must be static: {method.Name}");

                PluginRuntimePatchTarget? raw = null;
                if (string.IsNullOrWhiteSpace(attr.SymbolId))
                {
                    if (string.IsNullOrWhiteSpace(attr.Assembly) ||
                        string.IsNullOrWhiteSpace(attr.Type) ||
                        string.IsNullOrWhiteSpace(attr.Method))
                    {
                        throw new InvalidOperationException(
                            $"DirectInject on {GetType().Name}.{method.Name} requires SymbolId or Assembly/Type/Method.");
                    }

                    raw = new PluginRuntimePatchTarget(
                        attr.Assembly!,
                        attr.Type!,
                        attr.Method!,
                        attr.ParameterTypeNames,
                        TargetPluginId: attr.TargetPluginId);
                }

                PluginDirectInjectDescriptor descriptor = new()
                {
                    InjectId = builder.NextId(method.Name),
                    SymbolId = attr.SymbolId,
                    RawTarget = raw,
                    Priority = attr.Priority != 0 ? attr.Priority : Priority,
                    Prefix = attr.Kind == DirectInjectKind.Prefix ? method : null,
                    Postfix = attr.Kind == DirectInjectKind.Postfix ? method : null,
                    Transpiler = attr.Kind == DirectInjectKind.Transpiler ? method : null,
                    Finalizer = attr.Kind == DirectInjectKind.Finalizer ? method : null
                };
                builder.Add(descriptor);
            }
        }
    }
}

/// <summary>
/// Context for <see cref="IndirectInjector"/> — host-mediated API surface modifications
/// (launch args, future service decorations). Not Harmony / class Mixin.
/// </summary>
public interface IIndirectInjectContext
{
    IPluginContext Plugin { get; }

    /// <summary>Register a launch-request transform (game/JVM args, environment).</summary>
    IPluginRegistration ModifyLaunch(
        string modificationId,
        Func<PluginLaunchRequest, PluginLaunchRequest> apply);
}

/// <summary>
/// API modification injector (IndirectInject). Subclass and implement <see cref="Apply"/>.
/// Example class name convention: <c>ExampleII</c>.
/// </summary>
public abstract class IndirectInjector
{
    public abstract string InjectorId { get; }

    public virtual int Priority => 0;

    public abstract void Apply(IIndirectInjectContext context);

    public virtual void Revert(IIndirectInjectContext context)
    {
    }

    /// <summary>Convenience: register this injector via the host IndirectInject service.</summary>
    public IReadOnlyList<IPluginRegistration> ApplyTo(
        IPluginIndirectInjectService service,
        IPluginContext pluginContext)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(pluginContext);
        return service.Inject(pluginContext, this);
    }
}

/// <summary>
/// DirectInject host service: Mixin-style class/method injection via host symbol table + Harmony.
/// </summary>
public interface IPluginDirectInjectService : IPluginService
{
    IPluginHostSymbolTable Symbols { get; }

    IPluginRegistration Inject(PluginDirectInjectDescriptor descriptor);

    /// <summary>Apply a <see cref="DirectInjector"/> subclass (fluent + attributes).</summary>
    IReadOnlyList<IPluginRegistration> Inject(DirectInjector injector);

    IReadOnlyList<PluginDirectInjectInfo> ListOwned();
}

/// <summary>
/// IndirectInject host service: API-level injection (launch modifications and related surfaces).
/// </summary>
public interface IPluginIndirectInjectService : IPluginService
{
    /// <summary>Apply an <see cref="IndirectInjector"/> (ExampleII) against the plugin context.</summary>
    IReadOnlyList<IPluginRegistration> Inject(IPluginContext pluginContext, IndirectInjector injector);

    IPluginRegistration ModifyLaunch(
        string modificationId,
        Func<PluginLaunchRequest, PluginLaunchRequest> apply);

    IReadOnlyList<string> ListOwnedIds();
}
