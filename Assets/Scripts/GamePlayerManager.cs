using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using System.Linq;

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
        MyRole = MyRole == CellState.PlayerCross ? CellState.PlayerZero : CellState.PlayerCross;
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

    public void RequestCellUpdate(int cell)
	{
        RequestCellUpdateServerRpc(cell, MyRole);
	}
    [ServerRpc]
    public void RequestCellUpdateServerRpc(int cell, CellState Role)
	{
        TacticalTicTacToe.Instance.UpdateCellClientRpc(cell, Role);
    }

    [ServerRpc]
    public void SendMessageServerRpc(string message)
    {
        TacticalTicTacToe.Instance.CreateMessageObjClientRpc(message);
    }

    [ServerRpc]
    private void RequestRoleServerRpc(ulong clientId)
    {
        CellState newRole = NetworkManager.Singleton.ConnectedClientsList.Count <= 2 ? CellState.PlayerZero : CellState.Empty;
        ClientRpcParams clientRpcParams = GetClientRpcParams(clientId);
        TacticalTicTacToe.Instance.GetRoleClientRpc(newRole, clientRpcParams);
    }

    [ServerRpc]
    private void RequestDataServerRpc(ulong clientId)
    {
        bool[] cellsHesMoved = TacticalTicTacToe.AllCells.Select(cell => cell.hasMoved).ToArray();
        ClientRpcParams clientRpcParams = GetClientRpcParams(clientId);
        TacticalTicTacToe.Instance.SyncFieldsClientRpc(TacticalTicTacToe.Instance.Fields, cellsHesMoved, clientRpcParams);
    }
}
