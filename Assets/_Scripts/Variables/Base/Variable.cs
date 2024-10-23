using System;
using UnityEngine;

namespace Fishing.Variables
{
    public abstract class Variable : ScriptableObject
    {
        public virtual T GetValue<T>()
        {
            return default;
        }

        public virtual void Initialize() { }

        public virtual void OnApplicationQuit() { }
    }

    public abstract class Variable<T> : Variable
    {
        public bool debug;
#if UNITY_EDITOR
        [Multiline]
        public string developerDescription = "";
#endif
        public T value = default;
        public Action<T> onValueChange = null;

        public void Set(T Value, bool notify = true)
        {
            if (debug)
            {
                Debug.Log($"Setting {name}: {Value}");
            }
            if (notify)
            {
                onValueChange?.Invoke(Value);
            }
            value = Value;
        }

        public override T1 GetValue<T1>()
        {
            return (T1)Convert.ChangeType(value, typeof(T1));
        }
    }
}