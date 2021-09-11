using MLAPI;
using MLAPI.NetworkVariable;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Cell : MonoBehaviour
{
    [HideInInspector] public int localX, localY;
    [HideInInspector] public int fieldX, fieldY;
    [HideInInspector] public bool hasMoved;
    //[HideInInspector] public LocalCell cell;
    //[HideInInspector] public NetworkVariable<LocalCell> cell;
    //[HideInInspector] public LocalCell Cell_ { get { return cell.Value; } set { cell.Value = value; } }

    private void OnMouseEnter()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            TacticalTicTacToe.Instance.OnCellHover(this);
    }
    private void OnMouseExit()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            TacticalTicTacToe.Instance.OnCellLeave(this);
    }
    private void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            TacticalTicTacToe.Instance.OnCellClickServerRpc(localX, localY, fieldX, fieldY, hasMoved, transform.position);
    }
}

public struct LocalCell
{
    public int localX, localY;
    public int fieldX, fieldY;
    public bool hasMoved;
}
