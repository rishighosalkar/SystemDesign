using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandDesignPattern
{
    public class TransactionManager
    {
        private Stack<ICommand> _commands = new Stack<ICommand>();
        public void ExecuteTransaction(ICommand command)
        {
            command.Execute();
            _commands.Push(command);
        }

        public void UndoTransaction()
        {
            if(_commands.Count > 0)
            {
                var command = _commands.Pop();
                command.Undo();
            }
        }
    }
}
