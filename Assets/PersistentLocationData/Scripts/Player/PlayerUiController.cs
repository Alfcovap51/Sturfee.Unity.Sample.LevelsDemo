using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Sturfee.Unity.XR.Core.Events;
using Sturfee.Unity.XR.Core.Session;
using System.Text.RegularExpressions;

// Touching the screen while the AR view is active will bring different results depending on the player's InteractMode
public enum InteractMode
{
	Interact,
	Tier1Placement,
	Tier2Placement,
	Tier3Placement
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
	public GameObject ExitGameButton;
	public Transform Tier3PlacementButton;
	public Transform Tier2PlacementButton;
	public Transform Tier1PlacementButton;
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
	private int _sturfeeTier;

	private void Awake () 
	{
		_mapPlayer.SetActive (false);
		SaveDiscardButtons.SetActive (false);
		ItemSelectedButtons.SetActive (false);
		_playerCanvas.SetActive (false);
		_mapTouchControlPanel.SetActive (false);

		InteractMode = InteractMode.Interact;

		// Check what Sturfee tier is being used
		string tierStr = AccessHelper.CurrentTier.ToString ();
		tierStr = Regex.Replace(tierStr, "[^0-9]", "");
		_sturfeeTier = int.Parse (tierStr);

		if (_sturfeeTier < 3)
		{
			Tier3PlacementButton.GetComponent<Button> ().interactable = false;
		}
		if (_sturfeeTier < 2)
		{
			Tier2PlacementButton.GetComponent<Button> ().interactable = false;
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
		ExitGameButton.SetActive (!_fullScreenMapEnabled);

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

	public void OnTier1PlacementClick()
	{
		SetItemSelectedOptions (false);
		InteractMode = InteractMode.Tier1Placement;
		SelectorIcon.position = Tier1PlacementButton.position;
		ScreenMessageController.Instance.SetText ("Tap the environment to place an item");
	}

	public void OnTier2PlacementClick()
	{
		SetItemSelectedOptions (false);
		InteractMode = InteractMode.Tier2Placement;
		SelectorIcon.position = Tier2PlacementButton.position;
		ScreenMessageController.Instance.SetText ("Drag your finger across\nthe ground on screen");
	}

	public void OnTier3PlacementClick()
	{
		SetItemSelectedOptions (false);
		InteractMode = InteractMode.Tier3Placement;
		SelectorIcon.position = Tier3PlacementButton.position;
		ScreenMessageController.Instance.SetText ("Drag your finger across the\nground and buildings on screen");
	}

	public void OnInteractModeClick()
	{
		InteractMode = InteractMode.Interact;
		SelectorIcon.position = InteractButton.position;
		ScreenMessageController.Instance.SetText ("Tap AR items to remove them");
	}

	public void OnRemoveItemClick()
	{
		_arViewTouchController.RemoveSelectedArItem ();
		ItemSelectedButtons.SetActive (false);
	}
		
	public void OnSavePlacementClick()
	{
		SaveLoadManager.SaveArItem (_arViewTouchController.ActivePlacementItem);

		_arViewTouchController.ActivePlacementItem = null;
		SetItemPlacementUiState(true);

		ScreenMessageController.Instance.SetText ("Saved Item Placement", 2.5f);
	}

	public void OnDiscardObjectClick()
	{
		Destroy(_arViewTouchController.ActivePlacementItem.gameObject);
		SetItemPlacementUiState(true);

	}

	public void SetItemPlacementUiState(bool active, bool saveDiscardButtons = true /*bool setToInteract = true*/)
	{
		if (InteractMode == InteractMode.Interact)
		{
			return;
		}
		else
		{
			ExitGameButton.GetComponent<Button> ().interactable = active;
			MapViewButton.GetComponent<Button> ().interactable = active;
			for (int i = 1; i <= _sturfeeTier + 1; i++)
			{
				SideButtons.transform.GetChild (i).GetComponent<Button> ().interactable = active;
			}
				
//			if (InteractMode == InteractMode.Tier2Placement || InteractMode == InteractMode.Tier3Placement)
//			{
			if (saveDiscardButtons)
			{
				SaveDiscardButtons.SetActive (!active);
			}
//			}

//			if (active && setToInteract)
//			{
//				InteractMode = InteractMode.Interact;
//				SelectorIcon.position = InteractButton.position;
//			}
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
