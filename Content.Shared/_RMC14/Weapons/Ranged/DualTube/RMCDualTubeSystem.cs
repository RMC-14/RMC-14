using Content.Shared._RMC14.Input;
using Content.Shared._RMC14.Weapons.Ranged.Ammo;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Weapons.Ranged.DualTube;

public sealed class RMCDualTubeSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPumpActionSystem _pumpAction = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDualTubeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RMCDualTubeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCDualTubeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RMCDualTubeComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<RMCDualTubeComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<RMCDualTubeComponent, RMCToggleChamberSwapActionEvent>(OnToggleChamberSwapAction);

        CommandBinds.Builder
            .Bind(CMKeyFunctions.RMCToggleShotgunTube,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } user &&
                            _hands.GetActiveItem(user) is { } held &&
                            TryComp(held, out RMCDualTubeComponent? dualTube))
                        {
                            TrySwapTube((held, dualTube), user);
                        }
                    },
                    handle: false))
            .Bind(CMKeyFunctions.RMCCycleFireMode,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } user &&
                            _hands.GetActiveItem(user) is { } held &&
                            TryComp(held, out RMCDualTubeComponent? dualTube) &&
                            _actionBlocker.CanInteract(user, held))
                        {
                            ToggleChamberSwap((held, dualTube), user);
                        }
                    },
                    handle: false))
            .Register<RMCDualTubeSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RMCDualTubeSystem>();
    }

    private void OnComponentInit(Entity<RMCDualTubeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnMapInit(Entity<RMCDualTubeComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out BallisticAmmoProviderComponent? ballistic) || ballistic.Proto == null)
            return;

        ent.Comp.UnspawnedCount = HasComp<RMCEmptyMagComponent>(ent)
            ? 0
            : Math.Max(0, ballistic.Capacity - ent.Comp.Container.ContainedEntities.Count);
        Dirty(ent);
    }

    private void OnExamined(Entity<RMCDualTubeComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(RMCDualTubeComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-dual-tube-examine-tube",
                ("second", ent.Comp.SecondTubeActive),
                ("count", ent.Comp.Entities.Count + ent.Comp.UnspawnedCount)));
            args.PushMarkup(Loc.GetString("rmc-dual-tube-examine-chamber-swap", ("active", ent.Comp.ChamberSwap)));
        }
    }

    private void OnGetVerbs(Entity<RMCDualTubeComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using != ent.Owner)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("rmc-dual-tube-verb"),
            Act = () => TrySwapTube(ent, user),
        });
    }

    private void OnGetItemActions(Entity<RMCDualTubeComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;

        args.AddAction(ref ent.Comp.ChamberSwapAction, ent.Comp.ChamberSwapActionId);
        Dirty(ent);
        _actions.SetToggled(ent.Comp.ChamberSwapAction, ent.Comp.ChamberSwap);
    }

    private void OnToggleChamberSwapAction(Entity<RMCDualTubeComponent> ent, ref RMCToggleChamberSwapActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleChamberSwap(ent, args.Performer);
    }

    public void ToggleChamberSwap(Entity<RMCDualTubeComponent> ent, EntityUid user)
    {
        ent.Comp.ChamberSwap = !ent.Comp.ChamberSwap;
        Dirty(ent);

        _actions.SetToggled(ent.Comp.ChamberSwapAction, ent.Comp.ChamberSwap);
        _audio.PlayPredicted(ent.Comp.ChamberSwapSound, ent, user);

        var msg = ent.Comp.ChamberSwap
            ? Loc.GetString("rmc-dual-tube-chamber-swap-on")
            : Loc.GetString("rmc-dual-tube-chamber-swap-off");
        _popup.PopupClient(msg, user, user);
    }

    public bool TrySwapTube(Entity<RMCDualTubeComponent> ent, EntityUid user)
    {
        if (_hands.GetActiveItem(user) != ent.Owner)
        {
            _popup.PopupClient(Loc.GetString("rmc-dual-tube-must-hold", ("gun", ent)), user, user, PopupType.SmallCaution);
            return false;
        }

        if (!_actionBlocker.CanInteract(user, ent) ||
            !TryComp(ent, out BallisticAmmoProviderComponent? ballistic))
        {
            return false;
        }

        var tube = new Entity<BallisticAmmoProviderComponent>(ent, ballistic);
        TryComp(ent, out PumpActionComponent? pump);
        var pumped = pump is { Pumped: true };

        if (pumped && ent.Comp.ChamberSwap && ballistic.Count > ballistic.Capacity)
        {
            _popup.PopupClient(Loc.GetString("rmc-dual-tube-overloaded", ("gun", ent)), user, user, PopupType.SmallCaution);
            _gun.EjectBallisticRound(tube);
        }
        else if (pumped && !ent.Comp.ChamberSwap)
        {
            MoveChamberedRound(ent, tube);
        }

        _gun.SwapBallisticAmmo(tube, ent.Comp.Container, ent.Comp.Entities, ref ent.Comp.UnspawnedCount);
        ent.Comp.SecondTubeActive = !ent.Comp.SecondTubeActive;
        Dirty(ent);

        if (ent.Comp.ChamberSwap && pump != null)
            _pumpAction.SetPumped((ent, pump), ballistic.Count > 0);

        _audio.PlayPredicted(ent.Comp.SwitchSound, ent, user);
        return true;
    }

    private void MoveChamberedRound(Entity<RMCDualTubeComponent> ent, Entity<BallisticAmmoProviderComponent> tube)
    {
        if (!_gun.TryTransferBallisticRound(tube, ent.Comp.Container, out var round))
            return;

        if (round != null)
        {
            ent.Comp.Entities.Add(round.Value);
            return;
        }

        if (ent.Comp.Entities.Count == 0)
        {
            ent.Comp.UnspawnedCount++;
            return;
        }

        if (_net.IsClient || tube.Comp.Proto is not { } proto)
            return;

        if (!TrySpawnInContainer(proto, ent, ent.Comp.ContainerId, out var spawned))
            return;

        _stack.SetCount(spawned.Value, 1);
        ent.Comp.Entities.Add(spawned.Value);
    }
}
