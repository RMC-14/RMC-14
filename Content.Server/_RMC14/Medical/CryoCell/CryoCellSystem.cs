using System.Linq;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, AfterActivatableUIOpenEvent>(OnUiOpen);
        SubscribeLocalEvent<CryoCellComponent, CryoCellTogglePowerBuiMsg>(OnUiTogglePower);
        SubscribeLocalEvent<CryoCellComponent, CryoCellEjectBuiMsg>(OnUiEject);
        SubscribeLocalEvent<CryoCellComponent, CryoCellToggleAutoEjectBuiMsg>(OnUiToggleAutoEject);
        SubscribeLocalEvent<CryoCellComponent, CryoCellEjectBeakerBuiMsg>(OnUiEjectBeaker);
        SubscribeLocalEvent<CryoCellComponent, CryoCellToggleNotifyBuiMsg>(OnUiToggleNotify);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnCryoEntInserted);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnCryoEntRemoved);
    }

    private void OnUiOpen(Entity<CryoCellComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(ent);
    }

    private void OnUiTogglePower(Entity<CryoCellComponent> ent, ref CryoCellTogglePowerBuiMsg args)
    {
        ent.Comp.On = !ent.Comp.On;
        if (ent.Comp.On)
            ent.Comp.NextTick = _timing.CurTime + ent.Comp.TickDelay;

        Dirty(ent);
        UpdateUI(ent);
    }

    private void OnUiEject(Entity<CryoCellComponent> ent, ref CryoCellEjectBuiMsg args)
    {
        if (ent.Comp.Occupant is { } occupant)
            EjectOccupant(ent, occupant);

        UpdateUI(ent);
    }

    private void OnUiToggleAutoEject(Entity<CryoCellComponent> ent, ref CryoCellToggleAutoEjectBuiMsg args)
    {
        ent.Comp.AutoEject = !ent.Comp.AutoEject;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void OnUiEjectBeaker(Entity<CryoCellComponent> ent, ref CryoCellEjectBeakerBuiMsg args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.BeakerSlot, out var cont) || cont.ContainedEntities.Count == 0)
            return;

        var beaker = cont.ContainedEntities.First();
        _container.Remove(beaker, cont);
        _popup.PopupEntity(Loc.GetString("rmc-cryocell-beaker-ejected", ("entity", beaker)), ent);
        UpdateUI(ent);
    }

    private void OnUiToggleNotify(Entity<CryoCellComponent> ent, ref CryoCellToggleNotifyBuiMsg args)
    {
        ent.Comp.ReleaseNotice = !ent.Comp.ReleaseNotice;
        Dirty(ent);
        UpdateUI(ent);
    }

    private void OnCryoEntInserted(Entity<CryoCellComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        ent.Comp.Occupant = args.Entity;
        Dirty(ent);
        UpdateCryoVisuals(ent);
        UpdateUI(ent);
    }

    private void OnCryoEntRemoved(Entity<CryoCellComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ent.Comp.Occupant == args.Entity)
        {
            ent.Comp.Occupant = null;
            Dirty(ent);
        }

        UpdateCryoVisuals(ent);
        UpdateUI(ent);
    }

    private void UpdateUI(Entity<CryoCellComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, CryoCellUIKey.Key))
            return;

        var comp = ent.Comp;
        NetEntity? occupantNet = null;
        var occupantName = string.Empty;
        var health = 0f;
        var maxHealth = 0f;
        var bodyTemp = 0f;
        var hasBeaker = TryGetBeaker(ent.Owner, comp, out _);

        if (comp.Occupant is { } occupant)
        {
            occupantNet = GetNetEntity(occupant);
            health = GetEntityHealth(occupant);
            maxHealth = GetEntityMaxHealth();
            bodyTemp = GetEntityBodyTemperature(occupant);
            occupantName = MetaData(occupant).EntityName;
        }

        var state = new CryoCellBuiState(occupantNet, occupantName, health, maxHealth, bodyTemp, comp.On, hasBeaker, comp.AutoEject, comp.ReleaseNotice);
        _ui.SetUiState(ent.Owner, CryoCellUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CryoCellComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.On || comp.NextTick > now)
                continue;

            comp.NextTick = now + comp.TickDelay;
            Dirty(uid, comp);
            comp.Temperature -= comp.TemperatureDropPerTick;

            if (comp.Occupant is not { } occupant)
                continue;

            if (_mobState.IsDead(occupant))
            {
                EjectOccupant((uid, comp), occupant);
                if (comp.ReleaseNotice)
                    _popup.PopupEntity(Loc.GetString("rmc-cryocell-auto-eject-dead", ("entity", occupant)), uid);
                continue;
            }

            _rmcTemperature.ForceChangeTemperature(occupant, comp.Temperature);

            if (comp.Temperature <= 260f)
                ApplyHealing(occupant, comp);

            if (comp.AutoEject && ShouldAutoEject(occupant, comp))
            {
                EjectOccupant((uid, comp), occupant);
                if (comp.ReleaseNotice)
                    _popup.PopupEntity(Loc.GetString("rmc-cryocell-auto-eject-recovered", ("entity", occupant)), uid);
            }
        }
    }

    private void ApplyHealing(EntityUid occupant, CryoCellComponent comp)
    {
        if (!TryComp<DamageableComponent>(occupant, out var damageable))
            return;

        if (damageable.DamagePerGroup.GetValueOrDefault(BruteGroup) > 0)
        {
            var healing = _rmcDamageable.DistributeHealingCached(occupant, BruteGroup, 0.25);
            _damageable.TryChangeDamage(occupant, healing, true, false);
        }

        if (damageable.DamagePerGroup.GetValueOrDefault(BurnGroup) > 0)
        {
            var healing = _rmcDamageable.DistributeHealingCached(occupant, BurnGroup, 0.25);
            _damageable.TryChangeDamage(occupant, healing, true, false);
        }

        if (damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup) > 0)
        {
            var healing = _rmcDamageable.DistributeHealingCached(occupant, ToxinGroup, 0.25);
            _damageable.TryChangeDamage(occupant, healing, true, false);
        }

        if (damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup) > 0)
        {
            var healing = _rmcDamageable.DistributeHealingCached(occupant, AirlossGroup, 1);
            _damageable.TryChangeDamage(occupant, healing, true, false);
        }
    }

    private bool ShouldAutoEject(EntityUid occupant, CryoCellComponent comp)
    {
        if (!comp.AutoEject)
            return false;

        if (_mobState.IsDead(occupant))
            return true;

        if (TryComp<DamageableComponent>(occupant, out var damageable))
            return damageable.TotalDamage <= 0;

        return false;
    }

    private float GetEntityHealth(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return 100f;

        return Math.Max(0f, 100f - (float) damageable.TotalDamage);
    }

    private float GetEntityMaxHealth()
    {
        return 100f;
    }

    private float GetEntityBodyTemperature(EntityUid uid)
    {
        return _rmcTemperature.GetTemperature(uid);
    }
}
