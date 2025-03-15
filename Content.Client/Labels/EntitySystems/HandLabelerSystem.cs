using Content.Client.Labels.UI;
using Content.Shared.Labels;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Robust.Client.Audio;
using Robust.Shared.Audio;

namespace Content.Client.Labels.EntitySystems;

public sealed class HandLabelerSystem : SharedHandLabelerSystem
{
    [Dependency] AudioSystem _audioSystem = default!;

    protected override void UpdateUI(Entity<HandLabelerComponent> ent)
    {
        if (UserInterfaceSystem.TryGetOpenUi(ent.Owner, HandLabelerUiKey.Key, out var bui)
            && bui is HandLabelerBoundUserInterface cBui)
        {
            cBui.Reload();
        }
    }

    protected override void ClickSound(Entity<HandLabelerComponent> handLabeler)
    {
        _audioSystem.PlayPvs(handLabeler.Comp.ClickSound, handLabeler, AudioParams.Default.WithVolume(-2f));
    }
}
