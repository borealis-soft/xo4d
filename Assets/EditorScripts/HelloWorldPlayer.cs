using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

public class HelloWorldPlayer : NetworkBehaviour
{
	public NetworkVariableVector3 Position = new NetworkVariableVector3(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.ServerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});

	private void Awake()
	{
		Position.OnValueChanged += (Vector3 prev, Vector3 @new) => transform.position = @new;
	}

	public override void NetworkStart()
	{
		Move();
	}

	public void Move()
	{
		if (NetworkManager.Singleton.IsServer)
		{
			Position.Value = GetRandomPositionOnPlane();
		}
		else
		{
			SubmitPositionRequestServerRpc();
		}
	}

	[ServerRpc]
	void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
	{
		Position.Value = GetRandomPositionOnPlane();
	}

	static Vector3 GetRandomPositionOnPlane()
	{
		return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
	}
}