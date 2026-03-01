using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandDesignPattern
{
    public class BankAccountManager
    {
        private int _amount;
        public BankAccountManager(int amount)
        {
            this._amount = amount;
            Console.WriteLine($"Initial bank account balance is : {_amount}");
        }

        public void Deposit(int amount)
        {
            _amount += amount;
            Console.WriteLine($"Bank account balance after deposit is : {_amount}");
        }

        public void Withdraw(int amount)
        {
            _amount -= amount;
            Console.WriteLine($"Bank account balance after withdrawal is : {_amount}");
        }
    }
}
