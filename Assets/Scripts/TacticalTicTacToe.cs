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
using MLAPI.Transports.UNET;
using System.Net;

public class TacticalTicTacToe : NetworkBehaviour
{
	public static TacticalTicTacToe Instance;

	public CellState[] AllCells = new CellState[81];

	private FieldState[] fieldStates = new FieldState[9];
	private FieldState globalFieldState = FieldState.InProgress;
	private int moveFieldIndex = -1;

	public CellState CurrentPlayer { get; private set; } = CellState.Cross;
	public Text winningText, RoleText;
	public Text MessagePrefab;
	public Transform MessagesParent;
	public int MessagesMaxCount = 30;
	public AudioClip NewMessageSound;
	public AudioClip CrossDrawingSound;
	public AudioClip ZeroDrawingSound;

	[SerializeField] private CellButton cellPrefab;
	[SerializeField] private float localSpacing = 1.5f, fieldSpacing = 5f;
	[SerializeField] private Transform CurrentMoveZone, nextMoveZone, highlight /*выделение*/;
	[SerializeField] private GameObject crossPrefab, zeroPrefab, linePrefab;
	[SerializeField] private Text textInfo;
	[SerializeField] private GameObject winGameMenu;
	[SerializeField] private Transform GameFieldParent;

	class WinningCondition
	{
		public int[] Cells;
		public float Angle;
		public Vector3 Offset;
	}

	private static readonly WinningCondition[] winningConditions = new WinningCondition[] {
		new WinningCondition() { Cells = new int[] { 0, 1, 2 }, Angle = 0, Offset = new Vector3(1, 0), },// angle 0       //  3 4 5    left zero right
		new WinningCondition() { Cells = new int[] { 3, 4, 5 }, Angle = 0, Offset = new Vector3(1, 1), },// angle 0       //    7            up
		new WinningCondition() { Cells = new int[] { 6, 7, 8 }, Angle = 0, Offset = new Vector3(1, 2), },// angle 0       //    1           down

		new WinningCondition() { Cells = new int[] { 0, 3, 6 }, Angle = 90, Offset = new Vector3(0, 1), },// angle 90
		new WinningCondition() { Cells = new int[] { 1, 4, 7 }, Angle = 90, Offset = new Vector3(1, 1), },// angle 90
		new WinningCondition() { Cells = new int[] { 2, 5, 8 }, Angle = 90, Offset = new Vector3(2, 1), },// angle 90

		new WinningCondition() { Cells = new int[] { 2, 4, 6 }, Angle = -45, Offset = new Vector3(1, 1), },// angle -45
		new WinningCondition() { Cells = new int[] { 0, 4, 8 }, Angle = +45, Offset = new Vector3(1, 1), },// angle +45
	};

	private NetworkVariable<CellState> currentNetPlayer = new NetworkVariable<CellState>(new NetworkVariableSettings()
	{
		WritePermission = NetworkVariablePermission.ServerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	}, CellState.PlayerCross);

	private NetworkClient localNetworkClient;
	private Func<LocalField, LocalField> GetNextField;
	private Queue<Text> messageTexts = new Queue<Text>(20);
	private int serverListenPort;
	private Vector3 GetFieldPosition(int fieldX, int fieldY) => new Vector3(fieldX, fieldY, 0) * fieldSpacing;
	private Vector3 GetFieldPosition(int cell) => GetFieldPosition(cell / 9 % 3, cell / 9 / 3);

