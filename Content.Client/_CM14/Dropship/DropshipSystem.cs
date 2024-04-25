using Content.Shared._CM14.Dropship;

namespace Content.Client._CM14.Dropship;

public sealed class DropshipSystem : SharedDropshipSystem
{
    public readonly List<DropshipNavigationBui> Uis = new();

    public override void FrameUpdate(float frameTime)
    {
        foreach (var ui in Uis)
        {
            ui.Update();
        }
    }
}
