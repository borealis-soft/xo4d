using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeypadControl : MonoBehaviour
{
#if DEBUG
    public KeyCode CheatSwapKey = KeyCode.Delete;
#endif
    public KeyCode MenuKey = KeyCode.Escape;
    public GameObject MiniMenu;

    private void Update()
    {
        if (Input.GetKeyDown(MenuKey))
        {
            MiniMenu.SetActive(!MiniMenu.activeSelf);
        }

#if DEBUG
        if (Input.GetKeyDown(CheatSwapKey))
        {
            TacticalTicTacToe.Instance.SwapPlayer();
        }
#endif
    }
}
