using Content.Shared._RMC14.Medical.IV;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    protected override void UpdateIVAppearance(Entity<IVDripComponent> iv)
    {
        base.UpdateIVAppearance(iv);
        if (!TryComp(iv, out SpriteComponent? sprite))
            return;

        var hookedState = iv.Comp.AttachedTo == default
            ? iv.Comp.UnattachedState
            : iv.Comp.AttachedState;
        sprite.LayerSetState(IVDripVisualLayers.Base, hookedState);

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
            sprite.LayerSetVisible(IVDripVisualLayers.Reagent, false);
            return;
        }

        sprite.LayerSetVisible(IVDripVisualLayers.Reagent, true);
        sprite.LayerSetState(IVDripVisualLayers.Reagent, reagentState);
        sprite.LayerSetColor(IVDripVisualLayers.Reagent, iv.Comp.FillColor);
    }

    protected override void UpdatePackAppearance(Entity<BloodPackComponent> pack)
    {
        base.UpdatePackAppearance(pack);
        if (!TryComp(pack, out SpriteComponent? sprite))
            return;

        // TODO RMC14 blood types
        sprite.LayerSetVisible(BloodPackVisuals.Label, false);

        if (sprite.LayerMapTryGet(BloodPackVisuals.Fill, out var fillLayer))
        {
            var fill = pack.Comp.FillPercentage.Float();
            var level = ContentHelpers.RoundToLevels(fill, 1, pack.Comp.MaxFillLevels + 1);
            var state = level > 0 ? $"{pack.Comp.FillBaseName}{level}" : pack.Comp.FillBaseName;
            sprite.LayerSetState(fillLayer, state);
            sprite.LayerSetColor(fillLayer, pack.Comp.FillColor);
            sprite.LayerSetVisible(fillLayer, true);
        }
    }

    protected override void UpdateDialysisAppearance(Entity<PortableDialysisComponent> dialysis)
    {
        base.UpdateDialysisAppearance(dialysis);
        if (!TryComp(dialysis, out SpriteComponent? sprite))
            return;

        var attachmentState = dialysis.Comp.AttachedTo != null ? "hooked" : "unhooked";
        sprite.LayerSetState(DialysisVisualLayers.Attachment, attachmentState);

        if (dialysis.Comp.IsDetaching)
        {
            sprite.LayerSetVisible(DialysisVisualLayers.Effect, true);
            sprite.LayerSetState(DialysisVisualLayers.Effect, "draining");
        }
        else if (dialysis.Comp.IsAttaching)
        {
            sprite.LayerSetVisible(DialysisVisualLayers.Effect, true);
            sprite.LayerSetState(DialysisVisualLayers.Effect, "filling");
        }
        else if (dialysis.Comp.AttachedTo != null)
        {
            sprite.LayerSetVisible(DialysisVisualLayers.Effect, true);
            sprite.LayerSetState(DialysisVisualLayers.Effect, "running");
        }
        else
        {
            sprite.LayerSetVisible(DialysisVisualLayers.Effect, false);
        }

        var percent = dialysis.Comp.BatteryChargePercent;
        var batteryState = percent switch
        {
            >= 85 => "battery100",
            >= 60 => "battery85",
            >= 45 => "battery60",
            >= 30 => "battery45",
            >= 15 => "battery30",
            >= 1 => "battery15",
            _ => "battery0"
        };

        sprite.LayerSetState(DialysisVisualLayers.Battery, batteryState);
    }
}
