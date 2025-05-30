using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Interfaces
{
    public interface ITypeSerializer<TType, TNode> :
        ITypeReaderWriter<TType, TNode>
        where TNode : DataNode
    {
    }
}
