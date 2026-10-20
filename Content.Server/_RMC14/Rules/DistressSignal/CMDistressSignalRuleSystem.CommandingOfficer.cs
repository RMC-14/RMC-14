using Content.Shared._RMC14.Fax;
using Content.Shared._RMC14.Marines.Command;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private const string CommandingOfficerFaxId = "CommandingOfficer";

    private static readonly ProtoId<JobPrototype> CommandingOfficerJob = "CMCommandingOfficer";
    private static readonly TimeSpan CommandingOfficerBriefingDelay = TimeSpan.FromSeconds(15);

    private bool _commandingOfficerBriefingScheduled;

    private void OnCommandingOfficerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId != CommandingOfficerJob.Id ||
            ev.LateJoin ||
            _commandingOfficerBriefingScheduled)
        {
            return;
        }

        _commandingOfficerBriefingScheduled = true;

        var affiliation = CommandingOfficerAffiliation.Unaligned;
        if (TryComp(ev.Mob, out CommandingOfficerAffiliationComponent? affiliationComponent))
            affiliation = affiliationComponent.Affiliation;

        var commandingOfficer = ev.Mob;
        Timer.Spawn(CommandingOfficerBriefingDelay,
            () => TrySendCommandingOfficerBriefing(commandingOfficer, affiliation));
    }

    private void TrySendCommandingOfficerBriefing(
        EntityUid commandingOfficer,
        CommandingOfficerAffiliation affiliation)
    {
        if (!Exists(commandingOfficer) ||
            !HasComp<ActorComponent>(commandingOfficer) ||
            TryGetActiveRule() == null)
        {
            return;
        }

        if (SelectedPlanetMap is not { } planet)
        {
            Log.Warning("No planet is selected for the Commanding Officer affiliation briefing.");
            return;
        }

        if (planet.Comp.CommandingOfficerBriefings is not { } briefings)
        {
            Log.Warning($"No Commanding Officer briefing mapping is configured for {planet.Proto.ID}.");
            return;
        }

        if (!briefings.TryGetValue(affiliation, out var paper) &&
            !briefings.TryGetValue(CommandingOfficerAffiliation.Unaligned, out paper))
        {
            Log.Warning($"No Commanding Officer briefing is configured for {planet.Proto.ID} and {affiliation}.");
            return;
        }

        if (!paper.TryGet(out var paperComponent, _prototypes, _compFactory) ||
            !_prototypes.TryIndex(paper.Id, out var paperPrototype, logError: false))
        {
            Log.Warning($"Invalid Commanding Officer briefing paper prototype {paper.Id} on {planet.Proto.ID}.");
            return;
        }

        var faxes = EntityQueryEnumerator<FaxMachineComponent, SpecialFaxComponent>();
        while (faxes.MoveNext(out var faxId, out var fax, out var special))
        {
            if (special.FaxId != CommandingOfficerFaxId)
                continue;

            var content = Loc.GetString(paperComponent.Content);
            var printout = new FaxPrintout(content, paperPrototype.Name, prototypeId: paper.Id, locked: true);
            _fax.Receive(faxId, printout, component: fax);
            return;
        }

        Log.Warning("No Commanding Officer special fax was found for the round-start affiliation briefing.");
    }
}
