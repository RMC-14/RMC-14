using Content.Shared._RMC14.Medical.IV;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client._RMC14.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        if (!_overlay.HasOverlay<IVDripOverlay>())
            _overlay.AddOverlay(new IVDripOverlay());

        SubscribeNetworkEvent<DialysisDetachedEvent>(OnDialysisDetachedEvent);
    }

    private void OnDialysisDetachedEvent(DialysisDetachedEvent ev)
    {
        var dialysis = GetEntity(ev.Dialysis);
        if (!TryComp<PortableDialysisComponent>(dialysis, out var comp))
            return;

        comp.IsDetaching = ev.IsDetaching;
        UpdateDialysisAppearance((dialysis, comp));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<IVDripOverlay>();
    }

    protected override void UpdateIVAppearance(Entity<IVDripComponent> iv)
    {
        base.UpdateIVAppearance(iv);
        if (!TryComp(iv, out SpriteComponent? sprite))
            return;

        var hookedState = iv.Comp.AttachedTo == default
            ? iv.Comp.UnattachedState
            : iv.Comp.AttachedState;
        _spriteSystem.LayerSetRsiState((iv.Owner, sprite), IVDripVisualLayers.Base, hookedState);

        string? reagentState = null;
        for (var i = iv.Comp.ReagentStates.Count - 1; i >= 0; i--)
        {
            var (amount, state) = iv.Comp.ReagentStates[i];
            if (amount <= iv.Comp.FillPercentage)
            {
                reagentState = state;
                break;
            }
        }

        if (reagentState == null)
        {
            _spriteSystem.LayerSetVisible((iv.Owner, sprite), IVDripVisualLayers.Reagent, false);
            return;
        }

        _spriteSystem.LayerSetVisible((iv.Owner, sprite), IVDripVisualLayers.Reagent, true);
        _spriteSystem.LayerSetRsiState((iv.Owner, sprite), IVDripVisualLayers.Reagent, reagentState);
        _spriteSystem.LayerSetColor((iv.Owner, sprite), IVDripVisualLayers.Reagent, iv.Comp.FillColor);
    }

    protected override void UpdatePackAppearance(Entity<BloodPackComponent> pack)
    {
        base.UpdatePackAppearance(pack);
        if (!TryComp(pack, out SpriteComponent? sprite))
            return;

        // TODO RMC14 blood types
        _spriteSystem.LayerSetVisible((pack.Owner, sprite), BloodPackVisuals.Label, false);

        if (_spriteSystem.LayerMapTryGet((pack.Owner, sprite), BloodPackVisuals.Fill, out var fillLayer, false))
        {
            var fill = pack.Comp.FillPercentage.Float();
            var level = ContentHelpers.RoundToLevels(fill, 1, pack.Comp.MaxFillLevels + 1);
            var state = level > 0 ? $"{pack.Comp.FillBaseName}{level}" : pack.Comp.FillBaseName;
            _spriteSystem.LayerSetRsiState((pack.Owner, sprite), fillLayer, state);
            _spriteSystem.LayerSetColor((pack.Owner, sprite), fillLayer, pack.Comp.FillColor);
            _spriteSystem.LayerSetVisible((pack.Owner, sprite), fillLayer, true);
        }
    }

    protected override void UpdateDialysisAppearance(Entity<PortableDialysisComponent> dialysis)
    {
        base.UpdateDialysisAppearance(dialysis);
        if (!TryComp(dialysis, out SpriteComponent? sprite))
            return;

        var attachmentState = dialysis.Comp.AttachedTo != null ? "hooked" : "unhooked";
        if (_spriteSystem.LayerMapTryGet((dialysis.Owner, sprite), DialysisVisualLayers.Attachment, out var attachmentLayer, false))
            _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), attachmentLayer, attachmentState);

        if (_spriteSystem.LayerMapTryGet((dialysis.Owner, sprite), DialysisVisualLayers.Effect, out var effectLayer, false))
        {
            string? effectState = null;
            var showEffect = false;
            if (dialysis.Comp.IsDetaching)
            {
                effectState = "draining";
                showEffect = true;
            }
            else if (dialysis.Comp.IsAttaching)
            {
                effectState = "filling";
                showEffect = true;
            }
            else if (dialysis.Comp.AttachedTo != null)
            {
                effectState = "running";
                showEffect = true;
            }

            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), effectLayer, showEffect);
            if (showEffect && effectState != null)
                _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), effectLayer, effectState);
        }

        if (_spriteSystem.LayerMapTryGet((dialysis.Owner, sprite), DialysisVisualLayers.Filtering, out var filteringLayer, false))
        {
            var showFiltering = dialysis.Comp.AttachedTo != null && !dialysis.Comp.IsAttaching && !dialysis.Comp.IsDetaching;
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), filteringLayer, showFiltering);
        }
    }
}
