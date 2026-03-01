
using CommandDesignPattern;

BankAccountManager bankAccount = new BankAccountManager(10000);

ICommand withdrawCommand = new WithdrawalCommand(bankAccount, 2000);
ICommand depositCommand = new DepositCommand(bankAccount, 5000);

TransactionManager transactionManager = new TransactionManager();

transactionManager.ExecuteTransaction(withdrawCommand);
transactionManager.ExecuteTransaction(depositCommand);

transactionManager.UndoTransaction();