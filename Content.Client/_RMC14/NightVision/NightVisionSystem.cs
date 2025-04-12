using Content.Shared._RMC14.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.NightVision;

public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, LocalPlayerAttachedEvent>(OnNightVisionAttached);
        SubscribeLocalEvent<NightVisionComponent, LocalPlayerDetachedEvent>(OnNightVisionDetached);
    }

    private void OnNightVisionAttached(Entity<NightVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        NightVisionChanged(ent);
    }

    private void OnNightVisionDetached(Entity<NightVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    protected override void NightVisionChanged(Entity<NightVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        switch (ent.Comp.State)
        {
            case NightVisionState.Off:
                Off();
                break;
            case NightVisionState.Half:
                Half(ent);
                break;
            case NightVisionState.Full:
                Full(ent);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void NightVisionRemoved(Entity<NightVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off();
    }

    private void Off()
    {
        _overlay.RemoveOverlay<NightVisionOverlay>();
        _overlay.RemoveOverlay<NightVisionFilterOverlay>();
        _light.DrawLighting = true;
    }

    private void Half(Entity<NightVisionComponent> ent)
    {
        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new NightVisionOverlay());

        if (ent.Comp.Green)
            _overlay.AddOverlay(new NightVisionFilterOverlay());

        _light.DrawLighting = true;
    }

    private void Full(Entity<NightVisionComponent> ent)
    {
        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new NightVisionOverlay());

        if (ent.Comp.Green)
            _overlay.AddOverlay(new NightVisionFilterOverlay());

        _light.DrawLighting = false;
    }
}
