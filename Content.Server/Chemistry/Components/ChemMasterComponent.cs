using Content.Server.Chemistry.EntitySystems;
using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// An industrial grade chemical manipulator with pill and bottle production included.
    /// <seealso cref="ChemMasterSystem"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ChemMasterSystem))]
    public sealed partial class ChemMasterComponent : Component
    {
        [DataField("pillType"), ViewVariables(VVAccess.ReadWrite)]
        public uint PillType = 0;

        [DataField("mode"), ViewVariables(VVAccess.ReadWrite)]
        public ChemMasterMode Mode = ChemMasterMode.Transfer;

        [DataField]
        public ChemMasterSortingType SortingType = ChemMasterSortingType.None;

        [DataField("pillDosageLimit", required: true), ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosageLimit;

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        // RMC14 - Pill bottle colors
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public PillbottleColor PillBottleColor = 0;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public uint PillDosagePrevious = uint.MaxValue;
    }
}
