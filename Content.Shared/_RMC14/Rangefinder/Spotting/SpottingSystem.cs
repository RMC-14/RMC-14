using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

public sealed partial class SpottingSystem : EntitySystem
{
    [Dependency] private readonly TargetingSystem _targeting = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpottingComponent, GetItemActionsEvent>(OnSpotterGetItemActions);
        SubscribeLocalEvent<SpottingComponent, SpotTargetActionEvent>(OnSpotTarget);
    }

    /// <summary>
    ///     Add an action to the entity holding this item.
    /// </summary>
    private void OnSpotterGetItemActions(Entity<SpottingComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    ///    Applies the spotted component to the entity if it's a valid target and tries to draw a laser.
    /// </summary>
    private void OnSpotTarget(Entity<SpottingComponent> ent, ref SpotTargetActionEvent args)
    {
        if(!TryComp(args.Target, out SpottableComponent? spottable))
            return;

        if(!_targeting.TryLaserTarget(ent.Owner, args.Performer, args.Target, ent.Comp.SpottingDuration, ent.Comp.LaserProto, ent.Comp.ShowLaser,TargetedEffects.Spotted))
            return;

        _audio.PlayPredicted(ent.Comp.SpottingSound, ent, args.Performer);
        _appearance.SetData(ent, RangefinderLayers.Layer, RangefinderMode.Spotter);

        var spotted = EnsureComp<SpottedComponent>(args.Target);
        spotted.Spotter = args.Performer;
        Dirty(args.Target, spotted);

        args.Handled = true;
    }
}
