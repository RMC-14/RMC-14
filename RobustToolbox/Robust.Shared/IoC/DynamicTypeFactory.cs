﻿using System;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Shared.IoC
{
    /// <summary>
    ///     The sole purpose of this factory is to create arbitrary objects that have their
    ///     dependencies resolved. If you think you need Activator.CreateInstance(), use this
    ///     factory instead.
    /// </summary>
    /// <seealso cref="DynamicTypeFactoryExt"/>
    [PublicAPI]
    public interface IDynamicTypeFactory
    {
        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        ///     The type MUST have a parameterless constructor.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstance(Type type, bool oneOff = false, bool inject = true);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <param name="args">The arguments to be passed to the constructor.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstance(Type type, object[] args, bool oneOff = false, bool inject = true);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <typeparam name="T">Type of object to instantiate.</typeparam>
        /// <returns>Newly created object.</returns>
        T CreateInstance<T>(bool oneOff = false, bool inject = true) where T : new();
    }

    internal interface IDynamicTypeFactoryInternal : IDynamicTypeFactory
    {
        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        ///     The type MUST have a parameterless constructor.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstanceUnchecked(Type type, bool oneOff = false, bool inject = true);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="type">Type of object to instantiate.</param>
        /// <param name="args">The arguments to be passed to the constructor.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <returns>Newly created object.</returns>
        object CreateInstanceUnchecked(Type type, object[] args, bool oneOff = false, bool inject = true);

        /// <summary>
        ///     Constructs a new instance of the given type with Dependencies resolved.
        /// </summary>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <typeparam name="T">Type of object to instantiate.</typeparam>
        /// <returns>Newly created object.</returns>
        T CreateInstanceUnchecked<T>(bool oneOff = false, bool inject = true) where T : new();
    }

    /// <summary>
    ///     Extension methods for <see cref="IDynamicTypeFactory"/>.
    /// </summary>
    public static class DynamicTypeFactoryExt
    {
        /// <summary>
        ///     Constructs a new instance of the given type, and return it cast to the specified type.
        /// </summary>
        /// <param name="dynamicTypeFactory">The dynamic type factory to use.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <typeparam name="T">The type that the instance will be cast to.</typeparam>
        /// <returns>Newly created object, cast to <typeparamref name="T"/>.</returns>
        public static T CreateInstance<T>(this IDynamicTypeFactory dynamicTypeFactory, Type type,
            bool oneOff = false, bool inject = true)
        {
            DebugTools.Assert(typeof(T).IsAssignableFrom(type), "type must be subtype of T");
            return (T) dynamicTypeFactory.CreateInstance(type, oneOff, inject);
        }

        /// <summary>
        ///     Constructs a new instance of the given type, and return it cast to the specified type.
        /// </summary>
        /// <param name="dynamicTypeFactory">The dynamic type factory to use.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <typeparam name="T">The type that the instance will be cast to.</typeparam>
        /// <returns>Newly created object, cast to <typeparamref name="T"/>.</returns>
        public static T CreateInstance<T>(
            this IDynamicTypeFactory dynamicTypeFactory,
            Type type,
            object[] args,
            bool oneOff = false,
            bool inject = true)
        {
            DebugTools.Assert(typeof(T).IsAssignableFrom(type), "type must be subtype of T");
            return (T) dynamicTypeFactory.CreateInstance(type, args, oneOff, inject);
        }

        /// <summary>
        ///     Constructs a new instance of the given type, and return it cast to the specified type.
        /// </summary>
        /// <param name="dynamicTypeFactory">The dynamic type factory to use.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <typeparam name="T">The type that the instance will be cast to.</typeparam>
        /// <returns>Newly created object, cast to <typeparamref name="T"/>.</returns>
        internal static T CreateInstanceUnchecked<T>(
            this IDynamicTypeFactoryInternal dynamicTypeFactory,
            Type type,
            bool oneOff = false,
            bool inject = true)
        {
            DebugTools.Assert(typeof(T).IsAssignableFrom(type), "type must be subtype of T");
            return (T) dynamicTypeFactory.CreateInstanceUnchecked(type, oneOff, inject);
        }

        /// <summary>
        ///     Constructs a new instance of the given type, and return it cast to the specified type.
        /// </summary>
        /// <param name="dynamicTypeFactory">The dynamic type factory to use.</param>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="oneOff">If true, do not cache injector delegates.</param>
        /// <param name="inject">If false, will not inject dependencies.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <typeparam name="T">The type that the instance will be cast to.</typeparam>
        /// <returns>Newly created object, cast to <typeparamref name="T"/>.</returns>
        internal static T CreateInstanceUnchecked<T>(
            this IDynamicTypeFactoryInternal dynamicTypeFactory,
            Type type,
            object[] args,
            bool oneOff = false,
            bool inject = true)
        {
            DebugTools.Assert(typeof(T).IsAssignableFrom(type), "type must be subtype of T");
            return (T) dynamicTypeFactory.CreateInstanceUnchecked(type, args, oneOff, inject);
        }
    }

    /// <inheritdoc />
    internal sealed class DynamicTypeFactory : IDynamicTypeFactoryInternal
    {
        // https://blog.ploeh.dk/2012/03/15/ImplementinganAbstractFactory/

        [Dependency] private readonly IDependencyCollection _dependencies = default!;
        [Dependency] private readonly IModLoader _modLoader = default!;

        /// <inheritdoc />
        public object CreateInstance(Type type, bool oneOff = false, bool inject = true)
        {
            if (!_modLoader.IsContentTypeAccessAllowed(type))
            {
                throw new SandboxArgumentException("Creating non-content types is not allowed.");
            }

            return CreateInstanceUnchecked(type, oneOff, inject);
        }

        public object CreateInstance(Type type, object[] args, bool oneOff = false, bool inject = true)
        {
            if (!_modLoader.IsContentTypeAccessAllowed(type))
            {
                throw new SandboxArgumentException("Creating non-content types is not allowed.");
            }

            return CreateInstanceUnchecked(type, args, oneOff, inject);
        }

        /// <inheritdoc />
        public T CreateInstance<T>(bool oneOff = false, bool inject = true)
            where T : new()
        {
            if (!_modLoader.IsContentTypeAccessAllowed(typeof(T)))
            {
                throw new SandboxArgumentException("Creating non-content types is not allowed.");
            }

            return CreateInstanceUnchecked<T>(oneOff, inject);
        }

        public object CreateInstanceUnchecked(Type type, bool oneOff = false, bool inject = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var instance = Activator.CreateInstance(type)!;

            if (inject)
                _dependencies.InjectDependencies(instance, oneOff);
            return instance;
        }

        public object CreateInstanceUnchecked(Type type, object[] args, bool oneOff = false, bool inject = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var instance = Activator.CreateInstance(type, args)!;

            if (inject)
                _dependencies.InjectDependencies(instance, oneOff);
            return instance;
        }

        public T CreateInstanceUnchecked<T>(bool oneOff = false, bool inject = true) where T : new()
        {
            var instance = new T();

            if (inject)
                _dependencies.InjectDependencies(instance, oneOff);
            return instance;
        }
    }
}