	private Vector3 GetCellPosition(int fieldX, int fieldY, int localX, int localY) =>
		new Vector3(localX, localY, 0) * localSpacing + GetFieldPosition(fieldX, fieldY);
	private Vector3 GetCellPosition(int cell)
	{
		int localIndex = cell % 9;
		int fieldIndex = cell % 9;

		int localY = localIndex / 3;
		int localX = localIndex % 3;

		int fieldY = fieldIndex / 3;
		int fieldX = fieldIndex % 3;

		return GetCellPosition(fieldX, fieldY, localX, localY);
	}
	public void Awake()
	{
		currentNetField.OnValueChanged += (LocalField prev, LocalField @new) => currentField = @new;
		currentNetPlayer.OnValueChanged += (CellState prev, CellState @new) =>
		{
			CurrentPlayer = @new;
			textInfo.text = "Ходит " + CurrentPlayer.AsString();
		};
		Instance = this;
		NetAuntif();


		for (int fieldY = 0, i = 0; fieldY <= 2; ++fieldY, i++)
			for (int fieldX = 0; fieldX <= 2; ++fieldX, i++)
				for (int localY = 0; localY <= 2; ++localY, i++)
					for (int localX = 0; localX <= 2; ++localX, i++)
					{
						CellButton cell = Instantiate(cellPrefab, GetCellPosition(fieldX, fieldY, localX, localY), Quaternion.identity, GameFieldParent);
						cell.index = i;
					}

#if DEBUG
		//if (!NetworkManager.IsServer && !NetworkManager.IsClient)
		//{
		//	NetworkManager.Singleton.StartHost();
		//	Debug.Log("Дебаг сервер запущен!");
		//}
#endif
	}
	private void UpdateCell(int cell, CellState state)
	{
		AllCells[cell] = state;

		Instantiate(state == CellState.Cross ? crossPrefab : zeroPrefab, GetCellPosition(cell), Quaternion.identity);

		int fieldIndex = cell / 9;
		var localCells = AllCells.AsSpan(fieldIndex * 9, 9);
		if (localCells.Count(c => c == CellState.Empty) == 0)
		{
			fieldStates[fieldIndex] = FieldState.Draw;
			return;
		}
		foreach (var condition in winningConditions)
		{
			if (localCells[condition.Cells[0]] == CellState.Empty)
				continue;

			if (localCells[condition.Cells[0]] == localCells[condition.Cells[1]] &&
				localCells[condition.Cells[1]] == localCells[condition.Cells[2]])
			{
				var vec3rightUp = (Vector3.right + Vector3.up) * localSpacing;

				int fieldY = cell / 27;
				int fieldX = cell / 9 % 3;

				Instantiate(linePrefab, condition.Offset * localSpacing + new Vector3(fieldX, fieldY) * fieldSpacing, Quaternion.AngleAxis(condition.Angle, Vector3.forward));

				fieldStates[fieldIndex] = localCells[condition.Cells[0]] switch
				{
					CellState.Cross => FieldState.CrossWin,
					/* CellState.Zero */
					_ => FieldState.ZeroWin
				};
				break;
			}
		}
	}

