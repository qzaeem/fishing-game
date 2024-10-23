using System;
using System.Collections.Generic;

namespace Fishing.Variables
{
    public abstract class DictionaryVariable<T1, T2> : Variable<Dictionary<T1, T2>>
    {
        public Action<T1, T2> onListValueChange;

        public void Add(T1 type, T2 thing)
        {
            if (!value.ContainsKey(type))
            {
                value.Add(type, thing);
                onListValueChange?.Invoke(type, thing);
            }
        }

        public void Remove(T1 type, T2 thing)
        {
            if (value.ContainsKey(type))
            {
                value.Remove(type);
                onListValueChange?.Invoke(type, thing);
            }
        }

        public void Clear()
        {
            foreach (var item in value)
            {
                Remove(item.Key, item.Value);
            }
            onValueChange.Invoke(value);
        }
    }
}