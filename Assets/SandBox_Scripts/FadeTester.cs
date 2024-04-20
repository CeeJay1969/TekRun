using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FadeTester : MonoBehaviour
{
    [SerializeField] TextAlphaFader textAnimator;
    [SerializeField] TextMeshProUGUI textFader;

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            int alphaRampUpSpeed = 10;
            float sustainSeconds = 2f;
            int alphaRampDownSpeed = 10;
            float animationWaitTime = 0.01f;

            textAnimator.FadeAlpha(0, alphaRampUpSpeed, sustainSeconds, alphaRampDownSpeed, animationWaitTime, textFader);
        }

    }
}
