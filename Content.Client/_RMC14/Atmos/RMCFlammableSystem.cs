using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Atmos;

public sealed class RMCFlammableSystem : SharedRMCFlammableSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string RollKey = "StopDropRollAnimation";
    private static readonly ProtoId<StatusEffectPrototype> KnockdownedKey = "KnockedDown";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCStopDropRollVisualsComponent, ResistFireAlertEvent>(OnResistFireAlert);
        SubscribeLocalEvent<RMCStopDropRollVisualsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCStopDropRollVisualsComponent, StatusEffectEndedEvent>(OnStatusEffectEnded);
    }

    private void OnResistFireAlert(Entity<RMCStopDropRollVisualsComponent> ent, ref ResistFireAlertEvent args)
    {
        if (!TryComp<FlammableComponent>(ent.Owner, out var flammable))
            return;

        if (flammable.Resisting)
            return;

        if (_animation.HasRunningAnimation(ent.Owner, RollKey))
            return;

        var rollAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(5),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(TransformComponent),
                    Property = nameof(TransformComponent.LocalRotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.25f),
                    }
                }
            }
        };

        _animation.Play(ent.Owner, rollAnimation, RollKey);
    }

    private void OnMobStateChanged(Entity<RMCStopDropRollVisualsComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        _animation.Stop(ent.Owner, RollKey);
    }

    private void OnStatusEffectEnded(Entity<RMCStopDropRollVisualsComponent> ent, ref StatusEffectEndedEvent args)
    {
        if (args.Key != KnockdownedKey)
            return;

        _animation.Stop(ent.Owner, RollKey);
    }
}
