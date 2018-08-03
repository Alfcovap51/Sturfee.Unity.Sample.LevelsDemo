using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Holds game information and controls state of game
public class GameManager : MonoBehaviour {

	public static GameManager Instance;

	// TODO: Explanation....
	public bool AllowSaveLoad;

	[Header("UI")]
	public GameObject HasSaveDataPanel;		// Only appears if save data exists
	public PlayerUiController PlayerUi;

	[Header("AR Item Prefabs")]
	public GameObject Tier1ItemPrefab;
	public GameObject Tier2ItemPrefab;
	public GameObject Tier3ItemPrefab;

	[HideInInspector]
	public bool HasSaveData;

	private bool _loadGame = false;

	private void Awake()
	{
		Instance = this;
		HasSaveDataPanel.SetActive (false);
	}

	private void Start ()
	{
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
		//......
		SaveLoadManager.Unload ();
		SceneManager.LoadScene ("Game");
	}
}
