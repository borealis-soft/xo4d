using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace XO3D
{
	public enum PlayerTeam : byte
	{
		X, O, Count
	}
	public struct WinCondition
	{
		public int[] indices;
	}
	public class Game : MonoBehaviour
	{
		[SerializeField] private Cell cellPrefab;
		[SerializeField] private GameObject xPrefab, oPrefab;
		[SerializeField] private Transform highlight, centerCellParent;
		[HideInInspector] public PlayerTeam currentTeam;
		private Cell[] cells = new Cell[3*3*3];
		private int hideLayersCount;
		public static Game Instance { get; private set; }

		private static readonly WinCondition[] winConditions = new[]
		{
			// Z
			new WinCondition() { indices = new [] {  0,  1,  2 } },
			new WinCondition() { indices = new [] {  3,  4,  5 } },
			new WinCondition() { indices = new [] {  6,  7,  8 } },
			new WinCondition() { indices = new [] {  9, 10, 11 } },
			new WinCondition() { indices = new [] { 12, 13, 14 } },
			new WinCondition() { indices = new [] { 15, 16, 17 } },
			new WinCondition() { indices = new [] { 18, 19, 20 } },
			new WinCondition() { indices = new [] { 21, 22, 23 } },
			new WinCondition() { indices = new [] { 24, 25, 26 } },
			
			// Y
			new WinCondition() { indices = new [] {  0,  3,  6 } },
			new WinCondition() { indices = new [] {  1,  4,  7 } },
			new WinCondition() { indices = new [] {  2,  5,  8 } },
			new WinCondition() { indices = new [] {  9, 12, 15 } },
			new WinCondition() { indices = new [] { 10, 13, 16 } },
			new WinCondition() { indices = new [] { 11, 14, 17 } },
			new WinCondition() { indices = new [] { 18, 21, 24 } },
			new WinCondition() { indices = new [] { 19, 22, 25 } },
			new WinCondition() { indices = new [] { 20, 23, 26 } },
			
			// X
			new WinCondition() { indices = new [] { 0,  9, 18 } },
			new WinCondition() { indices = new [] { 1, 10, 19 } },
			new WinCondition() { indices = new [] { 2, 11, 20 } },
			new WinCondition() { indices = new [] { 3, 12, 21 } },
			new WinCondition() { indices = new [] { 4, 13, 22 } },
			new WinCondition() { indices = new [] { 5, 14, 23 } },
			new WinCondition() { indices = new [] { 6, 15, 24 } },
			new WinCondition() { indices = new [] { 7, 16, 25 } },
			new WinCondition() { indices = new [] { 8, 17, 26 } },
		};
		private CameraDirection cameraDirection, previousCameraDirection;

		private Cell GetCell(int x, int y, int z)
		{
			return cells[z * 3 * 3 + y * 3 + x];
		}
		private void SetCell(int x, int y, int z, Cell newCell)
		{
			cells[z * 3 * 3 + y * 3 + x] = newCell;
		}
		private void ResetTransform(Transform transform)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}
		private void Awake()
		{
			Debug.Assert(Instance == null);
			Instance = this;

			for (int x = 0; x < 3; ++x)
			{
				for (int y = 0; y < 3; ++y)
				{
					for (int z = 0; z < 3; ++z)
					{
						var cell = Instantiate(cellPrefab, new Vector3(x, y, z) - Vector3.one, Quaternion.identity);
						cell.index = new Vector3Int(x, y, z);

						if (x == 1 && y == 1 && z == 1)
						{
							cell.transform.parent = centerCellParent;
							ResetTransform(cell.transform);
						}
						
						SetCell(x, y, z, cell);
					}
				}
			}
		}

		public void MakeMove(Cell cell)
		{
			int x = cell.index.x;
			int y = cell.index.y;
			int z = cell.index.z;
			Debug.Assert(x < 3 && y < 3 && z < 3 && currentTeam < PlayerTeam.Count);

			if (cell.state != CellState.Empty)
				return;

			cell.state = GetCellState(currentTeam);

			var mark = Instantiate(GetMarkPrefab(currentTeam), cell.transform);
			ResetTransform(mark.transform);

			cell.SetMarkMaterial(mark.GetComponent<MeshRenderer>().material);

			currentTeam = GetOtherTeam(currentTeam);

			bool winIsPossible = false;
			foreach (var condition in winConditions)
			{
				var indexedCells = condition.indices.Select(i => cells[i]).ToArray();
				var distinct = indexedCells.Distinct(new CellStateComparer()).ToArray();
				var distinctCount = distinct.Count();
				if (distinctCount == 1)
				{
					var state = distinct.ElementAt(0).state;
					if (state == CellState.Empty)
					{
						continue;
					}
					Debug.Log($"Победил {state}!!!");
					break;
				} 
				else if (distinctCount == 2)
				{
					if (distinct.Any(cell => cell.state == CellState.Empty)) 
					{
						winIsPossible = true;
					}
				}
			}
			if (!winIsPossible)
			{
				Debug.Log($"Ничья!!!");
			}
		}
		public void Highlight(Cell cell)
		{
			highlight.parent = cell.transform;
			ResetTransform(highlight);
		}
		public void Unhighlight()
		{
			highlight.parent = null;
			highlight.position = Vector3.up * 1000;
		}
		public void HideLayer()
		{
			if (hideLayersCount != 2)
			{
				hideLayersCount += 1;
				HideAndShowCells();
			}
		}
		public void ShowLayer()
		{
			if (hideLayersCount != 0)
			{
				hideLayersCount -= 1;
				HideAndShowCells();
			}
		}

		enum CameraDirection
		{
			PositiveX,
			PositiveY,
			PositiveZ,
			NegativeX,
			NegativeY,
			NegativeZ,
		}

		private void Update()
		{
			Vector3 cameraForward = Camera.main.transform.forward;
			Vector3 absCameraForward = new Vector3(
				Mathf.Abs(cameraForward.x),
				Mathf.Abs(cameraForward.y),
				Mathf.Abs(cameraForward.z)
			);

			if (absCameraForward.x > absCameraForward.y)
			{
				if (absCameraForward.x > absCameraForward.z)
				{
					cameraDirection = cameraForward.x > 0 ? CameraDirection.PositiveX : CameraDirection.NegativeX;
				}
				else
				{
					cameraDirection = cameraForward.z > 0 ? CameraDirection.PositiveZ : CameraDirection.NegativeZ;
				}
			}
			else
			{
				if (absCameraForward.y > absCameraForward.z)
				{
					cameraDirection = cameraForward.y > 0 ? CameraDirection.PositiveY : CameraDirection.NegativeY;
				}
				else
				{
					cameraDirection = cameraForward.z > 0 ? CameraDirection.PositiveZ : CameraDirection.NegativeZ;
				}
			}

			if (previousCameraDirection != cameraDirection)
			{
				HideAndShowCells();
				previousCameraDirection = cameraDirection;
			}
		}
		private GameObject GetMarkPrefab(PlayerTeam team) => team switch
		{
			PlayerTeam.X => xPrefab,
			PlayerTeam.O => oPrefab,
			_ => null,
		};
		private void HideAndShowCells()
		{
			Vector3Int hideStart = Vector3Int.zero;
			Vector3Int hideEnd = Vector3Int.one * 3;
			Vector3Int showStart = Vector3Int.zero;
			Vector3Int showEnd = Vector3Int.one * 3;
			switch (cameraDirection)
			{
				case CameraDirection.PositiveX:
				{
					hideEnd.x = hideLayersCount;
					showStart.x = hideLayersCount;
					break;
				}
				case CameraDirection.PositiveY:
				{
					hideEnd.y = hideLayersCount;
					showStart.y = hideLayersCount;
					break;
				}
				case CameraDirection.PositiveZ:
				{
					hideEnd.z = hideLayersCount;
					showStart.z = hideLayersCount;
					break;
				}
				case CameraDirection.NegativeX:
				{
					showEnd.x = 3 - hideLayersCount;
					hideStart.x = 3 - hideLayersCount;
					break;
				}
				case CameraDirection.NegativeY:
				{
					showEnd.y = 3 - hideLayersCount;
					hideStart.y = 3 - hideLayersCount;
					break;
				}
				case CameraDirection.NegativeZ:
				{
					showEnd.z = 3 - hideLayersCount;
					hideStart.z = 3 - hideLayersCount;
					break;
				}
			}

			for (int x = hideStart.x; x < hideEnd.x; ++x)
			{
				for (int y = hideStart.y; y < hideEnd.y; ++y)
				{
					for (int z = hideStart.z; z < hideEnd.z; ++z)
					{
						GetCell(x, y, z).Hide();
					}
				}
			}

			for (int x = showStart.x; x < showEnd.x; ++x)
			{
				for (int y = showStart.y; y < showEnd.y; ++y)
				{
					for (int z = showStart.z; z < showEnd.z; ++z)
					{
						GetCell(x, y, z).Show();
					}
				}
			}
		}

		private PlayerTeam GetOtherTeam(PlayerTeam team) => team switch
		{
			PlayerTeam.X => PlayerTeam.O,
			PlayerTeam.O => PlayerTeam.X,
			_ => throw new System.ArgumentOutOfRangeException(nameof(team))
		};
		private CellState GetCellState(PlayerTeam team) => team switch
		{
			PlayerTeam.X => CellState.X,
			PlayerTeam.O => CellState.O,
			_ => throw new System.ArgumentOutOfRangeException(nameof(team))
		};
	}

	public class CellStateComparer : IEqualityComparer<Cell>
	{
		public bool Equals(Cell x, Cell y)
		{
			return x.state == y.state;
		}

		public int GetHashCode(Cell obj)
		{
			return obj.state.GetHashCode();
		}
	}
}