	public void Start()
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
		StartCoroutine(ConnectedSayCorutine());
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
		if (currentField == null)
			CurrentMoveZone.position = Vector3.up * 1000;
		else
			CurrentMoveZone.position = newPos;
	}

	[ClientRpc]
	public void InstantiateCrossOrZeroCLientRpc(CellState cellState, LocalCell cell)
	{
		if (cellState == CellState.PlayerCross)
		{
			Instantiate(crossPrefab, cell.pos, Quaternion.identity);
			SoundController.Instance.PlaySound(CrossDrawingSound);
		}
		else
		{
			Instantiate(zeroPrefab, cell.pos, Quaternion.identity);
			SoundController.Instance.PlaySound(ZeroDrawingSound);
		}

		var temp = AllCells.Find(val => val == cell);
		temp.hasMoved = true;
	}

	[ClientRpc]
	private void InstantiateLineCLientRpc(Vector3 pos, float angle)
	{
		Instantiate(linePrefab, pos, Quaternion.AngleAxis(angle, Vector3.forward));
	}

	[ClientRpc]
	private void ActivatedWinMenuClientRpc(string winText)
	{
		winningText.text = winText;
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

	public void SendMessages(string message)
	{
		var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
		string editedMessage = player.MyRole.AsString() + ": " + message;

		if (IsServer) CreateMessageObjClientRpc(editedMessage);
		else player.SendMessageServerRpc(editedMessage);
	}

	public void SwapPlayer()
	{
		currentNetPlayer.Value = currentNetPlayer.Value == CellState.PlayerCross ? CellState.PlayerZero : CellState.PlayerCross;
	}

	public void UpdateGameState()
	{
		foreach (var indices in winningConditions)
		{
			if (Fields[indices[0]].FieldState == GameState.Playing || Fields[indices[0]].FieldState == GameState.Draw) continue;
			if (Fields[indices[0]].FieldState == Fields[indices[1]].FieldState && Fields[indices[1]].FieldState == Fields[indices[2]].FieldState)
			{
				GlobalGameState = Fields[indices[0]].FieldState;
				return;
			}
		}
		int playingCount = Fields.Count(field => field.FieldState == GameState.Playing);
		if (playingCount == 0)
			GlobalGameState = GameState.Draw;
		else
			GlobalGameState = GameState.Playing;
		SwapPlayer();
	}

	public CellState MakeMove(LocalCell cell)
	{
		if (currentField == null)
			currentNetField.Value = Fields[cell.fieldY * 3 + cell.fieldX];

		Debug.Assert(currentField == Fields[cell.fieldY * 3 + cell.fieldX]);

		currentField.MakeMove(cell.localX, cell.localY, CurrentPlayer);
		currentNetField.Value = GetNextField(Fields[cell.localY * 3 + cell.localX]);

		var movedPlayer = CurrentPlayer;
		var vec3rightUp = Vector3.right + Vector3.up;
		SetMoveZoneClientRpc(new Vector3(cell.localX, cell.localY) * fieldSpacing + vec3rightUp * localSpacing);
		UpdateGameState();

		return movedPlayer;
	}

	public void OnCellHover(int cellIndex)
	{
		highlight.position = GetCellPosition(cellIndex);
		nextMoveZone.position = GetFieldPosition(cellIndex) + new Vector3(1, 1, 0) * localSpacing;
	}

	public void OnCellLeave(int index)
	{
		nextMoveZone.position = Vector3.up * 1000;
		highlight.position = Vector3.up * 1000;
	}

	public void MakeMoveNet(int cell, CellState role)
	{
		UpdateCell(cell, role);


		CellState tempState = MakeMove(cell);
		Debug.Assert(tempState != CellState.Empty);

		InstantiateCrossOrZeroCLientRpc(tempState, cell);
		CheckWin();
	}

	private bool CanMoveInCell(int cell)
	{
		int field = cell / 9;

		if (moveFieldIndex != -1 && field != moveFieldIndex)
			return false;

		if (fieldStates[field] != FieldState.InProgress)
			return false;

		return AllCells[cell] == CellState.Empty;
	}

	[ClientRpc]
	public void UpdateCellClientRpc(int cell, CellState state)
	{
		UpdateCell(cell, state);
	}

	public void OnCellClick(int cell)
	{
		if (globalFieldState == FieldState.InProgress && CanMoveInCell(cell))
		{
			var player = localNetworkClient.PlayerObject.GetComponent<GamePlayerManager>();
			if (CurrentPlayer == player?.MyRole)
			{
				if (IsServer)
					UpdateCellClientRpc(cell, player.MyRole);
				else
					player.RequestCellUpdate(cell);
			}
		}
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

	private static Vector3 GetLinePos(int id) => id switch
	{
		1 => Vector3.down,
		3 => Vector3.left,
		4 => Vector3.zero,
		5 => Vector3.right,
		7 => Vector3.up,
		_ => default,
	};

	private LocalField GetNextField_Easy(LocalField targetField)
	{
		return targetField.FieldState != GameState.Playing ? null : targetField;
	}

	private void CheckWin()
	{
		if (GlobalGameState != GameState.Playing)
		{
			string winText = string.Empty;

			switch (GlobalGameState)
			{
				case GameState.CrossWin:
					winText = "Победил крестик!";
					break;
				case GameState.ZeroWin:
					winText = "Победил нулик!";
					break;
				case GameState.Draw:
					winText = "Победила дружба!";
					break;
			}
			ActivatedWinMenuClientRpc(winText);
		}
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
