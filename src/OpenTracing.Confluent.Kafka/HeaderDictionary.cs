using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confluent.Kafka;

namespace OpenTracing.Confluent.Kafka
{
    internal class HeaderDictionary : IDictionary<string, string>
    {
        private readonly Headers _headers;
        private static Encoding Encoding =>
            Encoding.UTF8;

        public HeaderDictionary(Headers headers)
        {
            _headers = headers;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _headers
                .Select(header =>
                    new KeyValuePair<string, string>(
                        header.Key,
                        Encoding.GetString(header.Value)))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _headers.Add(new Header(item.Key, Encoding.GetBytes(item.Value)));
        }

        public void Clear()
        {
            while (_headers.Count > 0)
            {
                _headers.Remove(_headers[0].Key);
            }
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            if (_headers.TryGetLast(item.Key, out var lastHeader))
            {
                return item.Value == Encoding.GetString(lastHeader);
            }

            return false;
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (arrayIndex >= _headers.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (var i = arrayIndex; i < _headers.Count; i++)
            {
                var header = _headers[i];
                array[i - arrayIndex] =
                    new KeyValuePair<string, string>(header.Key, Encoding.GetString(header.Value));
            }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            var headersWithSameKey = _headers
                .Where(header => header.Key == item.Key)
                .ToList();

            var headersWithSameKeyAndValue = headersWithSameKey
                .Where(header => Encoding.GetString(header.Value) == item.Value)
                .ToList();

            if (headersWithSameKeyAndValue.Any() == false)
            {
                return false;
            }

            _headers.Remove(item.Key);

            foreach (var header in headersWithSameKeyAndValue)
            {
                headersWithSameKey.Remove(header);
            }

            foreach (var header in headersWithSameKey)
            {
                _headers.Add(header);
            }

            return true;
        }

        public int Count => _headers.Count;

        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            _headers.Add(key, Encoding.GetBytes(value));
        }

        public bool ContainsKey(string key)
        {
            return _headers.Any(header => header.Key == key);
        }

        public bool Remove(string key)
        {
            if (_headers.TryGetLast(key, out _))
            {
                _headers.Remove(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            if (_headers.TryGetLast(key, out var lastHeader))
            {
                value = Encoding.GetString(lastHeader);
                return true;
            }

            value = null;
            return false;
        }

        public string this[string key]
        {
            get => Encoding.GetString(_headers.GetLast(key));
            set
            {
                _headers.Remove(key);
                _headers.Add(key, Encoding.GetBytes(value));
            }
        }

        public ICollection<string> Keys => 
            _headers
                .Select(header => header.Key)
                .Distinct()
                .ToList();

        public ICollection<string> Values =>
            _headers
                .Select(header => new KeyValuePair<string, string>(header.Key, Encoding.GetString(header.Value)))
                .GroupBy(pair => pair.Key)
                .Select(pairs => pairs.Last().Value)
                .ToList();

    }
}