using System;
using System.Collections;
using System.Data;
using pure_unity_methods.StateManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StateManagement.States
{
    public class Evolve : State
    {
        private const float MinimumTime = float.Epsilon;
        private const float MaximumTime = 5;
        [SerializeField] [Range(MinimumTime, MaximumTime)] private float time;
        private WaitForSeconds waitTime;
        [SerializeField] public Slider speedSlider;

        private void Awake()
        {
            SetSliders();
            SetTime(time);
        }
        
        public override void OnStateEnter(Action callBack)
        {
            StartCoroutine(DelayProgression(callBack));
        }

        private void SetSliders()
        {
            speedSlider.wholeNumbers = false;
            speedSlider.value = time;
            speedSlider.minValue = MinimumTime;
            speedSlider.maxValue = MaximumTime;
            speedSlider.onValueChanged.AddListener(SetTime);
        }
        
        private void SetTime(float t)
        {
            time = t;
            waitTime = new WaitForSeconds(time);
        }

        private IEnumerator DelayProgression(Action callBack)
        {
            yield return waitTime;
            callBack();
        }
    }
}
