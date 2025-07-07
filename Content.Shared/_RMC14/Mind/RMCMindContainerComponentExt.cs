using Content.Shared.Mind.Components;

namespace Content.Shared.Mind.Components
{
    public sealed partial class MindContainerComponent
    {
        private EntityUid? _mind;

        [DataField, AutoNetworkedField]
        public bool EverHadMind = false;
    }
}
