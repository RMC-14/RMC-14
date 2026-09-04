using Content.Server.Power.Components;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.CryoCell;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.StatusEffectNew;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.CryoCell;

public sealed class CryoCellSystem : SharedCryoCellSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCSizeStunSystem _rmcSizeStun = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    private static readonly ProtoId<ReagentPrototype> Cryoxadone = "CMCryoxadone";
    private static readonly ProtoId<ReagentPrototype> Clonexadone = "CMClonexadone";

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
        SubscribeLocalEvent<CryoCellComponent, PowerChangedEvent>(OnCryoCellPower);
    }

    private void OnCellUIOpened(Entity<CryoCellComponent> cryoCell, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUIState(cryoCell);
    }

    private void OnTogglePower(Entity<CryoCellComponent> cryoCell, ref CryoCellTogglePowerBuiMsg args)
    {
        cryoCell.Comp.IsPoweredOn = !cryoCell.Comp.IsPoweredOn;

        Dirty(cryoCell);
        UpdateCryoCellVisuals(cryoCell);
        UpdateUIState(cryoCell);
    }

    private void OnEject(Entity<CryoCellComponent> cryoCell, ref CryoCellEjectBuiMsg args)
    {
        if (cryoCell.Comp.Occupant is { } occupant)
            EjectOccupant(cryoCell, occupant);
        else
            UpdateCryoCellVisuals(cryoCell);

        UpdateUIState(cryoCell);
    }

    private void OnToggleAutoEject(Entity<CryoCellComponent> cryoCell, ref CryoCellToggleAutoEjectBuiMsg args)
    {
        cryoCell.Comp.AutoEject = !cryoCell.Comp.AutoEject;

        Dirty(cryoCell);
        UpdateUIState(cryoCell);
    }

    private void OnToggleNotify(Entity<CryoCellComponent> cryoCell, ref CryoCellToggleNotifyBuiMsg args)
    {
        cryoCell.Comp.ReleaseNotice = !cryoCell.Comp.ReleaseNotice;

        Dirty(cryoCell);
        UpdateUIState(cryoCell);
    }

    private void OnEjectBeaker(Entity<CryoCellComponent> cryoCell, ref CryoCellEjectBeakerBuiMsg args)
    {
        if (!_itemSlots.TryGetSlot(cryoCell, cryoCell.Comp.BeakerSlot, out var slot))
            return;

        _itemSlots.TryEjectToHands(cryoCell, slot, args.Actor, true);
        Dirty(cryoCell);
        UpdateUIState(cryoCell);
    }

    private void UpdateUIState(Entity<CryoCellComponent> cryoCell)
    {
        if (!_ui.IsUiOpen(cryoCell.Owner, CryoCellUIKey.Key))
            return;

        var occupant = cryoCell.Comp.Occupant;
        cryoCell.Comp.UIOccupant = null;
        cryoCell.Comp.UIOccupantName = null;
        cryoCell.Comp.UIOccupantState = CryoCellOccupantMobState.None;
        cryoCell.Comp.UIHealth = 0f;
        cryoCell.Comp.UIMaxHealth = 0f;
        cryoCell.Comp.UIBruteLoss = 0f;
        cryoCell.Comp.UIBurnLoss = 0f;
        cryoCell.Comp.UIToxinLoss = 0f;
        cryoCell.Comp.UIOxygenLoss = 0f;
        cryoCell.Comp.UIBodyTemperature = 0f;
        cryoCell.Comp.UIIsBeakerLoaded = false;
        cryoCell.Comp.UIBeakerContents = [];

        if (occupant != null && TerminatingOrDeleted(occupant))
        {
            cryoCell.Comp.Occupant = null;
            _ui.CloseUi(cryoCell.Owner, CryoCellUIKey.Key);
            Dirty(cryoCell);
            return;
        }

        if (occupant is { } occupantUid)
        {
            cryoCell.Comp.UIOccupant = GetNetEntity(occupantUid);
            cryoCell.Comp.UIOccupantName = Identity.Name(occupantUid, EntityManager);

            if (_mobState.IsDead(occupantUid))
                cryoCell.Comp.UIOccupantState = CryoCellOccupantMobState.Dead;
            else if (_mobState.IsCritical(occupantUid))
                cryoCell.Comp.UIOccupantState = CryoCellOccupantMobState.Critical;
            else
                cryoCell.Comp.UIOccupantState = CryoCellOccupantMobState.Alive;

            if (TryComp<DamageableComponent>(occupantUid, out var damageable))
            {
                if (_mobThreshold.TryGetThresholdForState(occupantUid, MobState.Critical, out var critThreshold))
                {
                    cryoCell.Comp.UIMaxHealth = (float) critThreshold;
                    cryoCell.Comp.UIHealth = (float) (critThreshold - damageable.TotalDamage);
                }

                cryoCell.Comp.UIBruteLoss = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup).Float();
                cryoCell.Comp.UIBurnLoss = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup).Float();
                cryoCell.Comp.UIToxinLoss = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup).Float();
                cryoCell.Comp.UIOxygenLoss = damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup).Float();
            }

            _rmcTemperature.TryGetCurrentTemperature(occupantUid, out var bodyTemp);
            cryoCell.Comp.UIBodyTemperature = bodyTemp;
        }

        _beakerReagentBuffer.Clear();
        if (_itemSlots.TryGetSlot(cryoCell, cryoCell.Comp.BeakerSlot, out var slot) &&
            slot.ContainerSlot?.ContainedEntity is { } contained &&
            TryComp(contained, out FitsInDispenserComponent? fits) &&
            _solution.TryGetSolution(contained, fits.Solution, out _, out var beakerSol))
        {
            cryoCell.Comp.UIIsBeakerLoaded = true;
            foreach (var reagent in beakerSol.Contents)
            {
                _beakerReagentBuffer.Add(new CryoCellBeakerReagent(reagent.Reagent.Prototype, reagent.Quantity.Float()));
            }

            cryoCell.Comp.UIBeakerContents = _beakerReagentBuffer.ToArray();
        }

        Dirty(cryoCell);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var cells = EntityQueryEnumerator<CryoCellComponent>();
        while (cells.MoveNext(out var uid, out var cryoCell))
        {
            if (cryoCell.Occupant == null)
                continue;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                continue;

            if (time < cryoCell.NextUpdate)
                continue;

            cryoCell.NextUpdate = time + cryoCell.UpdateInterval;

            ProcessOccupant((uid, cryoCell));
            UpdateUIState((uid, cryoCell));
        }
    }

    private void ProcessOccupant(Entity<CryoCellComponent> cryoCell)
    {
        if (cryoCell.Comp.Occupant is not { } occupant)
            return;

        if (!TryComp<DamageableComponent>(occupant, out var damageable))
        {
            CryoPopupAndSound(cryoCell, "rmc-cryo-cell-popup-incompatible", false, true);
            EjectOccupant(cryoCell, occupant);
            return;
        }

        // Auto-eject if dead and unrevivable
        if (_mobState.IsDead(occupant) && _unrevivable.IsUnrevivable(occupant))
        {
            CryoPopupAndSound(cryoCell, "rmc-cryo-cell-popup-dead", false, true);
            EjectOccupant(cryoCell, occupant, true, true);
            return;
        }

        // Warnings if dead and revivable
        if (_mobState.IsDead(occupant) && !_unrevivable.IsUnrevivable(occupant))
        {
            var stage = _unrevivable.GetUnrevivableStage(occupant, 10);
            switch (stage)
            {
                case >= 8: // One minute left
                    CryoPopupAndSound(cryoCell, "rmc-cryo-cell-popup-revive-now", false, true);
                    break;
                case >= 5: // Halfway to unrevivable
                    CryoPopupAndSound(cryoCell, "rmc-cryo-cell-popup-warning", false, true);
                    break;
            }
        }

        // Cooling the occupant
        var cryoCellTemp = cryoCell.Comp.CryoCellTemperature;
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
                _statusEffects.TrySetStatusEffectDuration(occupant, SleepingSystem.StatusEffectForcedSleeping, cryoCell.Comp.SleepDuration);
                _rmcSizeStun.TryKnockOut(occupant, cryoCell.Comp.UnconsciousDuration);

                if (damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup) > 0)
                {
                    var oxyHeal = _rmcDamageable.DistributeHealingCached(occupant, AirlossGroup, 1);
                    _damageable.TryChangeDamage(occupant, oxyHeal, ignoreResistances: true, interruptsDoAfters: false);
                }

                // Severe damage heals slower without proper chemicals
                if (curBodyTemp <= cryoCell.Comp.BodyTempCryoLiquidThreshold)
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

        // Chemical healing if there are cryo meds
        if (_itemSlots.TryGetSlot(cryoCell, cryoCell.Comp.BeakerSlot, out var slot) &&
            slot.ContainerSlot?.ContainedEntity is { } contained &&
            TryComp(contained, out FitsInDispenserComponent? fits) &&
            _solution.TryGetSolution(contained, fits.Solution, out _, out var beaker) &&
            _rmcBloodstream.TryGetChemicalSolution(occupant, out var solutionEnt, out var bloodStream))
        {
            var beakerHasCryo = HasCryo(beaker);
            var occupantHasCryo = HasCryo(bloodStream);

            // To administer, either the occupant has cryo meds and the beaker doesn't, or vice versa (not both).
            var canAdminister = (occupantHasCryo ^ beakerHasCryo) && beaker.Contents.Count > 0;
            if (canAdminister && occupantHasCryo)
            {
                // Pace out the dosage by making sure the occupant doesn't already have any of the beaker's reagents.
                var occupantReagents = new HashSet<string>();
                foreach (var reagent in bloodStream.Contents)
                {
                    occupantReagents.Add(reagent.Reagent.Prototype);
                }

                foreach (var beakerReagent in beaker.Contents)
                {
                    if (occupantReagents.Contains(beakerReagent.Reagent.Prototype))
                    {
                        canAdminister = false;
                        break;
                    }
                }
            }

            if (canAdminister)
                _solution.TryTransferSolution(solutionEnt, beaker, cryoCell.Comp.BeakerTransferAmount);
        }

        // Auto-eject when fully healed
        if (cryoCell.Comp.AutoEject)
        {
            if (damageable.TotalDamage <= 0)
            {
                CryoPopupAndSound(cryoCell, "rmc-cryo-cell-popup-healed");
                EjectOccupant(cryoCell, occupant, isAutoEject: true);
            }
        }
    }

    private bool HasCryo(Solution solution)
    {
        foreach (var (reagent, quantity) in solution)
        {
            if (quantity < 1)
                continue;

            if (reagent.Prototype == Cryoxadone || reagent.Prototype == Clonexadone)
                return true;
        }

        return false;
    }

    private void OnCryoCellPower(Entity<CryoCellComponent> cryoCell, ref PowerChangedEvent args)
    {
        if (TryComp<ApcPowerReceiverComponent>(cryoCell, out var power) && power.Powered)
            return;

        _ui.CloseUi(cryoCell.Owner, CryoCellUIKey.Key);
        UpdateCryoCellVisuals(cryoCell);
    }
}
