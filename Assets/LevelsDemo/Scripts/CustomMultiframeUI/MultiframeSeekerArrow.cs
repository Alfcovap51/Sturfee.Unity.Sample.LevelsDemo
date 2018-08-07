using UnityEngine;
using UnityEngine.UI;
using Sturfee.Unity.XR.Core.Events;

// 2D UI arrow that points directly towards targets
public class MultiframeSeekerArrow : MonoBehaviour {

	public Camera XrCam;

	private GameObject _arrow;
	private bool _isScanning;
	private Transform _curTarget;		// Target that the arrow is currently pointing at
	Vector3 _screenMiddle;
	private float _removeArrowDist;		// Screen distance from the target center upon which the arrow should appear/disappear

	private void Awake()
	{
		_arrow = transform.GetChild (0).gameObject;
		SturfeeEventManager.Instance.OnRequestAddedForMultiframe += OnRequestAddedForMultiframe;
	}
		
	void Start () {
		_screenMiddle = new Vector3 (Screen.width / 2, Screen.height / 2, 0);
		_removeArrowDist = _screenMiddle.x / 2.5f;
	}

	private void OnDestroy()
	{
		SturfeeEventManager.Instance.OnRequestAddedForMultiframe -= OnRequestAddedForMultiframe;
	}

	void Update()
	{
		if (_isScanning)
		{
			Vector3 targetPos = XrCam.WorldToScreenPoint (_curTarget.position);

			// Compute the angle from center of screen to the target's position
			var tarAngle = (Mathf.Atan2(targetPos.x - _screenMiddle.x, Screen.height - targetPos.y - _screenMiddle.y) * Mathf.Rad2Deg) + 90;
	
			// Calculate the angle from the camera to the target
			var targetDir = _curTarget.position - XrCam.transform.position;
			var forward = XrCam.transform.forward;
			var angle = Vector3.Angle(targetDir, forward);

			// If the angle exceeds 90 degrees, inverse the rotation to point correctly
			if(Mathf.Abs(angle) > 90){
				transform.localRotation = Quaternion.Euler(0,0, -(tarAngle));
			} 
			else
			{
				Vector3 PosDiff = _screenMiddle - targetPos;

				// If center of screen is close to target, turn arrow off, so it doesn't overlap with the target image
				if ((Mathf.Abs (PosDiff.x) < _removeArrowDist) && (Mathf.Abs (PosDiff.y) < _removeArrowDist) && targetPos.z > 0)
				{
					_arrow.SetActive (false);
				}
				else
				{
					_arrow.SetActive (true);
					transform.localRotation = Quaternion.Euler (0, 0, tarAngle + 180);
				}
			}
		}
	}

	public void SetTarget(Transform target)
	{
		_arrow.SetActive (true);
		_curTarget = target;
	}

	private void OnRequestAddedForMultiframe(int requestNum, int requestLength)
	{
		if(requestNum == 1)
		{
			_isScanning = true;                
		}
		else if (requestNum == requestLength)
		{
			_isScanning = false;
		}
	}
}
