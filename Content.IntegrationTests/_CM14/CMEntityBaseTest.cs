using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests._CM14;

[TestFixture]
public sealed class CMEntityBaseTest
{
    private static readonly ImmutableHashSet<string> Exemptions = ImmutableHashSet.Create(
        "CMCorrodable"
    );

    [Test]
    public async Task Test()
    {
        var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var resources = server.ResolveDependency<IResourceManager>();

        var files = resources.ContentFindFiles("/Prototypes/_CM14/Entities");
        var missingBase = new HashSet<string>();
        var stack = new Stack<string>();
        foreach (var path in files)
        {
            if (path.Extension != "yml")
                continue;

            var yaml = resources.ContentFileReadYaml(path);
            if (yaml.Documents.Count == 0)
                continue;

            if (yaml.Documents[0].RootNode is not YamlSequenceNode sequence)
                continue;

            foreach (var node in sequence)
            {
                if (node is not YamlMappingNode mapping)
                    continue;

                var currentNode = mapping.ToDataNodeCast<MappingDataNode>();
                if (!currentNode.TryGet("type", out ValueDataNode typeNode) ||
                    typeNode.Value != "entity")
                {
                    continue;
                }

                if (!currentNode.TryGet("id", out ValueDataNode idNode))
                    continue;

                var initialId = idNode.Value;
                if (Exemptions.Contains(initialId))
                    continue;

                stack.Clear();
                var found = false;
                do
                {
                    if (currentNode.TryGet("id", out idNode) &&
                        idNode.Value == "CMEntityBase")
                    {
                        found = true;
                        break;
                    }

                    if (currentNode.TryGet("parent", out ValueDataNode parentValue))
                    {
                        stack.Push(parentValue.Value);
                    }
                    else if (currentNode.TryGet("parent", out SequenceDataNode parentSequence))
                    {
                        foreach (var parentSequenceNode in parentSequence)
                        {
                            if (parentSequenceNode is ValueDataNode parentSequenceValueNode)
                                stack.Push(parentSequenceValueNode.Value);
                        }
                    }
                } while (stack.TryPop(out var parentId) &&
                         prototypes.TryGetMapping(typeof(EntityPrototype), parentId, out currentNode));

                if (!found)
                {
                    missingBase.Add($"{initialId} in {path.ToString()}");
                }
            }
        }

        if (missingBase.Count > 0)
        {
            Assert.Fail($"Found CM14 entities that don't have CMEntityBase as their parent:\n{string.Join("\n", missingBase)}");
        }

        await pair.CleanReturnAsync();
    }
}
