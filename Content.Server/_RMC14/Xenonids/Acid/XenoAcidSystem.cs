using Content.Shared._RMC14.Entrenching;
using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Light.Components;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared._RMC14.CCVar;

namespace Content.Server._RMC14.Xenonids.Acid;

public sealed class XenoAcidSystem : SharedXenoAcidSystem
{
	[Dependency] private readonly IGameTiming _timing = default!;
	[Dependency] private readonly IConfigurationManager _config = default!;

	private int CorrosiveAcidDamageTimeSeconds;
	public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendableLightComponent, CorrodingEvent>(OnExpendableLightCorrodingEvent);
        SubscribeLocalEvent<BarricadeComponent, CorrodingEvent>(OnBarricadeCorrodingEvent);

        Subs.CVar(_config, RMCCVars.RMCCorrosiveAcidDamageTimeSeconds, obj => CorrosiveAcidDamageTimeSeconds = obj, true);
    }

    private void OnExpendableLightCorrodingEvent(Entity<ExpendableLightComponent> target, ref CorrodingEvent args)
    {
        // Rationale and formula: https://github.com/RMC-14/RMC-14/issues/2952#issuecomment-2227035752
        var expendable_light = target.Comp;
        var expendableLightDps = args.ExpendableLightDps + 1;
        expendable_light.StateExpiryTime /= expendableLightDps;
        // In case expandable light is activated shortly after being corroded. Or in case we decide to not destroy corrosive lights on timers like the rest of items for whatever the reason.
        expendable_light.GlowDuration /= expendableLightDps;
        expendable_light.FadeOutDuration /= expendableLightDps;
    }

    private void OnBarricadeCorrodingEvent(Entity<BarricadeComponent> target, ref CorrodingEvent args)
    {
        AddComp(target, new DamageableCorrodingComponent
        {
            Acid = args.Acid,
            Dps = args.Dps,
            Damage = new(PrototypeManager.Index<DamageTypePrototype>(CorrosiveAcidDamageTypeStr), args.Dps * CorrosiveAcidTickDelaySeconds),
            Strength = args.AcidStrength,
            AcidExpiresAt = _timing.CurTime + TimeSpan.FromSeconds(CorrosiveAcidDamageTimeSeconds),
        });

        args.Cancelled = true;
    }
}

