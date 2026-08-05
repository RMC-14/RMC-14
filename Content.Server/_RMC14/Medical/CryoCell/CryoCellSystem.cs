using Content.Server.Power.Components;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    private readonly List<CryoCellBeakerReagent> _beakerReagentBuffer = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, AfterActivatableUIOpenEvent>(OnCellUIOpened);
        SubscribeLocalEvent<CryoCellComponent, CryoCellTogglePowerBuiMsg>(OnTogglePower);
        SubscribeLocalEvent<CryoCellComponent, CryoCellEjectBuiMsg>(OnEject);
        SubscribeLocalEvent<CryoCellComponent, CryoCellToggleAutoEjectBuiMsg>(OnToggleAutoEject);
        SubscribeLocalEvent<CryoCellComponent, CryoCellToggleNotifyBuiMsg>(OnToggleNotify);
        SubscribeLocalEvent<CryoCellComponent, CryoCellEjectBeakerBuiMsg>(OnEjectBeaker);
        SubscribeLocalEvent<CryoCellComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnCellUIOpened(Entity<CryoCellComponent> cell, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(cell);
    }

    private void OnTogglePower(Entity<CryoCellComponent> cell, ref CryoCellTogglePowerBuiMsg args)
    {
        cell.Comp.IsPoweredOn = !cell.Comp.IsPoweredOn;
        var powered = IsPowered(cell);

        Dirty(cell);
        UpdateCryoCellVisuals(cell, powered);
        UpdateUI(cell);
    }

    private void OnEject(Entity<CryoCellComponent> cell, ref CryoCellEjectBuiMsg args)
    {
        if (cell.Comp.Occupant is { } occupant)
            EjectOccupant(cell, occupant);

        UpdateUI(cell);
    }

    private void OnToggleAutoEject(Entity<CryoCellComponent> cell, ref CryoCellToggleAutoEjectBuiMsg args)
    {
        cell.Comp.AutoEject = !cell.Comp.AutoEject;
        Dirty(cell);
        UpdateUI(cell);
    }

    private void OnToggleNotify(Entity<CryoCellComponent> cell, ref CryoCellToggleNotifyBuiMsg args)
    {
        cell.Comp.ReleaseNotice = !cell.Comp.ReleaseNotice;
        Dirty(cell);
        UpdateUI(cell);
    }

    private void OnEjectBeaker(Entity<CryoCellComponent> cell, ref CryoCellEjectBeakerBuiMsg args)
    {
        if (!TryGetBeaker(cell, out var beaker))
            return;

        if (_container.TryGetContainer(cell, cell.Comp.BeakerContainerId, out var container))
            _container.Remove(beaker, container);

        UpdateUI(cell);
    }

    private void OnInteractUsing(Entity<CryoCellComponent> cell, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<FitsInDispenserComponent>(args.Used, out _))
            return;

        if (TryGetBeaker(cell, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-beaker-already-loaded"), cell, args.User);
            args.Handled = true;
            return;
        }

        if (!_container.TryGetContainer(cell, cell.Comp.BeakerContainerId, out var container))
            return;

        if (_container.Insert(args.Used, container))
        {
            args.Handled = true;
            UpdateUI(cell);
        }
    }

    private void UpdateUI(Entity<CryoCellComponent> cell)
    {
        if (!_ui.IsUiOpen(cell.Owner, CryoCellUIKey.Key))
            return;

        var occupant = cell.Comp.Occupant;
        NetEntity? netOccupant = null;
        string? occupantName = null;
        var occupantState = CryoCellOccupantMobState.None;
        var health = 0f;
        var maxHealth = 0f;
        var bruteLoss = 0f;
        var burnLoss = 0f;
        var toxinLoss = 0f;
        var oxyLoss = 0f;
        var bodyTemp = 0f;

        if (occupant != null && TerminatingOrDeleted(occupant))
        {
            cell.Comp.Occupant = null;
            _ui.CloseUi(cell.Owner, CryoCellUIKey.Key);
            return;
        }

        if (occupant != null)
        {
            netOccupant = GetNetEntity(occupant.Value);
            occupantName = Identity.Name(occupant.Value, EntityManager);

            if (_mobState.IsDead(occupant.Value))
                occupantState = CryoCellOccupantMobState.Dead;
            else if (_mobState.IsCritical(occupant.Value))
                occupantState = CryoCellOccupantMobState.Critical;
            else
                occupantState = CryoCellOccupantMobState.Alive;

            if (TryComp<DamageableComponent>(occupant.Value, out var damageable))
            {
                if (_mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Critical, out var critThreshold))
                {
                    maxHealth = (float) critThreshold;
                    health = (float) (critThreshold - damageable.TotalDamage);
                }

                bruteLoss = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup).Float();
                burnLoss = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup).Float();
                toxinLoss = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup).Float();
                oxyLoss = damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup).Float();
            }

            _rmcTemperature.TryGetCurrentTemperature(occupant.Value, out bodyTemp);
        }

        _beakerReagentBuffer.Clear();
        var isBeakerLoaded = false;
        if (TryGetBeaker(cell, out var beaker) &&
            TryComp<FitsInDispenserComponent>(beaker, out var fits) &&
            _solution.TryGetSolution(beaker, fits.Solution, out _, out var beakerSol))
        {
            isBeakerLoaded = true;
            foreach (var reagent in beakerSol.Contents)
            {
                _beakerReagentBuffer.Add(new CryoCellBeakerReagent(reagent.Reagent.Prototype, reagent.Quantity.Float()));
            }
        }

        var state = new CryoCellBuiState(
            netOccupant,
            occupantName,
            occupantState,
            health,
            maxHealth,
            bruteLoss,
            burnLoss,
            toxinLoss,
            oxyLoss,
            bodyTemp,
            cell.Comp.IsPoweredOn,
            cell.Comp.AutoEject,
            cell.Comp.ReleaseNotice,
            isBeakerLoaded,
            _beakerReagentBuffer.ToArray());

        _ui.SetUiState(cell.Owner, CryoCellUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var cells = EntityQueryEnumerator<CryoCellComponent>();
        while (cells.MoveNext(out var uid, out var cell))
        {
            // Periodic UI refresh
            if (_ui.IsUiOpen(uid, CryoCellUIKey.Key) && time >= cell.NextTick)
                UpdateUI((uid, cell));

            if (cell.Occupant == null || !cell.IsPoweredOn)
                continue;

            if (time < cell.NextTick)
                continue;

            cell.NextTick = time + cell.TickDelay;

            if (!IsPowered(uid))
                continue;

            ProcessOccupant((uid, cell));
        }
    }

    private void ProcessOccupant(Entity<CryoCellComponent> cell)
    {
        if (cell.Comp.Occupant is not { } occupant)
            return;

        // Dead occupants are immediately auto-ejected
        if (_mobState.IsDead(occupant))
        {
            _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-patient-dead"), cell.Owner);
            _audio.PlayPvs(cell.Comp.WarningSound, cell.Owner);
            AutoEjectOccupant(cell, occupant, dead: true);
            return;
        }

        // Cool the occupant towards the cell target temperature
        _rmcTemperature.ForceChangeTemperature(occupant, 0f);
        _rmcTemperature.TryGetCurrentTemperature(occupant, out var bodyTemp);

        if (!TryComp<DamageableComponent>(occupant, out var damageable))
            return;

        // Passive oxy healing when body is below freezing
        if (bodyTemp < Atmospherics.T0C &&
            damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup) > 0)
        {
            var healAmount = FixedPoint2.New(cell.Comp.OxyHealAmount);
            var oxyHeal = _rmcDamageable.DistributeHealingCached(occupant, AirlossGroup, healAmount);
            _damageable.TryChangeDamage(occupant, oxyHeal, ignoreResistances: true, interruptsDoAfters: false);
        }

        // Enhanced passive healing at cryo liquid threshold
        if (bodyTemp < cell.Comp.CryoLiquidThreshold)
        {
            if (damageable.DamagePerGroup.GetValueOrDefault(BruteGroup) > 0)
            {
                var bruteGroup = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup);
                var bruteHealAmt = FixedPoint2.Min(
                    FixedPoint2.New(cell.Comp.PassiveBruteHealAmount),
                    FixedPoint2.New(20) / bruteGroup);
                var bruteHeal = _rmcDamageable.DistributeHealingCached(occupant, BruteGroup, bruteHealAmt);
                _damageable.TryChangeDamage(occupant, bruteHeal, ignoreResistances: true, interruptsDoAfters: false);
            }

            if (damageable.DamagePerGroup.GetValueOrDefault(BurnGroup) > 0)
            {
                var burnGroup = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup);
                var burnHealAmt = FixedPoint2.Min(
                    FixedPoint2.New(cell.Comp.PassiveBurnHealAmount),
                    FixedPoint2.New(20) / burnGroup);
                var burnHeal = _rmcDamageable.DistributeHealingCached(occupant, BurnGroup, burnHealAmt);
                _damageable.TryChangeDamage(occupant, burnHeal, ignoreResistances: true, interruptsDoAfters: false);
            }

            if (damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup) > 0)
            {
                var toxGroup = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup);
                var toxHealAmt = FixedPoint2.Min(
                    FixedPoint2.New(cell.Comp.PassiveToxHealAmount),
                    FixedPoint2.New(20) / toxGroup);
                var toxHeal = _rmcDamageable.DistributeHealingCached(occupant, ToxinGroup, toxHealAmt);
                _damageable.TryChangeDamage(occupant, toxHeal, ignoreResistances: true, interruptsDoAfters: false);
            }
        }

        // Chemical healing via beaker
        if (TryGetBeaker(cell, out var beakerEnt) &&
            TryComp<FitsInDispenserComponent>(beakerEnt, out var fits) &&
            _solution.TryGetSolution(beakerEnt, fits.Solution, out var beakerSolEnt, out var beakerSol) &&
            beakerSol.Volume > 0 &&
            _rmcBloodstream.TryGetChemicalSolution(occupant, out var chemSolEnt, out _))
        {
            _solution.TryTransferSolution(chemSolEnt, beakerSol, FixedPoint2.New(cell.Comp.BeakerTransferAmount));
            if (beakerSolEnt is { } beakerSolEntity)
                _solution.UpdateChemicals(beakerSolEntity);
        }

        // Auto-eject when fully healed
        if (cell.Comp.AutoEject)
        {
            // Re-read after healing
            if (!TryComp<DamageableComponent>(occupant, out var healCheck))
                return;

            var bruteLeft = healCheck.DamagePerGroup.GetValueOrDefault(BruteGroup);
            var burnLeft = healCheck.DamagePerGroup.GetValueOrDefault(BurnGroup);
            var toxLeft = healCheck.DamagePerGroup.GetValueOrDefault(ToxinGroup);

            if (bruteLeft <= 0 && burnLeft <= 0 && toxLeft <= 0)
            {
                _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-patient-recovered"), cell.Owner);
                _audio.PlayPvs(cell.Comp.HealingCompleteSound, cell.Owner);
                AutoEjectOccupant(cell, occupant, dead: false);
            }
        }
    }

    private void AutoEjectOccupant(Entity<CryoCellComponent> cell, EntityUid occupant, bool dead)
    {
        cell.Comp.IsPoweredOn = false;

        if (cell.Comp.ReleaseNotice)
        {
            var reason = dead
                ? Loc.GetString("rmc-cryo-cell-auto-eject-dead")
                : Loc.GetString("rmc-cryo-cell-auto-eject-recovered");
            _popup.PopupCoordinates(
                Loc.GetString("rmc-cryo-cell-auto-eject-announcement", ("entity", occupant), ("reason", reason)),
                Transform(cell).Coordinates,
                PopupType.Large);
        }

        EjectOccupant(cell, occupant);
        Dirty(cell);
        UpdateCryoCellVisuals(cell, IsPowered(cell));
    }

    private bool IsPowered(EntityUid uid)
    {
        return !TryComp<ApcPowerReceiverComponent>(uid, out var receiver) || receiver.Powered;
    }
}
