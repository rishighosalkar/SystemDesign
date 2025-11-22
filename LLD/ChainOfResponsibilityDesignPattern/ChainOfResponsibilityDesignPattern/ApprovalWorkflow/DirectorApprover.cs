using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.ApprovalWorkflow
{
    public class DirectorApprover : Approver
    {
        public DirectorApprover(Approver nextApprover) : base(nextApprover) { }

        public override void ProvideApproval(int amount)
        {
            if(amount > 200000)
            {
                Console.WriteLine($"Amount {amount} received for approval from Director");
                return;
            }

            _nextApprover?.ProvideApproval(amount);
        }
    }
}
