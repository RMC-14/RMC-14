using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class TypeSerializer : ITypeSerializer<Type, ValueDataNode>, ITypeCopyCreator<Type>
    {
        private static readonly Dictionary<string, Type> Shortcuts = new ()
        {
            {"bool", typeof(bool)}
        };

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            if (Shortcuts.ContainsKey(node.Value))
                return new ValidatedValueNode(node);

            return dependencies.Resolve<IReflectionManager>().GetType(node.Value) == null
                ? new ErrorNode(node, $"Type '{node.Value}' not found.")
                : new ValidatedValueNode(node);
        }

        public Type Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<Type>? instanceProvider = null)
        {
            if (Shortcuts.TryGetValue(node.Value, out var shortcutType))
                return shortcutType;

            var type = dependencies.Resolve<IReflectionManager>().GetType(node.Value);

            return type == null
                ? throw new InvalidMappingException($"Type '{node.Value}' not found.")
                : type;
        }

        public DataNode Write(ISerializationManager serializationManager, Type value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.FullName ?? value.Name);
        }

        public Type CreateCopy(ISerializationManager serializationManager, Type source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return source;
        }
    }
}
