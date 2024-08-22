using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Alert;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Waypoint;

public sealed partial class TrackerAlertSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private void InititlizeLists()
    {
        SubscribeLocalEvent<RMCTrackerListComponent, AddToTrackerListEvent>(OnAddToTrackerList);
        SubscribeLocalEvent<RMCTrackerListComponent, RemoveFromTrackerListEvent>(OnRemoveFromTrackerList);
        SubscribeLocalEvent<XenoComponent, GetTrackerAlertEntriesEvent>(OnGetTrackerAlertEntries);
        SubscribeLocalEvent<SquadMemberComponent, GetTrackerAlertEntriesEvent>(OnGetTrackerAlertEntries);
    }

    private void OnAddToTrackerList(Entity<RMCTrackerListComponent> ent, ref AddToTrackerListEvent args)
    {
        if (!TryComp<RMCTrackerAlertTargetComponent>(args.Target, out var trackerTargetComp))
            return;

        var trackers = ent.Comp.Trackers.GetOrNew(trackerTargetComp.AlertPrototype);
        trackers.Add(GetNetEntity(args.Target));
    }

    private void OnRemoveFromTrackerList(Entity<RMCTrackerListComponent> ent, ref RemoveFromTrackerListEvent args)
    {
        if (!TryComp(args.Target, out RMCTrackerAlertTargetComponent? tracker))
            return;

        if (!ent.Comp.Trackers.TryGetValue(tracker.AlertPrototype, out var trackers))
            return;

        trackers.Remove(GetNetEntity(args.Target));
        if (trackers.Count == 0)
            ent.Comp.Trackers.Remove(tracker.AlertPrototype);
    }

    private bool TryGetTrackers(Entity<RMCTrackerListComponent?> ent,
        ProtoId<AlertPrototype> alertProto,
        [NotNullWhen(true)] out List<EntityUid>? trackers)
    {
        trackers = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Trackers.TryGetValue(alertProto, out var netTrackers))
        {
            trackers = GetEntityList(netTrackers);
            return true;
        }

        return false;
    }

    private void OnGetTrackerAlertEntries(Entity<XenoComponent> ent, ref GetTrackerAlertEntriesEvent args)
    {
        if (ent.Comp.Hive == null)
            return;

        GetTrackerEntries(ent.Comp.Hive.Value, ent, ref args);
    }

    private void OnGetTrackerAlertEntries(Entity<SquadMemberComponent> ent, ref GetTrackerAlertEntriesEvent args)
    {
        if (ent.Comp.Squad == null)
            return;

        GetTrackerEntries(ent.Comp.Squad.Value, ent, ref args);
    }

    private void GetTrackerEntries(EntityUid team, EntityUid target, ref GetTrackerAlertEntriesEvent args)
    {
        if (!TryComp(team, out RMCTrackerListComponent? trackerList))
            return;

        if (!TryGetTrackers((team, trackerList), args.AlertPrototype, out var trackers))
        {
            _popup.PopupPredicted("No trackers to open", target, target);
            return;
        }

        var alertPrototype = args.AlertPrototype;

        args.Entries.AddRange(trackers.Select(uid =>
            new TrackerAlertEntry(GetNetEntity(uid), Name(uid), MetaData(uid).EntityPrototype?.ID, alertPrototype)));
    }
}

public record struct AddToTrackerListEvent(EntityUid Target);

public record struct RemoveFromTrackerListEvent(EntityUid Target);
