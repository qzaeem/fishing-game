using System;
using UnityEngine;

namespace Fishing.Actions
{
    [Serializable]
    [CreateAssetMenu(menuName = "Actions/Empty")]
    public class ActionSO : ScriptableObject
    {
        public bool debug = true;
#if UNITY_EDITOR
        [Multiline]
        public string developerDescription = "";
#endif
        public Action initializeAction, executeAction, stopAction;

        public virtual void Initialize()
        {
            initializeAction?.Invoke();
            DebugInitializeAction();
            debug = true; //making debug to be true forcefully
        }

        public virtual void Execute()
        {
            executeAction?.Invoke();
            DebugExecuteAction();
        }

        public virtual void StopExecute()
        {
            stopAction?.Invoke();
        }

        public virtual void Update() { }

        public virtual void DebugInitializeAction()
        {
            if (debug)
            {
                Debug.LogWarning($"I: {name}");
            }
        }

        public virtual void DebugExecuteAction()
        {
            if (debug)
            {
                Debug.LogWarning($"E: {name}");
            }
        }

        public virtual void DebugStopAction()
        {
            if (debug)
            {
                Debug.LogWarning($"Stopping: {name}");
            }
        }

        public virtual void OnApplicationQuit() { }

        public virtual void OnDestroy() { }
    }
}