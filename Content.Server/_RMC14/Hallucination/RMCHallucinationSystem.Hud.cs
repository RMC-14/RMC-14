using Content.Server.Body.Components;
using Content.Shared._RMC14.Hallucination;
using Content.Shared._RMC14.Hallucination.Events;
using Content.Shared.Alert;
using Content.Shared.Mobs.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Hallucination;

public sealed partial class RMCHallucinationSystem
{
    private static readonly EntProtoId[] FakeItems =
    [
        "RMCHallucinationItemRevolver",
        "RMCHallucinationItemC4",
        "RMCHallucinationItemSword",
        "RMCHallucinationItemStunBaton",
        "RMCHallucinationItemEmag",
        "RMCHallucinationItemFlashbang",
    ];

    private static readonly RMCFakeHud[] FakeHuds =
    [
        RMCFakeHud.Critical,
        RMCFakeHud.Dead,
        RMCFakeHud.Oxygen,
        RMCFakeHud.Oxygen,
        RMCFakeHud.Toxin,
        RMCFakeHud.Toxin,
    ];

    private static readonly string[] FakeItemSlots = ["belt", "pocket1", "pocket2"];

    private void InitializeHud()
    {
        SubscribeLocalEvent<RMCHallucinatingComponent, BeforeAlertSeverityCheckEvent>(OnAlertSeverityCheck);

        SubscribeLocalEvent<RMCHallucinationFakeHudComponent, RMCHallucinateEvent>(OnFakeHud);
        SubscribeLocalEvent<RMCHallucinationFakeItemComponent, RMCHallucinateEvent>(OnFakeItem);
    }

    private void OnFakeHud(Entity<RMCHallucinationFakeHudComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = ShowFakeHud(args.Victim);
    }

    private void OnFakeItem(Entity<RMCHallucinationFakeItemComponent> ent, ref RMCHallucinateEvent args)
    {
        args.Handled = ShowFakeItem(args.Victim);
    }

    private void OnAlertSeverityCheck(Entity<RMCHallucinatingComponent> ent, ref BeforeAlertSeverityCheckEvent args)
    {
        switch (ent.Comp.FakeHud)
        {
            case RMCFakeHud.Critical:
                args.CurrentAlert = ent.Comp.CriticalAlert;
                break;
            case RMCFakeHud.Dead:
                args.CurrentAlert = ent.Comp.DeadAlert;
                break;
            default:
                return;
        }

        args.CancelUpdate = true;
    }

    private bool ShowFakeHud(Entity<RMCHallucinatingComponent> ent)
    {
        ent.Comp.FakeHud = _random.Pick(FakeHuds);
        ent.Comp.FakeHudEnd = _timing.CurTime + RandomTime(10, 25);
        RefreshFakeHud(ent);
        return true;
    }

    private bool ShowFakeItem(Entity<RMCHallucinatingComponent> ent)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.ItemEnd)
            return false;

        var slots = GetFreeItemSlots(ent);
        if (slots.Count == 0)
            return false;

        var (slot, group) = _random.Pick(slots);
        var duration = RandomTime(1, 5);
        ent.Comp.ItemEnd = time + duration;
        SendToVictim(ent, new RMCHallucinationItemEvent(_random.Pick(FakeItems), slot, group, duration));
        return true;
    }

    private void RefreshFakeHud(Entity<RMCHallucinatingComponent> ent)
    {
        _mobThreshold.VerifyThresholds(ent);

        if (ent.Comp.FakeHud == RMCFakeHud.Oxygen)
        {
            _alerts.ShowAlert(ent, ent.Comp.OxygenAlert);
            ent.Comp.ShowingFakeOxygen = true;
        }
        else if (ent.Comp.ShowingFakeOxygen)
        {
            ent.Comp.ShowingFakeOxygen = false;
            if (!TryComp(ent, out RespiratorComponent? respirator) ||
                respirator.SuffocationCycles < respirator.SuffocationCycleThreshold)
            {
                _alerts.ClearAlert(ent, ent.Comp.OxygenAlert);
            }
        }

        if (ent.Comp.FakeHud == RMCFakeHud.Toxin)
        {
            _alerts.ShowAlert(ent, ent.Comp.ToxinAlert);
            ent.Comp.ShowingFakeToxin = true;
        }
        else if (ent.Comp.ShowingFakeToxin)
        {
            ent.Comp.ShowingFakeToxin = false;
            _alerts.ClearAlert(ent, ent.Comp.ToxinAlert);
        }
    }

    private List<(string Slot, string? Group)> GetFreeItemSlots(EntityUid uid)
    {
        var slots = new List<(string, string?)>();
        foreach (var hand in _hands.EnumerateHands(uid))
        {
            if (_hands.HandIsEmpty(uid, hand))
                slots.Add((hand, null));
        }

        foreach (var name in FakeItemSlots)
        {
            if (_inventory.TryGetSlotContainer(uid, name, out var container, out var definition) &&
                container.ContainedEntity == null)
            {
                slots.Add((name, definition.SlotGroup));
            }
        }

        return slots;
    }
}
