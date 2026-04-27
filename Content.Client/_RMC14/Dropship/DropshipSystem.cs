using Content.Shared._RMC14.Dropship;

namespace Content.Client._RMC14.Dropship;

public sealed class DropshipSystem : SharedDropshipSystem
{
    public readonly List<DropshipNavigationBui> Uis = new();
    public readonly List<DropshipNavigationERTBui> ERTUis = new();

    public override void FrameUpdate(float frameTime)
    {
        foreach (var ui in Uis)
        {
            ui.Update();
        }

        foreach (var ui in ERTUis)
        {
            ui.Update();
        }
    }
}
