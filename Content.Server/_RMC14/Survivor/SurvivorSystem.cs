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
        var planet = _distressSignal.SelectedPlanetMapName ?? string.Empty;
        var briefing = Loc.GetString("clf-civilian-role-briefing",("planet", planet));

        return briefing;

    }
}
