using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.ApprovalWorkflow
{
    public class TeamLeadApprover : Approver
    {
        public TeamLeadApprover(Approver nextApprover) : base(nextApprover) { }
        public override void ProvideApproval(int amount)
        {
            if(amount <= 10000)
            {
                Console.WriteLine($"Amount {amount} received for approval from Team Lead");
                return;
            }

            _nextApprover?.ProvideApproval(amount);
        }
    }
}
