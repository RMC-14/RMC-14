﻿namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// Generalized unboxing of a value from a containing structure.
/// </summary>
public interface IAsType<out T>
{
    public T AsType();
}
