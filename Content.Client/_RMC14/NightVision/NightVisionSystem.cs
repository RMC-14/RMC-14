using Content.Shared._RMC14.NightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._RMC14.NightVision;

/// <summary>
/// Applies client-side lighting, overlay, and FoV effects for RMC night vision.
/// </summary>
public sealed class NightVisionSystem : SharedNightVisionSystem
{
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;

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

    private void SetExperimentalMesons(bool on)
    {
        if (_player.LocalEntity == null)
            return;

        // Experimental synth mesons use FoV only; older RMC meson optics keep their existing visuals.
        _eye.SetDrawFov(_player.LocalEntity.Value, !on);
    }

    private void Off()
    {
        _overlay.RemoveOverlay<NightVisionOverlay>();
        _overlay.RemoveOverlay<NightVisionFilterOverlay>();
        _overlay.RemoveOverlay<HalfNightVisionBrightnessOverlay>();
        _light.DrawLighting = true;
        SetExperimentalMesons(false);
    }

    private void Half(Entity<NightVisionComponent> ent)
    {
        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new NightVisionOverlay());

        if (ent.Comp.Green)
            _overlay.AddOverlay(new NightVisionFilterOverlay());

        _overlay.AddOverlay(new HalfNightVisionBrightnessOverlay());

        _light.DrawLighting = true;
        SetExperimentalMesons(ent.Comp.ExperimentalMesonFov);
    }

    private void Full(Entity<NightVisionComponent> ent)
    {
        if (ent.Comp.Overlay)
            _overlay.AddOverlay(new NightVisionOverlay());

        if (ent.Comp.Green)
            _overlay.AddOverlay(new NightVisionFilterOverlay());

        _light.DrawLighting = false;
        SetExperimentalMesons(ent.Comp.ExperimentalMesonFov);
    }
}
