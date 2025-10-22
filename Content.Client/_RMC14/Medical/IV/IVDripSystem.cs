using Content.Shared._RMC14.Medical.IV;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

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
        _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), DialysisVisualLayers.Attachment, attachmentState);

        if (dialysis.Comp.IsDetaching)
        {
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), DialysisVisualLayers.Effect, true);
            _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), DialysisVisualLayers.Effect, "draining");
        }
        else if (dialysis.Comp.IsAttaching)
        {
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), DialysisVisualLayers.Effect, true);
            _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), DialysisVisualLayers.Effect, "filling");
        }
        else if (dialysis.Comp.AttachedTo != null)
        {
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), DialysisVisualLayers.Effect, true);
            _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), DialysisVisualLayers.Effect, "running");
        }
        else
        {
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), DialysisVisualLayers.Effect, false);
        }

        if (dialysis.Comp.AttachedTo != null && !dialysis.Comp.IsAttaching && !dialysis.Comp.IsDetaching)
        {
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), DialysisVisualLayers.Filtering, true);
        }
        else
        {
            _spriteSystem.LayerSetVisible((dialysis.Owner, sprite), DialysisVisualLayers.Filtering, false);
        }

        var percent = dialysis.Comp.BatteryChargePercent;
        var batteryState = percent switch
        {
            >= 100 => "battery100",
            >= 85 => "battery85",
            >= 60 => "battery60",
            >= 45 => "battery45",
            >= 30 => "battery30",
            >= 15 => "battery15",
            _ => "battery0"
        };

        if (dialysis.Comp.LastBatteryState == batteryState)
            return;
        dialysis.Comp.LastBatteryState = batteryState;
        _spriteSystem.LayerSetRsiState((dialysis.Owner, sprite), DialysisVisualLayers.Battery, batteryState);
    }
}
