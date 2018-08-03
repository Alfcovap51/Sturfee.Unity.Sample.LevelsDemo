using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Sturfee.Unity.XR.Core.Events;
using Sturfee.Unity.XR.Core.Session;
using System.Text.RegularExpressions;

// Touching the screen while the AR view is active will bring different results depending on the player's InteractMode
public enum InteractMode
{
	Remove,
	Level1Placement,
	Level2Placement,
	Level3Placement
}

// Handles all Player UI after localization
public class PlayerUiController : MonoBehaviour {

	[HideInInspector]
	public InteractMode InteractMode;

	[Header("Buttons")]
	public GameObject SideButtons;
	public GameObject SaveDiscardButtons;
	public GameObject ItemSelectedButtons;
	public GameObject MapViewButton;
	public GameObject ResetGameButton;
	public Transform Level3PlacementButton;
	public Transform Level2PlacementButton;
	public Transform Level1PlacementButton;
	public Transform InteractButton;
	public Transform SelectorIcon;

	[Header("Touch Controllers")]
	[SerializeField]
	private PlayerArTouchController _arViewTouchController;
	[SerializeField]
	private GameObject _mapTouchControlPanel;

	[Header("Components")]
	[SerializeField]
	private GameObject _playerCanvas;
	[SerializeField]
	private GameObject _mapPlayer;

	private bool _fullScreenMapEnabled = false;
	private int _sturfeeLevel;

	private void Awake () 
	{
		_mapPlayer.SetActive (false);
		SaveDiscardButtons.SetActive (false);
		ItemSelectedButtons.SetActive (false);
		_playerCanvas.SetActive (false);
		_mapTouchControlPanel.SetActive (false);

		InteractMode = InteractMode.Remove;

		// Check what Sturfee level is being used
		string levelStr = AccessHelper.CurrentTier.ToString ();
		levelStr = Regex.Replace(levelStr, "[^0-9]", "");
		_sturfeeLevel = int.Parse (levelStr);

		if (_sturfeeLevel < 3)
		{
			Level3PlacementButton.GetComponent<Button> ().interactable = false;
		}
		if (_sturfeeLevel < 2)
		{
			Level2PlacementButton.GetComponent<Button> ().interactable = false;
		}
	}

	public void Initialize()
	{
		_mapPlayer.SetActive (true);
		_playerCanvas.SetActive (true);
		SelectorIcon.position = InteractButton.position;
	}
		
	public void OnMapViewClick()
	{
		_fullScreenMapEnabled = !_fullScreenMapEnabled;

		_mapTouchControlPanel.SetActive (_fullScreenMapEnabled);
		_arViewTouchController.gameObject.SetActive (!_fullScreenMapEnabled);
		SideButtons.SetActive (!_fullScreenMapEnabled);
		ResetGameButton.SetActive (!_fullScreenMapEnabled);

		if (_fullScreenMapEnabled)
		{
			SetItemSelectedOptions (false);
			MapViewButton.GetComponentInChildren<Text> ().text = "AR\nView";
			ScreenMessageController.Instance.SetText ("Drag finger to scroll map", 3);
		}
		else
		{
			MapViewButton.GetComponentInChildren<Text> ().text = "Map\nView";
			ScreenMessageController.Instance.ClearText ();
		}
	}

	public void OnLevel1PlacementClick()
	{
		SetItemSelectedOptions (false);
		InteractMode = InteractMode.Level1Placement;
		SelectorIcon.position = Level1PlacementButton.position;
		ScreenMessageController.Instance.SetText ("Tap the environment to place an item");
	}

	public void OnLevel2PlacementClick()
	{
		SetItemSelectedOptions (false);
		InteractMode = InteractMode.Level2Placement;
		SelectorIcon.position = Level2PlacementButton.position;
		ScreenMessageController.Instance.SetText ("Drag your finger across\nthe ground on screen");
	}

	public void OnLevel3PlacementClick()
	{
		SetItemSelectedOptions (false);
		InteractMode = InteractMode.Level3Placement;
		SelectorIcon.position = Level3PlacementButton.position;
		ScreenMessageController.Instance.SetText ("Drag your finger across the\nground and buildings on screen");
	}

	public void OnRemoveModeClick()
	{
		InteractMode = InteractMode.Remove;
		SelectorIcon.position = InteractButton.position;
		ScreenMessageController.Instance.SetText ("Tap AR items to remove them");
	}

	public void OnRemoveItemClick()
	{
		_arViewTouchController.RemoveSelectedArItem ();
		ItemSelectedButtons.SetActive (false);
		ScreenMessageController.Instance.SetText ("Removed Item", 3);
	}
		
	public void OnSavePlacementClick()
	{
		if (GameManager.Instance.AllowSaveLoad)
		{
			SaveLoadManager.SaveArItem (_arViewTouchController.ActivePlacementItem);
			ScreenMessageController.Instance.SetText ("Saved Item Placement", 2.5f);
		}
		_arViewTouchController.ActivePlacementItem = null;
		SetItemPlacementUiState(true);
	}

	public void OnDiscardObjectClick()
	{
		Destroy(_arViewTouchController.ActivePlacementItem.gameObject);
		SetItemPlacementUiState(true);
	}

	public void SetItemPlacementUiState(bool active, bool saveDiscardButtons = true)
	{
		if (InteractMode == InteractMode.Remove)
		{
			return;
		}
		else
		{
			ResetGameButton.GetComponent<Button> ().interactable = active;
			MapViewButton.GetComponent<Button> ().interactable = active;
			for (int i = 1; i <= _sturfeeLevel + 1; i++)
			{
				SideButtons.transform.GetChild (i).GetComponent<Button> ().interactable = active;
			}
				
			if (saveDiscardButtons)
			{
				SaveDiscardButtons.SetActive (!active);
			}
		}
	}

	public void TurnOnItemSelectedOptions()
	{
		ItemSelectedButtons.SetActive (true);
	}

	public void TurnOffItemSelectedOptions()
	{
		if (ItemSelectedButtons.activeSelf)
		{
			ItemSelectedButtons.SetActive (false);
			_arViewTouchController.RemoveArItemOutline ();
		}
	}

	public void SetItemSelectedOptions(bool state)
	{
		if (!state && ItemSelectedButtons.activeSelf)
		{
			ItemSelectedButtons.SetActive (state);
			_arViewTouchController.RemoveArItemOutline ();
		}
		else if (state)
		{
			ItemSelectedButtons.SetActive (state);
		}
	}
}
