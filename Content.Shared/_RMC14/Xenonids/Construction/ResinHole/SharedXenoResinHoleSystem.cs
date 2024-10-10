using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Examine;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinHole;

public abstract partial class SharedXenoResinHoleSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly CMHandsSystem _rmcHands = default!;
    [Dependency] protected readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] protected readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly AreaSystem _areas = default!;
    [Dependency] private readonly SharedXenoAnnounceSystem _announce = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoResinHoleComponent, InteractUsingEvent>(OnPlaceParasiteInXenoResinHole);
        SubscribeLocalEvent<XenoResinHoleComponent, ActivateInWorldEvent>(OnActivateInWorldResinHole);

        SubscribeLocalEvent<XenoResinHoleComponent, XenoResinHoleActivationEvent>(OnResinHoleActivation);
        SubscribeLocalEvent<XenoResinHoleComponent, GettingAttackedAttemptEvent>(OnXenoResinHoleAttacked);
        SubscribeLocalEvent<XenoResinHoleComponent, DamageChangedEvent>(OnXenoResinHoleTakeDamage);
        SubscribeLocalEvent<XenoResinHoleComponent, StepTriggerAttemptEvent>(OnXenoResinHoleStepTriggerAttempt);
        SubscribeLocalEvent<XenoResinHoleComponent, StepTriggeredOffEvent>(OnXenoResinHoleStepTriggered);

        SubscribeLocalEvent<XenoResinHoleComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<XenoResinHoleComponent> resinHole, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(XenoResinHoleComponent)))
        {
            if (resinHole.Comp.TrapPrototype == null)
                args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-empty"));
            else
            {
                switch (resinHole.Comp.TrapPrototype)
                {
                    case XenoResinHoleComponent.AcidGasPrototype:
                    case XenoResinHoleComponent.NeuroGasPrototype:
                        args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-gas"));
                        break;
                    case XenoResinHoleComponent.WeakAcidPrototype:
                    case XenoResinHoleComponent.AcidPrototype:
                    case XenoResinHoleComponent.StrongAcidPrototype:
                        args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-acid"));
                        break;
                    case XenoResinHoleComponent.ParasitePrototype:
                        args.PushMarkup(Loc.GetString("rmc-xeno-construction-resin-hole-parasite"));
                        break;
                }
            }
        }
    }

    private void OnXenoResinHoleStepTriggerAttempt(Entity<XenoResinHoleComponent> resinHole, ref StepTriggerAttemptEvent args)
    {
        var (ent, comp) = resinHole;

        if (comp.TrapPrototype is null)
        {
            args.Continue = false;
            return;
        }

        if (_hive.FromSameHive(args.Tripper, resinHole.Owner))
        {
            args.Continue = false;
            return;
        }

        if (comp.TrapPrototype == XenoResinHoleComponent.ParasitePrototype)
        {
            if (!_interaction.InRangeUnobstructed(args.Source, args.Tripper, comp.ParasiteActivationRange))
            {
                args.Continue = false;
                return;
            }

            args.Continue = HasComp<InfectableComponent>(args.Tripper) &&
                !HasComp<VictimInfectedComponent>(args.Tripper);
            return;
        }
        else if (_mobState.IsDead(args.Tripper) || _standing.IsDown(args.Tripper))
        {
            args.Continue = false;
            return;
        }

        args.Continue = true;
    }

    private void OnXenoResinHoleStepTriggered(Entity<XenoResinHoleComponent> resinHole, ref StepTriggeredOffEvent args)
    {
        if (resinHole.Comp.TrapPrototype == XenoResinHoleComponent.ParasitePrototype && _net.IsServer)
            _stun.TryParalyze(args.Tripper, resinHole.Comp.StepStunDuration, true);
        ActivateTrap(resinHole);
    }

    private void OnXenoResinHoleAttacked(Entity<XenoResinHoleComponent> resinHole, ref GettingAttackedAttemptEvent args)
    {
        if (_hive.FromSameHive(args.Attacker, resinHole.Owner) && resinHole.Comp.TrapPrototype != null)
            args.Cancelled = true;
    }

    protected bool CanPlaceInHole(EntityUid uid, Entity<XenoResinHoleComponent> resinHole, EntityUid user)
    {
        if (!HasComp<XenoParasiteComponent>(uid) ||
            _mobState.IsDead(uid))
        {
            return false;
        }

        if (resinHole.Comp.TrapPrototype is not null)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-construction-resin-hole-full"), resinHole, user);
            return false;
        }

        if (!_rmcHands.IsPickupByAllowed(uid, user))
            return false;

        if (!HasComp<ParasiteAIComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-awake-child", ("parasite", uid)), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }
    private void OnPlaceParasiteInXenoResinHole(Entity<XenoResinHoleComponent> resinHole, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (!CanPlaceInHole(args.Used, resinHole, args.User))
            return;

        var ev = new XenoPlaceParasiteInHoleDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, resinHole.Comp.AddParasiteDelay, ev, resinHole, resinHole, args.Used)
        {
            BreakOnDropItem = true,
            BreakOnMove = true,
            BreakOnHandChange = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-filling-parasite"), resinHole, args.User);

    }

    private void OnActivateInWorldResinHole(Entity<XenoResinHoleComponent> resinHole, ref ActivateInWorldEvent args)
    {
        if (!HasComp<XenoParasiteComponent>(args.User))
            return;

        args.Handled = true;

        if (_mobState.IsDead(args.User))
            return;

        if (_net.IsClient)
            return;

        if (resinHole.Comp.TrapPrototype != null)
            return;

        resinHole.Comp.TrapPrototype = XenoResinHoleComponent.ParasitePrototype;
        Dirty(resinHole);
        _popup.PopupEntity(Loc.GetString("rmc-xeno-construction-resin-hole-enter-parasite", ("parasite", args.User)), resinHole);
        // Yes, parasites claim any resin traps as their own
        _hive.SetSameHive(args.User, resinHole.Owner);
        QueueDel(args.User);

        _appearanceSystem.SetData(resinHole.Owner, XenoResinHoleVisuals.Contained, ContainedTrap.Parasite);
    }

    private void OnXenoResinHoleTakeDamage(Entity<XenoResinHoleComponent> resinHole, ref DamageChangedEvent args)
    {
        if (args.Origin is { } origin && _hive.FromSameHive(origin, resinHole.Owner))
            return;

        //TODO Flames should make the trigger message never happen but destroyed will
        if (args.DamageDelta == null)
            return;

        var destroyed = args.Damageable.TotalDamage + args.DamageDelta.GetTotal() > resinHole.Comp.TotalHealth;
        ActivateTrap(resinHole, destroyed);
    }

    private void OnResinHoleActivation(Entity<XenoResinHoleComponent> ent, ref XenoResinHoleActivationEvent args)
    {
        if (_hive.GetHive(ent.Owner) is not { } hive)
            return;

        var locationName = "Unknown";

        if (_areas.TryGetArea(ent, out _, out var areaProto, out _))
            locationName = areaProto.Name;

        var msg = Loc.GetString(args.message, ("location", locationName), ("type", GetTrapTypeName(ent)));
        _announce.AnnounceToHive(ent.Owner, hive, msg, color: ent.Comp.MessageColor);
    }

    public string GetTrapTypeName(Entity<XenoResinHoleComponent> resinHole)
    {
        switch(resinHole.Comp.TrapPrototype)
        {
            case XenoResinHoleComponent.ParasitePrototype:
                return Loc.GetString("rmc-xeno-construction-resin-hole-parasite-name");
            case XenoResinHoleComponent.AcidGasPrototype:
            case XenoResinHoleComponent.NeuroGasPrototype:
                return Loc.GetString("rmc-xeno-construction-resin-hole-gas-name");
            case XenoResinHoleComponent.WeakAcidPrototype:
            case XenoResinHoleComponent.AcidPrototype:
            case XenoResinHoleComponent.StrongAcidPrototype:
                return Loc.GetString("rmc-xeno-construction-resin-hole-acid-name");
            default:
                return Loc.GetString("rmc-xeno-construction-resin-hole-empty-name");
        }
    }

    protected virtual void ActivateTrap(Entity<XenoResinHoleComponent> resinHole, bool destroyed = false) { }
}

[Serializable, NetSerializable]
public sealed partial class XenoResinHoleActivationEvent : EntityEventArgs
{
    public LocId message;

    public XenoResinHoleActivationEvent(LocId msg)
    {
        message = msg;
    }
}

[Serializable, NetSerializable]
public sealed partial class XenoPlaceParasiteInHoleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoPlaceFluidInHoleDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId ResinHolePrototype;
    public int PlasmaCost;

    public XenoPlaceResinHoleDestroyWeedSourceDoAfterEvent(EntProtoId resinHolePrototype, int plasmaCost)
    {
        ResinHolePrototype = resinHolePrototype;
        PlasmaCost = plasmaCost;
    }
}
