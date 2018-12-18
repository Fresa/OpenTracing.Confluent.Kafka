using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;

namespace OpenTracingKafkaClient
{
    internal static class HeadersExtensions
    {
        internal static IDictionary<string, string> ToDictionary(this Headers headers, Encoding encoding)
        {
            return headers.ToDictionary(header => header.Key, header => encoding.GetString(header.Value));
        }
    }
}