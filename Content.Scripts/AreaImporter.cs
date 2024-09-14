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
        // ReSharper restore InconsistentNaming
    }

    private const string Colors = @"
//Turf colors
#define MINIMAP_SOLID ""#ebe5e5ee""
#define MINIMAP_DOOR ""#451e5eb8""
#define MINIMAP_FENCE ""#8d2294ad""
#define MINIMAP_LAVA ""#db4206ad""
#define MINIMAP_DIRT ""#9c906dc2""
#define MINIMAP_SNOW ""#c4e3e9c7""
#define MINIMAP_MARS_DIRT ""#aa5f44cc""
#define MINIMAP_ICE ""#93cae0b0""
#define MINIMAP_WATER ""#94b0d59c""

//Area colors
#define MINIMAP_AREA_ENGI ""#c19504e7""
#define MINIMAP_AREA_ENGI_CAVE ""#5a4501e7""
#define MINIMAP_AREA_MEDBAY ""#3dbf75ee""
#define MINIMAP_AREA_MEDBAY_CAVE ""#17472cee""
#define MINIMAP_AREA_SEC ""#a22d2dee""
#define MINIMAP_AREA_SEC_CAVE ""#421313ee""
#define MINIMAP_AREA_RESEARCH ""#812da2ee""
#define MINIMAP_AREA_RESEARCH_CAVE ""#2d1342ee""
#define MINIMAP_AREA_COMMAND ""#2d3fa2ee""
#define MINIMAP_AREA_COMMAND_CAVE ""#132242ee""
#define MINIMAP_AREA_CAVES ""#3f3c3cef""
#define MINIMAP_AREA_JUNGLE ""#2b5b2bee""
#define MINIMAP_AREA_COLONY ""#6c6767d8""
#define MINIMAP_AREA_LZ ""#ebe5e5e3""
#define MINIMAP_AREA_CONTESTED_ZONE ""#0603c4ee""

#define MINIMAP_SQUAD_UNKNOWN ""#d8d8d8""
#define MINIMAP_SQUAD_ALPHA ""#ed1c24""
#define MINIMAP_SQUAD_BRAVO ""#fbc70e""
#define MINIMAP_SQUAD_CHARLIE ""#76418a""
#define MINIMAP_SQUAD_DELTA ""#0c0cae""
#define MINIMAP_SQUAD_ECHO ""#00b043""
#define MINIMAP_SQUAD_FOXTROT ""#fe7b2e""
#define MINIMAP_SQUAD_SOF ""#400000""
#define MINIMAP_SQUAD_INTEL ""#053818""

#define MINIMAP_ICON_BACKGROUND_CIVILIAN ""#7D4820""
#define MINIMAP_ICON_BACKGROUND_CIC ""#3f3f3f""
#define MINIMAP_ICON_BACKGROUND_USCM ""#888888""
#define MINIMAP_ICON_BACKGROUND_XENO ""#3a064d""

#define MINIMAP_ICON_COLOR_COMMANDER ""#c6fcfc""
#define MINIMAP_ICON_COLOR_HEAD ""#F0C542""
#define MINIMAP_ICON_COLOR_BRONZE ""#eb9545""

#define MINIMAP_ICON_COLOR_DOCTOR ""#b83737""


//Prison
#define MINIMAP_AREA_CELL_MAX ""#570101ee""
#define MINIMAP_AREA_CELL_HIGH ""#a54b01ee""
#define MINIMAP_AREA_CELL_MED ""#997102e7""
#define MINIMAP_AREA_CELL_LOW ""#5a9201ee""
#define MINIMAP_AREA_CELL_VIP ""#00857aee""
#define MINIMAP_AREA_SHIP ""#885a04e7""";

    private const string Areas = @"
//LV624 AREAS--------------------------------------//
/area/lv624
	icon_state = ""lv-626""
	can_build_special = TRUE
	powernet_name = ""ground""
	ambience_exterior = AMBIENCE_JUNGLE
	minimap_color = MINIMAP_AREA_COLONY

/area/lv624/ground
	name = ""Ground""
	icon_state = ""green""
	always_unpowered = 1 //Will this mess things up? God only knows

//Jungle
/area/lv624/ground/jungle
	minimap_color = MINIMAP_AREA_JUNGLE

