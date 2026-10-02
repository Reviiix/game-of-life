using System;

namespace pure_unity_methods.StateManagement
{
    public interface IState
    {
        public void OnStateEnter(Action callBack);
    }
}
