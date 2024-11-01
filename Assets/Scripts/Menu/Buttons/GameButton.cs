using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Buttons
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Animator))]
    public class GameButton : MonoBehaviour
    {
        private Animator onClickAnimation;
        private static readonly int Play = Animator.StringToHash("Play");

        protected virtual void Start()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
            onClickAnimation = GetComponent<Animator>();
            onClickAnimation.enabled = false;
        }
    
        protected virtual void OnClick()
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Override this method");
            #endif
        }

        protected IEnumerator AnimateButton(Action callBack)
        {
            onClickAnimation.SetTrigger(Play);
            var v = onClickAnimation.GetCurrentAnimatorClipInfo(0).Length;
            yield return new WaitForSeconds(onClickAnimation.GetCurrentAnimatorClipInfo(0).Length);

            callBack();
        }
    }
}
