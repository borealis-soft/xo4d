using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cell : MonoBehaviour
{
	[HideInInspector] public int localX, localY;
	[HideInInspector] public int fieldX, fieldY;
	[HideInInspector] public bool hasMoved;
	private void OnMouseEnter()
	{
		Manager.Instance.OnCellHover(this);
	}
	private void OnMouseExit()
	{
		Manager.Instance.OnCellLeave(this);
	}
	private void OnMouseUp()
	{
		Manager.Instance.OnCellClick(this);
	}
}
