using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    private class LocalField
    {
        public CellState[] Cells;
        public int EmptyCount { get; private set; }

        public LocalField()
        {
            Cells = new CellState[9];
            for (int i = 0; i < 9; i++)
                Cells[i] = CellState.Empty;
            EmptyCount = 9;
        }

        public CellState this[int x, int y]
        {
            get
            {
                if (x < 0 || x > 2 || y < 0 || y > 2)
                    throw new IndexOutOfRangeException("Неверный индекс локального поля!");
                return Cells[y * 3 + x];
            }

            set
            {
                if (x < 0 || x > 2 || y < 0 || y > 2)
                    throw new IndexOutOfRangeException("Неверный индекс локального поля!");
                Debug.Assert(value != CellState.Empty);
                Cells[y * 3 + x] = value;
                EmptyCount--;
            }
        }
    }

    public static Manager Instance;
    public CellState CurrentPlayer;

    private LocalField mainField;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mainField = new LocalField();
        CurrentPlayer = CellState.PlayerCross;
    }

    private void SwapPlayer()
    {
        CurrentPlayer = CurrentPlayer == CellState.PlayerCross ? CellState.PlayerZero : CellState.PlayerCross;
    }

    public void MakeMove(int x, int y)
    {
        mainField[x, y] = CurrentPlayer;
        Debug.Log(GetWinner());
        SwapPlayer();
    }

    public GameState GetWinner()
    {
        //        +1     |  +2 |      +3     |  +4
        //   012 345 678 | 246 | 036 147 258 | 048
        int[][] indexes = { 
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, 
            new[] { 6, 7, 8 }, new[] { 2, 4, 6 }, 
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, 
            new[] { 2, 5, 8 }, new[] { 0, 4, 8 } 
        };

        foreach (var item in indexes)
        {
            if (mainField.Cells[item[0]] == CellState.Empty) continue;
            if (mainField.Cells[item[0]] == mainField.Cells[item[1]] && mainField.Cells[item[1]] == mainField.Cells[item[2]])
                return (GameState)mainField.Cells[item[0]];
        }
        if (mainField.EmptyCount == 0)
            return GameState.Draw;
        return GameState.Playing;
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