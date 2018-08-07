using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

// Holds game information and controls state of game
public class GameManager : MonoBehaviour {

	public static GameManager Instance;
	public static bool HasSaveData;
	public static int SturfeeLevel;

	public bool AllowSaveLoad; 				// Allows the game to save and load data to phone during play

	[Header("UI")]
	public GameObject HasSaveDataPanel;		// Only appears if save data exists
	public PlayerUiController PlayerUi;

	[Header("AR Item Prefabs")]
	public GameObject Level1ItemPrefab;
	public GameObject Level2ItemPrefab;
	public GameObject Level3ItemPrefab;

	private bool _loadGame = false;

	private void Awake()
	{
		Instance = this;
		HasSaveDataPanel.SetActive (false);
	}

	private void Start ()
	{
		// Change start screen options depending on if save data exists
		HasSaveData = SaveLoadManager.HasSaveData ();
		if (AllowSaveLoad && HasSaveData)
		{
			HasSaveDataPanel.SetActive (true);
		}
		else
		{
			AlignmentManager.Instance.LookTrigger.GetComponent<LookUpTrigger> ().IsEnabled = true;
			ScreenMessageController.Instance.SetText ("Initializing Session...");
		}

		// Determine Sturfee level
		string levelStr = AccessHelper.CurrentTier.ToString ();
		levelStr = Regex.Replace(levelStr, "[^0-9]", "");
		SturfeeLevel = int.Parse (levelStr);
	}
		
	// If save data exists, then pressing 'New Game' or 'Load Game' button will lead to this call
	public void OnSaveDataStartScreenClick(bool loadGame)
	{
		_loadGame = loadGame;
		HasSaveDataPanel.SetActive (false);

		if (loadGame)
		{
			SaveLoadManager.Load ();
		}

		AlignmentManager.Instance.SetScanButton (loadGame);
	}
		
	public void InitializeGame()
	{
		if (_loadGame)
		{
			SaveLoadManager.LoadGameData ();
		}

		StreetMap.Instance.InitializeMap();
		PlayerUi.Initialize ();
	}

	public void ResetGame(float timer = 0)
	{
		Invoke ("ReloadScene", timer);
	}

	private void ReloadScene()
	{
		SaveLoadManager.Unload ();
		SceneManager.LoadScene ("Game");
	}
}
