using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ResultValueText;
    [SerializeField] private TextMeshProUGUI TotalValueText;

    private int currentTotal = 0;

    public void UpdateTextAfterRolling(int dieResult)
    {
        ResultValueText.text = "Result: " + dieResult.ToString();
        currentTotal += dieResult;
        TotalValueText.text = "Total: " + currentTotal.ToString();
    }

    public void InitiateRolling()
    {
        ResultValueText.text = "Result: ?";
    }
}
