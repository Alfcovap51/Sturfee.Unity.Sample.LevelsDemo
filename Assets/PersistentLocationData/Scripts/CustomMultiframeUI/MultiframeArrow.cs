﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sturfee.Unity.XR.Core.Events;
using Sturfee.Unity.XR.Core.Session;

// TODO: Maybe change the name to something like 'Seeking/er Arrow'
public class MultiframeArrow : MonoBehaviour {

//	public Transform Arrow;
	public Camera XrCam;

//	private Image _arrowImage;
	private GameObject _arrow;
	private bool _isScanning;
	private Transform _curTarget;
	Vector3 _screenMiddle;
	private float _removeArrowDist;

	private void Awake()
	{
		_arrow = transform.GetChild (0).gameObject;
		SturfeeEventManager.Instance.OnRequestAddedForMultiframe += OnRequestAddedForMultiframe;
	}
		
	void Start () {
		

		//Get the middle of the screen into a Vector3
		_screenMiddle = new Vector3 (Screen.width / 2, Screen.height / 2, 0);
		_removeArrowDist = _screenMiddle.x / 2.5f;
		print ("!!! SCREEN MIDDLE: " + _screenMiddle.x + ", " + _screenMiddle.y);
	}

	private void OnDestroy()
	{
		SturfeeEventManager.Instance.OnRequestAddedForMultiframe -= OnRequestAddedForMultiframe;
	}

	void Update()
	{
		if (_isScanning)
		{
			print ("!!! SCANNING ACTIVE AND ARROW LOOKING FOR TARGET");

			//Get the targets position on screen into a Vector3
			Vector3 targetPos = XrCam.WorldToScreenPoint (_curTarget.position);


			//Compute the angle from _screenMiddle to targetPos
			var tarAngle = (Mathf.Atan2(targetPos.x-_screenMiddle.x,Screen.height-targetPos.y-_screenMiddle.y) * Mathf.Rad2Deg)+90;
	

			//Calculate the angle from the camera to the target
			var targetDir = _curTarget.position - XrCam.transform.position;
			var forward = XrCam.transform.forward;

			//			var angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
			var angle = Vector3.Angle(targetDir, forward);

			//If the angle exceeds 90deg inverse the rotation to point correctly
			if(Mathf.Abs(angle) > 90){


				transform.localRotation = Quaternion.Euler(0,0, -(tarAngle));
			} else {

				Vector3 PosDiff = _screenMiddle - targetPos;

				if ( (Mathf.Abs (PosDiff.x) < _removeArrowDist /*(_screenMiddle.x / 2)*/) && (Mathf.Abs (PosDiff.y) < _removeArrowDist /*(_screenMiddle.y / 2)*/) && targetPos.z > 0)
				{
					print ("!!! TURN OFF ARROW");
					_arrow.SetActive (false);

				}
				else
				{
					print ("!!! TURN ON ARROW");
					_arrow.SetActive (true);

					transform.localRotation = Quaternion.Euler (0, 0, tarAngle + 180);
				}
					
			}





		}

	}

	public void SetTarget(Transform target)
	{
		print ("!!! SET ARROW'S TARGET");
		_arrow.SetActive (true);
//		_arrowImage.enabled = true;
//		Arrow.gameObject.SetActive (true);
		_curTarget = target;
	}

	private void OnRequestAddedForMultiframe(int requestNum, int requestLength)
	{
		if(requestNum == 1)
		{
			//Multiframe scan started
			//			_centerYaw = XRSessionManager.GetSession().GetXRCameraOrientation().eulerAngles.y;
			_isScanning = true;                
		}


		if (requestNum == requestLength)
		{
			//Multiframe scan complete
			_isScanning = false;
		}
	}
}