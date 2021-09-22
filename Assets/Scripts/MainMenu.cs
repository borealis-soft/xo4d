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
		SceneManager.LoadScene(sceneID);
	}

	public void CloseGame()
	{
		Application.Quit();
	}

	public void ResetGame()
	{
		if (GameMode == GameMode.SingleGame)
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		else TacticalTicTacToe.Instance.ResetGame();
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

	public void BackToMainMenu()
	{
		if (GameMode != GameMode.SingleGame)
			StopAllNetworks();
		LoadGame(0);
	}

	private void StopAllNetworks()
	{
		NetworkManager netManager = NetworkManager.Singleton;
		if (netManager.IsHost) netManager.StopHost();
		else netManager.StopClient();
	}
}

public enum GameMode
{
	SingleGame,
	HostGame,
	ClientGame
}