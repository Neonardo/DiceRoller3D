using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Die : MonoBehaviour
{
    private enum DieState
    {
        Idle,
        InHand,
        Rolling
    }
    
    [Header("Die settings")]
    [SerializeField] private int[] Values;

    [Header("References")]
    [SerializeField] private Transform FacesHolder;
    [SerializeField] private DieFace[] Faces;

    public bool IsDieRolling => _currentDieState == DieState.Rolling;
    public int CurrentValue => _lastRolledValue;

    private Rigidbody _rigidbody;
    private LayerMask _rollResultCatcherMask;
    private DieState _currentDieState = DieState.Idle;
    private int _lastRolledValue = 0;
    
    private float _clickZPosition = 0f;
    private Vector3 _clickOffset = Vector3.zero;
    private Vector3 _lastMousePosition = Vector3.zero;

#region Unity methods

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
            Debug.LogError("Die has no rigidbody attached");
        
        // we store information about layer mask used to detect rolls
        _rollResultCatcherMask = LayerMask.GetMask("DieResultCatcher");
    }

    private void Update()
    {
        EvaluateDieState();
    }

    private void OnMouseDrag()
    {
        Vector3 dragPosition = GetMousePositionWorld() + _clickOffset;
        // we set manually the y position, the height of die hovering over board 
        dragPosition.y = 2f;

        // limit the movement of die over the board
        if (dragPosition.x < RollManager.Instance.BoardLeftBorder)
            dragPosition.x = RollManager.Instance.BoardLeftBorder;
        else if (dragPosition.x > RollManager.Instance.BoardRightBorder)
            dragPosition.x = RollManager.Instance.BoardRightBorder;
        if (dragPosition.z < RollManager.Instance.BoardBottomBorder)
            dragPosition.z = RollManager.Instance.BoardBottomBorder;
        else if (dragPosition.z > RollManager.Instance.BoardTopBorder)
            dragPosition.z = RollManager.Instance.BoardTopBorder;
        
        transform.position = dragPosition;
        // we store last frame mouse position to use for roll force calculations
        _lastMousePosition = Input.mousePosition;
    }
    private void OnMouseDown()
    {
        _currentDieState = DieState.InHand;
        _rigidbody.useGravity = false;

        _clickZPosition = Camera.main.WorldToScreenPoint(transform.position).z;
        _clickOffset = transform.position - GetMousePositionWorld();
    }
    private void OnMouseUp()
    {
        _currentDieState = DieState.Rolling;
        _rigidbody.useGravity = true;
        Vector3 throwVector = (Input.mousePosition - _lastMousePosition) * 50f;
        // we swap y and z coords to accomodate for camera position in relation to the board
        throwVector.z = throwVector.y;
        throwVector.y = 0f;
        
        // check if throw force is not to low to ensure correct roll
        if (throwVector.magnitude < 300f)
        {
            // reset die with too low velocity to board center
            StopAllMovement();
            transform.position = RollManager.Instance.DiceRestTransform.position;
        }
        else
        {
            // proceed with throwing the die
            _rigidbody.AddForce(throwVector);
            RollManager.Instance.ManualRoll();
        }
    }
    private void OnValidate()
    {
        if (Values.Length != Faces.Length)
            ResetDieValues();
        
        // if we change die faces' values in inspector, we need to adjust values and textures of linked faces
        for (int i = 0; i < Values.Length; i++)
        {
            Faces[i].ChangeFaceValue(Values[i]);
        }
    }

#endregion

#region Class methods

    public void RandomRoll()
    {
        float xForce = Random.Range(-100f, 100f) * _rigidbody.mass;
        float yForce = 300f;
        float zForce = Random.Range(-100f, 100f) * _rigidbody.mass;
        
        float xTorque = 500f * _rigidbody.mass;
        float yTorque = 300f;
        float zTorque = 500f * _rigidbody.mass;
        
        _rigidbody.AddForce(xForce,yForce,zForce);
        _rigidbody.AddTorque(xTorque,yTorque,zTorque);

        _currentDieState = DieState.Rolling;
    }

    public void StopAllMovement()
    {
        _rigidbody.velocity = Vector3.zero;
        _currentDieState = DieState.Idle;
    }

    private void EvaluateDieState()
    {
        switch (_currentDieState)
        {
            case DieState.Idle:
                break;
            case DieState.Rolling:
                // if after rolling die comes to a halt
                if (_rigidbody.IsSleeping())
                {
                    _lastRolledValue = GetRollResult();
                    // value -1 means that roll wasn't performed correctly, so we need to adjust die position
                    if (_lastRolledValue != -1)
                    {
                        _currentDieState = DieState.Idle;
                    }
                    else
                    {
                        UnstuckDie();
                    }
                }
                break;
            case DieState.InHand:
                break;
        }
    }

    /// <summary>
    /// Method to determine which face was rolled. Raycasts from every face try to hit collider hovering over the board.
    /// If only one of them hits this collider, method returns connected face's value.
    /// </summary>
    /// <returns></returns>
    private int GetRollResult()
    {
        List<DieFace> upFace = new List<DieFace>();
        foreach (var dieFace in Faces)
        {
            RaycastHit hit;
            if (Physics.Raycast(dieFace.transform.position, -dieFace.transform.forward, out hit,
                Mathf.Infinity, _rollResultCatcherMask))
            {
                upFace.Add(dieFace);
            }
        }

        if (upFace.Count == 0)
        {
            Debug.LogWarning("Something went wrong, no face in line with catcher.");
            return -1;
        }
        else if (upFace.Count > 1)
        {
            Debug.LogWarning("Too many faces caught by catcher");
            return -1;
        }
        else
        {
            return upFace[0].Value;
        }
    }

    private void UnstuckDie()
    {
        _rigidbody.AddForce(0f,100f,0f);
        _rigidbody.AddTorque(100f,100f,100f);
    }
    
    private void ResetDieValues()
    {
     Faces = new DieFace[FacesHolder.childCount];
        if(Faces.Length == 0)
            Debug.LogError("Die has no defined faces");
        
        for (int i = 0; i < Faces.Length; i++)
        {
            Faces[i] = FacesHolder.GetChild(i).GetComponent<DieFace>();
        }
        
        Values = new int[Faces.Length];
        for (int i = 0; i < Values.Length; i++)
        {
            Faces[i].ChangeFaceValue(i+1);
        }
    }
    private Vector3 GetMousePositionWorld()
    {
        var mousePosition = Input.mousePosition;
        mousePosition.z = _clickZPosition;
        return Camera.main.ScreenToWorldPoint(mousePosition);
    }

#endregion
}
