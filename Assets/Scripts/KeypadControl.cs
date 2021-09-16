using System.Collections;
using System.Collections.Generic;
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

    private void Update()
    {
        if (Input.GetKeyDown(MenuKey))
        {
            MiniMenu.SetActive(!MiniMenu.activeSelf);
        }
        if (Input.GetKeyDown(ChatKey))
        {
            ChatPanel.SetActive(!ChatPanel.activeSelf);
        }

#if DEBUG
        if (Input.GetKeyDown(CheatSwapKey))
        {
            GamePlayerManager.Instance.SwapRole();
        }
#endif
    }
}
