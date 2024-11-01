using System;
using System.Collections.Generic;
using pure_unity_methods.Abstraction;
using UnityEngine;

namespace pure_unity_methods.StateManagement
{
    public abstract class SequentialStateManager : Singleton<SequentialStateManager>
    {
        public static Action OnStateChange;
        [SerializeField] private State[] states;
        private readonly Queue<State> stateQueue = new ();
        private State activeState;

        public virtual void Initialise()
        {
            ConvertArrayToQueue();
        }

        private void ConvertArrayToQueue()
        {
            foreach (var state in states)
            {
                stateQueue.Enqueue(state);
            }
        }
    
        public virtual void ProgressState()
        {
            activeState = stateQueue.Dequeue();
            stateQueue.Enqueue(activeState);
            OnStateChange?.Invoke();
            activeState.OnStateEnter(()=>
            {
                if (activeState.progressImmediately)
                {
                    ProgressState();
                }
            });
        }
    }
}
