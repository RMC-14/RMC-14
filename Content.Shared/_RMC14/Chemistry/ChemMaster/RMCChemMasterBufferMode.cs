using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[Serializable, NetSerializable]
public enum RMCChemMasterBufferMode
{
    ToBeaker = 0,
    ToDisposal,
}
