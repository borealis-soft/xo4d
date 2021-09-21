using UnityEngine;
using UnityEngine.EventSystems;

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
			TacticalTicTacToe.Instance.OnCellLeave();
	}
	private void OnMouseUp()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellClick(index);
	}
}
