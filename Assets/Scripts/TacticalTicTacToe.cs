﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class TacticalTicTacToe : MonoBehaviour
{
    private class LocalField // сделать через шаблон
    {
        public CellState[] Cells;
        public GameState GameState { get; private set; }
        public int positionX;
        public int positionY;

        private int EmptyCount;
        public LocalField(int positionX, int positionY)
        {
            Cells = new CellState[9];
            for (int i = 0; i < 9; i++)
                Cells[i] = CellState.Empty;
            EmptyCount = 9;
            GameState = GameState.Playing;
            this.positionX = positionX;
            this.positionY = positionY;
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

            float angle = -90;
            foreach (var indices in winningIndices)
            {
                if (indices[1] == 4) angle += 45;
                if (Cells[indices[0]] == CellState.Empty) continue;
                if (Cells[indices[0]] == Cells[indices[1]] && Cells[indices[1]] == Cells[indices[2]])
                {
                    GameState = (GameState)Cells[indices[0]];
                    TacticalTicTacToe inc = TacticalTicTacToe.Instance;
                    Vector3 globalLinePos = GetLinePos(indices[1]) * inc.localSpacing + new Vector3(positionX, positionY, 0) * inc.fieldSpacing;
                    Instantiate(inc.linePrefab, globalLinePos, Quaternion.AngleAxis(angle, Vector3.forward));
                    return;
                }
            }
            if (EmptyCount == 0)
                GameState = GameState.Draw;
            else
                GameState = GameState.Playing;
        }
    }

    public static TacticalTicTacToe Instance;
    [HideInInspector] public CellState CurrentPlayer;

    [SerializeField] private Cell cellPrefab;
    [SerializeField] private float localSpacing = 1.5f;
    [SerializeField] private float fieldSpacing = 5;
    [SerializeField] private Transform CurrentMoveZone, nextMoveZone, highlight /*выделение*/;
    [SerializeField] private GameObject crossPrefab, zeroPrefab, linePrefab;
    [SerializeField] private Text textInfo;
    [SerializeField] private GameObject winGameMenu;
    [SerializeField] private Text winningText;

    private LocalField[] fields = new LocalField[9];
    private LocalField currentField = null;
    private GameState GlobalGameState = GameState.Playing;

    private static readonly int[][] winningIndices = new int[][] {
        new int[] { 0, 4, 8 },// angle -45  
        new int[] { 3, 4, 5 },// angle 0       //    1            up
        new int[] { 0, 1, 2 },// angle 0       //  3 4 5    left zero right
        new int[] { 6, 7, 8 },// angle 0       //    7           down
        new int[] { 2, 4, 6 },// angle 45  
        new int[] { 1, 4, 7 },// angle 90  
        new int[] { 0, 3, 6 },// angle 90  
        new int[] { 2, 5, 8 } // angle 90  
    };

    private void Awake()
    {
        Instance = this;
        for (int fieldX = -1; fieldX <= 1; ++fieldX)
            for (int fieldY = -1; fieldY <= 1; ++fieldY)
                for (int localX = -1; localX <= 1; ++localX)
                    for (int localY = -1; localY <= 1; ++localY)
                    {
                        Vector3 localPos = new Vector3(localX, localY, 0) * localSpacing;
                        Vector3 fieldPos = new Vector3(fieldX, fieldY, 0) * fieldSpacing;
                        Cell cell = Instantiate(cellPrefab, localPos + fieldPos, Quaternion.identity);
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
            fields[i] = new LocalField((i % 3) - 1, 1 - (i / 3));
        }

        CurrentPlayer = CellState.PlayerCross;
        GetNextField = GetNextField_Easy;
    }

    private static Vector3 GetLinePos(int id) => id switch
    {
        1 => Vector3.up,
        3 => Vector3.left,
        4 => Vector3.zero,
        5 => Vector3.right,
        7 => Vector3.down,
        _ => default,
    };

    Func<LocalField, LocalField> GetNextField;

    private LocalField GetNextField_Easy(LocalField targetField)
    {
        return targetField.GameState != GameState.Playing ? null : targetField;
    }

    public void SwapPlayer()
    {
        CurrentPlayer = CurrentPlayer == CellState.PlayerCross ? CellState.PlayerZero : CellState.PlayerCross;
        textInfo.text = "Ходит: Игрок " + (CurrentPlayer == CellState.PlayerCross ? "X" : "O");
    }

    public void UpdateGameState()
    {
        foreach (var indices in winningIndices)
        {
            if (fields[indices[0]].GameState == GameState.Playing || fields[indices[0]].GameState == GameState.Draw) continue;
            if (fields[indices[0]].GameState == fields[indices[1]].GameState && fields[indices[1]].GameState == fields[indices[2]].GameState)
            {
                GlobalGameState = fields[indices[0]].GameState;
                return;
            }
        }
        int playingCount = fields.Count(field => field.GameState == GameState.Playing);
        if (playingCount == 0)
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
            CurrentMoveZone.position = Vector3.up * 1000;
        else
            CurrentMoveZone.position = new Vector3(cell.localX - 1, 1 - cell.localY) * fieldSpacing;


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
            winningText.text = winText;
            winGameMenu.SetActive(true);
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