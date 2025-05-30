using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Reflection
{
    public abstract class ReflectionManager : IReflectionManager
    {
        [Dependency] private readonly ILogManager _logMan = default!;

        /// <summary>
        /// Enumerable over prefixes that are added to the type provided to <see cref="GetType(string)"/>
        /// if the type can't be found in any assemblies.
        /// </summary>
        /// <remarks>
        /// First prefix should probably be <code>""</code>.
        /// </remarks>
        protected abstract IEnumerable<string> TypePrefixes { get; }

        private readonly List<Assembly> assemblies = new();

        public event EventHandler<ReflectionUpdateEventArgs>? OnAssemblyAdded;

        [ViewVariables] public IReadOnlyList<Assembly> Assemblies => assemblies;

        private readonly Dictionary<(Type baseType, string typeName), Type?> _yamlTypeTagCache = new();

        private readonly Dictionary<string, Type> _looseTypeCache = new();

        private readonly Dictionary<string, Enum> _enumCache = new();
        private readonly Dictionary<Enum, string> _reverseEnumCache = new();

        private readonly ReaderWriterLockSlim _enumCacheLock = new();
        private readonly ReaderWriterLockSlim _yamlTypeTagCacheLock = new();

        private readonly List<Type> _getAllTypesCache = new();
        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = _logMan.GetSawmill("Reflection");
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren<T>(bool inclusive = false)
        {
            return GetAllChildren(typeof(T), inclusive);
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren(Type baseType, bool inclusive = false)
        {
            EnsureGetAllTypesCache();

            foreach (var type in _getAllTypesCache)
            {
                if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                if (baseType == type && !inclusive)
                    continue;

                yield return type;
            }
        }

        private void EnsureGetAllTypesCache()
        {
            if (_getAllTypesCache.Count != 0)
                return;

            var totalLength = 0;
            var typeSets = new List<Type[]>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                typeSets.Add(types);
                totalLength += types.Length;
            }

            _getAllTypesCache.Capacity = totalLength;

            foreach (var typeSet in typeSets)
            {
                foreach (var type in typeSet)
                {
                    var attribute = (ReflectAttribute?)Attribute.GetCustomAttribute(type, typeof(ReflectAttribute));

                    if (!(attribute?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE))
                        continue;

                    _getAllTypesCache.Add(type);
                }
            }
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());

        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            this.assemblies.AddRange(assemblies);
            _getAllTypesCache.Clear();
            OnAssemblyAdded?.Invoke(this, new ReflectionUpdateEventArgs(this));
        }

        /// <seealso cref="TypePrefixes"/>
        public Type? GetType(string name)
        {
            // The priority in which types are retrieved is based on the TypePrefixes list.
            // This is an implementation detail. If you need it: make a better API.
            foreach (string prefix in TypePrefixes)
            {
                string appendedName = prefix + name;
                foreach (var assembly in Assemblies)
                {
                    var theType = assembly.GetType(appendedName);
                    if (theType != null)
                    {
                        return theType;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc />
        public Type LooseGetType(string name)
        {
            if (TryLooseGetType(name, out var ret))
            {
                return ret;
            }

            throw new ArgumentException($"Unable to find type: {name}.");
        }

        public bool TryLooseGetType(string name, [NotNullWhen(true)] out Type? type)
        {
            lock (_looseTypeCache)
            {
                if (_looseTypeCache.TryGetValue(name, out type))
                    return true;

                // Check standard types first.
                switch (name)
                {
                    case "Byte":
                        type = typeof(byte);
                        _looseTypeCache[name] = type;
                        return true;
                    case "Bool":
                        type = typeof(bool);
                        _looseTypeCache[name] = type;
                        return true;
                    case "Double":
                        type = typeof(double);
                        _looseTypeCache[name] = type;
                        return true;
                    case "SByte":
                        type = typeof(sbyte);
                        _looseTypeCache[name] = type;
                        return true;
                    case "Single":
                        type = typeof(float);
                        _looseTypeCache[name] = type;
                        return true;
                    case "String":
                        type = typeof(string);
                        _looseTypeCache[name] = type;
                        return true;
                }

                foreach (var assembly in assemblies)
                {
                    foreach (var tryType in assembly.DefinedTypes)
                    {
                        if (tryType.FullName!.EndsWith(name))
                        {
                            type = tryType;
                            _looseTypeCache[name] = type;
                            return true;
                        }
                    }
                }

                type = default;
                return false;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Type> FindTypesWithAttribute<T>() where T : Attribute
        {
            return FindTypesWithAttribute(typeof(T));
        }

        /// <inheritdoc />
        public IEnumerable<Type> FindTypesWithAttribute(Type attributeType)
        {
            EnsureGetAllTypesCache();
            return _getAllTypesCache.Where(type => Attribute.IsDefined(type, attributeType));
        }

        public IEnumerable<Type> FindAllTypes()
        {
            EnsureGetAllTypesCache();
            return _getAllTypesCache;
        }

        /// <inheritdoc />
        public string GetEnumReference(Enum @enum)
        {
            using (_enumCacheLock.ReadGuard())
            {
                if (_reverseEnumCache.TryGetValue(@enum, out var reference))
                    return reference;
            }

            using (_enumCacheLock.WriteGuard())
            {
                if (_reverseEnumCache.TryGetValue(@enum, out var reference))
                    return reference;

                // if there is more than one enum with the same basic name, the reference may need to be the fully qualified name.
                // but if possible we want to avoid that and use a shorter string.

                var fullName = @enum.GetType().FullName!;
                var dotIndex = fullName.LastIndexOf('.');
                if (dotIndex > 0 && dotIndex != fullName.Length)
                {
                    var name = fullName.Substring(dotIndex + 1);
                    reference = $"enum.{name}.{@enum}";

                    if (_enumCache.TryAdd(reference, @enum))
                    {
                        _reverseEnumCache.Add(@enum, reference);
                        return reference;
                    }
                }

                // If that failed, just use the full name.
                reference = $"enum.{fullName}.{@enum}";
                _reverseEnumCache.Add(@enum, reference);
                _enumCache.Add(reference, @enum);
                return reference;
            }
        }

        /// <inheritdoc />
        public bool TryParseEnumReference(string reference, [NotNullWhen(true)] out Enum? @enum,
            bool shouldThrow = true)
        {
            if (!reference.StartsWith("enum."))
            {
                @enum = default;
                return false;
            }

            using (_enumCacheLock.ReadGuard())
            {
                if (_enumCache.TryGetValue(reference, out @enum))
                    return true;
            }

            using var _ = _enumCacheLock.WriteGuard();
            if (_enumCache.TryGetValue(reference, out @enum))
                return true;

            var cropped = reference.Substring(5);

            // Doesn't exist, add it.
            var dotIndex = cropped.LastIndexOf('.');
            var typeName = cropped.Substring(0, dotIndex);

            var value = cropped.Substring(dotIndex + 1);

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    if (!type.IsEnum || !(
                            type.FullName!.Equals(typeName) ||
                            type.FullName!.EndsWith("." + typeName) ||
                            type.FullName!.EndsWith("+" + typeName)))
                    {
                        continue;
                    }

                    @enum = (Enum)Enum.Parse(type, value);
                    if (!_reverseEnumCache.TryAdd(@enum, reference))
                    {
                        _sawmill.Warning($"Conflicting enum references encountered. Enum: {@enum}. Existing: {_reverseEnumCache[@enum]}. New: {reference}");
                    }
                    _enumCache.Add(reference, @enum);
                    return true;
                }
            }

            if (shouldThrow)
                throw new ArgumentException($"Could not resolve enum reference: {reference}.");
            return false;
        }

        public Type? YamlTypeTagLookup(Type baseType, string typeName)
        {
            using (_yamlTypeTagCacheLock.ReadGuard())
            {
                if (_yamlTypeTagCache.TryGetValue((baseType, typeName), out var type))
                    return type;
            }

            using (_yamlTypeTagCacheLock.WriteGuard())
            {
                if (_yamlTypeTagCache.TryGetValue((baseType, typeName), out var type))
                    return type;
                Type? found = null;
                foreach (var derivedType in GetAllChildren(baseType))
                {
                    if (!derivedType.IsPublic)
                    {
                        continue;
                    }

                    if (derivedType.Name == typeName)
                    {
                        found = derivedType;
                        break;
                    }

                    var serializedAttribute = derivedType.GetCustomAttribute<SerializedTypeAttribute>();

                    if (serializedAttribute != null &&
                        serializedAttribute.SerializeName == typeName)
                    {
                        found = derivedType;
                        break;
                    }
                }

                // Fallback
                if (found == null)
                {
                    TryLooseGetType(typeName, out found);

                    // If we may have gotten the type but it's still abstract then don't return it.
                    if (found == null || found.IsAbstract || !found.IsAssignableTo(baseType))
                        found = null;
                }

                _yamlTypeTagCache.Add((baseType, typeName), found);
                return found;
            }
        }
    }
}
