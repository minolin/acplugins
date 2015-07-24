using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace acServerFake.viemodel
{
    public class RelayCommand : ICommand
    {
        public string DisplayName { get; private set; }
        private readonly Action<object> _execute = null;
        private readonly Predicate<object> _canExecute = null;

        #region Constructors

        public RelayCommand(string displayName, Action<object> execute)
            : this(displayName, execute, null) { }

        public RelayCommand(string displayName, Action<object> execute, Predicate<object> canExecute)
        {
            DisplayName = displayName;
            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion

        #region ICommand Members

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public bool CanExecute(object parameter)
        {
            return _canExecute != null ? _canExecute(parameter) : true;
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute(parameter);
        }

        public void OnCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

    }

}
