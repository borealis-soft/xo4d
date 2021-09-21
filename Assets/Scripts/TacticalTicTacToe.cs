using MLAPI;
using MLAPI.Connection;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using MLAPI.Transports.UNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TacticalTicTacToe : NetworkBehaviour
{
	class WinningCondition
	{
		public int[] Cells;
		public float Angle;
		public Vector3 Offset;
	}

	public static TacticalTicTacToe Instance;
	public List<CellState> AllCells { get; private set; } = new List<CellState>(new CellState[81]);
	public AudioClip NewMessageSound, CrossDrawingSound, ZeroDrawingSound;
	public Text WinningText, RoleText, CurrentPlayerText, MessagePrefab;
	public Transform CurrentMoveZone, nextMoveZone, highlight, CellsParent, MessagesParent;
	public GameObject crossPrefab, zeroPrefab, linePrefab, winGameMenu;
	public CellButton CellPrefab;
	public int MessagesMaxCount = 30;
	public float LocalSpacing = 1.5f, FieldSpacing = 5f;

	private static readonly WinningCondition[] winningConditions = new WinningCondition[]
	{
		new WinningCondition() { Cells = new int[] { 0, 1, 2 }, Angle = 0, Offset = new Vector3(1, 0), },
		new WinningCondition() { Cells = new int[] { 3, 4, 5 }, Angle = 0, Offset = new Vector3(1, 1), },
		new WinningCondition() { Cells = new int[] { 6, 7, 8 }, Angle = 0, Offset = new Vector3(1, 2), },
		new WinningCondition() { Cells = new int[] { 0, 3, 6 }, Angle = 90, Offset = new Vector3(0, 1), },
		new WinningCondition() { Cells = new int[] { 1, 4, 7 }, Angle = 90, Offset = new Vector3(1, 1), },
		new WinningCondition() { Cells = new int[] { 2, 5, 8 }, Angle = 90, Offset = new Vector3(2, 1), },
		new WinningCondition() { Cells = new int[] { 2, 4, 6 }, Angle = -45, Offset = new Vector3(1, 1), },
		new WinningCondition() { Cells = new int[] { 0, 4, 8 }, Angle = +45, Offset = new Vector3(1, 1), },
	};
	private Vector3 Offset { get { return new Vector3(1, 1, 0) * LocalSpacing; } }
	private List<FieldState> FieldStates = new List<FieldState>(new FieldState[9]);
	private Queue<Text> messageTexts = new Queue<Text>(20);
	private NetworkVariable<CellState> currentPlayerNet = new NetworkVariable<CellState>(new NetworkVariableSettings()
	{
		WritePermission = NetworkVariablePermission.ServerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	}, CellState.Cross);
	private NetworkVariable<int> moveFieldIndexNet = new NetworkVariable<int>(new NetworkVariableSettings()
	{
		WritePermission = NetworkVariablePermission.ServerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	}, -1);
	private NetworkClient localNetworkClient;
	private int serverListenPort;
	private Func<int, int> GetNextField;

	public void Awake()
	{
		Instance = this;
		currentPlayerNet.OnValueChanged += (CellState prev, CellState @new) =>
		{
			CurrentPlayerText.text = "Ходит " + @new.AsString();
		};
		NetAuntif();

		for (int fieldY = 0, i = 0; fieldY <= 2; ++fieldY, i++)
			for (int fieldX = 0; fieldX <= 2; ++fieldX, i++)
				for (int localY = 0; localY <= 2; ++localY, i++)
					for (int localX = 0; localX <= 2; ++localX, i++)
					{
						CellButton cell = Instantiate(CellPrefab, GetCellPosition(fieldX, fieldY, localX, localY), Quaternion.identity, CellsParent);
						cell.index = i;
					}

#if DEBUG
		if (!NetworkManager.IsServer && !NetworkManager.IsClient)
		{
			NetworkManager.Singleton.StartHost();
			Debug.Log("Дебаг сервер запущен!");
		}
#endif
	}

	public void Start()
	{
		GetNextField = GetIdNextField_Easy;
	}

	public override void NetworkStart()
	{
		base.NetworkStart();
		NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out localNetworkClient);
		StartCoroutine(ConnectedSayCorutine());
	}

	private Vector3 GetFieldPosition(int cell) => GetFieldPosition(cell / 9 % 3, cell / 27);

	private Vector3 GetFieldPosition(int fieldX, int fieldY) => new Vector3(fieldX, fieldY, 0) * FieldSpacing;

	private Vector3 GetCellPosition(int cell)
	{
		int localY = cell % 9 / 3;
		int localX = cell % 3;

		int fieldY = cell / 27;
		int fieldX = cell / 9 % 3;

		return GetCellPosition(fieldX, fieldY, localX, localY);
	}

	private Vector3 GetCellPosition(int fieldX, int fieldY, int localX, int localY) =>
		new Vector3(localX, localY, 0) * LocalSpacing + GetFieldPosition(fieldX, fieldY);

	private void UpdateCell(int cellId, CellState state)
	{
		AllCells[cellId] = state;
		Instantiate(state == CellState.Cross ? crossPrefab : zeroPrefab, GetCellPosition(cellId), Quaternion.identity);
		SoundController.Instance.PlaySound(state == CellState.Cross ? CrossDrawingSound : ZeroDrawingSound);

		UpdateField(cellId);
	}

	private void UpdateField(int cellId)
	{
		int fieldIndex = cellId / 9;
		var mineCells = AllCells.GetRange(fieldIndex * 9, 9);
		if (mineCells.Count(c => c == CellState.Empty) == 0)
		{
			FieldStates[fieldIndex] = FieldState.Draw;
			return;
		}
		foreach (var condition in winningConditions)
		{
			if (mineCells[condition.Cells[0]] == CellState.Empty)
				continue;

			if (mineCells[condition.Cells[0]] == mineCells[condition.Cells[1]] &&
				mineCells[condition.Cells[1]] == mineCells[condition.Cells[2]])
			{
				var vec3rightUp = (Vector3.right + Vector3.up) * LocalSpacing;

				int fieldY = cellId / 27;
				int fieldX = cellId / 9 % 3;

				Instantiate(linePrefab, condition.Offset * LocalSpacing + new Vector3(fieldX, fieldY) * FieldSpacing, Quaternion.AngleAxis(condition.Angle, Vector3.forward));
				SoundController.Instance.PlaySound(ZeroDrawingSound);// Найти звук для черты

				FieldStates[fieldIndex] = mineCells[condition.Cells[0]] switch
				{
					CellState.Cross => FieldState.CrossWin,
					_ => FieldState.ZeroWin
				};
				break;
			}
		}
	}

	[ClientRpc]
	public void GetRoleClientRpc(CellState newRole, ClientRpcParams clientRpcParams = default)
	{
		var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
		if (player)
		{
			player.MyRole = newRole;
			RoleText.text = "Текущая роль: " + player.MyRole.AsString();
			Debug.Log($"Моя роль: {player.MyRole}");
		}
	}

	[ClientRpc]
	public void SyncFieldsClientRpc(CellState[] cells, ClientRpcParams clientRpcParams = default)
	{
		for (int i = 0; i < cells.Length; i++)
			if (cells[i] != CellState.Empty)
				UpdateCell(i, cells[i]);
	}

	[ClientRpc]
	public void SetMoveZoneClientRpc(Vector3 newPos)
	{
		if (moveFieldIndexNet.Value == -1)
			CurrentMoveZone.position = Vector3.up * 1000;
		else
			CurrentMoveZone.position = newPos;
	}

	[ClientRpc]
	private void ActivatedWinMenuClientRpc(string winText)
	{
		WinningText.text = winText;
		winGameMenu.SetActive(true);
	}

	[ClientRpc]
	public void CreateMessageObjClientRpc(string message)
	{
		Text newMessage = Instantiate(MessagePrefab, MessagesParent);
		newMessage.text = message;
		messageTexts.Enqueue(newMessage);
		if (messageTexts.Count > MessagesMaxCount)
			Destroy(messageTexts.Dequeue().gameObject);
		SoundController.Instance.PlaySound(NewMessageSound);
	}

	[ClientRpc]
	public void UpdateCellClientRpc(int cell, CellState state)
	{
		UpdateCell(cell, state);
	}

	public void SendMessages(string message)
	{
		var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
		string editedMessage = player.MyRole.AsString() + ": " + message;

		if (IsServer) CreateMessageObjClientRpc(editedMessage);
		else player.SendMessageServerRpc(editedMessage);
	}

	public void SwapPlayer()
	{
		currentPlayerNet.Value = currentPlayerNet.Value == CellState.Cross ? CellState.Zero : CellState.Cross;
	}

	public void CheckWin()
	{
		if (FieldStates.Count(field => field == FieldState.InProgress) == 0)
			ActivatedWinMenuClientRpc(FieldState.Draw.AsString());
		else
			foreach (var indices in winningConditions)
			{
				int id0 = indices.Cells[0], id1 = indices.Cells[1], id2 = indices.Cells[2];

				if (FieldStates[id0] == FieldState.InProgress || FieldStates[id0] == FieldState.Draw) continue;
				if (FieldStates[id0] == FieldStates[id1] && FieldStates[id1] == FieldStates[id2])
				{
					ActivatedWinMenuClientRpc(FieldStates[id0].AsString());
					break;
				}
			}
	}

	public void MakeMove(int cell, CellState Role)
	{
		UpdateCellClientRpc(cell, Role);
		moveFieldIndexNet.Value = GetNextField(cell);
		SetMoveZoneClientRpc(GetFieldPosition(cell % 3, cell % 9 / 3) + Offset);

		SwapPlayer();
		CheckWin();
	}

	public void OnCellHover(int cellIndex)
	{
		highlight.position = GetCellPosition(cellIndex);
		nextMoveZone.position = GetFieldPosition(cellIndex) + Offset;
	}

	public void OnCellLeave()
	{
		nextMoveZone.position = Vector3.up * 1000;
		highlight.position = Vector3.up * 1000;
	}

	public void OnCellClick(int cell)
	{
		if (CanMoveInCell(cell))
		{
			var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
			if (player && player.MyRole == currentPlayerNet.Value)
			{
				if (IsServer)
					MakeMove(cell, player.MyRole);
				else
					player.RequestMove(cell);
			}
		}
	}

	private bool CanMoveInCell(int cell)
	{
		int field = cell / 9;

		if (moveFieldIndexNet.Value != -1 && field != moveFieldIndexNet.Value)
			return false;

		if (FieldStates[field] != FieldState.InProgress)
			return false;

		return AllCells[cell] == CellState.Empty;
	}

	private IEnumerator ConnectedSayCorutine()
	{
		yield return new WaitForSeconds(0.5f);
		SendMessages("Я подключился!");
		if (IsServer) SendMessages($"Порт игры: {serverListenPort}");
	}

	private void NetAuntif()
	{
		if (MainMenu.GameMode == GameMode.SingleGame) return;
		var transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
		switch (MainMenu.GameMode)
		{
			case GameMode.HostGame:
				serverListenPort = new System.Random().Next(1000, 20000);
				transport.ServerListenPort = serverListenPort;

				NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
				NetworkManager.Singleton.StartHost();
				GUIUtility.systemCopyBuffer = serverListenPort.ToString();
				Debug.Log($"Server created! Port {transport.ServerListenPort}");
				break;
			case GameMode.ClientGame:
				if (MainMenu.AddrText != string.Empty)
				{
					string[] addr = MainMenu.AddrText.Split(':');
					transport.ConnectAddress = addr[0];
					transport.ConnectPort = int.Parse(addr[1]);
				}

				var res = NetworkManager.Singleton.StartClient();
				if (!res.IsDone)
				{
					Debug.Log("Error connection!");
					NetworkManager.Singleton.StopClient();
					SceneManager.LoadScene(0);
				}
				break;
		}
	}

	private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
	{
		bool approve = true;
		bool createPlayerObj = true;
		ulong? prefHash = null;
		callback(createPlayerObj, prefHash, approve, Vector3.zero, Quaternion.identity);
	}

	private int GetIdNextField_Easy(int cell)
	{
		int FieldId = cell % 9;
		return FieldStates[FieldId] != FieldState.InProgress ? -1 : FieldId;
	}
}

public static class CellStateExtensions
{
	public static string AsString(this CellState s) => s switch
	{
		CellState.Cross => "Крестик",
		CellState.Zero => "Нолик",
		_ => "Зритель"
	};

	public static string AsString(this FieldState s) => s switch
	{
		FieldState.CrossWin => "Победил крестик!",
		FieldState.ZeroWin => "Победил нолик!",
		FieldState.Draw => "Победила дружба!",
		_ => "Игра продолжается!"
	};
}

public enum CellState : byte
{
	Cross,
	Zero,
	Empty,
}

public enum FieldState : byte
{
	InProgress,
	Draw,
	CrossWin,
	ZeroWin,
}
