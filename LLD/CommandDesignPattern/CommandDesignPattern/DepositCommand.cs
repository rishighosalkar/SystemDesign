using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandDesignPattern
{
    public class DepositCommand : ICommand
    {
        private readonly BankAccountManager _bankAccountManager;
        private int _amount;

        public DepositCommand(BankAccountManager bankAccountManager, int amount)
        {
            this._bankAccountManager = bankAccountManager;
            this._amount = amount;
        }

        public void Execute()
        {
            _bankAccountManager.Deposit(_amount);
        }

        public void Undo()
        {
            _bankAccountManager.Withdraw(_amount);
        }
    }
}
