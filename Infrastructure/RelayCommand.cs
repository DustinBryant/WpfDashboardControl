using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Infrastructure
{
    /// <summary>
    /// Provides execution of the command logic through <see cref="Action{Object}"/> and whether or not it can
    /// execute with <see cref="Predicate{Object}"/>
    /// Implements the <see cref="System.Windows.Input.ICommand" />
    /// </summary>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommand : ICommand
    {
        #region Private Fields

        /// <summary>
        /// The can execute
        /// </summary>
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// The execute
        /// </summary>
        private readonly Action<object> _execute;

        #endregion Private Fields

        #region Public Events

        /// <inheritdoc />
        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        #endregion Public Events

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <exception cref="ArgumentNullException">execute</exception>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <inheritdoc />
        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to <see langword="null" />.</param>
        /// <returns>
        ///   <see langword="true" /> if this command can be executed; otherwise, <see langword="false" />.
        /// </returns>
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <inheritdoc />
        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to <see langword="null" />.</param>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        #endregion Public Methods
    }
}