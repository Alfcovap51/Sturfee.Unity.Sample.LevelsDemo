using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Sturfee.Unity.XR.Core.Events;
using Sturfee.Unity.XR.Core.Session;

// Controls the creation, interaction, and removal of AR Items in the environment via player touch on the screen
public class PlayerArTouchController : MonoBehaviour, IPointerDownHandler, IDragHandler{

	[HideInInspector]
	public Transform ActivePlacementItem;	// The AR item that is currently being placed, deciding whether to save or discard it

	[Header("Layer Masks")]
	[SerializeField]
	private LayerMask _removalLayerMask;
	[SerializeField]
	private LayerMask _level2PlacementLayerMask;
	[SerializeField]
	private LayerMask _level3PlacementLayerMask;

	[Header("Outline Materials")]
	[SerializeField]
	private Material _eggOutline;
	[SerializeField]
	private Material _crystalOutline;
	[SerializeField]
	private Material _gemOutline;

	[Header("Player")]
	[SerializeField]
	private Camera _playerXrCam;
	[SerializeField]
	private PlayerUiController _playerUiController;

	private bool _activeHitscan = false;

	private GameObject _selectedArItem;
	private Material _selectedArItemMaterial;

	private void Start () {
		SturfeeEventManager.Instance.OnDetectSurfacePointComplete += OnDetectSurfacePointComplete;
		SturfeeEventManager.Instance.OnDetectSurfacePointFailed += OnDetectSurfacePointFailed;  
		ActivePlacementItem = null;
	}

	private void OnDestroy()
	{
		SturfeeEventManager.Instance.OnDetectSurfacePointComplete -= OnDetectSurfacePointComplete;
		SturfeeEventManager.Instance.OnDetectSurfacePointFailed -= OnDetectSurfacePointFailed;  
	}

	// Unity function that tracks the player's initial touch/mouse-click on this panel
	public void OnPointerDown(PointerEventData eventData)
	{
		switch(_playerUiController.InteractMode)
		{
		case InteractMode.Remove:
			ScreenTouchRaycast (eventData.pressPosition, _removalLayerMask);
			break;
		case InteractMode.Level1Placement:
			if (!_activeHitscan)
			{
				StartCoroutine (DetectSurfacePointTimer());
				XRSessionManager.GetSession ().DetectSurfaceAtPoint (eventData.pressPosition);
			}
			break;
		case InteractMode.Level2Placement:
			ScreenTouchRaycast (eventData.pressPosition, _level2PlacementLayerMask);
			break;
		case InteractMode.Level3Placement:
			ScreenTouchRaycast (eventData.pressPosition, _level3PlacementLayerMask);
			break;
		}
	}	

	// Unity function that tracks when the player is dragging their finger/mouse on the screen
	public void OnDrag(PointerEventData data)
	{
		if (_playerUiController.InteractMode == InteractMode.Level2Placement)
		{
			ScreenTouchRaycast (data.position, _level2PlacementLayerMask);
		}
		else if (_playerUiController.InteractMode == InteractMode.Level3Placement)
		{
			ScreenTouchRaycast (data.position, _level3PlacementLayerMask);
		}
	}
		
