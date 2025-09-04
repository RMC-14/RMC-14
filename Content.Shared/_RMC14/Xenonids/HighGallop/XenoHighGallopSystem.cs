using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared.Maps;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using System.Numerics;

namespace Content.Shared._RMC14.Xenonids.HighGallop;

public sealed partial class XenoHighGallopSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RMCPullingSystem _pulling = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly RMCSizeStunSystem _size = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoHighGallopComponent, XenoHighGallopActionEvent>(OnHighGallopAction);
    }

    private void OnHighGallopAction(Entity<XenoHighGallopComponent> xeno, ref XenoHighGallopActionEvent args)
    {
        if (args.Handled)
            return;

        if (_transform.GetGrid(args.Target) is not { } gridId ||
    !TryComp(gridId, out MapGridComponent? grid))
            return;

        args.Handled = true;

        _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote);
        _audio.PlayPredicted(xeno.Comp.Sound, xeno, xeno);

        var direction = (args.Target.Position - _transform.GetMoverCoordinates(xeno).Position).Normalized().ToAngle() - Angle.FromDegrees(90);

        var xenoCoord = _transform.GetMoverCoordinates(xeno);
        var area = Box2.CenteredAround(xenoCoord.Position, new(xeno.Comp.Width, xeno.Comp.Height)).Translated(new(0, (xeno.Comp.Height / 2) + 0.5f));
        var rot = new Box2Rotated(area, direction, xenoCoord.Position); // Correct the angle

        var bounds = rot.CalcBoundingBox();

        //Tiles should never fail
        if (_net.IsClient)
            return;

        foreach (var tile in _map.GetTilesIntersecting(gridId, grid, rot))
        {
            var spawn = xeno.Comp.TelegraphEffect;

            if (!bounds.Encloses(Box2.CenteredAround(_turf.GetTileCenter(tile).Position, Vector2.One)))
                spawn = xeno.Comp.TelegraphEffectEdge;

            SpawnAtPosition(spawn, _turf.GetTileCenter(tile));
        }

        foreach (var ent in _lookup.GetEntitiesIntersecting(Transform(xeno).MapID, rot))
        {
            if (_tags.HasTag(ent, xeno.Comp.Flingable))
            {
                _pulling.TryStopAllPullsFromAndOn(ent);

                var origin = _transform.GetMapCoordinates(xeno);
                _size.KnockBack(ent, origin, xeno.Comp.FlingDistance, xeno.Comp.FlingDistance, 10, true);
                continue;
            }

            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            if (_size.TryGetSize(ent, out var size) && size >= RMCSizes.Big)
                continue;

            _stun.TryParalyze(ent, _xeno.TryApplyXenoDebuffMultiplier(ent, xeno.Comp.StunDuration), true);
            _slow.TrySlowdown(ent, _xeno.TryApplyXenoDebuffMultiplier(ent, xeno.Comp.SlowDuration));
        }
    }
}
