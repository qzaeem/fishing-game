using System;
using System.Collections.Generic;

namespace Fishing.Variables
{
    public abstract class ListVariable<T> : Variable<List<T>>
    {
        public Action<T> onListValueChange;

        public virtual void Add(T thing)
        {
            if (!value.Contains(thing))
            {
                value.Add(thing);
                onListValueChange?.Invoke(thing);
            }
            value.RemoveAll(v => v == null);
        }

        public virtual void Remove(T thing)
        {
            if (value.Contains(thing))
            {
                value.Remove(thing);
                onListValueChange?.Invoke(thing);
            }
            value.RemoveAll(v => v == null);
        }

        public virtual void Clear()
        {
            foreach (var item in value)
            {
                Remove(item);
            }
            onValueChange.Invoke(value);
        }
    }
}