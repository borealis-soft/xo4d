using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cell : MonoBehaviour
{
	[SerializeField] private GameObject crossPrefab, zeroPrefab;
	[SerializeField] private GameObject highlight;

	[HideInInspector] public int localX, localY;
	[HideInInspector] public int fieldX, fieldY;

	private bool hasMoved;
	private void Awake()
	{
		highlight.SetActive(false);
	}
	private void OnMouseEnter()
	{
		highlight.SetActive(true);
	}
	private void OnMouseExit()
	{
		highlight.SetActive(false);
	}
	private void OnMouseUp()
	{
		if (hasMoved)
			return;

		/*
		switch (Manager.CurrentPlayer)
		{
			case CellState.Cross: Instantiate(crossPrefab, transform.position, Quaternion.identity); break;
			case CellState.Zero:  Instantiate(zeroPrefab,  transform.position, Quaternion.identity); break;
			default: Debug.Assert(false); break;
		}
		Manager.Instance.MakeMove(x + 1, 1 - y));
		*/
		switch (Random.Range(0, 2))
		{
			case 0: Instantiate(crossPrefab, transform.position, Quaternion.identity); break;
			case 1: Instantiate(zeroPrefab, transform.position, Quaternion.identity); break;
			default: Debug.Assert(false); break;
		}
		Debug.Log($"Clicked fieldX:{fieldX}, fieldX:{fieldY}, localX:{localX}, localX:{localY}");

		hasMoved = true;
	}
}
