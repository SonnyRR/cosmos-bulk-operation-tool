using System;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace Cosmos.BulkOperation.CLI;

/// <inheritdoc/>
public class SpectreCliTypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection builder;

    /// <inheritdoc/>
    public SpectreCliTypeRegistrar(IServiceCollection builder)
        => this.builder = builder;

    /// <inheritdoc/>
    public ITypeResolver Build() => new SpectreCliTypeResolver(this.builder.BuildServiceProvider());

    /// <inheritdoc/>
    public void Register(Type service, Type implementation)
        => this.builder.AddSingleton(service, implementation);

    /// <inheritdoc/>
    public void RegisterInstance(Type service, object implementation)
        => this.builder.AddSingleton(service, implementation);

    /// <inheritdoc/>
    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        this.builder.AddSingleton(service, _ => factory());
    }

    /// <inheritdoc/>
    public void RegisterType(Type service, Type implementation)
        => this.builder.AddTransient(service, implementation);

    /// <inheritdoc/>
    public void Add(Type service, Type implementation)
        => this.builder.AddTransient(service, implementation);
}

/// <inheritdoc/>
internal sealed class SpectreCliTypeResolver : ITypeResolver
{
    private readonly IServiceProvider provider;

    public SpectreCliTypeResolver(IServiceProvider provider)
        => this.provider = provider ?? throw new ArgumentNullException(nameof(provider));

    /// <inheritdoc/>
    public object Resolve(Type type) => this.provider.GetService(type);
}
