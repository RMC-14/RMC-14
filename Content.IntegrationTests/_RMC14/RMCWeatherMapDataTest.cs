using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

#nullable enable

namespace Content.IntegrationTests._RMC14;

[TestFixture]
public sealed class RMCWeatherMapDataTest
{
    private static readonly Regex EntityUidRegex = new("^  - uid: ", RegexOptions.Multiline);
    private static readonly Regex TileMapEntryRegex = new("^  (\\d+):\\s*(\\S+)", RegexOptions.Compiled);
    private static readonly Regex ChunkEntryRegex = new("^        (-?\\d+),(-?\\d+):", RegexOptions.Compiled);
    private static readonly Regex ProtoRegex = new("^- proto: (.*)", RegexOptions.Compiled);
    private static readonly Regex UidRegex = new("^  - uid: (\\d+)", RegexOptions.Compiled);
    private static readonly Regex PosRegex = new("^      pos: ([^,]+),([^\\s]+)", RegexOptions.Compiled);
    private static readonly Regex RotRegex = new("^      rot: ([^\\s]+) rad", RegexOptions.Compiled);

    [Test]
    public void RMCWeatherCyclesHaveGameplayData()
    {
        foreach (var file in WeatherMapFiles())
        {
            var blocks = ExtractWeatherCycleBlocks(File.ReadAllLines(file));
            Assert.That(blocks, Is.Not.Empty, $"{file} should contain an RMC weather cycle");

            foreach (var block in blocks)
            {
                var text = string.Join('\n', block);
                Assert.Multiple(() =>
                {
                    Assert.That(text, Does.Contain("minTimeBetweenChecks:"), $"{file} should define weather check cadence");
                    Assert.That(text, Does.Contain("minCheckVariance:"), $"{file} should define weather check variance");
                    Assert.That(text, Does.Contain("startChance:"), $"{file} should define weather start chance");
                    Assert.That(text, Does.Not.Contain("durationRemaining:"), $"{file} must not serialize runtime duration state");
                    Assert.That(text, Does.Not.Contain("RMCSolarisRocks"), $"{file} must not enable Solaris rockstorms by accident");
                });

                var weatherTypeCount = block.Count(line => line.Contains("weatherType:"));
                var smotherCount = block.Count(line => line.Contains("fireSmotheringStrength:"));
                Assert.That(smotherCount, Is.EqualTo(weatherTypeCount), $"{file} should give every weather event gameplay smothering data");
            }

            var fileName = Path.GetFileName(file);
            var fullText = string.Join('\n', blocks.SelectMany(block => block));
            if (fileName.Contains("varadero"))
            {
                Assert.That(fullText, Does.Contain("weather_warning_varadero.wav"), $"{file} should use the Varadero storm siren");
                Assert.That(fullText, Does.Contain("warningSirenKind: Storm"), $"{file} should target physical storm sirens");
            }

            if (fileName.Contains("sorokyne"))
            {
                Assert.That(fullText, Does.Contain("weather_warning.wav"), $"{file} should use the Sorokyne monsoon siren");
                Assert.That(fullText, Does.Contain("warningSirenKind: Weather"), $"{file} should target physical weather sirens");
            }
        }
    }

