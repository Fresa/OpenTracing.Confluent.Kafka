using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;

namespace OpenTracing.Confluent.Kafka
{
    internal static class HeadersExtensions
    {
        internal static IDictionary<string, string> ToDictionary(this Headers headers, Encoding encoding)
        {
            if (headers == null)
            {
                return new Dictionary<string, string>();
            }

            return new HeaderDictionary(headers);
        }
    }
}