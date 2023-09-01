using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RollManager : MonoBehaviour
{
    public static RollManager Instance;
    
    [SerializeField] public Transform AutoRollStartPosition;
    [SerializeField] public Transform DiceRestTransform;
    
    [SerializeField] private UIController UIController;
    [SerializeField] private Transform BoardBoundsHolder;
    [SerializeField] private Die[] Dice;

    public float BoardLeftBorder = -10f, BoardRightBorder = 6f;
    public float BoardBottomBorder = -10f, BoardTopBorder = 10f;

    private bool _areDiceCurrentlyRolling = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Update()
    {
        if (!_areDiceCurrentlyRolling)
        {
            // if no dice are rolling, we disable bounding colliders
            if(BoardBoundsHolder.gameObject.activeInHierarchy)
                BoardBoundsHolder.gameObject.SetActive(false);
        }
        else
        {
            // if dice are rolling, we enable bounding colliders if needed
            if(!BoardBoundsHolder.gameObject.activeInHierarchy)
                BoardBoundsHolder.gameObject.SetActive(true);
            
            // in rare case of dice rolling out of board, we return them to the table
            foreach (var die in Dice)
            {
                if (Vector3.Distance(die.transform.position, Vector3.zero) > 30f)
                {
                    die.transform.position = DiceRestTransform.position + new Vector3(0f,1f,0f);
                    die.StopAllMovement();
                }
            }
        }
    }

    public void AutoRoll()
    {
        if (_areDiceCurrentlyRolling) return;
        
        _areDiceCurrentlyRolling = true;
        UIController.InitiateRolling();
        
        foreach (var die in Dice)
        {
            die.transform.position = AutoRollStartPosition.position;
            die.RandomRoll();
        }

        StartCoroutine(AwaitRollResult());
    }

    public void ManualRoll()
    {
        _areDiceCurrentlyRolling = true;
        UIController.InitiateRolling();
        StartCoroutine(AwaitRollResult());
    }

    private IEnumerator AwaitRollResult()
    {
        int numberOfDiceFinishedRolling = 0;

        while (numberOfDiceFinishedRolling < Dice.Length)
        {
            numberOfDiceFinishedRolling = 0;
            foreach (var die in Dice)
            {
                if (!die.IsDieRolling)
                    numberOfDiceFinishedRolling++;
            }
            yield return null;
        }
        
        UIController.UpdateTextAfterRolling(Dice[0].CurrentValue);
        _areDiceCurrentlyRolling = false;
    }
}
