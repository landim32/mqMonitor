namespace MqMonitor.Domain.Messaging.Interfaces;

public interface IMessagePublisher
{
    void PublishEvent(object eventData, string routingKey);
    void PublishEvent(object eventData, string routingKey, byte priority);
    void PublishToPipeline(object eventData, string routingKey, byte priority = 0);
    void PublishCommand(object command, string exchange, string routingKey);
}
