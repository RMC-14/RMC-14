using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class FlammableComponent : Component
    {
        [DataField, AutoNetworkedField]
        public bool Resisting;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public bool OnFire;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public float FireStacks;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public float MaximumFireStacks = 45f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public float MinimumFireStacks = 0f; // TODO RMC14 -20

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public string FlammableFixtureID = "flammable";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public float MinIgnitionTemperature = 373.15f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public bool FireSpread { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public bool CanResistFire { get; set; } = false;

        [DataField(required: true), AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new(); // Empty by default, we don't want any funny NREs.

        /// <summary>
        ///     Used for the fixture created to handle passing firestacks when two flammable objects collide.
        /// </summary>
        [DataField, AutoNetworkedField]
        public IPhysShape FlammableCollisionShape = new PhysShapeCircle(0.35f);

        /// <summary>
        ///     Should the component be set on fire by interactions with isHot entities
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public bool AlwaysCombustible = false;

        /// <summary>
        ///     Can the component anyhow lose its FireStacks?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public bool CanExtinguish = true;

        /// <summary>
        ///     How many firestacks should be applied to component when being set on fire?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField, AutoNetworkedField]
        public float FirestacksOnIgnite = 2.0f;

        /// <summary>
        /// Determines how quickly the object will fade out. With positive values, the object will flare up instead of going out.
        /// </summary>
        [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
        public float FirestackFade = -0.1f;

        [DataField, AutoNetworkedField]
        public ProtoId<AlertPrototype> FireAlert = "Fire";

        [DataField, AutoNetworkedField]
        public int ResistStacks = -10;

        [DataField, AutoNetworkedField]
        public int Intensity;

        [DataField, AutoNetworkedField]
        public int Duration;

        [DataField, AutoNetworkedField]
        public TimeSpan ResistDuration = TimeSpan.FromSeconds(8);
    }
}
