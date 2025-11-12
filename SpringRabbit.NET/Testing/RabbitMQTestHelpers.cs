using RabbitMQ.Client;

namespace SpringRabbit.NET.Testing;

/// <summary>
/// Helper utilities for testing RabbitMQ integrations.
/// </summary>
public static class RabbitMQTestHelpers
{
    /// <summary>
    /// Purges all messages from a queue (useful for test cleanup).
    /// </summary>
    public static void PurgeQueue(IModel channel, string queueName)
    {
        channel.QueuePurge(queueName);
    }

    /// <summary>
    /// Deletes a queue (useful for test cleanup).
    /// </summary>
    public static void DeleteQueue(IModel channel, string queueName)
    {
        channel.QueueDelete(queueName, ifUnused: false, ifEmpty: false);
    }

    /// <summary>
    /// Gets the message count in a queue.
    /// </summary>
    public static uint GetMessageCount(IModel channel, string queueName)
    {
        var result = channel.QueueDeclarePassive(queueName);
        return result.MessageCount;
    }

    /// <summary>
    /// Gets the consumer count for a queue.
    /// </summary>
    public static uint GetConsumerCount(IModel channel, string queueName)
    {
        var result = channel.QueueDeclarePassive(queueName);
        return result.ConsumerCount;
    }
}


