﻿using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom
{
    public sealed class ConstantSerializer<TTag> : ITypeSerializer<int, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            var constType = serializationManager.GetConstantTypeFromTag(typeof(TTag));
            return Enum.TryParse(constType, node.Value, out _) ? new ValidatedValueNode(node) : new ErrorNode(node, "Failed parsing constant.", false);
        }

        public int Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<int>? instanceProvider = null)
        {
            var constType = serializationManager.GetConstantTypeFromTag(typeof(TTag));
            return (int) Enum.Parse(constType, node.Value);
        }

        public DataNode Write(ISerializationManager serializationManager, int value, IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var constType = serializationManager.GetConstantTypeFromTag(typeof(TTag));
            var constantName = Enum.GetName(constType, value);

            if (constantName == null)
            {
                throw new InvalidOperationException($"No constant corresponding to value {value} in {constType}.");
            }

            return new ValueDataNode(constantName);
        }
    }
}
