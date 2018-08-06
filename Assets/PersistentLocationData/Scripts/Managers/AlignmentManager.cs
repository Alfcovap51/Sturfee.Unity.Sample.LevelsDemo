using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sturfee.Unity.XR.Core.Events;
using Sturfee.Unity.XR.Core.Session;

// Customized Alignment script
// - Combines the code from the default provided 'AlignmentManager' and 'MultiframeManager'
// - Makes several custom adjustments to the multiframe UI
// - Also adds code to work with this sample app's UI and works in tandem with 'GameManager' script
//
// Multiframe UI Adjustments:
// - Gaze Targets are adjusted to appear in one direction and one at a time, instead of to the left and right of where the player begins scanning
// - Default left/right arrows have been changed to use a single arrow that points directly towards the gaze target
public class AlignmentManager : MonoBehaviour {

	public static AlignmentManager Instance;

	[Header("Buttons")]
	public GameObject ScanButton;
	public GameObject BackButton;

	[Header("Multiframe UI")]
	public GameObject GazeTargetPrefab;
	public GameObject Cursor;
	public GameObject Arrow;
	public GameObject ScanAnimation;
	public GameObject LookTrigger;

	[Header("Other")]
	public Camera XrCamera;
	public LayerMask MultiframeLayerMask;

	private List<GameObject> _gazeTargets;
	private int _totalTargets = 3;   			// Total # of pictures required. The actual amount of targets the user must align manually is (_totalTargets - 1)
	private int _targetCount;					
	private int _angle = 50;					// Angle between the gaze targets placed. The current SDK requires the angle be 50, otherwise a 'request error' may occur.
	private bool _isScanning; 
	private int _multiframeRequestId;
	private bool _resetCalled;
	private bool _midAlignmentError = false;
	private bool _loadGame;

	// Prerequisites required to begin scan
	private bool _sessionReady = false;
	private bool _coveredArea = false;			// Denotes if you are using the app in a Sturfee-enabled location
	private bool _lookedUp = false;

	private void Awake()
	{            
		Instance = this;

		SturfeeEventManager.Instance.OnSessionReady += OnSessionReady;
		SturfeeEventManager.Instance.OnSessionFailed += OnSessionFailed;
		SturfeeEventManager.Instance.OnCoverageCheckComplete += OnCoverageCheckComplete;
		SturfeeEventManager.Instance.OnLocalizationComplete += OnLocalizationComplete;
		SturfeeEventManager.Instance.OnLocalizationLoading += OnLocalizationLoading;

		LookUpTrigger.OnUserLookedUp += HandleOnLookUpComplete;

		ScanButton.SetActive (false);
		BackButton.SetActive(false);
		Cursor.SetActive (false);
		Arrow.SetActive (false);
		ScanAnimation.SetActive (false);
	}

	private void Start () {
		_targetCount = _totalTargets - 1;
	}

	private void OnDestroy()
	{
		// *IMPORTANT* Unregister event handlers for Sturfee events

		// XR SESSION
		SturfeeEventManager.Instance.OnSessionReady -= OnSessionReady;
		SturfeeEventManager.Instance.OnSessionFailed -= OnSessionFailed;

		// LOCALIZATION
		SturfeeEventManager.Instance.OnCoverageCheckComplete -= OnCoverageCheckComplete;
		SturfeeEventManager.Instance.OnLocalizationComplete -= OnLocalizationComplete;
		SturfeeEventManager.Instance.OnLocalizationLoading -= OnLocalizationLoading;

		LookUpTrigger.OnUserLookedUp -= HandleOnLookUpComplete;
	}

	public void SetScanButton(bool loadGame)
	{
		_loadGame = loadGame;

		if (loadGame)
		{
			ScanButton.GetComponentInChildren<Text> ().text = "Scan (Load Game)";
		}
		else
		{
			ScanButton.GetComponentInChildren<Text> ().text = "Scan (New Game)";
		}

		LookTrigger.GetComponent<LookUpTrigger> ().IsEnabled = true;
		EnableScanButton ();
	}

	public void OnScanClick()
	{
		ScanButton.SetActive (false);
		BackButton.SetActive (true);

		ScreenMessageController.Instance.SetText ("Align center of screen with circles");

		_isScanning = true;

		StartCoroutine(MultiframeCallAsync());
	}
		
	public void OnBackClick()
	{
		// Cancels multiframe alignment
		if (_isScanning)
		{
			_resetCalled = true;
			ScanButton.SetActive (true);
			if (!GameManager.HasSaveData)
			{
				BackButton.SetActive (false);
			}
			ScreenMessageController.Instance.ClearText ();
		}
		else // Goes back to choose whether you would like to load game or start new game
		{
			ScanButton.SetActive (false);
			BackButton.SetActive (false);
			GameManager.Instance.HasSaveDataPanel.SetActive (true);
		}
	}

	public List<GameObject> GetGazeTargets()
	{
		return _gazeTargets;
	}

	private IEnumerator MultiframeCallAsync()
	{
		// Setup variables for starting multiframe requests
		SetupMultiframe();

		// Create gaze targets
		CreateGazeTargets();

		// Wait until all the requests are complete
		while (_multiframeRequestId != _totalTargets)
		{
			yield return null;
		}

		_isScanning = false;
		Cursor.SetActive(false);
		Arrow.SetActive (false);
	}

	private void SetupMultiframe()
	{
		_resetCalled = false;
		_gazeTargets = new List<GameObject>();
		Cursor.SetActive(true);
		_multiframeRequestId = 0;
		AddToMultiframeLocalizationCall();
	}