    [Test]
    public void RMCWeatherScreenOverlaysMatchCMFullscreenData()
    {
        var withOverlay = new[]
        {
            ("Resources/Maps/_RMC14/solaris.yml", "Duststorm", "Low"),
            ("Resources/Maps/_RMC14/solaris.yml", "Sandstorm", "Medium"),
            ("Resources/Maps/_RMC14/lv624.yml", "Light Rain", "Low"),
            ("Resources/Maps/_RMC14/lv624.yml", "Heavy Rain", "Medium"),
            ("Resources/Maps/_RMC14/varadero.yml", "Tropical Storm", "Low"),
            ("Resources/Maps/_RMC14/varadero.yml", "Monsoon", "High"),
            ("Resources/Maps/_RMC14/sorokyne.yml", "Tropical Storm", "Low"),
            ("Resources/Maps/_RMC14/sorokyne.yml", "Monsoon", "High"),
            ("Resources/Maps/_RMC14/chances.yml", "Light Rain", "Low"),
            ("Resources/Maps/_RMC14/PVE/sorokyne_repaired.yml", "Tropical Storm", "Low"),
            ("Resources/Maps/_RMC14/PVE/sorokyne_repaired.yml", "Monsoon", "High"),
            ("Resources/Maps/_RMC14/PVE/chances_repaired.yml", "Light Rain", "Low"),
            ("Resources/Maps/_RMC14/hybrisa.yml", "Hybrisa Light Rain", "Low"),
            ("Resources/Maps/_RMC14/hybrisa.yml", "Hybrisa Very Light Rain", "Low"),
            ("Resources/Maps/_RMC14/PVE/hybrisa_greenshift.yml", "Hybrisa Light Rain", "Low"),
            ("Resources/Maps/_RMC14/PVE/hybrisa_greenshift.yml", "Hybrisa Very Light Rain", "Low"),
            ("Resources/Maps/_RMC14/shiva.yml", "Snow", "Low"),
            ("Resources/Maps/_RMC14/shiva.yml", "Snowstorm", "Medium"),
            ("Resources/Maps/_RMC14/shiva.yml", "Blizzard", "High"),
            ("Resources/Maps/_RMC14/kutjevo.yml", "Sandstorm", "Medium"),
            ("Resources/Maps/_RMC14/kutjevo.yml", "Rainstorm", "Medium"),
            ("Resources/Maps/_RMC14/trijent.yml", "Duststorm", "Low"),
            ("Resources/Maps/_RMC14/trijent.yml", "Rainstorm", "Medium"),
        };

        foreach (var (relativePath, eventName, overlay) in withOverlay)
        {
            var block = ExtractWeatherEventBlock(relativePath, eventName);
            Assert.That(block,
                Does.Contain($"screenOverlay: {overlay}"),
                $"{relativePath} {eventName} should match CM fullscreen weather overlay {overlay}");
        }

        var withoutOverlay = new[]
        {
            ("Resources/Maps/_RMC14/sorokyne.yml", "Soro Light Rain"),
            ("Resources/Maps/_RMC14/PVE/sorokyne_repaired.yml", "Soro Light Rain"),
        };

        foreach (var (relativePath, eventName) in withoutOverlay)
        {
            var block = ExtractWeatherEventBlock(relativePath, eventName);
            Assert.That(block,
                Does.Not.Contain("screenOverlay:"),
                $"{relativePath} {eventName} should not invent a fullscreen weather overlay");
        }
    }

    [Test]
    public void RMCWeatherSirenMapsHavePhysicalSirens()
    {
        var rootDir = RootDir();
        var cases = new[]
        {
            ("Resources/Maps/_RMC14/varadero.yml", "RMCStormSiren", 99),
            ("Resources/Maps/_RMC14/Inserts/Varadero/clf_raid.yml", "RMCStormSiren", 4),
            ("Resources/Maps/_RMC14/Inserts/Varadero/engi_hold.yml", "RMCStormSiren", 1),
            ("Resources/Maps/_RMC14/Inserts/Varadero/varadero_resturant.yml", "RMCStormSiren", 1),
            ("Resources/Maps/_RMC14/sorokyne.yml", "RMCWeatherSiren", 81),
            ("Resources/Maps/_RMC14/PVE/sorokyne_repaired.yml", "RMCWeatherSiren", 81),
            ("Resources/Maps/_RMC14/Inserts/Sorokyne/clfcamp.yml", "RMCWeatherSiren", 6),
            ("Resources/Maps/_RMC14/Inserts/Sorokyne/flamer_bodypile.yml", "RMCWeatherSiren", 1),
        };

        foreach (var (relativePath, proto, expectedCount) in cases)
        {
            var file = Path.Combine(rootDir, relativePath);
            var text = File.ReadAllText(file);
            Assert.That(CountProtoEntities(text, proto),
                Is.EqualTo(expectedCount),
                $"{file} should contain the CM physical weather sirens");
        }
    }

