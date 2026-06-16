using Content.Shared._RMC14.Armor;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Armor;

public sealed partial class ArmorAdjustBoundUserInterface : BoundUserInterface
{
        private ArmorAdjustWindow? _window;

        public ArmorAdjustBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<ArmorAdjustWindow>();

            _window.OnMeleeChanged += OnMeleeChanged;
            _window.OnBulletChanged += OnBulletChanged;
            _window.OnBioChanged += OnBioChanged;
            _window.OnExplosionChanged += OnExplosionChanged;
        }

        private void OnMeleeChanged(string value)
        {
            SendMessage(new AdjustableArmorSetValueMessage(ArmorType.Melee, value));
        }

        private void OnBulletChanged(string value)
        {
            SendMessage(new AdjustableArmorSetValueMessage(ArmorType.Bullet, value));
        }

        private void OnBioChanged(string value)
        {
            SendMessage(new AdjustableArmorSetValueMessage(ArmorType.Bio, value));
        }

        private void OnExplosionChanged(string value)
        {
            SendMessage(new AdjustableArmorSetValueMessage(ArmorType.Explosion, value));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null || state is not AdjustableArmorBoundUserInterfaceState cast)
                return;

            _window.SetBullet(cast.BulletArmor);
            _window.SetMelee(cast.MeleeArmor);
            _window.SetExplosion(cast.ExplosionArmor);
            _window.SetBio(cast.BioArmor);
        }
}
