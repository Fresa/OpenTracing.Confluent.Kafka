using System;
using OpenTracing;

namespace OpenTracingKafkaClient
{
    internal class DelegateOnDisposeScopeDecorator : IScope
    {
        private readonly Action _delegate;
        private readonly IScope _scope;

        public DelegateOnDisposeScopeDecorator(Action @delegate, IScope scope)
        {
            _delegate = @delegate;
            _scope = scope;
        }

        public void Dispose()
        {
            _delegate();
            _scope.Dispose();
        }

        public ISpan Span => _scope.Span;
    }
}