using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextAlphaFader : MonoBehaviour
{
    public void FadeAlpha(float initialDelaySeconds, int alphaRampUpSpeed, float sustainSeconds, int alphaRampDownSpeed, float animationWaitTime,
        TextMeshProUGUI targetTextField)
    {
        StartCoroutine (FadeAlphaCoroutine (initialDelaySeconds, alphaRampUpSpeed, sustainSeconds, alphaRampDownSpeed, animationWaitTime, targetTextField));
    }

    IEnumerator FadeAlphaCoroutine(float initialDelaySeconds, int alphaRampUpSpeed, float sustainSeconds, int alphaRampDownSpeed, float animationWaitTime, TextMeshProUGUI targetTextField)
    {
        Color32 fieldColor = targetTextField.color;

        //Wait initial fade start
        if (initialDelaySeconds > 0)
        {
            yield return new WaitForSeconds(initialDelaySeconds);
        }

        //Start fade in the alpha to opaque
        while ((fieldColor.a < 255) && (alphaRampUpSpeed != 0))
        {
            int alphaFade = fieldColor.a;

            if (alphaFade + alphaRampUpSpeed > 255)
                alphaFade = 255;
            else
                alphaFade += alphaRampUpSpeed;

            //Modify the alpha component
            fieldColor.a = (byte)alphaFade;
            targetTextField.color = fieldColor;

            yield return new WaitForSeconds(animationWaitTime);
        }

        //Wait for duration of sustain seconds
        if (sustainSeconds > 0)
        {
            yield return new WaitForSeconds(sustainSeconds);
        }

        //Start fade out of alpha to transparent
        while ((fieldColor.a > 0) && (alphaRampDownSpeed != 0))
        {       
            int alphaFade = fieldColor.a;

            if (alphaFade - alphaRampDownSpeed < 0)
                alphaFade = 0;
            else
                alphaFade -= alphaRampDownSpeed;

            //Modify the alpha component
            fieldColor.a = (byte)alphaFade;
            targetTextField.color = fieldColor;

            yield return new WaitForSeconds(animationWaitTime);
        }
    }
}
