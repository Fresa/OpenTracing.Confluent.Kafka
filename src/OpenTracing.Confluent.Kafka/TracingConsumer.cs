using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Confluent.Kafka;

namespace OpenTracing.Confluent.Kafka
{
    public class TracingConsumer<TKey, TValue> : IConsumer<TKey, TValue>
    {
        private readonly ITracer _tracer;
        private readonly IConsumer<TKey, TValue> _consumer;

        public TracingConsumer(ITracer tracer, IConsumer<TKey, TValue> consumer)
        {
            _tracer = tracer;
            _consumer = consumer;
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public int AddBrokers(string brokers)
        {
            return _consumer.AddBrokers(brokers);
        }

        public Handle Handle => _consumer.Handle;
        public string Name => _consumer.Name;
        public event EventHandler<LogMessage> OnLog
        {
            add => _consumer.OnLog += value;
            remove => _consumer.OnLog += value;
        }

        public event EventHandler<ErrorEvent> OnError
        {
            add => _consumer.OnError += value;
            remove => _consumer.OnError += value;
        }

        public event EventHandler<string> OnStatistics
        {
            add => _consumer.OnStatistics += value;
            remove => _consumer.OnStatistics += value;
        }

        public ConsumeResult<TKey, TValue> Consume(TimeSpan timeout)
        {
            return _consumer.Consume(timeout);
        }

        public IScope Consume(TimeSpan timeout, out ConsumeResult<TKey, TValue> result)
        {
            result = _consumer.Consume(timeout);

            if (result == null)
            {
                return null;
            }

            result.Headers = result.Headers ?? new Headers();

            var scope = _tracer.CreateAndInjectActiveConsumerScopeFrom(result.Headers.ToDictionary(Encoding.UTF8));

            scope.Span.SetTag("kafka.topic", result.Topic);
            scope.Span.SetTag("kafka.partition", result.Partition);
            scope.Span.SetTag("kafka.offset", result.Offset);
            return scope;
        }
        
        public ConsumeResult<TKey, TValue> Consume(CancellationToken cancellationToken)
        {
            return _consumer.Consume(cancellationToken);
        }

        public IScope Consume(CancellationToken cancellationToken, out ConsumeResult<TKey, TValue> result)
        {
            result = _consumer.Consume(cancellationToken);

            if (result == null)
            {
                return null;
            }

            result.Headers = result.Headers ?? new Headers();

            var scope = _tracer.CreateAndInjectActiveConsumerScopeFrom(result.Headers.ToDictionary(Encoding.UTF8));

            scope.Span.SetTag("kafka.topic", result.Topic);
            scope.Span.SetTag("kafka.partition", result.Partition);
            scope.Span.SetTag("kafka.offset", result.Offset);
            return scope;
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            _consumer.Subscribe(topics);
        }

        public void Subscribe(string topic)
        {
            _consumer.Subscribe(topic);
        }

        public void Unsubscribe()
        {
            _consumer.Unsubscribe();
        }

        public void Assign(TopicPartition partition)
        {
            _consumer.Assign(partition);
        }

        public void Assign(TopicPartitionOffset partition)
        {
            _consumer.Assign(partition);
        }

        public void Assign(IEnumerable<TopicPartitionOffset> partitions)
        {
            _consumer.Assign(partitions);
        }

        public void Assign(IEnumerable<TopicPartition> partitions)
        {
            _consumer.Assign(partitions);
        }

        public void Unassign()
        {
            _consumer.Unassign();
        }

        public void StoreOffset(ConsumeResult<TKey, TValue> result)
        {
            _consumer.StoreOffset(result);
        }

        public void StoreOffsets(IEnumerable<TopicPartitionOffset> offsets)
        {
            _consumer.StoreOffsets(offsets);
        }

        public List<TopicPartitionOffset> Commit(CancellationToken cancellationToken = new CancellationToken())
        {
            return _consumer.Commit(cancellationToken);
        }

        public TopicPartitionOffset Commit(ConsumeResult<TKey, TValue> result, CancellationToken cancellationToken = new CancellationToken())
        {
            return _consumer.Commit(result, cancellationToken);
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets, CancellationToken cancellationToken = new CancellationToken())
        {
            _consumer.Commit(offsets, cancellationToken);
        }

        public void Seek(TopicPartitionOffset tpo)
        {
            _consumer.Seek(tpo);
        }

        public void Pause(IEnumerable<TopicPartition> partitions)
        {
            _consumer.Pause(partitions);
        }

        public void Resume(IEnumerable<TopicPartition> partitions)
        {
            _consumer.Resume(partitions);
        }

        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout, CancellationToken cancellationToken = new CancellationToken())
        {
            return _consumer.Committed(partitions, timeout, cancellationToken);
        }

        public List<TopicPartitionOffset> Position(IEnumerable<TopicPartition> partitions)
        {
            return _consumer.Position(partitions);
        }

        public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _consumer.OffsetsForTimes(timestampsToSearch, timeout, cancellationToken);
        }

        public string MemberId => _consumer.MemberId;
        public List<TopicPartition> Assignment => _consumer.Assignment;
        public List<string> Subscription => _consumer.Subscription;

        public event EventHandler<List<TopicPartition>> OnPartitionsAssigned
        {
            add => _consumer.OnPartitionsAssigned += value;
            remove => _consumer.OnPartitionsAssigned += value;
        }

        public event EventHandler<List<TopicPartition>> OnPartitionsRevoked
        {
            add => _consumer.OnPartitionsRevoked += value;
            remove => _consumer.OnPartitionsRevoked += value;
        }

        public event EventHandler<CommittedOffsets> OnOffsetsCommitted
        {
            add => _consumer.OnOffsetsCommitted += value;
            remove => _consumer.OnOffsetsCommitted += value;
        }
    }
}
