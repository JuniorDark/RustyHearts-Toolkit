using System;

namespace RHToolkit.Factory;

public class AbstractFactory<T>(Func<T> factory) : IAbstractFactory<T>
{
    private readonly Func<T> _factory = factory;

    public T CreateWindow()
    {
        return _factory();
    }
}