/area/lv624/ground/jungle/south_east_jungle
	name =""\improper Southeast Jungle""
	icon_state = ""southeast""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/south_central_jungle
	name =""\improper Southern Central Jungle""
	icon_state = ""south""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/south_west_jungle
	name =""\improper Southwest Jungle""
	icon_state = ""southwest""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/south_west_jungle/ceiling
	ceiling = CEILING_GLASS

/area/lv624/ground/jungle/west_jungle
	name =""\improper Western Jungle""
	icon_state = ""west""
	//ambience = list('sound/ambience/jungle_amb1.ogg')
	is_resin_allowed = FALSE

/area/lv624/ground/jungle/west_jungle/ceiling
	ceiling = CEILING_GLASS

/area/lv624/ground/jungle/east_jungle
	name =""\improper Eastern Jungle""
	icon_state = ""east""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/north_west_jungle
	name =""\improper Northwest Jungle""
	icon_state = ""northwest""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/north_jungle
	name =""\improper Northern Jungle""
	icon_state = ""north""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/north_east_jungle
	name =""\improper Northeast Jungle""
	icon_state = ""northeast""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/central_jungle
	name =""\improper Central Jungle""
	icon_state = ""central""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/west_central_jungle
	name =""\improper West Central Jungle""
	icon_state = ""west""
	//ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/jungle/east_central_jungle
	name =""\improper East Central Jungle""
	icon_state = ""east""
	//ambience = list('sound/ambience/jungle_amb1.ogg')


//The Barrens
/area/lv624/ground/barrens
	name = ""\improper Barrens""
	icon_state = ""yellow""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/west_barrens
	name = ""\improper Western Barrens""
	icon_state = ""west""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/west_barrens/ceiling
	ceiling = CEILING_GLASS

/area/lv624/ground/barrens/east_barrens
	name = ""\improper Eastern Barrens""
	icon_state = ""east""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/east_barrens/ceiling
	ceiling = CEILING_GLASS

/area/lv624/ground/barrens/containers
	name = ""\improper Containers""
	icon_state = ""blue-red""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/north_east_barrens
	name = ""\improper North Eastern Barrens""
	icon_state = ""northeast""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/north_east_barrens/ceiling
	ceiling = CEILING_GLASS

/area/lv624/ground/barrens/south_west_barrens
	name = ""\improper South Western Barrens""
	icon_state = ""southwest""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/central_barrens
	name = ""\improper Central Barrens""
	icon_state = ""away1""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/south_eastern_barrens
	name = ""\improper South Eastern Barrens""
	icon_state = ""southeast""
// ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/barrens/south_eastern_jungle_barrens
	name = ""\improper South East Jungle Barrens""
	icon_state = ""southeast""
// ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen4.ogg','sound/ambience/ambisin4.ogg')

/area/lv624/ground/river
	name = ""\improper River""
	icon_state = ""blueold""
// ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/river/west_river
	name = ""\improper Western River""
	icon_state = ""blueold""
// ambience = list('sound/ambience/jungle_amb1.ogg')
/area/lv624/ground/river/central_river
	name = ""\improper Central River""
	icon_state = ""purple""
// ambience = list('sound/ambience/jungle_amb1.ogg')

/area/lv624/ground/river/east_river
	name = ""\improper Eastern River""
	icon_state = ""bluenew""
// ambience = list('sound/ambience/jungle_amb1.ogg')


//Colony Areas
/area/lv624/ground/colony
	name = ""\improper Weyland-Yutani Compound""
	icon_state = ""green""

/area/lv624/ground/colony/north_nexus_road
	name = ""\improper North Nexus Road""
	icon_state = ""north""

/area/lv624/ground/colony/south_medbay_road
	name = ""\improper South Medbay Road""
	icon_state = ""south""

/area/lv624/ground/colony/south_nexus_road
	name = ""\improper South Nexus Road""
	icon_state = ""south""

/area/lv624/ground/colony/west_nexus_road
	name = ""\improper West Nexus Road""
	icon_state = ""west""

/area/lv624/ground/colony/north_tcomms_road
	name = ""\improper North T-Comms Road""
	icon_state = ""north""

/area/lv624/ground/colony/west_tcomms_road
	name = ""\improper West T-Comms Road""
	icon_state = ""west""

