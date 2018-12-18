using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OpenTracing.Mock;
using OpenTracing.Tag;
using Test.It.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace OpenTracing.Confluent.Kafka.Tests
{
    public partial class Given_a_tracer
    {
        public partial class When_creating_a_producer_scope : Specification
        {
            private MockTracer _tracer;
            private Dictionary<string, string> _headers;
            private IScope _scope;

            public When_creating_a_producer_scope()
            {
                Setup();
            }

            protected override void Given()
            {
                _tracer = new MockTracer();
                _headers = new Dictionary<string, string>();
            }

            protected override void When()
            {
                using (_scope = _tracer.CreateActiveProducerScopeFrom(_headers)) { }
            }

            [Fact]
            public void It_should_have_set_span_kind()
            {
                ((MockSpan)_scope.Span).Tags
                    .Should()
                    .Contain(new KeyValuePair<string, object>(Tags.SpanKind.Key, Tags.SpanKindProducer));
            }

            [Fact]
            public void It_should_have_finished_the_span()
            {
                _tracer.FinishedSpans().Should().Contain((MockSpan)_scope.Span);
            }

            [Fact]
            public void It_should_have_injected_the_context()
            {
                _headers.Should()
                    .Contain(new KeyValuePair<string, string>("spanid", _scope.Span.Context.SpanId))
                    .And
                    .Contain(new KeyValuePair<string, string>("traceid", _scope.Span.Context.TraceId));
            }
        }

        public partial class When_creating_a_producer_scope_but_not_finishing_it : Specification
        {
            private MockTracer _tracer;
            private Dictionary<string, string> _headers;

            public When_creating_a_producer_scope_but_not_finishing_it()
            {
                Setup();
            }

            protected override void Given()
            {
                _tracer = new MockTracer();
                _headers = new Dictionary<string, string>();
            }

            protected override void When()
            {
                _tracer.CreateActiveProducerScopeFrom(_headers);
            }

            [Fact]
            public void It_should_have_no_finished_spans()
            {
                _tracer.FinishedSpans().Should().HaveCount(0);
            }

            [Fact]
            public void It_should_not_have_injected_the_context()
            {
                _headers.Should().BeEmpty();
            }
        }

        public partial class When_creating_a_consumer_scope_but_not_finishing_it : Specification
        {
            private MockTracer _tracer;
            private Dictionary<string, string> _headers;

            public When_creating_a_consumer_scope_but_not_finishing_it()
            {
                Setup();
            }

            protected override void Given()
            {
                _tracer = new MockTracer();
                _headers = new Dictionary<string, string>();
            }

            protected override void When()
            {
                _tracer.CreateActiveConsumerScopeFrom(_headers);
            }

            [Fact]
            public void It_should_have_no_finished_spans()
            {
                _tracer.FinishedSpans().Should().HaveCount(0);
            }

            [Fact]
            public void It_should_not_have_injected_the_context()
            {
                _headers.Should().BeEmpty();
            }
        }

        public partial class When_sending_context_to_consumer : Specification
        {
            private MockTracer _tracer;
            private Dictionary<string, string> _headers;

            public When_sending_context_to_consumer()
            {
                Setup();
            }

            protected override void Given()
            {
                _tracer = new MockTracer();
                _headers = new Dictionary<string, string>();
            }

            protected override void When()
            {
                using (var scope = _tracer.CreateActiveProducerScopeFrom(_headers)) { }
                using (var scope = _tracer.CreateActiveConsumerScopeFrom(_headers)) { }
            }

            [Fact]
            public void It_should_have_finished_two_spans()
            {
                _tracer.FinishedSpans().Should().HaveCount(2);
            }

            [Fact]
            public void It_should_have_set_a_follow_reference_from_the_send_span_to_the_receive_span()
            {
                _tracer.FinishedSpans().Last().References.Should()
                    .Contain(reference => 
                        reference.Context.SpanId == _tracer.FinishedSpans().First().Context.SpanId)
                    .And
                    .Contain(reference => 
                        reference.Context.TraceId == _tracer.FinishedSpans().First().Context.TraceId)
                    .And
                    .HaveCount(1)
                    .And
                    .Contain(reference => 
                        reference.ReferenceType == References.FollowsFrom);
            }
        }
    }
}
