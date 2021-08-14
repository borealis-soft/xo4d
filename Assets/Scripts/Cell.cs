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
            TacticalTicTacToe.Instance.OnCellClick(this);
    }
}
