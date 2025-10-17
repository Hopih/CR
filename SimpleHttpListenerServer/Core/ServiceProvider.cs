using Microsoft.Extensions.DependencyInjection;

namespace SimpleHttpListenerServer.Core;

public enum ServiceLifetime
{
    Singleton,
    Transient
}

/// <summary>
/// Описание сервиса для DI-контейнера.
/// </summary>
public sealed class ServiceDescriptor
{
    public Type ServiceType { get; init; } = default!;
    public Type? ImplementationType { get; init; }
    public object? ImplementationInstance { get; init; }
    public ServiceLifetime Lifetime { get; init; }
    public Func<IServiceProvider, object>? ImplementationFactory { get; init; }
}

/// <summary>
/// Коллекция регистраций (аналог IServiceCollection).
/// </summary>
public sealed class ServiceCollection
{
    private readonly List<ServiceDescriptor> _descriptors = new();

    public ServiceCollection AddSingleton<TService, TImpl>()
        where TImpl : class, TService
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImpl),
            Lifetime = ServiceLifetime.Singleton
        });
        return this;
    }

    public ServiceCollection AddSingleton<TService>(TService instance)
        where TService : class
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationInstance = instance!,
            Lifetime = ServiceLifetime.Singleton
        });
        return this;
    }

    public ServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationFactory = sp => factory(sp),
            Lifetime = ServiceLifetime.Singleton
        });
        return this;
    }

    public ServiceCollection AddTransient<TService, TImpl>()
        where TImpl : class, TService
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImpl),
            Lifetime = ServiceLifetime.Transient
        });
        return this;
    }

    public ServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationFactory = sp => factory(sp),
            Lifetime = ServiceLifetime.Transient
        });
        return this;
    }
    
    public ServiceCollection AddSingleton<TImpl>() where TImpl : class
    {
        _descriptors.Add(new ServiceDescriptor
        {
            ServiceType = typeof(TImpl),
            ImplementationType = typeof(TImpl),
            Lifetime = ServiceLifetime.Singleton
        });
        return this;
    }

    // 🔹 Удобная self-binding перегрузка для Transient
    public ServiceCollection AddTransient<TImpl>() where TImpl : class
    {
        _descriptors.Add(new ServiceDescriptor
        {
            ServiceType = typeof(TImpl),
            ImplementationType = typeof(TImpl),
            Lifetime = ServiceLifetime.Transient
        });
        return this;
    }

    // (необязательно) Негeneric-оверлоады
    public ServiceCollection AddSingleton(Type impl)
    {
        _descriptors.Add(new ServiceDescriptor
        {
            ServiceType = impl,
            ImplementationType = impl,
            Lifetime = ServiceLifetime.Singleton
        });
        return this;
    }

    public ServiceCollection AddTransient(Type impl)
    {
        _descriptors.Add(new ServiceDescriptor
        {
            ServiceType = impl,
            ImplementationType = impl,
            Lifetime = ServiceLifetime.Transient
        });
        return this;
    }
    
    public ServiceCollection AddTransient(Type abstraction, Type impl)
    {
        _descriptors.Add(new ServiceDescriptor
        {
            ServiceType = abstraction,
            ImplementationType = impl,
            Lifetime = ServiceLifetime.Transient
        });
        return this;
    }

    public ServiceProvider BuildServiceProvider() => new ServiceProvider(_descriptors);
}

/// <summary>
/// Простой IoC-контейнер.
/// </summary>
public sealed class ServiceProvider : IServiceProvider, IDisposable
{
    private readonly Dictionary<Type, object> _singletons = new();
    private readonly Dictionary<Type, bool> _creationStack = new();
    private readonly List<ServiceDescriptor> _descriptors;
    private bool _disposed;

    public ServiceProvider(List<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
            return this;
        
        var descriptor = _descriptors.LastOrDefault(x => x.ServiceType == serviceType)
                         ?? _descriptors.LastOrDefault(x => x.ImplementationType == serviceType);

        if (descriptor is null)
            return null;

        // --- готовый instance ---
        if (descriptor.ImplementationInstance is not null)
            return descriptor.ImplementationInstance;

        // --- singleton cache ---
        if (descriptor.Lifetime == ServiceLifetime.Singleton &&
            _singletons.TryGetValue(serviceType, out var existing))
            return existing;

        // --- создаем объект ---
        var instance = CreateInstance(descriptor);

        if (descriptor.Lifetime == ServiceLifetime.Singleton)
            _singletons[serviceType] = instance!;

        return instance;
    }

    private object? CreateInstance(ServiceDescriptor descriptor)
    {
        // 1️⃣ Фабрика
        if (descriptor.ImplementationFactory is not null)
            return descriptor.ImplementationFactory(this);

        // 2️⃣ Конкретный тип
        var implType = descriptor.ImplementationType ?? descriptor.ServiceType;

        // защита от рекурсии (циклические зависимости)
        if (_creationStack.ContainsKey(implType))
            throw new InvalidOperationException($"Circular dependency detected: {implType}");

        _creationStack[implType] = true;

        try
        {
            var ctor = implType
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor is null)
                throw new InvalidOperationException($"No public constructors found for {implType}");

            var args = ctor.GetParameters()
                .Select(p => GetService(p.ParameterType)
                             ?? throw new InvalidOperationException($"Cannot resolve dependency: {p.ParameterType.Name}"))
                .ToArray();

            return Activator.CreateInstance(implType, args);
        }
        finally
        {
            _creationStack.Remove(implType);
        }
    }

    public T GetRequiredService<T>()
        => (T)(GetService(typeof(T)) ?? throw new InvalidOperationException($"Service {typeof(T)} not registered"));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var inst in _singletons.Values)
        {
            if (inst is IDisposable disp)
                disp.Dispose();
        }

        _singletons.Clear();
    }
    
    public object GetRequiredService(Type type)
    {
        return GetService(type)
               ?? throw new InvalidOperationException($"Service {type} not registered and cannot be constructed.");
    }
}