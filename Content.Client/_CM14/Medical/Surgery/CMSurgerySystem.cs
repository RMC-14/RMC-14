using Content.Shared._CM14.Medical.Surgery;

namespace Content.Client._CM14.Medical.Surgery;

public sealed class CMSurgerySystem : SharedCMSurgerySystem
{
    public event Action? OnRefresh;

    public override void Update(float frameTime)
    {
        OnRefresh?.Invoke();
    }
}
