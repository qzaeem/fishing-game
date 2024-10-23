using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Fishing.Actions
{
    [CreateAssetMenu(menuName = "Actions/Action List")]
    public class ActionListSO : ScriptableObject
    {
        public bool once;
        public List<ActionSO> list = new();
        int size;

        public void Initialize()
        {
            size = list.Count;
            if (once)
            {
                return;
            }
            //Debug.LogWarning($"Initialize: {name}");
            if (list != null)
            {
                for (int i = 0; i < size; i++)
                {
                    Assert.IsNotNull(list[i], $"{name}: {i}");
                    list[i].Initialize();
                }
            }
        }

        public void Execute()
        {
            size = list.Count;
            //Debug.LogWarning($"Execute: {name}");
            if (list != null)
            {
                for (int i = 0; i < size; i++)
                {
                    Assert.IsNotNull(list[i], $"{name}: {i}");
                    list[i].Execute();
                }
            }
        }

        public void Update()
        {
            for (int i = 0; i < size; i++)
            {
                list[i].Update();
            }
        }

        public void OnDestroy()
        {
            once = false;
            if (list != null)
            {
                int size = list.Count;
                for (int i = 0; i < size; i++)
                {
                    Assert.IsNotNull(list[i], $"{name}: {i}");
                    list[i].OnDestroy();
                }
            }
        }

        public void OnApplicationQuit()
        {
            once = false;
            if (list != null)
            {
                int size = list.Count;
                for (int i = 0; i < size; i++)
                {
                    Assert.IsNotNull(list[i], $"{name}: {i}");
                    list[i].OnApplicationQuit();
                }
            }
        }
    }
}