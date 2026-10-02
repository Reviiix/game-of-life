using System;
using UnityEngine;

namespace pure_unity_methods.StateManagement
{
    [Serializable]
    public abstract class State : MonoBehaviour, IState
    {
        public bool progressImmediately;
    
        public virtual void OnStateEnter(Action callBack)
        {
            throw new NotImplementedException();
        }
    }
}
