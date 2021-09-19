using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackOnEsc : MonoBehaviour
{
    public GameObject PrevMenu;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
            if (PrevMenu)
                PrevMenu.SetActive(true);
        }
    }
}