/area/lv624/ground/colony/telecomm
	name = ""\improper LZ1 Communications Relay""
	icon_state = ""ass_line""
	ceiling = CEILING_UNDERGROUND_METAL_ALLOW_CAS
	is_resin_allowed = FALSE
	ceiling_muffle = FALSE
	base_muffle = MUFFLE_LOW
	always_unpowered = FALSE

/area/lv624/ground/colony/telecomm/cargo
	name = ""\improper Far North Storage Dome Communications Relay""


/area/lv624/ground/colony/telecomm/sw_lz1
	name = ""\improper South-West LZ1 Communications Relay""
	ceiling = CEILING_NONE

/area/lv624/ground/colony/telecomm/tcommdome
	name = ""\improper Telecomms Dome Communications Relay""

/area/lv624/ground/colony/telecomm/tcommdome/south
	name = ""\improper South Telecomms Dome Communications Relay""
	ceiling = CEILING_NONE

/area/lv624/ground/colony/telecomm/sw_lz2
	name = ""\improper South-West LZ2 Communications Relay""
	ceiling = CEILING_NONE

// ambience = list('sound/ambience/jungle_amb1.ogg')


//The Caves
/area/lv624/ground/caves //Does not actually exist
	name =""\improper Caves""
	icon_state = ""cave""
	//ambience = list('sound/ambience/ambimine.ogg','sound/ambience/ambigen10.ogg','sound/ambience/ambigen12.ogg','sound/ambience/ambisin4.ogg')
	ambience_exterior = AMBIENCE_CAVE
	soundscape_playlist = SCAPE_PL_CAVE
	soundscape_interval = 25
	ceiling = CEILING_UNDERGROUND_BLOCK_CAS
	sound_environment = SOUND_ENVIRONMENT_AUDITORIUM
	minimap_color = MINIMAP_AREA_CAVES

/area/lv624/ground/caves/west_caves
	name =""\improper Western Caves""
	icon_state = ""away1""

/area/lv624/ground/caves/south_west_caves
	name =""\improper South Western Caves""
	icon_state = ""red""

/area/lv624/ground/caves/east_caves
	name =""\improper Eastern Caves""
	icon_state = ""away""

/area/lv624/ground/caves/central_caves
	name =""\improper Central Caves""
	icon_state = ""away4"" //meh

/area/lv624/ground/caves/north_west_caves
	name =""\improper North Western Caves""
	icon_state = ""cave""

/area/lv624/ground/caves/north_east_caves
	name =""\improper North Eastern Caves""
	icon_state = ""cave""

/area/lv624/ground/caves/north_central_caves
	name =""\improper North Central Caves""
	icon_state = ""away3"" //meh

/area/lv624/ground/caves/south_central_caves
	name =""\improper South Central Caves""
	icon_state = ""away2"" //meh

/area/lv624/ground/caves/south_east_caves
	name =""\improper South East Caves""
	icon_state = ""away2"" //meh

/area/lv624/ground/caves/sand_temple
	name = ""\improper Sand Temple""
	icon_state = ""bluenew""

/area/lv624/ground/caves/sand_temple/powered
	name = ""\improper Sand Temple - Powered""
	icon_state = ""green""
	requires_power = FALSE

//Lazarus landing
/area/lv624/lazarus
	name = ""\improper Lazarus""
	icon_state = ""green""
	ceiling = CEILING_METAL

/area/lv624/lazarus/landing_zones
	ceiling = CEILING_NONE
	is_resin_allowed = FALSE
	is_landing_zone = TRUE

/area/lv624/lazarus/landing_zones/lz1
	name = ""\improper Alamo Landing Zone""

/area/lv624/lazarus/landing_zones/lz2
	name = ""\improper Normandy Landing Zone""

/area/lv624/lazarus
	name = ""\improper Lazarus""
	icon_state = ""green""
	ceiling = CEILING_METAL

/area/lv624/lazarus/corporate_dome
	name = ""\improper Corporate Dome""
	icon_state = ""green""

/area/lv624/lazarus/yggdrasil
	name = ""\improper Yggdrasil Tree""
	icon_state = ""atmos""
	ceiling = CEILING_GLASS

/area/lv624/lazarus/medbay
	name = ""\improper Medbay""
	icon_state = ""medbay""
	minimap_color = MINIMAP_AREA_MEDBAY

