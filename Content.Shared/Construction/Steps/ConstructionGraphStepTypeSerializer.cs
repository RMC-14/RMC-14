using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence; // RMC14
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;    // RMC14
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared.Construction.Steps
{
    [TypeSerializer]
    public sealed class ConstructionGraphStepTypeSerializer : ITypeReader<ConstructionGraphStep, MappingDataNode>
    {
        private Type? GetType(MappingDataNode node)
        {
            if (node.Has("material"))
            {
                return typeof(MaterialConstructionGraphStep);
            }

            if (node.Has("tools")) // RMC14
            {
                return typeof(ToolConstructionGraphStep);
            }

            if (node.Has("component"))
            {
                return typeof(ComponentConstructionGraphStep);
            }

            if (node.Has("tag"))
            {
                return typeof(TagConstructionGraphStep);
            }

            if (node.Has("allTags") || node.Has("anyTags"))
            {
                return typeof(MultipleTagsConstructionGraphStep);
            }

            if (node.Has("minTemperature") || node.Has("maxTemperature"))
            {
                return typeof(TemperatureConstructionGraphStep);
            }

            if (node.Has("assemblyId") || node.Has("guideString"))
            {
                return typeof(PartAssemblyConstructionGraphStep);
            }

            return null;
        }

        // begin RMC14
        // helper function to convert old yml "tool" to new format "tools"
        private void ConvertLegacyTool(MappingDataNode node)
        {
        
            if (node.Has("tool") && node.Has("tools"))
            {
                throw new InvalidOperationException(
                    "ConstructionGraphStep cannot contain both 'tool' and 'tools'.");
            }
            
            if (node.Has("tool") && !node.Has("tools"))
            {
                var tool = node.Get<ValueDataNode>("tool");
        
                node.Remove("tool");
                node.Add("tools", new SequenceDataNode(new[] { tool }));
            }
        }
        // end RMC14

        public ConstructionGraphStep Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ConstructionGraphStep>? instanceProvider = null)
        {
            ConvertLegacyTool(node); // RMC14

            var type = GetType(node) ??
                       throw new ArgumentException(
                           "Tried to convert invalid YAML node mapping to ConstructionGraphStep!");

            return (ConstructionGraphStep)serializationManager.Read(type, node, hookCtx, context)!;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            ConvertLegacyTool(node); // RMC14
            
            var type = GetType(node);

            if (type == null)
                return new ErrorNode(node, "No construction graph step type found.");

            return serializationManager.ValidateNode(type, node, context);
        }
    }
}
