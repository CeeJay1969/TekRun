using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyDomeReference : MonoBehaviour
{
    private void Awake()
    {
        Utility.SkyDomeEffect = gameObject;
    }
}
