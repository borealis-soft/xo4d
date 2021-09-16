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
        TacticalTicTacToe.Instance.RoleText.text = "Текущая роль: " + MyRole;
    }

    public void Awake()
    {
        Instance = this;
    }
#endif

    public void MakeMove(LocalCell cell)
    {
        if (TacticalTicTacToe.Instance.CurrentPlayer != MyRole) return;

        if (!IsServer)
            MakeMoveServerRpc(cell);
        else
            TacticalTicTacToe.Instance.MakeMoveNet(cell);
    }

    private ClientRpcParams GetClientRpcParams(ulong clientId) => new ClientRpcParams()
    {
        Send = new ClientRpcSendParams
        {
            TargetClientIds = new ulong[] { clientId }
        }
    };

    [ServerRpc]
    public void SendMessageServerRpc(string message)
    {
        TacticalTicTacToe.Instance.CreateMessageObjClientRpc(message);
    }

    [ServerRpc]
    private void MakeMoveServerRpc(LocalCell cell)
    {
        TacticalTicTacToe.Instance.MakeMoveNet(cell);
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
