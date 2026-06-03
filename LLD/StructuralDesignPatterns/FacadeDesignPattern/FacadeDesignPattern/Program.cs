using FacadeDesignPattern.LoggingFramework;

var logger = new AppLoggerFacade(new AppLogger());

logger.Info("Test Info");
logger.Error("Test Error");