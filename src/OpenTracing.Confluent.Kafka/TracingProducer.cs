using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using OpenTracing.Tag;

namespace OpenTracing.Confluent.Kafka
{
    public class TracingProducer<TKey, TValue> : IProducer<TKey, TValue>
    {
        private readonly ITracer _tracer;
        private readonly IProducer<TKey, TValue> _producer;

        public TracingProducer(ITracer tracer, IProducer<TKey, TValue> producer)
        {
            _tracer = tracer;
            _producer = producer;
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

        public int AddBrokers(string brokers)
        {
            return _producer.AddBrokers(brokers);
        }

        public Handle Handle => _producer.Handle;

        public string Name => _producer.Name;

        public event EventHandler<LogMessage> OnLog
        {
            add => _producer.OnLog += value;
            remove => _producer.OnLog += value;
        }

        public event EventHandler<ErrorEvent> OnError
        {
            add => _producer.OnError += value;
            remove => _producer.OnError += value;
        }

        public event EventHandler<string> OnStatistics
        {
            add => _producer.OnStatistics += value;
            remove => _producer.OnStatistics += value;
        }

        public async Task<DeliveryReport<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message,
            CancellationToken cancellationToken = new CancellationToken())
        {
            using (var scope = _tracer.CreateActiveProducerScopeFrom(message.Headers.ToDictionary(Encoding.UTF8)))
            {
                scope.Span.SetTag(Tags.MessageBusDestination, topic);

                var report = await _producer.ProduceAsync(topic, message, cancellationToken);

                scope.Span.SetTag("kafka.topic", report.Topic);
                scope.Span.SetTag("kafka.partition", report.Partition);
                scope.Span.SetTag("kafka.offset", report.Offset);

                return report;
            }
        }

        public async Task<DeliveryReport<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message,
            CancellationToken cancellationToken = new CancellationToken())
        {
            using (var scope = _tracer.CreateActiveProducerScopeFrom(message.Headers.ToDictionary(Encoding.UTF8)))
            {
                scope.Span.SetTag(Tags.MessageBusDestination, topicPartition.Topic);

                var report = await _producer.ProduceAsync(topicPartition, message, cancellationToken);

                scope.Span.SetTag("kafka.topic", report.Topic);
                scope.Span.SetTag("kafka.partition", report.Partition);
                scope.Span.SetTag("kafka.offset", report.Offset);

                return report;
            }
        }

        public void BeginProduce(string topic, Message<TKey, TValue> message, Action<DeliveryReportResult<TKey, TValue>> deliveryHandler)
        {
            var scope = _tracer.CreateActiveProducerScopeFrom(message.Headers.ToDictionary(Encoding.UTF8));
            scope.Span.SetTag(Tags.MessageBusDestination, topic);

            _producer.BeginProduce(topic, message, TracingDeliveryHandler);

            void TracingDeliveryHandler(DeliveryReportResult<TKey, TValue> report)
            {
                scope.Span.SetTag("kafka.topic", report.Topic);
                scope.Span.SetTag("kafka.partition", report.Partition);
                scope.Span.SetTag("kafka.offset", report.Offset);

                deliveryHandler(report);

                scope.Dispose();
            }
        }

        public void BeginProduce(TopicPartition topicPartition, Message<TKey, TValue> message, Action<DeliveryReportResult<TKey, TValue>> deliveryHandler)
        {
            var scope = _tracer.CreateActiveProducerScopeFrom(message.Headers.ToDictionary(Encoding.UTF8));
            scope.Span.SetTag(Tags.MessageBusDestination, topicPartition.Topic);

            _producer.BeginProduce(topicPartition, message, TracingDeliveryHandler);

            void TracingDeliveryHandler(DeliveryReportResult<TKey, TValue> report)
            {
                scope.Span.SetTag("kafka.topic", report.Topic);
                scope.Span.SetTag("kafka.partition", report.Partition);
                scope.Span.SetTag("kafka.offset", report.Offset);

                deliveryHandler(report);

                scope.Dispose();
            }
        }

        public int Poll(TimeSpan timeout)
        {
            return _producer.Poll(timeout);
        }

        public int Flush(TimeSpan timeout)
        {
            return _producer.Flush(timeout);
        }

        public void Flush(CancellationToken cancellationToken = new CancellationToken())
        {
            _producer.Flush(cancellationToken);
        }
    }
}