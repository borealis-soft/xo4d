using UnityEngine;

public class CameraMover : MonoBehaviour
{
	[SerializeField] private float noiseSpeed = 0.25f;
	[SerializeField] private float noiseScale = 0.5f;
	Vector3 position;
	private void Awake()
	{
		position = transform.position;
	}
	void Update()
	{
		transform.position = position + new Vector3(
			noiseScale * (Mathf.PerlinNoise(Time.time * noiseSpeed, 0.5f) - 0.5f),
			noiseScale * (Mathf.PerlinNoise(Time.time * noiseSpeed + 3.1415f, 0.5f) - 0.5f),
			0
		);

	}
}
