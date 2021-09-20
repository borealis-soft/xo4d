using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

public class GamePlayerManager : NetworkBehaviour
{
	public CellState MyRole;

	public override void NetworkStart()
	{
		base.NetworkStart();
		Debug.Log($"Успешное подключение!");
		if (!IsServer && IsOwner)
		{
			RequestDataServerRpc(NetworkManager.LocalClientId);
			RequestRoleServerRpc(NetworkManager.LocalClientId);
		}
	}

#if DEBUG
	public static GamePlayerManager Instance;
	public void SwapRole()
	{
		MyRole = MyRole == CellState.Cross ? CellState.Zero : CellState.Cross;
		TacticalTicTacToe.Instance.RoleText.text = "Текущая роль: " + MyRole.AsString();
	}

	public void Awake()
	{
		Instance = this;
	}
#endif

	private ClientRpcParams GetClientRpcParams(ulong clientId) => new ClientRpcParams()
	{
		Send = new ClientRpcSendParams
		{
			TargetClientIds = new ulong[] { clientId }
		}
	};

	public void RequestMove(int cell)
	{
		RequestMoveServerRpc(cell, MyRole);
	}

	[ServerRpc]
	public void RequestMoveServerRpc(int cell, CellState Role)
	{
		TacticalTicTacToe.Instance.MakeMove(cell, Role);
	}

	[ServerRpc]
	public void SendMessageServerRpc(string message)
	{
		TacticalTicTacToe.Instance.CreateMessageObjClientRpc(message);
	}

	[ServerRpc]
	private void RequestRoleServerRpc(ulong clientId)
	{
		CellState newRole = NetworkManager.Singleton.ConnectedClientsList.Count <= 2 ? CellState.Zero : CellState.Empty;
		ClientRpcParams clientRpcParams = GetClientRpcParams(clientId);
		TacticalTicTacToe.Instance.GetRoleClientRpc(newRole, clientRpcParams);
	}

	[ServerRpc]
	private void RequestDataServerRpc(ulong clientId)
	{
		ClientRpcParams clientRpcParams = GetClientRpcParams(clientId);
		TacticalTicTacToe.Instance.SyncFieldsClientRpc(TacticalTicTacToe.Instance.AllCells.ToArray(), clientRpcParams);
	}
}
