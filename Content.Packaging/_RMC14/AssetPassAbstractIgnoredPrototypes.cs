using Robust.Packaging.AssetProcessing;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Packaging._RMC14;

public sealed class AssetPassAbstractIgnoredPrototypes : AssetPass
{
    private readonly IReadOnlySet<string> _ignoredPrototypePaths;

    public AssetPassAbstractIgnoredPrototypes(IReadOnlySet<string> ignoredPrototypePaths)
    {
        _ignoredPrototypePaths = ignoredPrototypePaths;
    }

    protected override AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        if (!IsIgnoredPrototypePath(file.Path))
            return AssetFileAcceptResult.Pass;

        using var stream = file.Open();
        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var documents = DataNodeParser.ParseYamlStream(reader).ToArray();

        foreach (var document in documents)
        {
            if (document.Root is not SequenceDataNode sequence)
                continue;

            foreach (var node in sequence.Sequence)
            {
                if (node is MappingDataNode mapping)
                    AbstractPrototype(mapping);
            }
        }

        using var output = new MemoryStream();
        using (var writer = new StreamWriter(output, EncodingHelpers.UTF8, leaveOpen: true))
        {
            foreach (var document in documents)
            {
                document.Root.Write(writer);
                writer.WriteLine();
            }
        }

        SendFile(new AssetFileMemory(file.Path, output.ToArray()));
        return AssetFileAcceptResult.Consumed;
    }

    private bool IsIgnoredPrototypePath(string path)
    {
        path = NormalizeResourcePath(path);

        foreach (var ignoredPath in _ignoredPrototypePaths)
        {
            if (path == ignoredPath)
                return true;

            if (path.StartsWith(ignoredPath.TrimEnd('/') + "/", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static void AbstractPrototype(MappingDataNode mapping)
    {
        if (mapping.TryGet(AbstractDataFieldAttribute.Name, out var abstractNode))
        {
            if (abstractNode is not ValueDataNode abstractValueNode)
            {
                mapping[AbstractDataFieldAttribute.Name] = new ValueDataNode("true");
                return;
            }

            abstractValueNode.Value = "true";
            return;
        }

        mapping.Add(AbstractDataFieldAttribute.Name, "true");
    }

    private static string NormalizeResourcePath(string path)
    {
        return path
            .Replace('\\', '/')
            .TrimStart('/');
    }
}
