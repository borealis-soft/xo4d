using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeypadControl : MonoBehaviour
{
    public KeyCode CheatSwapKey = KeyCode.Delete;
    //Убрать на релизе!!! ^
    public KeyCode MenuKey = KeyCode.Escape;
    public GameObject MiniMenu;

    private void Update()
    {
        if (Input.GetKeyDown(MenuKey))
        {
            MiniMenu.SetActive(!MiniMenu.activeSelf);
        }

        // !!!!Убрать на релизе!!!!
        if (Input.GetKeyDown(CheatSwapKey))
        {
            Manager.Instance.SwapPlayer();
        }
    }
}
