using System;
using Robust.Client.UserInterface;

namespace Robust.Client.ViewVariables
{
    /// <summary>
    ///     An editor for the value of a property.
    /// </summary>
    public abstract class VVPropEditor
    {
        /// <summary>
        ///     Invoked when the value was changed.
        /// </summary>
        internal event Action<object?, bool>? OnValueChanged;

        protected bool ReadOnly { get; private set; }

        public Control Initialize(object? value, bool readOnly)
        {
            ReadOnly = readOnly;
            return MakeUI(value);
        }

        protected abstract Control MakeUI(object? value);

        protected void ValueChanged(object? newValue, bool reinterpretValue = false)
        {
            OnValueChanged?.Invoke(newValue, reinterpretValue);
        }

        public virtual void WireNetworkSelector(uint sessionId, object[] selectorChain)
        {

        }
    }
}
