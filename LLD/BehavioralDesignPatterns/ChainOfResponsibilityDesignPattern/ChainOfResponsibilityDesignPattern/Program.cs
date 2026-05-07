
using ChainOfResponsibilityDesignPattern.ApprovalWorkflow;
using ChainOfResponsibilityDesignPattern.Logger;
using ChainOfResponsibilityDesignPattern.Middleware;

LogProcessor log = new InfoLog(LogLevel.Info, new ErrorLog(LogLevel.Error, null));

log.Log(LogLevel.Info, "test");

//Approval workflow
Approver directApproval = new DirectorApprover(null);
Approver managerApproval = new ManagerApprover(directApproval);
Approver teamLeadApproval = new TeamLeadApprover(managerApproval);


teamLeadApproval.ProvideApproval(2000000);

Middleware logging = new LoggingMiddleware();
Middleware auth = new AuthenticationMiddleware();
var authorization = new AuthorizationMiddleware();

logging.Next(auth).Next(authorization);

logging.Invoke();