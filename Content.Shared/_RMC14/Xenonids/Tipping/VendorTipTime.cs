using Content.Shared._RMC14.Stun;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Tipping;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VendorTipTimeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DefaultDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan BigDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan CrusherDelay = TimeSpan.FromSeconds(1.5);

    [DataField, AutoNetworkedField]
    public bool IsCrusher = false;

    public TimeSpan GetTippingDelay(RMCSizes size)
    {
        if (IsCrusher)
            return CrusherDelay;

        if (size >= RMCSizes.Big)
            return BigDelay;

        return DefaultDelay;
    }
}
