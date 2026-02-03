using Content.Shared._RMC14.Medical.MedicalPods;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCStorageSystem), typeof(SharedSleeperSystem))]
public sealed partial class NoStunOnExitComponent : Component;
