using Content.Server._RMC14.IconLabel;
using Content.Shared._RMC14.IconLabel;
using Content.Shared.Labels.EntitySystems;

namespace Content.Server.Labels.Label;

public sealed class HandLabelerSystem : SharedHandLabelerSystem
{
    [Dependency] private readonly RMCIconLabelSystem _rmcIconLabel = default!;

    protected override void UpdateIconLabel(
      EntityUid owner,
      IconLabelComponent iconLabelComponent,
      string customLabel
    ) {
        _rmcIconLabel.SetText(
            (owner, iconLabelComponent),
            "rmc-custom-container-label-text",
            ("customLabel", customLabel)
        );
    }
}
