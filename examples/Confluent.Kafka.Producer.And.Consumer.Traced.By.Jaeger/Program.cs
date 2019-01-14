using System;
using System.Threading;
using System.Threading.Tasks;
using Jaeger;
using Jaeger.Samplers;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTracing;
using OpenTracing.Confluent.Kafka;

namespace Confluent.Kafka.Clients.Traced.By.Jaeger
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var tracer = GetTracer();

                var tracingProducer = GetTracingProducer(tracer);
                var produceResult = await tracingProducer.ProduceAsync("topic1", new Message<string, string>
                {
                    Key = "TestMessage",
                    Value = "I am your tracer"
                });

                Console.WriteLine(
                    $"Sent message to partition {produceResult.Partition.Value}, offset {produceResult.Offset.Value} on topic {produceResult.Topic}");

                var tracingConsumer = GetTracingConsumer(tracer);
                tracingConsumer.Subscribe("topic1");

                const int maxConsumeAttempts = 10;
                var attemptsTried = 0;
                tracingConsumer.OnError += (sender, @event) =>
                    attemptsTried = @event.IsFatal ? maxConsumeAttempts : attemptsTried;

                while (attemptsTried < maxConsumeAttempts)
                {
                    using (var scope = tracingConsumer.Consume(TimeSpan.FromSeconds(5), out var consumeResult))
                    {
                        attemptsTried++;
                        if (consumeResult == null)
                            continue;

                        if (consumeResult.Offset < produceResult.Offset)
                        {
                            attemptsTried--;
                            continue;
                        }

                        scope.Span.SetTag("Consumer.Timeout", 5);
                        scope.Span.SetTag("Consumer.Message.Key", consumeResult.Key);
                        scope.Span.SetTag("Consumer.Message.Value", consumeResult.Value);

                        // Simulate consumption load for nicer traces
                        await Task.Delay(TimeSpan.FromSeconds(1));

                        Console.WriteLine(
                            $"Received message from partition {consumeResult.Partition.Value}, offset {consumeResult.Offset.Value} on topic {consumeResult.Topic}");

                        break;
                    }
                }

                if (attemptsTried == maxConsumeAttempts)
                {
                    Console.WriteLine("Never received any messages");
                }

                tracingProducer.Dispose();
                tracingConsumer.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static ITracer GetTracer()
        {
            var logFactory = new NullLoggerFactory();

            return new Configuration("kafka.jaeger.example", logFactory)
                .WithReporter(new Configuration.ReporterConfiguration(logFactory)
                    .WithSender(new Configuration.SenderConfiguration(logFactory)
                        .WithEndpoint("http://localhost:14268/api/traces")))
                .WithSampler(new Configuration.SamplerConfiguration(logFactory)
                    .WithType(ConstSampler.Type)
                    .WithParam(1))
                .GetTracer();
        }

        private static ClientConfig GetClientConfig()
        {
            return new ClientConfig
            {
                GroupId = "kafka.jaeger",
                BootstrapServers = "127.0.0.1:9092"
            };
        }

        private static TracingConsumer<string, string> GetTracingConsumer(ITracer tracer)
        {
            var consumerConfig = new ConsumerConfig(GetClientConfig())
            {
                AutoOffsetReset = AutoOffsetResetType.Earliest
            };

            var consumer = new Consumer<string, string>(consumerConfig);
            var tracingConsumer = new TracingConsumer<string, string>(tracer, consumer);
            consumer.OnLog += (sender, message) =>
                Console.WriteLine($"CONSUMER: {message.Level} {message.Message}");
            consumer.OnError += (sender, @event) => Console.WriteLine($"CONSUMER: {@event.Code} {@event.Reason}");

            return tracingConsumer;
        }

        private static TracingProducer<string, string> GetTracingProducer(ITracer tracer)
        {
            var producerConfig = new ProducerConfig(GetClientConfig())
            {
                MessageTimeoutMs = 5000
            };
            var producer = new Producer<string, string>(producerConfig);
            var tracingProducer = new TracingProducer<string, string>(tracer, producer);
            producer.OnLog += (sender, message) =>
                Console.WriteLine($"PRODUCER: {message.Level} {message.Message}");
            producer.OnError += (sender, @event) => Console.WriteLine($"PRODUCER: {@event.Code} {@event.Reason}");

            return tracingProducer;
        }
    }
}
