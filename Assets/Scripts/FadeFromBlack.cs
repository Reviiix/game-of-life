using System.Collections;
using pure_unity_methods.Effects;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FadeFromBlack : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.color = Color.black;
    }

    private IEnumerator Start()
    {
        // Skip the first frame so the load hitch's large deltaTime doesn't eat into the fade.
        yield return null;
        yield return Fade.FadeImageAlphaDown(image, fadeDuration, () => gameObject.SetActive(false));
    }
}
