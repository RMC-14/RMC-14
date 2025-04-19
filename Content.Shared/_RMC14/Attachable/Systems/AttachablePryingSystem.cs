using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Prying.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachablePryingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachablePryingComponent, AttachableAlteredEvent>(OnAttachableAltered);
    }

    private void OnAttachableAltered(Entity<AttachablePryingComponent> ent, ref AttachableAlteredEvent args)
    {
        if (_timing.ApplyingState)
            return;

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                var prying = EnsureComp<PryingComponent>(args.Holder);
                var tool = EnsureComp<ToolComponent>(args.Holder);
#pragma warning disable RA0002
                prying.SpeedModifier = 0.5f;
                tool.Qualities.Add("Prying", _prototype);
                tool.UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
#pragma warning restore RA0002

                Dirty(args.Holder, prying);
                Dirty(args.Holder, tool);
                break;
            case AttachableAlteredType.Detached:
                RemCompDeferred<PryingComponent>(args.Holder);
                RemCompDeferred<ToolComponent>(args.Holder);
                break;
        }
    }
}
