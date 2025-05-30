﻿using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeReader<TType, TNode> : ITypeValidator<TType, TNode> where TNode : DataNode
    {
        TType Read(ISerializationManager serializationManager,
            TNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<TType>? instanceProvider = null);
    }
}
