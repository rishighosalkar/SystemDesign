
using DecoratorPattern.Core;
using DecoratorPattern.Decorators;
using DecoratorPattern.NotificationCore;
using DecoratorPattern.NotificationDecorators;

IDataStream dataStream = new EncryptDecorator(new CachingDecorator(new FileDataStream()));

dataStream.Write("abcdefghisasasx");

//
INotification notification = new SMSNotification(new SlackNotification(new EmailNotification()));

notification.Notify("You've completed decorator pattern");