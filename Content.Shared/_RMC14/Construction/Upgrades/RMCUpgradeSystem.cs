using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction.Upgrades;

public sealed class RMCUpgradeSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedXenoAcidSystem _xenoAcid = default!;

    private readonly Dictionary<EntProtoId, RMCConstructionUpgradeComponent> _upgradePrototypes = new();
    private EntityQuery<RMCConstructionUpgradeItemComponent> _upgradeItemQuery;
    private EntityQuery<MultitoolComponent> _downgradeItemQuery;

    public override void Initialize()
    {
        _upgradeItemQuery = GetEntityQuery<RMCConstructionUpgradeItemComponent>();
        _downgradeItemQuery = GetEntityQuery<MultitoolComponent>();

        SubscribeLocalEvent<RMCConstructionUpgradeTargetComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<RMCConstructionUpgradeTargetComponent>(RMCConstructionUpgradeUiKey.Key,
            subs =>
            {
                subs.Event<RMCConstructionUpgradeBuiMsg>(OnUpgradeBuiMsg);
            });

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RefreshUpgradePrototypes();
    }

    private void OnInteractUsing(Entity<RMCConstructionUpgradeTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        var used = args.Used;

        _upgradeItemQuery.TryComp(used, out var upgradeItem);
        _downgradeItemQuery.TryComp(used, out var downgradeItem);

        if (upgradeItem != null && downgradeItem != null)
            return;

        if (upgradeItem != null && !_whitelist.IsValid(upgradeItem.Whitelist, ent))
            return;

        if (!_skills.HasSkill(user, ent.Comp.Skill, ent.Comp.SkillAmountRequired))
        {
            var failPopup = Loc.GetString("rmc-construction-failure", ("ent", ent));
            _popup.PopupClient(failPopup, ent, user, PopupType.SmallCaution);

            args.Handled = true;
            return;
        }

        if (_xenoAcid.IsMelted(ent))
        {
            var failPopup = Loc.GetString("rmc-construction-melted");
            _popup.PopupClient(failPopup, ent, user, PopupType.SmallCaution);

            args.Handled = true;
            return;
        }

        if (downgradeItem != null && ent.Comp.Downgrade != null)
        {
            RemoveUpgrade(ent, user);
            args.Handled = true;
            return;
        }

        if (upgradeItem != null && ent.Comp.Upgrades != null)
        {
            _ui.OpenUi(ent.Owner, RMCConstructionUpgradeUiKey.Key, user);
            args.Handled = true;
        }
    }

    private void RemoveUpgrade(Entity<RMCConstructionUpgradeTargetComponent> ent, EntityUid user)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Downgrade == null)
            return;

        if (!_upgradePrototypes.TryGetValue(ent.Comp.Downgrade.Value, out var upgradeComp))
            return;

        if (upgradeComp.Material != null && upgradeComp.Amount > 0 && _prototypes.TryIndex(upgradeComp.Material.Value, out var stackProto))
        {
            var coordinates = _transform.GetMapCoordinates(ent);
            var materialStack = Spawn(stackProto.Spawn, coordinates);
            if (TryComp<StackComponent>(materialStack, out var stack))
            {
                _stack.SetCount(materialStack, upgradeComp.Amount, stack);
            }
        }

        var downgradePopup = Loc.GetString("rmc-construction-downgrade", ("ent", ent));
        ApplyEntityUpgrade(ent, upgradeComp.BaseEntity, user, downgradePopup);
    }

    private void OnUpgradeBuiMsg(Entity<RMCConstructionUpgradeTargetComponent> ent, ref RMCConstructionUpgradeBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, RMCConstructionUpgradeUiKey.Key);

        var user = args.Actor;

        if (!_upgradePrototypes.TryGetValue(args.Upgrade, out var upgradeComp))
            return;

        EntityUid? upgradeItem = null;

        foreach (var hand in _hands.EnumerateHeld(user))
        {
            if (!_upgradeItemQuery.HasComp(hand))
                continue;

            upgradeItem = hand;
            break;
        }

        if (upgradeItem == null)
            return;

        if (upgradeComp.Material != null)
        {
            if (TryComp<StackComponent>(upgradeItem, out var stack) && stack.StackTypeId == upgradeComp.Material)
            {
                if (!_stack.Use(upgradeItem.Value, upgradeComp.Amount, stack))
                {
                    var failPopup = Loc.GetString(upgradeComp.FailurePopup, ("ent", ent));
                    _popup.PopupClient(failPopup, ent, user, PopupType.SmallCaution);
                    return;
                }
            }
        }
        else
        {
            QueueDel(upgradeItem);
        }

        var upgradePopup = Loc.GetString(upgradeComp.UpgradedPopup);
        ApplyEntityUpgrade(ent, upgradeComp.UpgradedEntity, user, upgradePopup);
    }

    private void ApplyEntityUpgrade(Entity<RMCConstructionUpgradeTargetComponent> ent,
        EntProtoId targetEntity,
        EntityUid user,
        string popup)
    {
        if (_net.IsClient)
            return;

        var coordinates = _transform.GetMapCoordinates(ent);
        var rotation = _transform.GetWorldRotation(ent);

        DamageSpecifier? transferredDamage = null;

        if (TryComp<DamageableComponent>(ent, out var damageComp))
            transferredDamage = damageComp.Damage;

        var spawn = Spawn(targetEntity, coordinates, rotation: rotation.GetCardinalDir().ToAngle());
        _popup.PopupEntity(popup, spawn, user);

        // transfer damage
        if (transferredDamage != null && TryComp<DamageableComponent>(spawn, out var newDamageComp))
            _damageable.SetDamage(spawn, newDamageComp, transferredDamage);

        var upgradeEv = new RMCConstructionUpgradedEvent(spawn, ent.Owner);
        RaiseLocalEvent(ent.Owner, upgradeEv);
        RaiseLocalEvent(spawn, upgradeEv, broadcast: true);

        QueueDel(ent.Owner);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            RefreshUpgradePrototypes();
    }

    private void RefreshUpgradePrototypes()
    {
        _upgradePrototypes.Clear();

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.TryGetComponent(out RMCConstructionUpgradeComponent? upgrade, _compFactory))
                _upgradePrototypes[prototype.ID] = upgrade;
        }
    }
}

/// <summary>
///     This event that is raised when an entity is upgraded
/// </summary>
public sealed class RMCConstructionUpgradedEvent : EntityEventArgs
{
    public readonly EntityUid New;
    public readonly EntityUid Old;

    public RMCConstructionUpgradedEvent(EntityUid newUid, EntityUid oldUid)
    {
        New = newUid;
        Old = oldUid;
    }
}
