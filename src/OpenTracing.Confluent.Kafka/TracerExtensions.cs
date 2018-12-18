using System.Collections.Generic;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Confluent.Kafka
{
    public static class TracerExtensions
    {
        public static IScope CreateActiveProducerScopeFrom(this ITracer tracer, IDictionary<string, string> headers)
        {
            var spanBuilder = tracer.BuildSpan("send")
                .WithTag(Tags.SpanKind.Key, Tags.SpanKindProducer);

            var spanContext = tracer.Extract(BuiltinFormats.TextMap,
                new TextMapExtractAdapter(headers));

            spanBuilder.AsChildOf(spanContext);

            var scope = spanBuilder.StartActive(true);

            return new DelegateOnDisposeScopeDecorator(() =>
            {
                tracer.Inject(scope.Span.Context, BuiltinFormats.TextMap,
                    new TextMapInjectAdapter(headers));
            }, scope);
        }

        public static IScope CreateActiveConsumerScopeFrom(this ITracer tracer, IDictionary<string, string> headers)
        {
            var spanBuilder = tracer.BuildSpan("receive")
                .WithTag(Tags.SpanKind.Key, Tags.SpanKindConsumer);

            var parentSpanContext = tracer.Extract(BuiltinFormats.TextMap,
                new TextMapExtractAdapter(headers));

            spanBuilder.AddReference(References.FollowsFrom, parentSpanContext);

            var scope = spanBuilder.StartActive(true);

            return new DelegateOnDisposeScopeDecorator(() =>
            {
                tracer.Inject(scope.Span.Context, BuiltinFormats.TextMap,
                    new TextMapInjectAdapter(headers));
            }, scope);
        }
    }
}