    [Test]
    public void RMCWeatherSirensAreMountedAgainstWalls()
    {
        var rootDir = RootDir();
        var files = new[]
        {
            "Resources/Maps/_RMC14/varadero.yml",
            "Resources/Maps/_RMC14/Inserts/Varadero/clf_raid.yml",
            "Resources/Maps/_RMC14/Inserts/Varadero/engi_hold.yml",
            "Resources/Maps/_RMC14/Inserts/Varadero/varadero_resturant.yml",
            "Resources/Maps/_RMC14/sorokyne.yml",
            "Resources/Maps/_RMC14/PVE/sorokyne_repaired.yml",
            "Resources/Maps/_RMC14/Inserts/Sorokyne/clfcamp.yml",
            "Resources/Maps/_RMC14/Inserts/Sorokyne/flamer_bodypile.yml",
        };

        foreach (var relativePath in files)
        {
            var file = Path.Combine(rootDir, relativePath);
            var lines = File.ReadAllLines(file);
            var tiles = ReadTiles(lines);
            var entities = ReadEntities(lines);
            var wallCells = entities
                .Where(entity => entity.Position != null && IsMountWallPrototype(entity.Prototype))
                .Select(entity => Cell(entity.Position!.Value))
                .ToHashSet();

            foreach (var siren in entities.Where(entity => IsWeatherSiren(entity.Prototype)))
            {
                Assert.That(siren.Position, Is.Not.Null, $"{file} siren {siren.Uid} should have a map position");

                var position = siren.Position!.Value;
                var cell = Cell(position);
                var tile = tiles.GetValueOrDefault(cell, "MISSING");

                Assert.Multiple(() =>
                {
                    Assert.That(IsValidSirenTile(tile), Is.True, $"{file} siren {siren.Uid} is on invalid tile {tile} at {position}");
                    Assert.That(wallCells.Contains(cell), Is.False, $"{file} siren {siren.Uid} is inside a wall at {position}");
                    Assert.That(HasWallForRotation(cell, siren.Rotation, wallCells), Is.True,
                        $"{file} siren {siren.Uid} at {position} is not mounted against a wall matching rotation {siren.Rotation}");
                });
            }
        }
    }

    private static IEnumerable<string> WeatherMapFiles()
    {
        var rootDir = RootDir();
        var mapsDir = Path.Combine(rootDir, "Resources", "Maps", "_RMC14");
        return Directory.GetFiles(mapsDir, "*.yml", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("RMCWeatherCycle"));
    }

    private static string RootDir()
    {
        return Path.Join(Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.ToString());
    }

    private static string ExtractWeatherEventBlock(string relativePath, string eventName)
    {
        var file = Path.Combine(RootDir(), relativePath);
        var lines = File.ReadAllLines(file);
        var nameLine = $"        name: {eventName}";

        var index = Array.FindIndex(lines, line => line == nameLine);
        Assert.That(index, Is.GreaterThanOrEqualTo(0), $"{file} should contain weather event {eventName}");

        var start = index;
        while (start > 0 && !lines[start].StartsWith("      - "))
        {
            start--;
        }

        var end = index + 1;
        while (end < lines.Length &&
               !lines[end].StartsWith("      - ") &&
               !lines[end].StartsWith("    - type: "))
        {
            end++;
        }

        return string.Join('\n', lines[start..end]);
    }

    private static int CountProtoEntities(string text, string proto)
    {
        var start = text.IndexOf($"- proto: {proto}", StringComparison.Ordinal);
        if (start < 0)
            return 0;

        var nextProto = text.IndexOf("\n- proto:", start + 1, StringComparison.Ordinal);
        var end = nextProto >= 0
            ? nextProto
            : text.IndexOf("\n...", start + 1, StringComparison.Ordinal);
        if (end < 0)
            end = text.Length;

        return EntityUidRegex.Matches(text[start..end]).Count;
    }

    private static Dictionary<(int X, int Y), string> ReadTiles(string[] lines)
    {
        var tileMap = new Dictionary<int, string>();
        var inTileMap = false;
        foreach (var line in lines)
        {
            if (line == "tilemap:")
            {
                inTileMap = true;
                continue;
            }

            if (!inTileMap)
                continue;

            if (!line.StartsWith("  "))
            {
                inTileMap = false;
                continue;
            }

            var match = TileMapEntryRegex.Match(line);
            if (match.Success)
                tileMap[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)] = match.Groups[2].Value;
        }

