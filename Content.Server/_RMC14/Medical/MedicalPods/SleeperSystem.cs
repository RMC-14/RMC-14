using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Medical.MedicalPods;
using Content.Shared.Body.Components;
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
using Robust.Shared.Audio;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier BuzzSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");
    private static readonly SoundSpecifier PingSound = new SoundPathSpecifier("/Audio/Machines/ping.ogg");

    private readonly List<ProtoId<ReagentPrototype>> _reagentRemovalBuffer = new();

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
        if (!sleeper.Comp.AutoEjectDead)
            return;

        if (sleeper.Comp.Occupant != args.Target)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            _audio.PlayPvs(BuzzSound, sleeper);
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

        // Check if chemical is in available list
        if (!sleeper.AvailableChemicals.Contains(args.Chemical))
            return;

        // Check if amount is valid
        if (!sleeper.InjectionAmounts.Contains(args.Amount))
            return;

        // Check health threshold for non-emergency chemicals
        if (TryComp<DamageableComponent>(occupant, out var damageable))
        {
            var health = damageable.TotalDamage;
            var maxHealth = _mobThreshold.TryGetThresholdForState(occupant, MobState.Dead, out var deadThreshold)
                ? deadThreshold
                : FixedPoint2.New(100);

            var currentHealth = maxHealth - health;
            if (currentHealth < sleeper.MinHealth && !sleeper.EmergencyChemicals.Contains(args.Chemical))
                return;
        }

        // Try to get the chemical solution
        if (!_solution.TryGetSolution(occupant, "chemicals", out var chemSolEnt, out var chemSol))
            return;

        // Check current amount
        var reagent = new ReagentId(args.Chemical, null);
        var currentAmount = chemSol.GetReagentQuantity(reagent);
        if (currentAmount + args.Amount > sleeper.MaxChemical)
            return;

        // Add the chemical
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
            var emptyState = new SleeperBuiState(
                null,
                null,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                0,
                0,
                0,
                0,
                false,
                0,
                0,
                false,
                0,
                0,
                [],
                []);
            _ui.SetUiState(console.Owner, SleeperUIKey.Key, emptyState);
            return;
        }

        var occupant = sleeper.Occupant;
        NetEntity? netOccupant = null;
        string? occupantName = null;
        var stat = 0;
        var health = 0f;
        var maxHealth = 100f;
        var minHealth = -100f;
        var bruteLoss = 0f;
        var burnLoss = 0f;
        var toxinLoss = 0f;
        var oxyLoss = 0f;
        var hasBlood = false;
        FixedPoint2 bloodLevel = 0;
        FixedPoint2 bloodMax = 0;
        var bloodPercent = 0f;
        var bodyTemp = 310f;
        FixedPoint2 totalReagents = 0;

        if (occupant != null && TryComp<DamageableComponent>(occupant, out var damageable))
        {
            netOccupant = GetNetEntity(occupant.Value);
            occupantName = Identity.Name(occupant.Value, EntityManager);

            if (_mobState.IsDead(occupant.Value))
                stat = 2;
            else if (_mobState.IsCritical(occupant.Value))
                stat = 1;

            var totalDamage = damageable.TotalDamage;
            if (_mobThreshold.TryGetThresholdForState(occupant.Value, MobState.Dead, out var deadThreshold))
            {
                maxHealth = (float) deadThreshold;
                minHealth = 0f;
            }

            health = maxHealth - totalDamage.Float();

            if (damageable.Damage.DamageDict.TryGetValue("Blunt", out var blunt))
                bruteLoss += blunt.Float();
            if (damageable.Damage.DamageDict.TryGetValue("Slash", out var slash))
                bruteLoss += slash.Float();
            if (damageable.Damage.DamageDict.TryGetValue("Piercing", out var piercing))
                bruteLoss += piercing.Float();

            if (damageable.Damage.DamageDict.TryGetValue("Heat", out var heat))
                burnLoss += heat.Float();
            if (damageable.Damage.DamageDict.TryGetValue("Cold", out var cold))
                burnLoss += cold.Float();
            if (damageable.Damage.DamageDict.TryGetValue("Shock", out var shock))
                burnLoss += shock.Float();

            if (damageable.Damage.DamageDict.TryGetValue("Poison", out var poison))
                toxinLoss += poison.Float();
            if (damageable.Damage.DamageDict.TryGetValue("Radiation", out var radiation))
                toxinLoss += radiation.Float();

            if (damageable.Damage.DamageDict.TryGetValue("Asphyxiation", out var asphyx))
                oxyLoss = asphyx.Float();

            if (TryComp<BloodstreamComponent>(occupant, out var blood) &&
                blood.BloodSolution != null &&
                _solution.TryGetSolution(occupant.Value, blood.BloodSolutionName, out _, out var bloodSol))
            {
                hasBlood = true;
                bloodLevel = bloodSol.Volume;
                bloodMax = bloodSol.MaxVolume;
                bloodPercent = bloodMax > 0 ? (bloodLevel / bloodMax).Float() * 100f : 0f;
            }

            if (_solution.TryGetSolution(occupant.Value, "chemicals", out _, out var chemSol))
            {
                totalReagents = chemSol.Volume;
            }
        }

        // Build chemical list
        var chemicals = new List<SleeperChemicalData>();
        foreach (var chemId in sleeper.AvailableChemicals)
        {
            if (!_reagent.TryIndex(chemId, out var reagentProto))
                continue;

            FixedPoint2 occupantAmount = 0;
            var injectable = occupant != null;
            var overdosing = false;
            var odWarning = false;

            if (occupant != null && _solution.TryGetSolution(occupant.Value, "chemicals", out _, out var chemSol))
            {
                var reagent = new ReagentId(chemId, null);
                occupantAmount = chemSol.GetReagentQuantity(reagent);

                // Check overdose
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

                // Check health threshold for non-emergency chemicals
                if (TryComp<DamageableComponent>(occupant, out var dmg))
                {
                    var currentHealth = maxHealth - dmg.TotalDamage.Float();
                    if (currentHealth < sleeper.MinHealth && !sleeper.EmergencyChemicals.Contains(chemId))
                    {
                        injectable = false;
                    }
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
            hasBlood,
            bloodLevel,
            bloodMax,
            bloodPercent,
            bodyTemp,
            sleeper.Filtering,
            totalReagents,
            sleeper.DialysisStartedReagentVolume,
            sleeper.AutoEjectDead,
            sleeper.MaxChemical,
            sleeper.MinHealth,
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
            if (!sleeper.Filtering || sleeper.Occupant == null)
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
                    _solution.RemoveReagent(chemSolEnt.Value, reagent, sleeper.ReagentRemovalRate);
                }

                // Check if dialysis is complete
                if (chemSol.Volume <= 0)
                {
                    sleeper.Filtering = false;
                    sleeper.DialysisStartedReagentVolume = 0;
                    _audio.PlayPvs(PingSound, uid);
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
