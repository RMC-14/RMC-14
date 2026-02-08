using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Medical.MedicalPods;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.MedicalPods;

public sealed class SleeperSystem : SharedSleeperSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _temperature = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly List<ProtoId<ReagentPrototype>> _reagentRemovalBuffer = new();
    private readonly HashSet<string> _emergencyChemLookup = new();
    private readonly HashSet<string> _nonTransferableLookup = new();

    private const string BruteGroup = "Brute";
    private const string BurnGroup = "Burn";
    private const string ToxinGroup = "Toxin";
    private const string AirlossGroup = "Airloss";
    private const string GeneticGroup = "Genetic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SleeperComponent, MobStateChangedEvent>(OnSleeperMobStateChanged);
        SubscribeLocalEvent<SleeperConsoleComponent, AfterActivatableUIOpenEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperInjectChemicalBuiMsg>(OnConsoleInjectChemical);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperToggleFilterBuiMsg>(OnConsoleToggleFilter);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperEjectBuiMsg>(OnConsoleEject);
        SubscribeLocalEvent<SleeperConsoleComponent, SleeperAutoEjectDeadBuiMsg>(OnConsoleAutoEjectDead);
    }

    private void OnSleeperMobStateChanged(Entity<SleeperComponent> sleeper, ref MobStateChangedEvent args)
    {
        if (sleeper.Comp.Occupant != args.Target)
            return;

        UpdateSleeperVisuals(sleeper);

        if (!sleeper.Comp.AutoEjectDead)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            _audio.PlayPvs(sleeper.Comp.AutoEjectDeadSound, sleeper);
            EjectOccupant(sleeper, args.Target);
        }
    }

    private void OnConsoleUIOpened(Entity<SleeperConsoleComponent> console, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUI(console);
    }

    private void OnConsoleInjectChemical(Entity<SleeperConsoleComponent> console, ref SleeperInjectChemicalBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId ||
            !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        if (sleeper.Occupant is not { } occupant)
            return;

        if (_mobState.IsDead(occupant))
            return;

        if (!sleeper.InjectionAmounts.Contains(args.Amount))
            return;

        // Build emergency chem lookup for O(1) checks
        _emergencyChemLookup.Clear();
        foreach (var chem in sleeper.EmergencyChemicals)
        {
            _emergencyChemLookup.Add(chem);
        }

        var isAvailable = sleeper.AvailableChemicals.Contains(args.Chemical);
        var isEmergency = _emergencyChemLookup.Contains(args.Chemical);
        if (!isAvailable && !isEmergency)
            return;

        // Emergency chemicals only work in crisis mode
        if (isEmergency && !isAvailable)
        {
            if (!TryComp<DamageableComponent>(occupant, out var damageable) ||
                damageable.TotalDamage <= sleeper.CrisisMinDamage)
                return;
        }

        if (!_solution.TryGetSolution(occupant, "chemicals", out var chemSolEnt, out var chemSol))
            return;

        var reagent = new ReagentId(args.Chemical, null);
        var currentAmount = chemSol.GetReagentQuantity(reagent);
        if (currentAmount + args.Amount > sleeper.MaxChemical)
            return;

        _solution.TryAddReagent(chemSolEnt.Value, args.Chemical, args.Amount);

        UpdateUI(console);
    }

    private void OnConsoleToggleFilter(Entity<SleeperConsoleComponent> console, ref SleeperToggleFilterBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId ||
            !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        ToggleDialysis((sleeperId, sleeper));
        UpdateUI(console);
    }

    private void OnConsoleEject(Entity<SleeperConsoleComponent> console, ref SleeperEjectBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId ||
            !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        TryEjectOccupant((sleeperId, sleeper), args.Actor);
        UpdateUI(console);
    }

    private void OnConsoleAutoEjectDead(Entity<SleeperConsoleComponent> console, ref SleeperAutoEjectDeadBuiMsg args)
    {
        if (console.Comp.LinkedSleeper is not { } sleeperId ||
            !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        sleeper.AutoEjectDead = args.Enabled;
        Dirty(sleeperId, sleeper);
        UpdateUI(console);
    }

    private void UpdateUI(Entity<SleeperConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, SleeperUIKey.Key))
            return;

        // If no sleeper is connected, the UI shouldn't be open (handled by ActivatableUIOpenAttemptEvent)
        if (console.Comp.LinkedSleeper is not { } sleeperId || !TryComp(sleeperId, out SleeperComponent? sleeper))
            return;

        var occupant = sleeper.Occupant;
        NetEntity? netOccupant = null;
        string? occupantName = null;
        var occupantState = SleeperOccupantMobState.None;
        var health = 150f;
        var maxHealth = 150f;
        var minHealth = -50f;
        var totalDamage = FixedPoint2.Zero;
        var bruteLoss = 0f;
        var burnLoss = 0f;
        var toxinLoss = 0f;
        var oxyLoss = 0f;
        var geneticLoss = 0f;
        var hasBlood = false;
        FixedPoint2 bloodLevel = 0;
        var bloodPercent = 0f;
        var bodyTemp = 0f;
        FixedPoint2 totalReagents = 0;

        if (occupant != null && TryComp<DamageableComponent>(occupant, out var damageable))
        {
            netOccupant = GetNetEntity(occupant.Value);
            occupantName = Identity.Name(occupant.Value, EntityManager);

            if (_mobState.IsDead(occupant.Value))
                occupantState = SleeperOccupantMobState.Dead;
            else if (_mobState.IsCritical(occupant.Value))
                occupantState = SleeperOccupantMobState.Critical;
            else
                occupantState = SleeperOccupantMobState.Alive;

            totalDamage = damageable.TotalDamage;

            if (_mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Critical, out var critThreshold) &&
                _mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Dead, out var deadThreshold))
            {
                maxHealth = (float) critThreshold;
                minHealth = (float) (critThreshold - deadThreshold);
                health = (float) (critThreshold - totalDamage);
            }

            bruteLoss = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup).Float();
            burnLoss = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup).Float();
            toxinLoss = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup).Float();
            oxyLoss = damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup).Float();
            geneticLoss = damageable.DamagePerGroup.GetValueOrDefault(GeneticGroup).Float();

            if (TryComp<BloodstreamComponent>(occupant, out var blood) &&
                blood.BloodSolution != null &&
                _solution.TryGetSolution(occupant.Value, blood.BloodSolutionName, out _, out var bloodSol))
            {
                hasBlood = true;
                bloodLevel = bloodSol.Volume;
                var bloodMax = bloodSol.MaxVolume;
                bloodPercent = bloodMax > 0 ? (bloodLevel / bloodMax).Float() * 100f : 0f;
            }

            if (_temperature.TryGetCurrentTemperature(occupant.Value, out var temp))
                bodyTemp = temp;
        }

        // Cache chemical solution to avoid repeated lookups in the loop
        Solution? cachedChemSol = null;
        if (occupant != null)
        {
            _solution.TryGetSolution(occupant.Value, "chemicals", out _, out cachedChemSol);
            if (cachedChemSol != null)
                totalReagents = cachedChemSol.Volume;
        }

        // Build emergency chem lookup for O(1) checks
        _emergencyChemLookup.Clear();
        foreach (var chem in sleeper.EmergencyChemicals)
        {
            _emergencyChemLookup.Add(chem);
        }

        var inCrisis = totalDamage > sleeper.CrisisMinDamage;
        var totalChemCount = sleeper.AvailableChemicals.Length;
        if (inCrisis)
            totalChemCount += sleeper.EmergencyChemicals.Length;

        // Build chemical list - always show AvailableChemicals
        var chemicals = new List<SleeperChemicalData>(totalChemCount);
        foreach (var chemId in sleeper.AvailableChemicals)
        {
            AddChemicalToList(chemicals, chemId, occupant, cachedChemSol, true);
        }

        if (inCrisis)
        {
            foreach (var chemId in sleeper.EmergencyChemicals)
            {
                // Skip any duplicates in EmergencyChemicals that are already in AvailableChemicals
                if (sleeper.AvailableChemicals.Contains(chemId))
                    continue;

                AddChemicalToList(chemicals, chemId, occupant, cachedChemSol, true);
            }
        }

        var state = new SleeperBuiState(
            netOccupant,
            occupantName,
            occupantState,
            health,
            maxHealth,
            minHealth,
            bruteLoss,
            burnLoss,
            toxinLoss,
            oxyLoss,
            geneticLoss,
            hasBlood,
            bloodLevel,
            bloodPercent,
            bodyTemp,
            sleeper.IsFiltering,
            totalReagents,
            sleeper.DialysisStartedReagentVolume,
            sleeper.AutoEjectDead,
            sleeper.MaxChemical,
            sleeper.CrisisMinDamage,
            chemicals,
            sleeper.InjectionAmounts);

        _ui.SetUiState(console.Owner, SleeperUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var consoles = EntityQueryEnumerator<SleeperConsoleComponent>();
        while (consoles.MoveNext(out var uid, out var console))
        {
            if (!_ui.IsUiOpen(uid, SleeperUIKey.Key))
                continue;

            if (time < console.UpdateAt)
                continue;

            console.UpdateAt = time + console.UpdateCooldown;
            UpdateUI((uid, console));
        }

        var sleepers = EntityQueryEnumerator<SleeperComponent>();
        while (sleepers.MoveNext(out var uid, out var sleeper))
        {
            if (!sleeper.IsFiltering || sleeper.Occupant == null)
                continue;

            if (time < sleeper.NextDialysisTick)
                continue;

            sleeper.NextDialysisTick = time + sleeper.DialysisTickDelay;

            // Perform dialysis
            if (!_solution.TryGetSolution(sleeper.Occupant.Value, "chemicals", out var chemSolEnt, out var chemSol))
                continue;

            if (sleeper.DialysisStartedReagentVolume == 0)
            {
                sleeper.DialysisStartedReagentVolume = chemSol.Volume;
                Dirty(uid, sleeper);
            }

            // Build non-transferable lookup for O(1) checks
            _nonTransferableLookup.Clear();
            foreach (var reagent in sleeper.NonTransferableReagents)
            {
                _nonTransferableLookup.Add(reagent);
            }

            _reagentRemovalBuffer.Clear();
            foreach (var reagentQuantity in chemSol.Contents)
            {
                if (!_nonTransferableLookup.Contains(reagentQuantity.Reagent.Prototype))
                {
                    _reagentRemovalBuffer.Add(reagentQuantity.Reagent.Prototype);
                }
            }

            foreach (var reagent in _reagentRemovalBuffer)
            {
                _solution.RemoveReagent(chemSolEnt.Value, reagent, sleeper.DialysisAmount);
            }

            // Check if dialysis is complete
            var hasTransferableReagents = false;
            foreach (var reagentQuantity in chemSol.Contents)
            {
                if (!_nonTransferableLookup.Contains(reagentQuantity.Reagent.Prototype) &&
                    reagentQuantity.Quantity > 0)
                {
                    hasTransferableReagents = true;
                    break;
                }
            }

            if (!hasTransferableReagents)
            {
                sleeper.IsFiltering = false;
                sleeper.DialysisStartedReagentVolume = 0;
                _audio.PlayPvs(sleeper.DialysisCompleteSound, uid);
                Dirty(uid, sleeper);
            }
        }
    }

    private void AddChemicalToList(
        List<SleeperChemicalData> chemicals,
        ProtoId<ReagentPrototype> chemId,
        EntityUid? occupant,
        Solution? cachedChemSol,
        bool injectable)
    {
        if (!_reagent.TryIndex(chemId, out var reagentProto))
            return;

        FixedPoint2 occupantAmount = 0;
        var overdosing = false;
        var odWarning = false;
        if (cachedChemSol != null)
        {
            var reagent = new ReagentId(chemId, null);
            occupantAmount = cachedChemSol.GetReagentQuantity(reagent);

            if (reagentProto.Overdose != null)
            {
                if (occupantAmount >= reagentProto.Overdose)
                {
                    overdosing = true;
                }
                else if (occupantAmount + 10 > reagentProto.Overdose)
                {
                    odWarning = true;
                }
            }
        }

        chemicals.Add(new SleeperChemicalData(
            reagentProto.LocalizedName,
            chemId,
            occupantAmount,
            injectable && occupant != null,
            overdosing,
            odWarning));
    }
}
