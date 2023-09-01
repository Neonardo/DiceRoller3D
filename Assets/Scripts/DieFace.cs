using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DieFace : MonoBehaviour
{
    [SerializeField] private TextMeshPro FaceValueText;

    public int Value => _value;
    private int _value;
    
    public void ChangeFaceValue(int newValue)
    {
        _value = newValue;
        FaceValueText.text = _value.ToString();
    }
}