        var tiles = new Dictionary<(int X, int Y), string>();
        (int X, int Y)? chunk = null;
        foreach (var line in lines)
        {
            var chunkMatch = ChunkEntryRegex.Match(line);
            if (chunkMatch.Success)
            {
                chunk = (
                    int.Parse(chunkMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(chunkMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                continue;
            }

            if (chunk == null || !line.StartsWith("          tiles: "))
                continue;

            var data = Convert.FromBase64String(line["          tiles: ".Length..]);
            for (var i = 0; i < data.Length / 7; i++)
            {
                var tileId = BitConverter.ToInt32(data, i * 7);
                var x = chunk.Value.X * 16 + i % 16;
                var y = chunk.Value.Y * 16 + i / 16;
                tiles[(x, y)] = tileMap.GetValueOrDefault(tileId, $"#{tileId}");
            }
        }

        return tiles;
    }

    private static List<MapEntity> ReadEntities(string[] lines)
    {
        var entities = new List<MapEntity>();
        string? proto = null;
        MapEntity? entity = null;
        var inTransform = false;

        foreach (var line in lines)
        {
            var protoMatch = ProtoRegex.Match(line);
            if (protoMatch.Success)
            {
                proto = protoMatch.Groups[1].Value.Trim().Trim('"');
                entity = null;
                inTransform = false;
                continue;
            }

            var uidMatch = UidRegex.Match(line);
            if (uidMatch.Success)
            {
                entity = new MapEntity(
                    int.Parse(uidMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    proto ?? string.Empty);
                entities.Add(entity);
                inTransform = false;
                continue;
            }

            if (entity == null)
                continue;

            if (line.StartsWith("    - type: "))
            {
                inTransform = line.Trim() == "- type: Transform";
                continue;
            }

            if (!inTransform)
                continue;

            var posMatch = PosRegex.Match(line);
            if (posMatch.Success)
            {
                entity.Position = (
                    double.Parse(posMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    double.Parse(posMatch.Groups[2].Value, CultureInfo.InvariantCulture));
                continue;
            }

            var rotMatch = RotRegex.Match(line);
            if (rotMatch.Success)
                entity.Rotation = double.Parse(rotMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return entities;
    }

    private static bool IsWeatherSiren(string proto)
    {
        return proto is "RMCWeatherSiren" or "RMCStormSiren";
    }

    private static bool IsMountWallPrototype(string proto)
    {
        if (!proto.Contains("Wall") || IsWeatherSiren(proto))
            return false;

        var ignored = new[] { "FogWall", "InflatableWall", "Invisible", "Prop", "Membrane", "Resin", "BulletPassible" };
        return ignored.All(part => !proto.Contains(part)) &&
            (proto.StartsWith("CMWall") || proto.StartsWith("RMCWall") || proto.StartsWith("RMCERTShuttleWall"));
    }

    private static bool IsValidSirenTile(string tile)
    {
        return tile is not ("Space" or "MISSING" or "Lattice");
    }

    private static (int X, int Y) Cell((double X, double Y) position)
    {
        return ((int)Math.Floor(position.X), (int)Math.Floor(position.Y));
    }

    private static bool HasWallForRotation((int X, int Y) cell, double rotation, HashSet<(int X, int Y)> wallCells)
    {
        var normalized = NormalizeRotation(rotation);

        if (RotationEquals(normalized, 0))
            return wallCells.Contains((cell.X, cell.Y + 1));

        if (RotationEquals(normalized, Math.PI / 2))
            return wallCells.Contains((cell.X - 1, cell.Y));

        if (RotationEquals(normalized, Math.PI))
            return wallCells.Contains((cell.X, cell.Y - 1));

        if (RotationEquals(normalized, Math.PI * 3 / 2))
            return wallCells.Contains((cell.X + 1, cell.Y));

        return false;
    }

    private static double NormalizeRotation(double rotation)
    {
        var full = Math.PI * 2;
        rotation %= full;
        return rotation < 0 ? rotation + full : rotation;
    }

    private static bool RotationEquals(double rotation, double expected)
    {
        return Math.Abs(rotation - expected) < 0.001;
    }

    private static List<List<string>> ExtractWeatherCycleBlocks(string[] lines)
    {
        var blocks = new List<List<string>>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i] != "    - type: RMCWeatherCycle")
                continue;

            var block = new List<string>();
            for (var j = i; j < lines.Length; j++)
            {
                if (j > i && (lines[j].StartsWith("    - type: ") || lines[j].StartsWith("  - uid: ")))
                    break;

                block.Add(lines[j]);
            }

            blocks.Add(block);
        }

        return blocks;
    }

    private sealed record MapEntity(int Uid, string Prototype)
    {
        public (double X, double Y)? Position { get; set; }
        public double Rotation { get; set; }
    }
}
