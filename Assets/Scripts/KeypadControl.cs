using UnityEngine;

public class KeypadControl : MonoBehaviour
{
#if DEBUG
	public KeyCode CheatSwapKey = KeyCode.Delete;
#endif
	public KeyCode MenuKey = KeyCode.Escape;
	public KeyCode ChatKey = KeyCode.Return;

	public GameObject MiniMenu;
	public GameObject ChatPanel;
	public GameObject WinMenu;

	private void Update()
	{
		if (Input.GetKeyDown(MenuKey))
		{
			if (!ChatPanel.activeSelf && !WinMenu.activeSelf)
				MiniMenu.SetActive(!MiniMenu.activeSelf);
		}
		if (Input.GetKeyDown(ChatKey))
		{
			ChatPanel.SetActive(true);
		}

#if DEBUG
		if (Input.GetKeyDown(CheatSwapKey))
		{
			GamePlayerManager.Instance.SwapRole();
		}
#endif
	}
}