	private void ScreenTouchRaycast(Vector2 touchPos, LayerMask layerMask)
	{
		Ray ray = _playerXrCam.ScreenPointToRay (touchPos);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 1000, layerMask))
		{
			if (_playerUiController.InteractMode == InteractMode.Level2Placement || _playerUiController.InteractMode == InteractMode.Level3Placement)
			{
				if (ActivePlacementItem == null)
				{
					if (_playerUiController.InteractMode == InteractMode.Level2Placement)
					{
						ActivePlacementItem = Instantiate (GameManager.Instance.Level2ItemPrefab).transform;
					}
					else
					{
						ActivePlacementItem = Instantiate (GameManager.Instance.Level3ItemPrefab).transform;
					}
					_playerUiController.SetItemPlacementUiState(false);
					ScreenMessageController.Instance.ClearText ();
				}

				ActivePlacementItem.position = hit.point;
				ActivePlacementItem.rotation = Quaternion.LookRotation (hit.normal);
			}
			else if (_playerUiController.InteractMode == InteractMode.Remove)
			{
				RemoveArItemOutline (true);

				_selectedArItem = hit.collider.gameObject;
				_selectedArItemMaterial = _selectedArItem.GetComponent<Renderer> ().material;

				if (_selectedArItem.GetComponentInParent<ArItem> ().ItemType == ArItemType.level1)
				{
					_selectedArItem.GetComponent<Renderer> ().material = _crystalOutline;
				}
				else if (_selectedArItem.GetComponentInParent<ArItem> ().ItemType == ArItemType.level2)
				{
					_selectedArItem.GetComponent<Renderer> ().material = _eggOutline;
				}
				else
				{
					_selectedArItem.GetComponent<Renderer> ().material = _gemOutline;
				}

				_playerUiController.SetItemSelectedOptions (true);
			}
		}
		else
		{
			RemoveArItemOutline ();
		}
	}

	public void RemoveArItemOutline(bool newArItemSelected = false)
	{
		if (_selectedArItem != null)
		{
			_selectedArItem.GetComponent<Renderer> ().material = _selectedArItemMaterial;

			if (!newArItemSelected)
			{
				_selectedArItem = null;
				_selectedArItemMaterial = null;
				_playerUiController.SetItemSelectedOptions (false);
			}
		}
	}

	public void RemoveSelectedArItem()
	{
		if (GameManager.Instance.AllowSaveLoad)
		{
			SaveLoadManager.RemoveArItem (_selectedArItem.GetComponentInParent<ArItem> ().Id);
		}
		Destroy (_selectedArItem.transform.parent.gameObject);
	}

	// Sturfee event called when 'DetectSurfaceAtPoint' completes
	public void OnDetectSurfacePointComplete(Sturfee.Unity.XR.Core.Models.Location.GpsPosition gpsPos, UnityEngine.Vector3 normal)
	{
		_activeHitscan = false;
		if (ActivePlacementItem == null)
		{
			ActivePlacementItem = Instantiate (GameManager.Instance.Level1ItemPrefab).transform;
		}

		ActivePlacementItem.position = XRSessionManager.GetSession ().GpsToLocalPosition (gpsPos);
		ActivePlacementItem.rotation = Quaternion.LookRotation (normal);

		ScreenMessageController.Instance.ClearText ();
		_playerUiController.SetItemPlacementUiState(false);
	}
		
	// Sturfee event called when 'DetectSurfaceAtPoint' fails
	public void OnDetectSurfacePointFailed()
	{
		_activeHitscan = false;
		ScreenMessageController.Instance.SetText ("Placement Failed\nTap on the ground or a building.");
		_playerUiController.SetItemPlacementUiState(true, false);
	}

	// Error handling timer when Sturfee function 'OnDetectSurfaceAtPoint'is called
	private IEnumerator DetectSurfacePointTimer()
	{
		_activeHitscan = true;
		_playerUiController.SetItemPlacementUiState(false, false);
		ScreenMessageController.Instance.SetText ("Placing Item...");

		float endTimer = Time.time;

		// Sets the error timer to 5 seconds for actual server calls when using level 1 or 2 Sturfee API keys.
		// Using a level 3 key automatically overwrites level 1 API calls to use its preloaded terrain and building data rather than making a call to the server.
		// Thus using level 1 calls with a level 3 key in this sample app simply simulates level 1 calls with a one second error timer for failures, as all successful call wills be instant.
		endTimer += (GameManager.SturfeeLevel < 3) ? 5 : 1;

		while (_activeHitscan && Time.time < endTimer)
		{
			yield return null;
		}

		if (_activeHitscan)
		{
			// Level 1 & 2
			if (GameManager.SturfeeLevel < 3)
			{
				// Either a Unity error occurred during the server call due to loss of connection, or the server is acting slow
				ScreenMessageController.Instance.SetText ("API hitscan call timed out");
			}
			else  // Level 3
			{
				// User did not tap on a terrain or building collider
				ScreenMessageController.Instance.SetText ("Call failed\nDid not tap on terrain or building", 3);
			}

			if (!ActivePlacementItem)
			{
				_playerUiController.SetItemPlacementUiState (true);
			}
		}
			
		_activeHitscan = false;
	}
}
