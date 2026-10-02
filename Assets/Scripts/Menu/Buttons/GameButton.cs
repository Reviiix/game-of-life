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
            GetComponent<Button>().onClick.AddListener(() => OnClick(() => { }));
            onClickAnimation = GetComponent<Animator>();
            onClickAnimation.enabled = false;
        }
    
        protected virtual void OnClick(Action callBack)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Override this method");
            #endif
        }

        protected IEnumerator AnimateButton(Action callBack)
        {
            // The animator is left disabled while idle; disabling it again afterwards resets it to its default state.
            onClickAnimation.enabled = true;
            onClickAnimation.SetTrigger(Play);
            yield return null;
            yield return new WaitUntil(() => !onClickAnimation.IsInTransition(0) && onClickAnimation.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
            onClickAnimation.enabled = false;

            callBack();
        }
    }
}
