using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class CellButton : MonoBehaviour
{
	public int index;
	private void OnMouseEnter()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellHover(index);
	}
	private void OnMouseExit()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellLeave(index);
	}
	private void OnMouseUp()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellClick(index);
	}
}