	private void CreateGazeTargets()
	{
		// Changed how gaze targets are placed from default 'MultiframeManager' script
		// Targets are placed in one direction, instead of to the left and right
		// Targets also appear one at a time, instead of both appearing at once

		for (int i = 1; i <= _targetCount; i++)
		{
			Vector3 dir = Quaternion.AngleAxis (_angle * i, Vector3.up) * (XRSessionManager.GetSession ().GetXRCameraOrientation () * Vector3.forward);

			var gaze = Instantiate (GazeTargetPrefab);
			gaze.transform.position = XRSessionManager.GetSession ().GetXRCameraPosition () + (dir * 10.0f);
			gaze.transform.LookAt (XrCamera.transform);
			gaze.name = "Gaze target (" + (_angle * i) + ")";
			gaze.SetActive (false);

			_gazeTargets.Add (gaze);
		}

		Arrow.SetActive (true);
		StartCoroutine(CheckGazeTargetHit(_gazeTargets[0]));
	}
		
	private IEnumerator CheckGazeTargetHit(GameObject target)
	{
		// Sets one gaze target active at a time
		_gazeTargets [_multiframeRequestId - 1].SetActive (true);
		Arrow.GetComponent<MultiframeSeekerArrow>().SetTarget (target.transform);

		RaycastHit hit;
		bool done = false;

		while (!done && !_resetCalled)
		{
			if (Physics.Raycast(XrCamera.transform.position, XrCamera.transform.forward, out hit, Mathf.Infinity, MultiframeLayerMask))
			{
				if (hit.transform.gameObject == target)
				{                    
					done = true;
				}
				else
				{
					yield return null;
				}
			}
			else
			{
				yield return null;
			}
		}

		if (!_resetCalled)
		{
			target.GetComponent<GazeTargetConfirmation> ().TargetAlignedSuccess ();
		}
		else
		{
			Destroy(target);
		}

		AddToMultiframeLocalizationCall();
	}

	private void AddToMultiframeLocalizationCall()
	{
		_multiframeRequestId++;

			//If reset is called, send -1 otherwise the count
			XRSessionManager.GetSession ().PerformLocalization ((_resetCalled) ? -1 : _totalTargets /*TargetCount*/);

		if (_multiframeRequestId > 1 && _multiframeRequestId < _totalTargets)
		{
			// Activate the next gaze target
			StartCoroutine (CheckGazeTargetHit (_gazeTargets [_multiframeRequestId - 1]));
		}
	}

	// Event called from LookUpTrigger
	private void HandleOnLookUpComplete()
	{
		_lookedUp = true;
		EnableScanButton ();
		LookTrigger.SetActive (false);
	}

	#region XR SESSION
	private void OnSessionReady()
	{
		_sessionReady = true;
		XRSessionManager.GetSession().CheckCoverage();
	}

	private void OnSessionFailed (string error)
	{
		ScreenMessageController.Instance.SetText("Session Intialization failed : " + error);
	}

	private void OnCoverageCheckComplete (bool result)
	{
		if (result == false)
		{
			ScreenMessageController.Instance.SetText ("Localization not available at this location\nLocalization calls won't work", 5);
		}
		else
		{
			ScreenMessageController.Instance.ClearText ();
			_coveredArea = true;
			if (!GameManager.Instance.HasSaveDataPanel.activeSelf)
			{
				EnableScanButton ();
			}
		}
	}
	#endregion

	#region LOCALIZATION
	private void OnLocalizationLoading()
	{
		ScreenMessageController.Instance.ClearText ();
		ScanAnimation.SetActive (true);
		BackButton.SetActive (false);
	}

	private void OnLocalizationComplete (Sturfee.Unity.XR.Core.Constants.Enums.AlignmentStatus status)
	{
		ScanAnimation.SetActive (false);

		if (status == Sturfee.Unity.XR.Core.Constants.Enums.AlignmentStatus.Done)
		{
			ScreenMessageController.Instance.SetText ("Camera Alignment Complete", 3);
			GameManager.Instance.InitializeGame ();
		}
		else
		{
			if (status == Sturfee.Unity.XR.Core.Constants.Enums.AlignmentStatus.OutOfCoverage)
			{
				ScreenMessageController.Instance.SetText("Localization not available at this location");
			}
			else if (status == Sturfee.Unity.XR.Core.Constants.Enums.AlignmentStatus.IndoorsError)
			{
				ScreenMessageController.Instance.SetText("Localization Failed: Indoors Error\nResetting Game");
				_midAlignmentError = true;
			}
			else if (status == Sturfee.Unity.XR.Core.Constants.Enums.AlignmentStatus.RequestError)
			{
				ScreenMessageController.Instance.SetText("Localization Failed: Request Error\nResetting Game");
				_midAlignmentError = true;
			}
			else if (status == Sturfee.Unity.XR.Core.Constants.Enums.AlignmentStatus.Error)
			{
				ScreenMessageController.Instance.SetText("Localization failed");
			}

			// Current SDK requires reset when these errors occur. Plan to fix this problem in future release.
			if (_midAlignmentError)
			{
				GameManager.Instance.ResetGame (3);
			}
			else
			{
				ResetToStartScreen ();
			}
		}
	}
	#endregion

	private void ResetToStartScreen()
	{
		if (GameManager.HasSaveData)
		{
			GameManager.Instance.OnSaveDataStartScreenClick (_loadGame);
		}
		else
		{
			TurnOnScanButton ();
		}
	}

	// Turn on Scan button if all prerequisites are met
	private bool EnableScanButton()
	{
		bool enableScan = !ScanButton.activeSelf && _sessionReady && _coveredArea && _lookedUp;
		if (enableScan)
		{
			TurnOnScanButton ();
		}
		return enableScan;
	}
		
	private void TurnOnScanButton()
	{
		ScanButton.SetActive (true);
		if (GameManager.HasSaveData)
		{
			BackButton.SetActive (true);
		}
	}
}
