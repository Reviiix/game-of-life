using System;
using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class CountdownStartText : MonoBehaviour
{
    private readonly WaitForSeconds oneSecond = new (1);
    private TMP_Text text;
    
    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    public IEnumerator CountDown(int seconds, bool randomisedColour, Action callBack)
    {
        if (seconds == 1)
        {
            text.text = "GO!";
            yield return oneSecond;
            text.text = string.Empty;
            callBack();
            yield break;
        }
        for (var i = seconds; i >= 1; i--)
        {
            text.text = i.ToString();

            if (randomisedColour)
            {
                text.color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1);
            }
            yield return oneSecond;
        }
        text.text = string.Empty;
        callBack();
    }
}
