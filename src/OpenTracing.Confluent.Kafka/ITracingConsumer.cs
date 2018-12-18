using System;
using System.Threading;
using Confluent.Kafka;

namespace OpenTracing.Confluent.Kafka
{
    public interface ITracingConsumer<TKey, TValue> : IConsumer<TKey, TValue>
    {
        IScope Consume(TimeSpan timeout, out ConsumeResult<TKey, TValue> result);

        IScope Consume(CancellationToken cancellationToken, out ConsumeResult<TKey, TValue> result);
    }
}