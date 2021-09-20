using MLAPI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	public static GameMode GameMode { get; set; } = GameMode.SingleGame;
	public static string AddrText { get; set; }
	public InputField inputField;

	public void LoadGame(int sceneID)
	{
		StopAllNetworks();
		SceneManager.LoadScene(sceneID);
	}

	public void CloseGame()
	{
		Application.Quit();
	}

	public void ResetGame()
	{
		StopAllNetworks();
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public void CreateServer()
	{
		GameMode = GameMode.HostGame;
		SceneManager.LoadScene(1);
	}

	public void ConnectToServer(Text InputAddrText)
	{
		AddrText = InputAddrText.text;
		GameMode = GameMode.ClientGame;
		SceneManager.LoadScene(1);
	}

	public void SendMessage()
	{
		if (inputField.text == string.Empty) return;
		TacticalTicTacToe.Instance.SendMessages(inputField.text);
		inputField.text = string.Empty;
	}

	public void OnEndEditMessage()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			SendMessage();
			inputField.ActivateInputField();
		}
	}

	private void StopAllNetworks()
	{
		if (NetworkManager.Singleton.IsClient) NetworkManager.Singleton.StopClient();
		if (NetworkManager.Singleton.IsServer) NetworkManager.Singleton.StopServer();
	}
}

public enum GameMode
{
	SingleGame,
	HostGame,
	ClientGame
}