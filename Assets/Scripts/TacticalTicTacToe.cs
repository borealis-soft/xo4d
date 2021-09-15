using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using MLAPI.NetworkVariable.Collections;
using MLAPI.Connection;

public class TacticalTicTacToe : NetworkBehaviour
{
    public class LocalField : INetworkSerializable
    {
        public CellState[] Cells;
        public GameState GameState;
        public int positionX;
        public int positionY;
        private int emptyCount;

        public LocalField()
        {
            Cells = new CellState[9];
            for (int i = 0; i < 9; i++)
                Cells[i] = CellState.Empty;
            GameState = GameState.Playing;
            positionX = 0;
            positionY = 0;
            emptyCount = 9;
        }

        public LocalField(int positionX, int positionY)
        {
            Cells = new CellState[9];
            for (int i = 0; i < 9; i++)
                Cells[i] = CellState.Empty;
            emptyCount = 9;
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
            emptyCount--;

            float angle = -90;
            foreach (var indices in winningIndices)
            {
                if (indices[1] == 4) angle += 45;
                if (Cells[indices[0]] == CellState.Empty) continue;
                if (Cells[indices[0]] == Cells[indices[1]] && Cells[indices[1]] == Cells[indices[2]])
                {
                    GameState = (GameState)Cells[indices[0]];
                    TacticalTicTacToe inc = Instance;
                    Vector3 globalLinePos = GetLinePos(indices[1]) * inc.localSpacing + new Vector3(positionX, positionY, 0) * inc.fieldSpacing;
                    inc.InstantiateLineCLientRpc(globalLinePos, angle);
                    return;
                }
            }
            if (emptyCount == 0)
                GameState = GameState.Draw;
            else
                GameState = GameState.Playing;
        }

        public void NetworkSerialize(NetworkSerializer serializer)
        {
            int length = Cells.Length;
            serializer.Serialize(ref length);
            for (int i = 0; i < length; i++)
                serializer.Serialize(ref Cells[i]);
            serializer.Serialize(ref GameState);
            serializer.Serialize(ref positionX);
            serializer.Serialize(ref positionY);
            serializer.Serialize(ref emptyCount);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(LocalField val1, LocalField val2)
        {
            if (val1 is null && val2 is null) return true;
            if (val1 is null || val2 is null) return false;
            return val1.positionX == val2.positionX && val1.positionY == val2.positionY;
        }

        public static bool operator !=(LocalField val1, LocalField val2) => !(val1 == val2);
    }

    public static TacticalTicTacToe Instance;
    public CellState CurrentPlayer { get; private set; } = CellState.PlayerCross;
    private NetworkVariable<CellState> currentNetPlayer = new NetworkVariable<CellState>(new NetworkVariableSettings()
    {
        WritePermission = NetworkVariablePermission.ServerOnly,
        ReadPermission = NetworkVariablePermission.Everyone
    }, CellState.PlayerCross);


    [SerializeField] private Cell cellPrefab;
    [SerializeField] private float localSpacing = 1.5f;
    [SerializeField] private float fieldSpacing = 5;
    [SerializeField] private Transform CurrentMoveZone, nextMoveZone, highlight /*выделение*/;
    [SerializeField] private GameObject crossPrefab, zeroPrefab, linePrefab;
    [SerializeField] private Text textInfo;
    [SerializeField] private GameObject winGameMenu;
    [SerializeField] private Transform GameFieldParent;
    [SerializeField] private Text winningText;
    [SerializeField] private Text RoleText;

    public static readonly List<LocalCell> AllCells = new List<LocalCell>(81);
    public LocalField[] Fields { get; private set; } = new LocalField[9];
    private LocalField currentField = null;
    private NetworkVariable<LocalField> currentNetField = new NetworkVariable<LocalField>(new NetworkVariableSettings()
    {
        ReadPermission = NetworkVariablePermission.Everyone,
        WritePermission = NetworkVariablePermission.ServerOnly
    });
    private Cell currentCell = null;
    private GameState GlobalGameState = GameState.Playing;
    private NetworkClient localNetworkClient;

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
        currentNetField.OnValueChanged += (LocalField prev, LocalField @new) => currentField = @new;
        currentNetPlayer.OnValueChanged += (CellState prev, CellState @new) =>
        {
            CurrentPlayer = @new;
            textInfo.text = "Ходит: Игрок " + (CurrentPlayer == CellState.PlayerCross ? "X" : "O");
        };
        Instance = this;
        NetAuntif();
        for (int fieldX = 0, i = 0; fieldX <= 2; ++fieldX, i++)
            for (int fieldY = 0; fieldY <= 2; ++fieldY, i++)
                for (int localX = 0; localX <= 2; ++localX, i++)
                    for (int localY = 0; localY <= 2; ++localY, i++)
                    {
                        Vector3 localPos = new Vector3(localX, localY, 0) * localSpacing;
                        Vector3 fieldPos = new Vector3(fieldX, fieldY, 0) * fieldSpacing;
                        Cell cell = Instantiate(cellPrefab, localPos + fieldPos, Quaternion.identity, GameFieldParent);
                        cell.cell.localX = localX;
                        cell.cell.localY = localY;
                        cell.cell.fieldX = fieldX;
                        cell.cell.fieldY = fieldY;
                        cell.cell.pos = cell.transform.position;
                        AllCells.Add(cell.cell);
                    }
    }

