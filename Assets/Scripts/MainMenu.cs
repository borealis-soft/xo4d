using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLAPI;
using MLAPI.Transports.UNET;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public void LoadGame(int sceneID)
    {
        NetworkSingleton.Instance.GameMode = GameMode.SingleGame;
        SceneManager.LoadScene(sceneID);
    }

    public void CloseGame()
    {
        Application.Quit();
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void CreateServer()
    {
        NetworkSingleton.Instance.GameMode = GameMode.HostGame;
        SceneManager.LoadScene(1);
    }

    public void ConnectToServer(Text InputAddrText)
    {
        if (InputAddrText.text != string.Empty)
        {
            var transport = NetworkManager.Singleton.GetComponent<UNetTransport>();
            string[] addr = InputAddrText.text.Split(':');
            transport.ConnectAddress = addr[0];
            transport.ConnectPort = int.Parse(addr[1]);
        }

        NetworkSingleton.Instance.GameMode = GameMode.ClientGame;
        SceneManager.LoadScene(1);
    }
}
