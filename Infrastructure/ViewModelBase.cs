using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Infrastructure
{
    /// <summary>
    /// Base abstract class for ViewModel's providing INotifyPropertyChanged implementation
    /// Implements the <see cref="System.ComponentModel.INotifyPropertyChanged" />
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        #region Public Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Methods

        /// <summary>
        /// Called to raise [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Fully implements a Setter for a read-write property using CallerMemberName to raise
        /// the notification and the ref to the backing field to set the property.
        /// This only occurs if the backingField does not already equal the newValue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="backingField">The backing field.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if backingField was changed, <c>false</c> otherwise.</returns>
        protected bool RaiseAndSetIfChanged<T>(ref T backingField, T newValue,
            [CallerMemberName] string propertyName = "")
        {
            if (Equals(backingField, newValue))
                return false;

            backingField = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        #endregion Protected Methods
    }
}