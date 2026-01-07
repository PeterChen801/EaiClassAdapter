using System.Collections.Generic;

namespace EaiClassAdapter
{
    public class AdapterContext
    {
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public void Set(string key, string value)
        {
            _values[key] = value;
        }

        public string Get(string key)
        {
            return _values.ContainsKey(key) ? _values[key] : null;
        }
    }
}