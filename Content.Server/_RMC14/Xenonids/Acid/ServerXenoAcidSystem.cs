using Content.Server.Light.Components;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Light.Components;

namespace Content.Server._RMC14.Xenonids.Acid;

public sealed class ServerXenoAcidSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendableLightComponent, ServerCorrodingEvent>(OnServerCorrodingEvent);
    }

    private void OnServerCorrodingEvent(Entity<ExpendableLightComponent> target, ref ServerCorrodingEvent args)
    {
        if (TryComp<ExpendableLightComponent>(target, out var expendable_light))
        {
            expendable_light.StateExpiryTime /= args.ExpendableLightDps + 1;
            // In case expandable light is activated shortly after being corroded. Or in case we decide to not destroy corrosive lights on timers like the rest of items for whatever the reason.
            expendable_light.GlowDuration /= args.ExpendableLightDps + 1;
            expendable_light.FadeOutDuration /= args.ExpendableLightDps + 1;
        }
    }
}

