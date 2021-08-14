using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Manager : MonoBehaviour
{
	[SerializeField] private Cell cellPrefab;
	[SerializeField] private float localSpacing = 1;
	[SerializeField] private float fieldSpacing = 5;
	[SerializeField] private Transform currentMoveZone, nextMoveZone, highlight;
	[SerializeField] private GameObject crossPrefab, zeroPrefab;
	[SerializeField] private Text textInfo;
	[SerializeField] private GameObject winGame;
	[SerializeField] private Text winnerText;
	static readonly int[][] winningIndices = new[] {
		new[] { 0, 1, 2 }, new[] { 3, 4, 5 },
		new[] { 6, 7, 8 }, new[] { 2, 4, 6 },
		new[] { 0, 3, 6 }, new[] { 1, 4, 7 },
		new[] { 2, 5, 8 }, new[] { 0, 4, 8 }
	};
	private class LocalField
	{
		public CellState[] Cells;
		public int EmptyCount { get; private set; }
		public GameState GameState { get; private set; }

		public LocalField()
		{
			Cells = new CellState[9];
			for (int i = 0; i < 9; i++)
				Cells[i] = CellState.Empty;
			EmptyCount = 9;
			GameState = GameState.Playing;
		}
		public void MakeMove(int x, int y, CellState newState)
		{
			if (GameState != GameState.Playing)
			{
				return;
			}

			Debug.Assert(x >= 0 && x < 3 && y >= 0 && y < 3);
			Debug.Assert(newState != CellState.Empty);
			Cells[y * 3 + x] = newState;
			EmptyCount--;

			foreach (var line in winningIndices)
			{
				if (Cells[line[0]] == CellState.Empty) continue;
				if (Cells[line[0]] == Cells[line[1]] && Cells[line[1]] == Cells[line[2]])
				{
					GameState = (GameState)Cells[line[0]];
					return;
				}
			}
			if (EmptyCount == 0)
				GameState = GameState.Draw;
			else
				GameState = GameState.Playing;
		}
	}

	public static Manager Instance;
	[HideInInspector] public CellState CurrentPlayer;

	private LocalField[] fields = new LocalField[9];
	private LocalField currentField = null;
	private GameState GlobalGameState = GameState.Playing;

	private void Awake()
	{
		Instance = this;
		for (int localX = -1; localX <= 1; ++localX)
		for (int localY = -1; localY <= 1; ++localY)
		for (int fieldX = -1; fieldX <= 1; ++fieldX)
		for (int fieldY = -1; fieldY <= 1; ++fieldY)
		{
			Vector3 localPos = new Vector3(localX, localY, 0);
			Vector3 fieldPos = new Vector3(fieldX, fieldY, 0);
			Cell cell = Instantiate(cellPrefab, localPos * localSpacing + fieldPos * fieldSpacing, Quaternion.identity);
			cell.localX = 1 + localX;
			cell.localY = 1 - localY;
			cell.fieldX = 1 + fieldX;
			cell.fieldY = 1 - fieldY;
		}
	}

	private void Start()
	{
		for (int i = 0; i < fields.Length; ++i)
		{
			fields[i] = new LocalField();
		}
		CurrentPlayer = CellState.PlayerCross;

		GetNextField = GetNextField_Easy;
	}

	Func<LocalField, LocalField> GetNextField;

	private LocalField GetNextField_Easy(LocalField targetField)
	{
		if (targetField.GameState != GameState.Playing)
			return null;
		return targetField;
	}

	public void SwapPlayer()
	{
		CurrentPlayer = CurrentPlayer == CellState.PlayerCross ? CellState.PlayerZero : CellState.PlayerCross;
		textInfo.text = "Ходит: Игрок " + (CurrentPlayer == CellState.PlayerCross ? "X" : "O");
	}

	public void UpdateGameState()
	{
		foreach (var line in winningIndices)
		{
			if (fields[line[0]].GameState == GameState.Playing || fields[line[0]].GameState == GameState.Draw) continue;
			if (fields[line[0]].GameState == fields[line[1]].GameState && fields[line[1]].GameState == fields[line[2]].GameState)
			{
				GlobalGameState = fields[line[0]].GameState;
				return;
			}
		}
		if (fields.Count(field => field.EmptyCount == 0) == 9)
			GlobalGameState = GameState.Draw;
		else
			GlobalGameState = GameState.Playing;
	}
	public CellState MakeMove(Cell cell)
	{
		if (GlobalGameState != GameState.Playing)
		{
			return CellState.Empty;
		}

		if (currentField == null)
			currentField = fields[cell.fieldY * 3 + cell.fieldX];
		if (currentField != fields[cell.fieldY * 3 + cell.fieldX])
			return CellState.Empty;

		currentField.MakeMove(cell.localX, cell.localY, CurrentPlayer);


		currentField = GetNextField(fields[cell.localY * 3 + cell.localX]);
		if (currentField == null) 
			currentMoveZone.position = Vector3.up * 1000;
		else
			currentMoveZone.position = new Vector3(cell.localX - 1, 1 - cell.localY) * fieldSpacing;


		UpdateGameState();

		var movedPlayer = CurrentPlayer;
		SwapPlayer();

		Debug.Log($"total: {GlobalGameState}, 0:{fields[0].GameState}, 1:{fields[1].GameState}, 2:{fields[2].GameState}, 3:{fields[3].GameState}, 4:{fields[4].GameState}, 5:{fields[5].GameState}, 6:{fields[6].GameState}, 7:{fields[7].GameState}, 8:{fields[8].GameState}");
		return movedPlayer;
	}

	public bool IsHighlightable(Cell cell)
	{
		var targetField = fields[cell.fieldY * 3 + cell.fieldX];
		if (targetField.GameState != GameState.Playing)
			return false;
		if (currentField == null)
			return true;
		return currentField == targetField;

	}
	public void OnCellHover(Cell cell)
	{
		if (cell.hasMoved) return;
		if (IsHighlightable(cell))
		{
			nextMoveZone.position = new Vector3(cell.localX - 1, 1 - cell.localY) * fieldSpacing;
			highlight.position = cell.transform.position;
		}
	}
	public void OnCellLeave(Cell cell)
	{
		if (cell.hasMoved) return;
		nextMoveZone.position = Vector3.up * 1000;
		highlight.position = Vector3.up * 1000;
	}
	public void OnCellClick(Cell cell)
	{
		if (cell.hasMoved) return;


		if (IsHighlightable(cell))
		{
			switch (MakeMove(cell))
			{
				case CellState.PlayerCross: Instantiate(crossPrefab, cell.transform.position, Quaternion.identity); break;
				case CellState.PlayerZero: Instantiate(zeroPrefab, cell.transform.position, Quaternion.identity); break;
				default: return;
			}

			//Debug.Log($"Clicked fieldX:{fieldX}, fieldX:{fieldY}, localX:{localX}, localX:{localY}");
			cell.hasMoved = true;
			CheckWin();
		}
	}

	private void CheckWin()
	{
		if (GlobalGameState != GameState.Playing)
		{
			string winText = string.Empty;

			switch (GlobalGameState)
			{
				case GameState.PlayerCrossWin:
					winText = "Победил крестовой игрок";
					break;
				case GameState.PlayerZeroWin:
					winText = "Победил нулевой игрок";
					break;
				case GameState.Draw:
					winText = "Победила дружба!";
					break;
			}
			winnerText.text = winText;
			winGame.SetActive(true);
		}
	}
}

/// <summary>
/// PlayerCross и PlayerZero из CellState ДОЛЖНЫ БЫТЬ РАВНЫ PlayerCross и PlayerZero из GameState
/// </summary>
public enum CellState
{
	PlayerCross,
	PlayerZero,
	Empty
}

/// <summary>
/// PlayerCross и PlayerZero из CellState ДОЛЖНЫ БЫТЬ РАВНЫ PlayerCross и PlayerZero из GameState
/// </summary>
public enum GameState
{
	PlayerCrossWin = CellState.PlayerCross,
	PlayerZeroWin = CellState.PlayerZero,

	Draw,
	Playing,
}