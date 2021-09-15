using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Cell : MonoBehaviour
{
	//[HideInInspector] public int localX, localY;
	//[HideInInspector] public int fieldX, fieldY;
	//[HideInInspector] public bool hasMoved;
	[HideInInspector] public LocalCell cell = new LocalCell();
	//[HideInInspector] public NetworkVariable<LocalCell> cell;
	//[HideInInspector] public LocalCell Cell_ { get { return cell.Value; } set { cell.Value = value; } }

	private void OnMouseEnter()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellHover(this);
		//Debug.Log($"localX: {cell.localX}, localY: {cell.localY}");
		//Debug.Log($"fieldX: {cell.fieldX}, fieldY: {cell.fieldY}");
	}
	private void OnMouseExit()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellLeave();
	}
	private void OnMouseUp()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
			TacticalTicTacToe.Instance.OnCellClick();
	}
}

public class LocalCell : INetworkSerializable
{
	public int localX, localY;
	public int fieldX, fieldY;
	public bool hasMoved = false;
	public Vector3 pos;

	public void NetworkSerialize(NetworkSerializer serializer)
	{
		serializer.Serialize(ref localX);
		serializer.Serialize(ref localY);
		serializer.Serialize(ref fieldX);
		serializer.Serialize(ref fieldY);
		serializer.Serialize(ref hasMoved);
		serializer.Serialize(ref pos);
	}

	public static bool operator ==(LocalCell val1, LocalCell val2)
    {
		if (val1 is null && val2 is null) return true;
		if (val1 is null || val2 is null) return false;
		return val1.localX == val2.localX && val1.localY == val2.localY && val1.fieldX == val2.fieldX && val1.fieldY == val2.fieldY;
	}
	public static bool operator !=(LocalCell val1, LocalCell val2) => !(val1 == val2);

	public override bool Equals(object obj)
	{
		return object.ReferenceEquals(this, obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