/area/lv624/lazarus/armory
	name = ""\improper Armory""
	icon_state = ""armory""
	minimap_color = MINIMAP_AREA_SEC

/area/lv624/lazarus/security
	name = ""\improper Security""
	icon_state = ""security""
	minimap_color = MINIMAP_AREA_SEC

/area/lv624/lazarus/captain
	name = ""\improper Commandant's Quarters""
	icon_state = ""captain""
	minimap_color = MINIMAP_AREA_COMMAND

/area/lv624/lazarus/hop
	name = ""\improper Head of Personnel's Office""
	icon_state = ""head_quarters""
	minimap_color = MINIMAP_AREA_COMMAND

/area/lv624/lazarus/kitchen
	name = ""\improper Kitchen""
	icon_state = ""kitchen""
	is_resin_allowed = FALSE

/area/lv624/lazarus/canteen
	name = ""\improper Canteen""
	icon_state = ""cafeteria""
	is_resin_allowed = FALSE

/area/lv624/lazarus/main_hall
	name = ""\improper Main Hallway""
	icon_state = ""hallC1""
	is_resin_allowed = FALSE

/area/lv624/lazarus/toilet
	name = ""\improper Dormitory Toilet""
	icon_state = ""toilet""

/area/lv624/lazarus/chapel
	name = ""\improper Chapel""
	icon_state = ""chapel""
	//ambience = list('sound/ambience/ambicha1.ogg','sound/ambience/ambicha2.ogg','sound/ambience/ambicha3.ogg','sound/ambience/ambicha4.ogg')

/area/lv624/lazarus/toilet
	name = ""\improper Dormitory Toilet""
	icon_state = ""toilet""

/area/lv624/lazarus/sleep_male
	name = ""\improper Male Dorm""
	icon_state = ""Sleep""

/area/lv624/lazarus/sleep_female
	name = ""\improper Female Dorm""
	icon_state = ""Sleep""
	is_resin_allowed = FALSE

/area/lv624/lazarus/quart
	name = ""\improper Quartermasters""
	icon_state = ""quart""
	is_resin_allowed = FALSE

/area/lv624/lazarus/quartstorage
	name = ""\improper Cargo Bay""
	icon_state = ""quartstorage""
	is_resin_allowed = FALSE

/area/lv624/lazarus/quartstorage/outdoors
	name = ""\improper Cargo Bay Area""
	icon_state = ""purple""
	ceiling = CEILING_NONE
	is_resin_allowed = FALSE
	always_unpowered = TRUE

/area/lv624/lazarus/engineering
	name = ""\improper Engineering""
	icon_state = ""engine_smes""
	minimap_color = MINIMAP_AREA_ENGI

/area/lv624/lazarus/comms
	name = ""\improper Communications Relay""
	icon_state = ""tcomsatcham""
	minimap_color = MINIMAP_AREA_ENGI

/area/lv624/lazarus/secure_storage
	name = ""\improper Secure Storage""
	icon_state = ""storage""
	flags_area = AREA_NOTUNNEL

/area/lv624/lazarus/robotics
	name = ""\improper Robotics""
	icon_state = ""ass_line""
	is_resin_allowed = FALSE

/area/lv624/lazarus/research
	name = ""\improper Research Lab""
	icon_state = ""toxlab""
	minimap_color = MINIMAP_AREA_RESEARCH

/area/lv624/lazarus/fitness
	name = ""\improper Fitness Room""
	icon_state = ""fitness""

/area/lv624/lazarus/hydroponics
	name = ""\improper Hydroponics""
	icon_state = ""hydro""
	ceiling = CEILING_GLASS

/area/lv624/landing/console
	name = ""\improper LZ1 'Nexus'""
	icon_state = ""tcomsatcham""
	requires_power = FALSE

/area/lv624/landing/console2
	name = ""\improper LZ2 'Robotics'""
	icon_state = ""tcomsatcham""
	requires_power = FALSE

/area/lv624/lazarus/crashed_ship
	name = ""\improper Crashed Ship""
	icon_state = ""syndie-ship""

/area/lv624/lazarus/crashed_ship_containers
	name = ""\improper Crashed Ship Containers""
	icon_state = ""syndie-ship""
";
}
