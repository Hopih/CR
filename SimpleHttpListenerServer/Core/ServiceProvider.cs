using Microsoft.Extensions.DependencyInjection;

namespace SimpleHttpListenerServer.Core;

public enum ServiceLifetime
{
    Singleton,
    Scoped,
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
    
    public ServiceCollection AddScoped<TService, TImpl>()
        where TImpl : class, TService
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationType = typeof(TImpl),
            Lifetime = ServiceLifetime.Scoped
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
            ImplementationFactory = factory,
            Lifetime = ServiceLifetime.Transient
        });
        return this;
    }
    
    public ServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        _descriptors.Add(new()
        {
            ServiceType = typeof(TService),
            ImplementationFactory = factory,
            Lifetime = ServiceLifetime.Scoped
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
    
    private void ValidateDependencies()
    {
        foreach (var descriptor in _descriptors)
        {
            if (descriptor.ImplementationType is null)
                continue;

            ValidateDescriptor(descriptor, new Stack<Type>(), descriptor.Lifetime);
        }
    }
    
    private void ValidateDescriptor(ServiceDescriptor descriptor, Stack<Type> stack, ServiceLifetime rootLifetime)
    {
        if (descriptor.ImplementationType is null)
            return;

        if (stack.Contains(descriptor.ImplementationType))
            return; // избегаем рекурсии по циклам

        stack.Push(descriptor.ImplementationType);

        var ctor = descriptor.ImplementationType
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (ctor == null)
        {
            stack.Pop();
            return;
        }

        foreach (var param in ctor.GetParameters())
        {
            var dependency = _descriptors
                .LastOrDefault(d => d.ServiceType == param.ParameterType);

            if (dependency == null)
                continue; // не зарегистрирован — пропускаем
            
            if (rootLifetime == ServiceLifetime.Singleton &&
                dependency.Lifetime == ServiceLifetime.Scoped)
            {
                throw new InvalidOperationException(
                    $"Invalid lifetime chain: root Singleton ({descriptor.ImplementationType?.Name}) " +
                    $"depends (directly or indirectly) on Scoped " +
                    $"({dependency.ImplementationType?.Name ?? dependency.ServiceType.Name}).");
            }

            // рекурсивная проверка зависимостей потомка
            ValidateDescriptor(dependency, stack, rootLifetime);
        }

        stack.Pop();
    }
    

    public ServiceProvider BuildServiceProvider(bool validateScopes = true)
    {
        if (validateScopes)
            ValidateDependencies();
        
        return new ServiceProvider(_descriptors);
    }
}

/// <summary>
/// Простой IoC-контейнер.
/// </summary>
public sealed class ServiceProvider : IServiceProvider, IServiceScopeFactory, IDisposable
{
    private readonly Dictionary<Type, object> _singletons = new();
    private readonly Dictionary<Type, bool> _creationStack = new();
    private readonly List<ServiceDescriptor> _descriptors;
    private bool _disposed;

    public ServiceProvider(List<ServiceDescriptor> descriptors)
    {
        _descriptors = descriptors;
        _singletons[typeof(IServiceProvider)] = this;
        _singletons[typeof(IServiceScopeFactory)] = this;
    }

    public object? GetService(Type serviceType) //ICalculator, ILogger
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
        
        // --- scoped запрещён в корневом провайдере ---
        if (descriptor.Lifetime == ServiceLifetime.Scoped)
            throw new InvalidOperationException($"Cannot resolve scoped service {serviceType} from root provider. Use CreateScope().");

        // --- создаем объект ---
        var instance = CreateInstance(descriptor, this);

        if (descriptor.Lifetime == ServiceLifetime.Singleton)
            _singletons[serviceType] = instance!;

        return instance;
    }

    private object CreateInstance(ServiceDescriptor descriptor, IServiceProvider provider)
    {
        // 1️⃣ Фабрика
        if (descriptor.ImplementationFactory is not null)
            return descriptor.ImplementationFactory(this);

        // 2️⃣ Конкретный тип
        var implType = descriptor.ImplementationType ?? descriptor.ServiceType;

        // защита от рекурсии (циклические зависимости)
        if (!_creationStack.TryAdd(implType, true))
            throw new InvalidOperationException($"Circular dependency detected: {implType}");

        try
        {
            var ctor = implType
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length) 
                .FirstOrDefault() 
                       ?? throw new InvalidOperationException($"No public constructors found for {implType}"); 

            var args = ctor.GetParameters() 
                .Select(p => GetService(p.ParameterType)
                             ?? throw new InvalidOperationException($"Cannot resolve dependency: {p.ParameterType.Name}"))
                .ToArray();

            return Activator.CreateInstance(implType, args)!;
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

    public IServiceScope CreateScope()
    {
        return new ServiceScope(_descriptors, _singletons);
    }
}

/// <summary>
/// Область (scope) — создаётся из ServiceProvider и хранит scoped-экземпляры.
/// </summary>
public sealed class ServiceScope : IServiceScope, IServiceProvider
{
    private readonly Dictionary<Type, object> _scopedInstances = new();
    private readonly Dictionary<Type, bool> _creationStack = new();
    private readonly List<ServiceDescriptor> _descriptors;
    private readonly Dictionary<Type, object> _rootSingletons;
    private bool _disposed;

    public IServiceProvider ServiceProvider => this;

    public ServiceScope(List<ServiceDescriptor> descriptors, Dictionary<Type, object> singletons)
    {
        _descriptors = descriptors;
        _rootSingletons = singletons;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
            return this;

        var descriptor = _descriptors.LastOrDefault(x => x.ServiceType == serviceType)
                         ?? _descriptors.LastOrDefault(x => x.ImplementationType == serviceType);

        if (descriptor is null)
            return null;

        // --- singleton ---
        if (descriptor.Lifetime == ServiceLifetime.Singleton &&
            _rootSingletons.TryGetValue(serviceType, out var singleton))
            return singleton;

        // --- scoped cache ---
        if (descriptor.Lifetime == ServiceLifetime.Scoped &&
            _scopedInstances.TryGetValue(serviceType, out var scoped))
            return scoped;

        // --- создаем ---
        var instance = CreateInstance(descriptor);

        switch (descriptor.Lifetime)
        {
            case ServiceLifetime.Singleton:
                _rootSingletons[serviceType] = instance!;
                break;
            case ServiceLifetime.Scoped:
                _scopedInstances[serviceType] = instance!;
                break;
        }

        return instance;
    }

    private object CreateInstance(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationFactory is not null)
            return descriptor.ImplementationFactory(this);

        var implType = descriptor.ImplementationType ?? descriptor.ServiceType;

        if (!_creationStack.TryAdd(implType, true))
            throw new InvalidOperationException($"Circular dependency detected: {implType}");

        try
        {
            var ctor = implType
                           .GetConstructors()
                           .OrderByDescending(c => c.GetParameters().Length)
                           .FirstOrDefault()
                       ?? throw new InvalidOperationException($"No public constructors found for {implType}");

            var args = ctor.GetParameters()
                .Select(p => GetService(p.ParameterType)
                             ?? throw new InvalidOperationException(
                                 $"Cannot resolve dependency: {p.ParameterType.Name}"))
                .ToArray();

            return Activator.CreateInstance(implType, args)!;
        }
        finally
        {
            _creationStack.Remove(implType);
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var scoped in _scopedInstances.Values)
        {
            if (scoped is IDisposable disp)
                disp.Dispose();
        }

        _scopedInstances.Clear();
    }
}
