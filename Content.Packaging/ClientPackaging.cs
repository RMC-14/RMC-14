using System.Diagnostics;
using System.IO.Compression;
using Content.Packaging._RMC14;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;
using Robust.Packaging.Utility;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Packaging;

public static class ClientPackaging
{
    //RMC14
    private const string ResourcesDirectory = "Resources";
    private const string IgnoredPrototypesDirectory = "IgnoredPrototypes";
    private const string YamlSearchPattern = "*.yml";
    //RMC14

    /// <summary>
    /// Be advised this can be called from server packaging during a HybridACZ build.
    /// </summary>
    public static async Task PackageClient(bool skipBuild, string configuration, IPackageLogger logger)
    {
        logger.Info("Building client...");

        if (!skipBuild)
        {
            await ProcessHelpers.RunCheck(new ProcessStartInfo
            {
                FileName = "dotnet",
                ArgumentList =
                {
                    "build",
                    Path.Combine("Content.Client", "Content.Client.csproj"),
                    "-c", configuration,
                    "--nologo",
                    "/v:m",
                    "/t:Rebuild",
                    "/p:FullRelease=true",
                    "/m"
                }
            });
        }

        logger.Info("Packaging client...");

        var sw = RStopwatch.StartNew();
        {
            await using var zipFile =
                File.Open(Path.Combine("release", "SS14.Client.zip"), FileMode.Create, FileAccess.ReadWrite);
            using var zip = new ZipArchive(zipFile, ZipArchiveMode.Update);
            var writer = new AssetPassZipWriter(zip);

            await WriteResources("", writer, logger, default);
            await writer.FinishedTask;
        }

        logger.Info($"Finished packaging client in {sw.Elapsed}");
    }

    public static async Task WriteResources(
        string contentDir,
        AssetPass pass,
        IPackageLogger logger,
        CancellationToken cancel)
    {
        var graph = new RobustClientAssetGraph();
        pass.Dependencies.Add(new AssetPassDependency(graph.Output.Name));

        //RMC14
        var abstractIgnoredPrototypesPass = new AssetPassAbstractIgnoredPrototypes(
            ReadIgnoredPrototypePaths(contentDir))
        {
            Name = "AbstractIgnoredPrototypesPass",
        };

        abstractIgnoredPrototypesPass
            .AddDependency(graph.Input)
            .AddBefore(graph.PresetPasses);

        graph.PresetPasses.AddDependency(abstractIgnoredPrototypesPass);
        //RMC14

        var dropSvgPass = new AssetPassFilterDrop(f => f.Path.EndsWith(".svg"))
        {
            Name = "DropSvgPass",
        };
        dropSvgPass.AddDependency(graph.Input).AddBefore(graph.PresetPasses);

        AssetGraph.CalculateGraph([pass, abstractIgnoredPrototypesPass, dropSvgPass, ..graph.AllPasses], logger);

        var inputPass = graph.Input;

        await RobustSharedPackaging.WriteContentAssemblies(
            inputPass,
            contentDir,
            "Content.Client",
            new[] { "Content.Client", "Content.Shared", "Content.Shared.Database" },
            cancel: cancel);

        await RobustClientPackaging.WriteClientResources(contentDir, inputPass, cancel);

        inputPass.InjectFinished();
    }

    //RMC14
    private static HashSet<string> ReadIgnoredPrototypePaths(string contentDir)
    {
        var ignored = new HashSet<string>(StringComparer.Ordinal);
        var ignoredDir = Path.Combine(contentDir, ResourcesDirectory, IgnoredPrototypesDirectory);

        if (!Directory.Exists(ignoredDir))
            return ignored;

        foreach (var file in Directory.EnumerateFiles(ignoredDir, YamlSearchPattern))
        {
            using var reader = new StreamReader(file, EncodingHelpers.UTF8);

            foreach (var document in DataNodeParser.ParseYamlStream(reader))
            {
                if (document.Root is not SequenceDataNode sequence)
                    continue;

                foreach (var node in sequence.Sequence)
                {
                    if (node is not ValueDataNode value)
                        continue;

                    var path = NormalizeResourcePath(value.Value);

                    if (path.Length != 0)
                        ignored.Add(path);
                }
            }
        }

        return ignored;
    }

    private static string NormalizeResourcePath(string path)
    {
        return path
            .Replace('\\', '/')
            .TrimStart('/');
    }
    //RMC14
}
