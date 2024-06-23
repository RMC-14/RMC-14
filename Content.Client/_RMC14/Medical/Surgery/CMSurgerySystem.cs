using Content.Shared._RMC14.Medical.Surgery;

namespace Content.Client._RMC14.Medical.Surgery;

public sealed class CMSurgerySystem : SharedCMSurgerySystem
{
    public event Action? OnRefresh;

    public override void Update(float frameTime)
    {
        OnRefresh?.Invoke();
    }
}
