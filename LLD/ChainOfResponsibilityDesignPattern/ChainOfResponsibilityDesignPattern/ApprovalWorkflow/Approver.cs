using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChainOfResponsibilityDesignPattern.ApprovalWorkflow
{
    public abstract class Approver
    {
        protected Approver _nextApprover;
        protected Approver(Approver approver)
        {
            _nextApprover = approver;
        }

        public abstract void ProvideApproval(int amount);
    }
}
