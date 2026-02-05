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

    private const string BruteGroup = "Brute";
    private const string BurnGroup = "Burn";
    private const string ToxinGroup = "Toxin";
    private const string AirlossGroup = "Airloss";
    private const string GeneticGroup = "Genetic";

    // Empty state used when no sleeper is connected - cached for performance
    private static readonly SleeperBuiState EmptyState = new(
        null,
        null,
        0,
        150, // health (critThreshold with 0 damage)
        150, // maxHealth (critThreshold)
        -50, // minHealth (critThreshold - deadThreshold = 150 - 200)
        0,
        0,
        0,
        0,
        0,
        false,
        0,
        0,
        0,
        false,
        0,
        0,
        false,
        0,
        [],
        []);

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

        if (!sleeper.AvailableChemicals.Contains(args.Chemical))
            return;

        if (!sleeper.InjectionAmounts.Contains(args.Amount))
            return;

        // Crisis mode when damage exceeds MinHealth threshold
        if (!sleeper.EmergencyChemicals.Contains(args.Chemical) &&
            TryComp<DamageableComponent>(occupant, out var damageable))
        {
            if (damageable.TotalDamage > sleeper.CrisisMinDamage)
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
        if (console.Comp.LinkedSleeper is not { } sleeperId ||
            !TryComp(sleeperId, out SleeperComponent? sleeper))
        {
            _ui.SetUiState(console.Owner, SleeperUIKey.Key, EmptyState);
            return;
        }

        var occupant = sleeper.Occupant;
        NetEntity? netOccupant = null;
        string? occupantName = null;
        var stat = 0;
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
                stat = 2;
            else if (_mobState.IsCritical(occupant.Value))
                stat = 1;

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

        // Build chemical list
        var chemicals = new List<SleeperChemicalData>(sleeper.AvailableChemicals.Length);
        foreach (var chemId in sleeper.AvailableChemicals)
        {
            if (!_reagent.TryIndex(chemId, out var reagentProto))
                continue;

            FixedPoint2 occupantAmount = 0;
            var injectable = occupant != null;
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

                // Crisis mode when damage exceeds MinHealth threshold
                if (totalDamage > sleeper.CrisisMinDamage && !sleeper.EmergencyChemicals.Contains(chemId))
                {
                    injectable = false;
                }
            }

            chemicals.Add(new SleeperChemicalData(
                reagentProto.LocalizedName,
                chemId,
                occupantAmount,
                injectable,
                overdosing,
                odWarning));
        }

        var state = new SleeperBuiState(
            netOccupant,
            occupantName,
            stat,
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
            chemicals,
            sleeper.InjectionAmounts);

        _ui.SetUiState(console.Owner, SleeperUIKey.Key, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var sleepers = EntityQueryEnumerator<SleeperComponent>();

        while (sleepers.MoveNext(out var uid, out var sleeper))
        {
            if (!sleeper.IsFiltering || sleeper.Occupant == null)
                continue;

            if (time < sleeper.NextDialysisTick)
                continue;

            sleeper.NextDialysisTick = time + sleeper.DialysisTickDelay;

            // Perform dialysis
            if (_solution.TryGetSolution(sleeper.Occupant.Value, "chemicals", out var chemSolEnt, out var chemSol))
            {
                if (sleeper.DialysisStartedReagentVolume == 0)
                {
                    sleeper.DialysisStartedReagentVolume = chemSol.Volume;
                }

                _reagentRemovalBuffer.Clear();

                foreach (var reagentQuantity in chemSol.Contents)
                {
                    if (!sleeper.NonTransferableReagents.Contains(reagentQuantity.Reagent.Prototype))
                    {
                        _reagentRemovalBuffer.Add(reagentQuantity.Reagent.Prototype);
                    }
                }

                foreach (var reagent in _reagentRemovalBuffer)
                {
                    _solution.RemoveReagent(chemSolEnt.Value, reagent, sleeper.DialysisAmount);
                }

                // Check if dialysis is complete
                if (chemSol.Volume <= 0)
                {
                    sleeper.IsFiltering = false;
                    sleeper.DialysisStartedReagentVolume = 0;
                    _audio.PlayPvs(sleeper.DialysisCompleteSound, uid);
                }
            }

            Dirty(uid, sleeper);

            // Update linked console UI
            if (sleeper.LinkedConsole is { } consoleId && TryComp<SleeperConsoleComponent>(consoleId, out var console))
            {
                UpdateUI((consoleId, console));
            }
        }
    }
}
