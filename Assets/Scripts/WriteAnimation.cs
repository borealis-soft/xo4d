using System.Collections;
using UnityEngine;

public class WriteAnimation : MonoBehaviour
{
	Material material;
	float startTime;
	void Awake()
	{
		startTime = Time.time;
		material = GetComponent<MeshRenderer>().material;
		StartCoroutine(Animate());
	}
	IEnumerator Animate()
	{
		while (true)
		{
			float t = Time.time - startTime;
			material.SetFloat("_TimeOffset", t);
			if (t >= 1)
			{
				yield break;
			}
			yield return null;
		}
	}
}
