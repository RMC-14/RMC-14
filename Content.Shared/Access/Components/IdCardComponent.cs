using Content.Shared.Access.Systems;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Access.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedIdCardSystem), typeof(SharedPdaSystem), typeof(SharedAgentIdCardSystem), Other = AccessPermissions.ReadWrite)]
    public sealed partial class IdCardComponent : Component
    {
        [DataField("fullName")]
        [AutoNetworkedField]
        // FIXME Friends
        public string? FullName;

        [DataField("jobTitle")]
        [AutoNetworkedField]
        [Access(typeof(SharedIdCardSystem), typeof(SharedPdaSystem), typeof(SharedAgentIdCardSystem), Other = AccessPermissions.ReadWrite)]
        public string? JobTitle;

        /// <summary>
        /// The state of the job icon rsi.
        /// </summary>
        [DataField("jobIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
        [AutoNetworkedField]
        public string JobIcon = "JobIconUnknown";

        // TODO CM14 move this out and reset the file to upstream once source gen is fixed
        [DataField]
        public LocId NameLocId = "access-id-card-component-owner-name-job-title-text";

        [DataField]
        public LocId FullNameLocId = "access-id-card-component-owner-full-name-job-title-text";
    }
}
