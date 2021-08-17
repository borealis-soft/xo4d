using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XO3D
{

	public class CameraControl : MonoBehaviour
	{
		[SerializeField] private float sensitivity = 1;
		[SerializeField] private float distance = 5;
		[SerializeField] private float lerpSpeed = 15;
		private Vector3 targetAngles;
		void Update()
		{
			if (Input.GetKey(KeyCode.Mouse1))
			{
				targetAngles.y += Input.GetAxis("Mouse X") * sensitivity;
				targetAngles.x -= Input.GetAxis("Mouse Y") * sensitivity;
			}
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetAngles), Time.deltaTime * lerpSpeed);
			transform.position = transform.rotation * Vector3.back * distance;
		}
	}

}
