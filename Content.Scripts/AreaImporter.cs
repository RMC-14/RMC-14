using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Content.Shared._RMC14.Areas;
using Content.Tools;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization.NamingConventions;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Scripts;

public class AreaImporter
{
    private static readonly Regex IdRegex = new("/(\\w)", RegexOptions.Compiled);
    private static readonly Regex UnderscoreRegex = new("_(\\w)", RegexOptions.Compiled);

    public void Run()
    {
        var colorLines = Colors.Split("\n");
        var colors = new Dictionary<string, Color>();
        for (var i = 0; i < colorLines.Length; i++)
        {
            var line = colorLines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("#define"))
                continue;

            var parts = line.Replace("#define", "").Trim().Split(" ").Select(p => p.Replace("\"", "").Trim()).ToArray();
            var name = parts[0];
            var color = parts[1];
            if (!color.StartsWith("#"))
                continue;

            colors[name] = Color.FromHex(color.Replace("\"", ""));
        }

        var areas = new List<Area>();
        var lines = new Queue<string>(Areas.Split("\n"));
        string? areaPath = null;
        while (lines.TryDequeue(out var line))
        {
            line = line.Trim();

            const string areaPrefix = "/area/";
            if (line.StartsWith(areaPrefix))
                areaPath = line[areaPrefix.Length..];
            else if (areaPath == null)
                continue;

            var parents = new List<string>();
            if (!areaPath.Contains('/'))
                parents.Add("RMCAreaBase");
            else
                parents.Add(PathToId(areaPath[..areaPath.LastIndexOf('/')]));

            var area = new List<(string Key, string Value)>();
            var rsi = new Rsi();
            string? areaName = null;
            var isDefault = true;

            void SetAreaComp(AreaField key, bool value)
            {
                var keyStr = key.ToString();
                for (var i = area.Count - 1; i >= 0; i--)
                {
                    if (area[i].Key == keyStr)
                        area.RemoveAt(i);
                }

                area.Add((keyStr, value.ToString().ToLowerInvariant()));
            }

            while (lines.TryPeek(out var next) && !next.StartsWith(areaPrefix))
            {
                lines.Dequeue();
                next = next.Trim();

                var varPrefix = "var/";
                if (next.StartsWith(varPrefix))
                    next = next[varPrefix.Length..];

                bool TryExtract(string part, [NotNullWhen(true)] out string? result)
                {
                    if (next.StartsWith(part))
                    {
                        result = next[part.Length..].Trim();
                        var comment = result.IndexOf("//", StringComparison.OrdinalIgnoreCase);
                        if (comment >= 0)
                            result = result[..comment].Trim();

                        return true;
                    }

                    result = null;
                    return false;
                }

                const string name = "name =";
                const string icon = "icon =";
                const string iconState = "icon_state =";
                const string ceiling = "ceiling =";
                const string powerNet = "powernet_name =";
                const string hijackEvacuationArea = "hijack_evacuation_area =";
                const string hijackEvacuationWeight = "hijack_evacuation_weight =";
                const string hijackEvacuationType = "hijack_evacuation_type =";
                const string minimapColorType = "minimap_color =";
                const string fakeZLevel = "fake_zlevel =";
                const string flagsArea = "flags_area =";
                const string canBuildSpecial = "can_build_special =";
                const string isResinAllowed = "is_resin_allowed =";
                const string resinConstructionAllowed = "resin_construction_allowed =";
                const string landingZone = "is_landing_zone =";
                const string linkedLz = "linked_lz =";
                if (TryExtract(name, out var result))
                {
                    areaName = result.Replace("\\improper", "").Replace("\"", "").Trim();
                }
                else if (TryExtract(icon, out result))
                {
                    isDefault = false;
                    var rsiPath = result.Replace(".dmi", ".rsi").Replace("icons/turf/", "").Replace("'", "");
                    rsiPath = rsiPath switch
                    {
                        "area_shiva.rsi" => "areas_shiva.rsi",
                        _ => rsiPath,
                    };

                    rsi = new Rsi(
                        new ResPath($"_RMC14/Areas/{rsiPath}"),
                        rsi.RsiState
                    );
                }
                else if (TryExtract(iconState, out result))
                {
                    isDefault = false;
                    rsi = new Rsi(
                        rsi.RsiPath,
                        result.ToLowerInvariant()
                    );
                }
                else if (TryExtract(ceiling, out result))
                {
                    switch (result)
                    {
                        case "CEILING_NO_PROTECTION":
                        case "CEILING_NONE":
                        case "CEILING_GLASS":
                            isDefault = false;
                            SetAreaComp(AreaField.CAS, true);
                            SetAreaComp(AreaField.fulton, true);
                            SetAreaComp(AreaField.mortarPlacement, true);
                            SetAreaComp(AreaField.mortarFire, true);
                            SetAreaComp(AreaField.lasing, true);
                            SetAreaComp(AreaField.medevac, true);
                            SetAreaComp(AreaField.OB, true);
                            SetAreaComp(AreaField.supplyDrop, true);
                            SetAreaComp(AreaField.paradropping, true);
                            break;
                        case "CEILING_PROTECTION_TIER_1":
                        case "CEILING_METAL":
                            isDefault = false;
                            SetAreaComp(AreaField.CAS, true);
                            SetAreaComp(AreaField.fulton, true);
                            SetAreaComp(AreaField.mortarPlacement, false);
                            SetAreaComp(AreaField.mortarFire, true);
                            SetAreaComp(AreaField.lasing, false);
                            SetAreaComp(AreaField.medevac, false);
                            SetAreaComp(AreaField.OB, true);
                            SetAreaComp(AreaField.supplyDrop, true);
                            SetAreaComp(AreaField.paradropping, false);
                            break;
                        case "CEILING_PROTECTION_TIER_2":
                        case "CEILING_UNDERGROUND_METAL_ALLOW_CAS":
                        case "CEILING_UNDERGROUND_ALLOW_CAS":
                            isDefault = false;
                            SetAreaComp(AreaField.CAS, true);
                            SetAreaComp(AreaField.fulton, false);
                            SetAreaComp(AreaField.mortarPlacement, false);
                            SetAreaComp(AreaField.mortarFire, false);
                            SetAreaComp(AreaField.lasing, false);
                            SetAreaComp(AreaField.medevac, false);
                            SetAreaComp(AreaField.OB, true);
                            SetAreaComp(AreaField.supplyDrop, false);
                            SetAreaComp(AreaField.paradropping, false);
                            break;
                        case "CEILING_PROTECTION_TIER_3":
                        case "CEILING_UNDERGROUND_BLOCK_CAS":
                        case "CEILING_UNDERGROUND_METAL_BLOCK_CAS":
                            isDefault = false;
                            SetAreaComp(AreaField.CAS, false);
                            SetAreaComp(AreaField.fulton, false);
                            SetAreaComp(AreaField.lasing, false);
                            SetAreaComp(AreaField.mortarPlacement, false);
                            SetAreaComp(AreaField.mortarFire, false);
                            SetAreaComp(AreaField.medevac, false);
                            SetAreaComp(AreaField.OB, true);
                            SetAreaComp(AreaField.supplyDrop, false);
                            SetAreaComp(AreaField.paradropping, false);
                            break;
                        case "CEILING_PROTECTION_TIER_4":
                        case "CEILING_DEEP_UNDERGROUND":
                        case "CEILING_DEEP_UNDERGROUND_METAL":
                        case "CEILING_REINFORCED_METAL":
                        case "CEILING_RESIN":
                        case "CEILING_MAX":
                            isDefault = false;
                            SetAreaComp(AreaField.CAS, false);
                            SetAreaComp(AreaField.fulton, false);
                            SetAreaComp(AreaField.lasing, false);
                            SetAreaComp(AreaField.mortarPlacement, false);
                            SetAreaComp(AreaField.mortarFire, false);
                            SetAreaComp(AreaField.medevac, false);
                            SetAreaComp(AreaField.OB, false);
                            SetAreaComp(AreaField.supplyDrop, false);
                            SetAreaComp(AreaField.paradropping, false);
                            break;
                        default:
                            Console.WriteLine($"Found unknown ceiling {ceiling}");
                            break;
                    }
                }
                else if (TryExtract(powerNet, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.PowerNet), result.Replace("\"", "")));
                }
                else if (TryExtract(hijackEvacuationArea, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.HijackEvacuationArea), result.ToLowerInvariant()));
                }
                else if (TryExtract(hijackEvacuationWeight, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.HijackEvacuationWeight), result));
                }
                else if (TryExtract(hijackEvacuationType, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.HijackEvacuationType), result switch
                    {
                        "EVACUATION_TYPE_NONE" => "None",
                        "EVACUATION_TYPE_ADDITIVE" => "Add",
                        "EVACUATION_TYPE_MULTIPLICATIVE" => "Multiply",
                        _ => throw new ArgumentOutOfRangeException(result),
                    }));
                }
                else if (TryExtract(minimapColorType, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.MinimapColor), colors[result].ToHex()));
                }
                else if (TryExtract(fakeZLevel, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.ZLevel), result));
                }
                else if (TryExtract(flagsArea, out result))
                {
                    isDefault = false;
                    area.Add((
                        nameof(AreaComponent.AvoidBioscan),
                        result.Contains("AREA_AVOID_BIOSCAN").ToString().ToLowerInvariant()
                    ));
                    area.Add((
                        nameof(AreaComponent.NoTunnel),
                        result.Contains("AREA_NOTUNNEL").ToString().ToLowerInvariant()
                    ));
                    area.Add((
                        nameof(AreaComponent.Unweedable),
                        result.Contains("AREA_UNWEEDABLE").ToString().ToLowerInvariant()
                    ));
                }
                else if (TryExtract(canBuildSpecial, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.BuildSpecial), result.ToLowerInvariant()));
                }
                else if (TryExtract(isResinAllowed, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.ResinAllowed), result.ToLowerInvariant()));
                }
                else if (TryExtract(resinConstructionAllowed, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.ResinConstructionAllowed), result.ToLowerInvariant()));
                }
                else if (TryExtract(landingZone, out result))
                {
                    isDefault = false;
                    area.Add((nameof(AreaComponent.LandingZone), result.ToLowerInvariant()));
                }
                else if (TryExtract(linkedLz, out result))
                {
                    isDefault = false;
                    if (result.Contains('(') && result.Contains(')'))
                    {
                        //In the middle of a list(), remove the list part
                        int startIndex = result.IndexOf('(') + 1;
                        result = result.Substring(startIndex, result.LastIndexOf(')') - startIndex);
                    }
                    area.Add((nameof(AreaComponent.LinkedLz), result.ToLowerInvariant()));
                }
            }

            areas.Add(new Area(parents, PathToId(areaPath), areaName, area, rsi, isDefault));
        }

        var sequence = new YamlSequenceNode();
        foreach (var area in areas)
        {
            var ent = new YamlMappingNode() { { "type", "entity" } };

            var parentsSelf = false;
            if (area.Parents.Count > 1)
            {
                var parents = new YamlSequenceNode();
                foreach (var parent in area.Parents)
                {
                    if (parent == area.Id)
                        parentsSelf = true;

                    parents.Add(parent);
                }

                ent.Add("parent", parents);
            }
            else if (area.Parents.Count == 1)
            {
                var parent = area.Parents[0];
                if (parent == area.Id)
                    parentsSelf = true;

                ent.Add("parent", parent);
            }

            ent.Add("id", $"{(parentsSelf ? $"{area.Id} // TODO RMC14 fix parenting self" : area.Id)}");

            if (area.Name is { } name)
                ent.Add("name", name);

            if (!area.IsDefault)
            {
                var components = new YamlSequenceNode();

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (area.Rsi.RsiPath != default || area.Rsi.RsiState != default)
                {
                    var spriteComp = new YamlMappingNode();
                    spriteComp.Add("type", "Sprite");

                    if (area.Rsi.RsiPath != default)
                        spriteComp.Add("sprite", area.Rsi.RsiPath.ToString());

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (area.Rsi.RsiState != default)
                        spriteComp.Add("state", area.Rsi.RsiState.Replace("\"", ""));

                    components.Add(spriteComp);
                }

                if (area.Comp.Count > 0)
                {
                    var areaComp = new YamlMappingNode();
                    areaComp.Add("type", "Area");

                    foreach (var tuple in area.Comp)
                    {
                        var (key, value) = tuple;
                        if (!key.All(char.IsUpper))
                            key = CamelCaseNamingConvention.Instance.Apply(key);

                        areaComp.Add(key, value);
                    }

                    components.Add(areaComp);
                }

                ent.Add("components", components);
            }

            sequence.Add(ent);
        }

        var document = new YamlDocument(sequence);
        var stream = new YamlStream(document);
        var memStream = new MemoryStream();
        var writer = new StreamWriter(memStream);
        var emitter = new Emitter(writer);
        var fixer = new TypeTagPreserver(emitter);

        stream.Save(fixer, false);
        writer.Flush();

        memStream.Position = 0;

        var text = new StreamReader(memStream);
        Console.WriteLine(text.ReadToEnd().Replace("- type: entity", "\n- type: entity").Trim());
    }

    private string PathToId(string areaPath)
    {
        var id = IdRegex.Replace(areaPath, m => $"{char.ToUpperInvariant(m.Groups[1].ValueSpan[0])}{m.Groups[1].ValueSpan[1..]}");
        id = UnderscoreRegex.Replace(id, m => $"{char.ToUpperInvariant(m.Groups[1].ValueSpan[0])}{m.Groups[1].ValueSpan[1..]}");
        return $"RMCArea{char.ToUpperInvariant(id[0])}{id[1..]}";
    }

    [DataRecord]
    public readonly record struct Area(
        List<string> Parents,
        string Id,
        string? Name,
        List<(string, string)> Comp,
        Rsi Rsi,
        bool IsDefault
    );

    public enum AreaField
    {
        // ReSharper disable InconsistentNaming
        CAS,
        fulton,
        lasing,
        mortarPlacement,
        mortarFire,
        medevac,
        OB,
        supplyDrop,
        paradropping
        // ReSharper restore InconsistentNaming
    }

    private const string Colors = @"";

    private const string Areas = @"";
}
