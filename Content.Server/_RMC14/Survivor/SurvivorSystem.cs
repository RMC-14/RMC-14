using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Server.Antag;
using Content.Shared._RMC14.Survivor;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Localizations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Roles;
using Content.Shared.Mind.Components;
using Content.Server.Maps;
using Content.Server._RMC14.Rules;

namespace Content.Server.GameTicking.Rules;


public sealed class SurvivorRuleSystem : SharedSurvivorSystem
{

    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;

    ///<summary>
    ///     Initializes and organises survivor players roundstart briefing for flavourtext.
    ///</summary>
    ///

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EquipSurvivorPresetComponent, MindAddedMessage>(AfterSurvivorSpawn);
    }

    private void AfterSurvivorSpawn(Entity<EquipSurvivorPresetComponent> mob, ref MindAddedMessage args) //References MindRoleAdded from Jobsystem.cs, hopefully not an issue
    {

        _antag.SendBriefing(mob, MakeBriefing(mob), null, null);

    }
    /// <summary>
    ///     *SHOULD* select briefing from assigned dictionary, generating a rolebriefing at roundstart depending on survivor preset.
    /// </summary>
    /// <param name="mob">Relevant survivor/player entity that receives rolebriefing</param>
    /// <returns></returns>
    private string MakeBriefing(EntityUid mob)
    {
                string planet = _distressSignal.SelectedPlanetMapName ?? string.Empty;
        string ModifyPlanetNameSurvivor(string planet)
        {
            // TODO RMC14 save these somewhere and avert the shitcode
            var name = planet.Replace("/Maps/_RMC14/", "").Replace(".yml", "");
            return name switch
            {
                "LV-624" => "the self-sustaining LV-624 colony",
                "Solaris Ridge" => "an underfunded virology labsite most called Solaris Ridge",
                "Fiorina Science Annex" => "the Fiorina research complex",
                "Shivas Snowball" => "the Shivas habitat-factory",
                "Trijent Dam" => "the famous Trijent dam",
                "New Varadero" => "the archaeology digsite, Varadero",
                _ => name,
            };

        }
        var briefing = Loc.GetString("clf-civilian-role-briefing", ("planet", ModifyPlanetNameSurvivor(planet)));

        return briefing;

    }
 }
