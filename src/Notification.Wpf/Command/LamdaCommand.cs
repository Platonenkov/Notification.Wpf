using System;
using System.Windows.Input;

namespace Notifications.Wpf.Command
{
    /// <summary>
    /// An <see cref="ICommand"/> implementation that delegates execution and execution checks to supplied delegates.
    /// </summary>
    public class LamdaCommand : ICommand
    {
        private readonly Action<object> _OnExecuteAction;
        private readonly Func<object, bool> _CanExecuteFunc;

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command; may be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the command can execute; otherwise, <see langword="false"/>.</returns>
        public bool CanExecute(object parameter) { return _CanExecuteFunc?.Invoke(parameter) ?? true; }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Data used by the command; may be <see langword="null"/>.</param>
        public void Execute(object parameter)
        {
            _OnExecuteAction?.Invoke(parameter);
        }

        /// <summary>
        /// Occurs when changes happen that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LamdaCommand"/> class.
        /// </summary>
        /// <param name="OnExecuteAction">The action invoked when the command is executed.</param>
        /// <param name="CanExecuteFunc">The optional predicate that determines whether the command can execute.</param>
        public LamdaCommand(Action<object> OnExecuteAction, Func<object, bool> CanExecuteFunc = null)
        {
            _OnExecuteAction = OnExecuteAction;
            _CanExecuteFunc = CanExecuteFunc;
        }
    }
}
