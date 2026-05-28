using Content.Shared._RMC14.Dropship;

namespace Content.Client._RMC14.Dropship;

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
