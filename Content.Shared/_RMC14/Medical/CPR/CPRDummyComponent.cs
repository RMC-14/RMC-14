using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CPR;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CPRDummyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CPRSuccess;

    [DataField, AutoNetworkedField]
    public int CPRFailed;

    [DataField, AutoNetworkedField]
    public List<ProtoId<JobPrototype>> ResetCPRCounterJobs = new()
    {
        "CMSeniorEnlistedAdvisor",
        "CMCMO",
    };
}

[Serializable, NetSerializable]
public enum CPRDummyVisuals : byte
{
    Deployed
}
