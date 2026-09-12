using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Xenonids.Construction.Nest;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Paratoxin.DrainingBite;

public sealed class XenoDrainingBiteSystem : EntitySystem
{
    [Dependency] private readonly ParatoxinSystem _paratoxin = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcblood = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoDrainingBiteComponent, XenoDrainingBiteActionEvent>(OnXenoDrainingBiteEvent);
    }

    private void OnXenoDrainingBiteEvent(Entity<XenoDrainingBiteComponent> xeno, ref XenoDrainingBiteActionEvent args)
    {
        if (args.Handled || HasComp<XenoNestedComponent>(args.Target))
            return;

        var stacks = _paratoxin.GetStacks(args.Target);

        var stunDuration = xeno.Comp.MinStunTime;

        args.Handled = true;

        // TODO RMC14 have seperate loop for removing stims
        // Also resisting neuro should prevent medicine drain but not stim drain
        if (stacks > 0)
        {
            stunDuration = TimeSpan.FromSeconds(Math.Max(stunDuration.TotalSeconds, ((stacks / xeno.Comp.StackDivisor) - 1) * 2));
            if (_rmcblood.TryGetChemicalSolution(args.Target, out var solEnt, out var solu))
            {
                foreach (var chemical in solu.GetReagentPrototypes(_proto).Keys)
                {
                    if (chemical.Group == xeno.Comp.DrainGroup)
                        _solution.RemoveReagent(solEnt, chemical.ID, stacks * xeno.Comp.ChemicalDrainStackMultiplier);
                }
            }

            _paratoxin.TryChangeStacks(args.Target, xeno, (int)-(stacks * xeno.Comp.ProportialStacksToRemoveMultiplier));
        }

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.BiteEffect, args.Target.ToCoordinates());

        _stun.TryParalyze(args.Target, stunDuration, true);
        _audio.PlayPredicted(xeno.Comp.HitSound, xeno, xeno);
    }
}
