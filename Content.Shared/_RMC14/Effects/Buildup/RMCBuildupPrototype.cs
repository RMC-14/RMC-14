using Content.Shared._RMC14.Stun;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Effects.Buildup;

[Prototype("rmcBuildup")]
public sealed partial class RMCBuildupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public int Threshold = 1;

    [DataField]
    public int DecayAmount = 1;

    [DataField]
    public TimeSpan DecayEvery = TimeSpan.FromSeconds(2);

    [DataField]
    public bool RefreshDecayOnApply;

    [DataField]
    public bool AffectsDead;

    [DataField]
    public RMCSizes? MinimumSize;

    [DataField]
    public RMCSizes? MaximumSize;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public LocId? AppliedPopup;

    [DataField]
    public PopupType AppliedPopupType = PopupType.MediumCaution;

    [DataField]
    public LocId? TriggeredPopup;

    [DataField]
    public PopupType TriggeredPopupType = PopupType.MediumCaution;
}
