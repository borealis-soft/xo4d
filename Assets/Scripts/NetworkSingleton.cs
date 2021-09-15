using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSingleton : MonoBehaviour
{
    public static NetworkSingleton Instance;
    public GameMode GameMode { get; set; } = GameMode.SingleGame;

    private void Awake()
    {
        Instance = this;
    }
}

//public enum GameMode
//{
//    SingleGame,
//    HostGame,
//    ClientGame
//}