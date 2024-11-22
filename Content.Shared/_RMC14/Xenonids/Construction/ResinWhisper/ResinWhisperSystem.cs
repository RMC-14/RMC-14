using Content.Shared._RMC14.Xenonids.Construction.Events;
using Content.Shared.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

public sealed partial class ResinWhisperSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResinWhisperComponent, XenoSecreteStructureAttemptEvent>(OnRemoteSecreteStructure);
    }

    private void OnRemoteSecreteStructure(EntityUid ent, ResinWhisperComponent comp, XenoSecreteStructureAttemptEvent args)
    {
        if (_interaction.InRangeUnobstructed(ent, args.TargetCoords) && comp.ConstructDelay is TimeSpan)
        {
            constructComp.BuildDelay = comp.ConstructDelay.Value;
            return;
        }

        if (!_interaction.InRangeUnobstructed(ent, args.TargetCoords, 0))
        {
            args.Cancel();
            return;
        }

        if (!TryComp(ent, out XenoConstructionComponent? constructComp))
        {
            args.Cancel();
            return;
        }

        if (comp.ConstructDelay is null)
        {
            comp.ConstructDelay = constructComp.BuildDelay;
        }

        constructComp.BuildDelay = comp.ConstructDelay.Value.Multiply(comp.RemoteConstructDelayMultiplier);
    }
}
