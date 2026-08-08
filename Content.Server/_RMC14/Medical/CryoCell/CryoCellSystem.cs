using System.Linq;
using Content.Server.Power.Components;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Bed.Sleep;
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
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
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
    [Dependency] private readonly RMCSizeStunSystem _rmcSizeStun = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;
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
        cell.Comp.Notice = !cell.Comp.Notice;
        Dirty(cell);
        UpdateUI(cell);
    }

    private void OnEjectBeaker(Entity<CryoCellComponent> cell, ref CryoCellEjectBeakerBuiMsg args)
    {
        if (!TryGetBeaker(cell, out var beaker))
            return;

        if (_container.TryGetContainer(cell, cell.Comp.BeakerSlot, out var container))
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
            args.Handled = true;
            return;
        }

        if (!_container.TryGetContainer(cell, cell.Comp.BeakerSlot, out var container))
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
            _solutionContainer.TryGetSolution(beaker, fits.Solution, out _, out var beakerSol))
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
            cell.Comp.CryoCellTemperature,
            cell.Comp.IsPoweredOn,
            cell.Comp.AutoEject,
            cell.Comp.Notice,
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
            if (cell.Occupant == null)
                continue;

            if (!IsPowered(uid))
                continue;

            if (time < cell.NextTick)
                continue;

            cell.NextTick = time + cell.TickDelay;

            ProcessOccupant((uid, cell));
            UpdateUI((uid, cell));
        }
    }

    private void ProcessOccupant(Entity<CryoCellComponent> cell)
    {
        if (cell.Comp.Occupant is not { } occupant)
            return;

        if (!TryComp<DamageableComponent>(occupant, out var damageable))
        {
            EjectOccupant(cell, occupant);
            return;
        }

        // Auto-eject if dead and unrevivable
        if (_mobState.IsDead(occupant) && HasComp<UnrevivableComponent>(occupant))
        {
            _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-patient-dead"), cell);
            _audio.PlayPvs(cell.Comp.WarningSound, cell);
            AutoEjectOccupant(cell, occupant, dead: true);
            return;
        }

        // Cooling the occupant
        var cryoCellTemp = cell.Comp.CryoCellTemperature;
        _rmcTemperature.TryGetCurrentTemperature(occupant, out var curBodyTemp);

        if (Math.Abs(curBodyTemp - cryoCellTemp) >= 0.01)
        {
            var change = 2 * (cryoCellTemp + curBodyTemp);
            var temp = curBodyTemp > cryoCellTemp
                ? Math.Max(cryoCellTemp, curBodyTemp - change)
                : Math.Min(cryoCellTemp, curBodyTemp + change);

            _rmcTemperature.ForceChangeTemperature(occupant, temp);
        }

        // Passive healing if alive and cold enough
        if (!_mobState.IsDead(occupant))
        {
            if (curBodyTemp < Atmospherics.T0C)
            {
                _statusEffects.TryAddStatusEffectDuration(occupant, SleepingSystem.StatusEffectForcedSleeping, cell.Comp.SleepDuration);
                _rmcSizeStun.TryKnockOut(occupant, cell.Comp.UnconsciousDuration, true);

                if (damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup) > 0)
                {
                    var oxyHeal = _rmcDamageable.DistributeHealingCached(occupant, AirlossGroup, 1);
                    _damageable.TryChangeDamage(occupant, oxyHeal, ignoreResistances: true, interruptsDoAfters: false);
                }

                // Severe damage heals slower without proper chemicals
                if (curBodyTemp <= 210f) // 210 Kelvin
                {
                    var bruteDamage = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup);
                    if (bruteDamage > 0)
                    {
                        var bruteHealAmt = FixedPoint2.Min(1, 20 / bruteDamage);
                        var bruteHeal = _rmcDamageable.DistributeHealingCached(occupant, BruteGroup, bruteHealAmt);
                        _damageable.TryChangeDamage(occupant, bruteHeal, ignoreResistances: true, interruptsDoAfters: false);
                    }

                    var burnDamage = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup);
                    if (burnDamage > 0)
                    {
                        var burnHealAmt = FixedPoint2.Min(1, 20 / burnDamage);
                        var burnHeal = _rmcDamageable.DistributeHealingCached(occupant, BurnGroup, burnHealAmt);
                        _damageable.TryChangeDamage(occupant, burnHeal, ignoreResistances: true, interruptsDoAfters: false);
                    }

                    var toxinDamage = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup);
                    if (toxinDamage > 0)
                    {
                        var toxinHealAmt = FixedPoint2.Min(1, 20 / toxinDamage);
                        var toxinHeal = _rmcDamageable.DistributeHealingCached(occupant, ToxinGroup, toxinHealAmt);
                        _damageable.TryChangeDamage(occupant, toxinHeal, ignoreResistances: true, interruptsDoAfters: false);
                    }
                }
            }
        }

        // Chemical healing via beaker
        if (TryGetBeaker(cell, out var beakerEnt) &&
            TryComp<FitsInDispenserComponent>(beakerEnt, out var fits) &&
            _solutionContainer.TryGetSolution(beakerEnt, fits.Solution, out _, out var beakerSol) &&
            _rmcBloodstream.TryGetChemicalSolution(occupant, out var chemSolEnt, out var chemSol))
        {
            if (beakerSol.Volume <= FixedPoint2.Zero)
                return;

            bool HasAtLeastOne(Solution sol, string reagentId)
                => sol.Contents.Any(r => r.Reagent.Prototype == reagentId && r.Quantity >= FixedPoint2.New(1));

            var occupantHasCryo = HasAtLeastOne(chemSol, "cryoxadone") || HasAtLeastOne(chemSol, "clonexadone");
            var beakerHasCryo = HasAtLeastOne(beakerSol, "cryoxadone") || HasAtLeastOne(beakerSol, "clonexadone");

            var canAdminister = (occupantHasCryo ^ beakerHasCryo) && beakerSol.Contents.Count > 0;
            if (canAdminister && occupantHasCryo)
            {
                var occupantSol = chemSol.Contents.Select(r => r.Reagent.Prototype).ToHashSet();
                foreach (var beakerReagent in beakerSol.Contents)
                {
                    if (occupantSol.Contains(beakerReagent.Reagent.Prototype))
                    {
                        canAdminister = false;
                        break;
                    }
                }
            }

            if (canAdminister)
                _solutionContainer.TryTransferSolution(chemSolEnt, beakerSol, cell.Comp.BeakerTransferAmount);
        }

        // Auto-eject when fully healed
        if (cell.Comp.AutoEject)
        {
            if (damageable.TotalDamage <= 0)
            {
                _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-patient-recovered"), cell);
                _audio.PlayPvs(cell.Comp.HealingCompleteSound, cell);
                AutoEjectOccupant(cell, occupant, dead: false);
            }
        }
    }

    private void AutoEjectOccupant(Entity<CryoCellComponent> cell, EntityUid occupant, bool dead)
    {
        cell.Comp.IsPoweredOn = false;

        if (cell.Comp.Notice)
        {
            var reason = dead
                ? Loc.GetString("rmc-cryo-cell-auto-eject-dead")
                : Loc.GetString("rmc-cryo-cell-auto-eject-recovered");
            _popup.PopupCoordinates(
                Loc.GetString("rmc-cryo-cell-auto-eject-popup", ("entity", occupant), ("reason", reason)),
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
