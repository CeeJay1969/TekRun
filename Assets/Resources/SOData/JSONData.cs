using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]

public class JSONData: ScriptableObject
{
    //Level data field
    [TextArea(10, 200)]
    public string FieldData;
}