    private void Start()
    {
        for (int i = 0; i < Fields.Length; ++i)
        {
            Fields[i] = new LocalField(i % 3, i / 3);
        }

        GetNextField = GetNextField_Easy;
    }

    public override void NetworkStart()
    {
        base.NetworkStart();
        NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out localNetworkClient);
    }

    [ClientRpc]
    public void GetRoleClientRpc(CellState newRole, ClientRpcParams clientRpcParams = default)
    {
        var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
        if (player)
        {
            player.MyRole = newRole;
            RoleText.text = "Текущая роль: " + (player.MyRole == CellState.PlayerZero ? "Нолик": "Зритель");
            Debug.Log($"Моя роль: {player.MyRole}");
        }
    }

    [ClientRpc]
    public void SyncFieldsClientRpc(LocalField[] localFields, bool[] localCellsHasMoved, ClientRpcParams clientRpcParams = default)
    {
        Debug.Assert(!IsServer);
        if (!IsServer)
        {
            for (int i = 0; i < localCellsHasMoved.Length; i++)
                if (AllCells[i].hasMoved != localCellsHasMoved[i])
                    AllCells[i].hasMoved = localCellsHasMoved[i];

            for (int i = 0; i < localFields.Length; i++)
            //(var field in localFields)
            {
                for (int cellId = 0; cellId < localFields[i].Cells.Length; cellId++)
                {
                    if (localFields[i].Cells[cellId] != CellState.Empty)
                    {
                        int localX = cellId % 3, localY = cellId / 3;
                        Vector3 localPos = new Vector3(localX, localY, 0) * localSpacing;
                        Vector3 fieldPos = new Vector3(localFields[i].positionX, localFields[i].positionY, 0) * fieldSpacing;
                        switch (localFields[i].Cells[cellId])
                        {
                            case CellState.PlayerCross:
                                Instantiate(crossPrefab, localPos + fieldPos, Quaternion.identity);
                                break;
                            case CellState.PlayerZero:
                                Instantiate(zeroPrefab, localPos + fieldPos, Quaternion.identity);
                                break;
                        }
                    }
                }
                if (localFields[i].GameState != Fields[i].GameState)
                    Fields[i].GameState = localFields[i].GameState;
                float angle = -90;
                foreach (var indices in winningIndices)
                {
                    if (indices[1] == 4) angle += 45;
                    if (localFields[i].Cells[indices[0]] == CellState.Empty) continue;
                    if (localFields[i].Cells[indices[0]] == localFields[i].Cells[indices[1]] && localFields[i].Cells[indices[1]] == localFields[i].Cells[indices[2]])
                    {
                        TacticalTicTacToe inc = Instance;
                        Vector3 globalLinePos = GetLinePos(indices[1]) * inc.localSpacing + new Vector3(localFields[i].positionX, localFields[i].positionY, 0) * inc.fieldSpacing;
                        Instantiate(linePrefab, globalLinePos, Quaternion.AngleAxis(angle, Vector3.forward));
                        return;
                    }
                }
            }
        }
    }

    [ClientRpc]
    private void InstantiateLineCLientRpc(Vector3 pos, float angle)
    {
        Instantiate(linePrefab, pos, Quaternion.AngleAxis(angle, Vector3.forward));
    }

    private void NetAuntif()
    {
        switch (MainMenu.GameMode)
        {
            case GameMode.SingleGame:
                break;
            case GameMode.HostGame:
                NetworkManager.Singleton.StartHost();
                Debug.Log("Server created!");
                break;
            case GameMode.ClientGame:
                if (MainMenu.AddrText != string.Empty)
                {
                    var transport = NetworkManager.Singleton.GetComponent<MLAPI.Transports.UNET.UNetTransport>();
                    string[] addr = MainMenu.AddrText.Split(':');
                    transport.ConnectAddress = addr[0];
                    transport.ConnectPort = int.Parse(addr[1]);
                }
                var res = NetworkManager.Singleton.StartClient();
                //if (!res.Success) // Не рабочее говно!
                //{
                //    Debug.Log("Error connection!");
                //    NetworkManager.Singleton.StopClient();
                //    SceneManager.LoadScene(0);
                //}
                Debug.Log("Sucesseful connect to server!");
                break;
        }
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
        currentNetPlayer.Value = currentNetPlayer.Value == CellState.PlayerCross ? CellState.PlayerZero : CellState.PlayerCross;
    }

    public void UpdateGameState()
    {
        foreach (var indices in winningIndices)
        {
            if (Fields[indices[0]].GameState == GameState.Playing || Fields[indices[0]].GameState == GameState.Draw) continue;
            if (Fields[indices[0]].GameState == Fields[indices[1]].GameState && Fields[indices[1]].GameState == Fields[indices[2]].GameState)
            {
                GlobalGameState = Fields[indices[0]].GameState;
                return;
            }
        }
        int playingCount = Fields.Count(field => field.GameState == GameState.Playing);
        if (playingCount == 0)
            GlobalGameState = GameState.Draw;
        else
            GlobalGameState = GameState.Playing;
        SwapPlayer();
    }

    [ClientRpc]
    public void SetMoveZoneClientRpc(Vector3 newPos)
    {
        if (currentField == null)
            CurrentMoveZone.position = Vector3.up * 1000;
        else
            CurrentMoveZone.position = newPos;
    }

    public CellState MakeMove(LocalCell cell)
    {

        if (currentField == null)
            currentNetField.Value = Fields[cell.fieldY * 3 + cell.fieldX];
        Debug.Assert(currentField == Fields[cell.fieldY * 3 + cell.fieldX]);
        //    return CellState.Empty;

        currentField.MakeMove(cell.localX, cell.localY, CurrentPlayer);
        currentNetField.Value = GetNextField(Fields[cell.localY * 3 + cell.localX]);

        var movedPlayer = CurrentPlayer;
        var vec3rightUp = Vector3.right + Vector3.up;
        SetMoveZoneClientRpc(new Vector3(cell.localX, cell.localY) * fieldSpacing + vec3rightUp * localSpacing);
        UpdateGameState();

        //Debug.Log($"total: {GlobalGameState}, 0:{fields[0].GameState}, 1:{fields[1].GameState}, 2:{fields[2].GameState}, 3:{fields[3].GameState}, " +
        //    $"4:{fields[4].GameState}, 5:{fields[5].GameState}, 6:{fields[6].GameState}, 7:{fields[7].GameState}, 8:{fields[8].GameState}");
        return movedPlayer;
    }

    public bool IsHighlightable() => IsHighlightable(currentCell.cell);

    public bool IsHighlightable(LocalCell cell)
    {
        var targetField = Fields[cell.fieldY * 3 + cell.fieldX];
        if (targetField.GameState != GameState.Playing)
            return false;
        if (currentField == null)
            return true;
        return currentField == targetField;
    }

    public void OnCellHover(Cell cell)
    {
        bool isViewer = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>().MyRole == CellState.Empty;
        if (!isViewer && cell.cell.hasMoved) return;
        if (isViewer || IsHighlightable(cell.cell))
        {
            var vec3rightUp = Vector3.right + Vector3.up;
            nextMoveZone.position = new Vector3(cell.cell.localX, cell.cell.localY) * fieldSpacing + localSpacing * vec3rightUp;
            highlight.position = cell.cell.pos;
            currentCell = cell;
        }
    }
    public void OnCellLeave()
    {
        if (currentCell == null) return;
        nextMoveZone.position = Vector3.up * 1000;
        highlight.position = Vector3.up * 1000;
        currentCell = null;
    }

    public void MakeMoveNet(LocalCell cell)
    {
        //Debug.Log($"Clicked fieldX:{fieldX}, fieldX:{fieldY}, localX:{localX}, localX:{localY}");
        CellState tempState = MakeMove(cell);
        Debug.Assert(tempState != CellState.Empty);

        InstantiateCLientRpc(tempState, cell);
        CheckWin();
    }

    [ClientRpc]
    public void InstantiateCLientRpc(CellState cellState, LocalCell cell)
    {
        if (cellState == CellState.PlayerCross)
            Instantiate(crossPrefab, cell.pos, Quaternion.identity);
        else
            Instantiate(zeroPrefab, cell.pos, Quaternion.identity);

        var temp = AllCells.Find(val => val == cell);
        temp.hasMoved = true;
    }

    public void OnCellClick()
    {
        if (GlobalGameState == GameState.Playing && currentCell != null && IsHighlightable())
        {
            var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
            if (player)
            {
                player.MakeMove(currentCell.cell);
            }
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
            ActivatedWinMenuClientRpc(winText);
        }
    }

    [ClientRpc]
    private void ActivatedWinMenuClientRpc(string winText)
    {
        winningText.text = winText;
        winGameMenu.SetActive(true);
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