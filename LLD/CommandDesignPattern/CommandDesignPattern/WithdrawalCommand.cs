using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandDesignPattern
{
    public class WithdrawalCommand : ICommand
    {
        private readonly BankAccountManager _bankAccountManager;
        private int _amount;

        public WithdrawalCommand(BankAccountManager bankAccountManager, int amount)
        {
            _bankAccountManager = bankAccountManager;
            _amount = amount;
        }
        public void Execute()
        {
            _bankAccountManager.Withdraw(_amount);
        }

        public void Undo()
        {
            _bankAccountManager.Deposit(_amount);
        }
    }
}
