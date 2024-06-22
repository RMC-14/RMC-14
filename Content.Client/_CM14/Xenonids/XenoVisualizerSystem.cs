using Content.Shared._CM14.Xenonids;
using Content.Shared._CM14.Xenonids.Egg;
using Content.Shared._CM14.Xenonids.Leap;
using Content.Shared._CM14.Xenonids.Movement;
using Content.Shared._CM14.Xenonids.Rest;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._CM14.Xenonids;

public sealed class XenoVisualizerSystem : VisualizerSystem<XenoComponent>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private EntityQuery<XenoAnimateMovementComponent> _animateQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, KnockedDownEvent>(OnXenoKnockedDown);
        SubscribeLocalEvent<XenoComponent, StatusEffectEndedEvent>(OnXenoStatusEffectEnded);
        SubscribeLocalEvent<XenoComponent, GetDrawDepthEvent>(OnXenoGetDrawDepth);

        _animateQuery = GetEntityQuery<XenoAnimateMovementComponent>();
    }

    private void OnXenoKnockedDown(Entity<XenoComponent> xeno, ref KnockedDownEvent args)
    {
        UpdateSprite(xeno.Owner);
    }

    private void OnXenoStatusEffectEnded(Entity<XenoComponent> xeno, ref StatusEffectEndedEvent args)
    {
        UpdateSprite(xeno.Owner);
    }

    private void OnXenoGetDrawDepth(Entity<XenoComponent> ent, ref GetDrawDepthEvent args)
    {
        if (_mobState.IsDead(ent))
        {
            if (args.DrawDepth > DrawDepth.DeadMobs)
            {
                args.DrawDepth = DrawDepth.DeadMobs;
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;
        UpdateSprite((uid, sprite, null, args.Component, null, null));
        UpdateDrawDepth((uid, sprite));
    }

    public void UpdateSprite(Entity<SpriteComponent?, MobStateComponent?, AppearanceComponent?, InputMoverComponent?, ThrownItemComponent?, XenoLeapingComponent?, KnockedDownComponent?> entity)
    {
        var (_, sprite, mobState, appearance, input, thrown, leaping, knocked) = entity;
        if (!Resolve(entity, ref sprite, ref appearance))
            return;

        var state = MobState.Alive;
        if (Resolve(entity, ref mobState, false))
            state = mobState.CurrentState;

        Resolve(entity, ref input, ref thrown, ref leaping, ref knocked, false);
        if (knocked != null)
            state = MobState.Critical;

        if (sprite is not { BaseRSI: { } rsi } ||
            !sprite.LayerMapTryGet(XenoVisualLayers.Base, out var layer))
        {
            return;
        }

        // TODO CM14 split this up into multiple systems with ordered event subscription
        switch (state)
        {
            case MobState.Critical:
                if (rsi.TryGetState("crit", out _))
                    sprite.LayerSetState(layer, "crit");
                break;
            case MobState.Dead:
                if (rsi.TryGetState("dead", out _))
                    sprite.LayerSetState(layer, "dead");
                break;
            default:
                if (sprite.LayerMapTryGet(XenoVisualLayers.Ovipositor, out var oviLayer))
                {
                    if (HasComp<XenoAttachedOvipositorComponent>(entity) &&
                        TryComp(entity, out XenoOvipositorCapableComponent? capable))
                    {
                        sprite.LayerSetState(oviLayer, capable.AttachedState);
                        sprite.LayerSetVisible(oviLayer, true);
                        sprite.LayerSetVisible(layer, false);
                        return;
                    }

                    sprite.LayerSetVisible(oviLayer, false);
                    sprite.LayerSetVisible(layer, true);
                }

                if (AppearanceSystem.TryGetData(entity, XenoVisualLayers.Base, out XenoRestState resting, appearance) &&
                    resting == XenoRestState.Resting &&
                    rsi.TryGetState("sleeping", out _))
                {
                    sprite.LayerSetState(layer, "sleeping");
                    break;
                }

                if ((leaping != null || thrown != null) &&
                    rsi.TryGetState("thrown", out _))
                {
                    sprite.LayerSetState(layer, "thrown");
                    break;
                }

                if (AppearanceSystem.TryGetData(entity, XenoVisualLayers.Fortify, out bool fortify, appearance) &&
                    fortify &&
                    rsi.TryGetState("fortify", out _))
                {
                    sprite.LayerSetState(layer, "fortify");
                    break;
                }

                if (AppearanceSystem.TryGetData(entity, XenoVisualLayers.Crest, out bool crest, appearance) &&
                    crest &&
                    rsi.TryGetState("crest", out _))
                {
                    sprite.LayerSetState(layer, "crest");
                    break;
                }

                if (input?.HeldMoveButtons > MoveButtons.None &&
                    input.HeldMoveButtons != MoveButtons.Walk &&
                    rsi.TryGetState("running", out _))
                {
                    sprite.LayerSetState(layer, "running");
                    break;
                }

                if (rsi.TryGetState("alive", out _))
                    sprite.LayerSetState(layer, "alive");

                break;
        }
    }

    public void UpdateDrawDepth(Entity<SpriteComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp))
            return;

        var ev = new GetDrawDepthEvent(DrawDepth.Mobs);
        RaiseLocalEvent(xeno, ref ev);

        xeno.Comp.DrawDepth = (int) ev.DrawDepth;
    }

    public override void Update(float frameTime)
    {
        var xenoQuery = EntityQueryEnumerator<XenoComponent>();
        while (xenoQuery.MoveNext(out var uid, out _))
        {
            if (_animateQuery.HasComp(uid))
                continue;

            UpdateSprite(uid);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        var animateQuery = EntityQueryEnumerator<XenoAnimateMovementComponent>();
        while (animateQuery.MoveNext(out var uid, out _))
        {
            UpdateSprite(uid);
        }
    }
}
