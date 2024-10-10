using Content.Shared._RMC14.Evacuation;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Marines.ControlComputer;

public abstract class SharedMarineControlComputerSystem : EntitySystem
{
    [Dependency] private readonly SharedEvacuationSystem _evacuation = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EvacuationEnabledEvent>(OnRefreshComputers);
        SubscribeLocalEvent<EvacuationDisabledEvent>(OnRefreshComputers);
        SubscribeLocalEvent<EvacuationProgressEvent>(OnRefreshComputers);

        SubscribeLocalEvent<MarineControlComputerComponent, BeforeActivatableUIOpenEvent>(OnComputerBeforeUIOpen);

        Subs.BuiEvents<MarineControlComputerComponent>(MarineControlComputerUi.Key,
            subs =>
            {
                subs.Event<MarineControlComputerToggleEvacuationMsg>(OnToggleEvacuationMsg);
            });
    }

    private void OnRefreshComputers<T>(ref T ev)
    {
        RefreshComputers();
    }

    private void OnComputerBeforeUIOpen(Entity<MarineControlComputerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        RefreshComputers();
    }

    private void OnToggleEvacuationMsg(Entity<MarineControlComputerComponent> ent, ref MarineControlComputerToggleEvacuationMsg args)
    {
        _ui.CloseUi(ent.Owner, MarineControlComputerUi.Key, args.Actor);
        if (!ent.Comp.CanEvacuate)
            return;

        var time = _timing.CurTime;
        if (time < ent.Comp.LastToggle + ent.Comp.ToggleCooldown)
            return;

        ent.Comp.LastToggle = time;

        // TODO RMC14 evacuation start sound
        _evacuation.ToggleEvacuation(null, null);
        RefreshComputers();
    }

    private void RefreshComputers()
    {
        var canEvacuate = _evacuation.IsEvacuationInProgress();
        var evacuationEnabled = _evacuation.IsEvacuationEnabled();
        var computers = EntityQueryEnumerator<MarineControlComputerComponent>();
        while (computers.MoveNext(out var uid, out var computer))
        {
            computer.Evacuating = evacuationEnabled;
            computer.CanEvacuate = canEvacuate;
            Dirty(uid, computer);
        }
    }
}
