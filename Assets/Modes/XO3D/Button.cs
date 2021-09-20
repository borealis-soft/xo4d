using UnityEngine;

public class Button : MonoBehaviour
{
	[SerializeField] private UnityEngine.Events.UnityEvent method;
	private void OnMouseUpAsButton()
	{
		method.Invoke();
	}
}
