using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client._RMC14.Drugs;

public sealed class RMCDrugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;

    private RMCRainbowOverlay _overlay = default!;
    private bool _allowed;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<RMCSeeingRainbowsStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new();

        Subs.CVar(_config, RMCCVars.RMCAllowFlashingLights, OnAllowedChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnAllowedChanged(bool allowed)
    {
        _allowed = allowed;
        if (!allowed)
        {
            RemoveOverlay();
            return;
        }

        if (_statusEffects.HasEffectComp<RMCSeeingRainbowsStatusEffectComponent>(_player.LocalEntity))
            AddOverlay();
    }

    private void OnRemoved(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (_statusEffects.TryEffectsWithComp<RMCSeeingRainbowsStatusEffectComponent>(args.Target, out var effects))
        {
            foreach (var effect in effects)
            {
                if (effect.Owner != ent.Owner)
                    return;
            }
        }

        RemoveOverlay();
    }

    private void OnApplied(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        AddOverlay();
    }

    private void OnPlayerAttached(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        AddOverlay();
    }

    private void OnPlayerDetached(Entity<RMCSeeingRainbowsStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (!_allowed || _overlayMan.HasOverlay<RMCRainbowOverlay>())
            return;

        _overlay.Phase = _random.NextFloat(MathF.Tau);
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        _overlay.Intoxication = 0;
        _overlayMan.RemoveOverlay(_overlay);
    }
}
