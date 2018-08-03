using UnityEngine;
using UnityEngine.EventSystems;

// Controls movement of the full-screen-map-camera via player touch on the screen
public class PlayerMapTouchController : MonoBehaviour , IDragHandler, IBeginDragHandler{

	public float MapMoveSensitivity = 0.4f;

	[SerializeField]
	private Transform _fullScreenMapCam;
	[SerializeField]
	private GameObject _miniMapCam;
	[SerializeField]
	private Transform _player;
	[SerializeField]
	private GameObject _centerMapCamButton;

	private int _camBoundsUp;
	private int _camBoundsDown;
	private int _camBoundsRight;
	private int _camBoundsLeft;

	// Extent that the full-screen-map-camera sees vertically and horizontally
	private float _vertFullCamExtent;
	private float _horFullCamExtent;

	private Vector2 _prevTouchPos;

	private void Start () {
		ComputeCameraBounds ();
	}

	private void OnEnable()
	{
		_miniMapCam.SetActive (false);
		_fullScreenMapCam.gameObject.SetActive (true);
		_centerMapCamButton.SetActive (true);
		CenterMapCamOnPlayer ();
	}

	private void OnDisable()
	{
		_fullScreenMapCam.gameObject.SetActive (false);
		_miniMapCam.SetActive (true);
		_centerMapCamButton.SetActive (false);
	}

	public void OnBeginDrag(PointerEventData data)
	{
		_prevTouchPos = data.position;
	}

	public void OnDrag(PointerEventData data)
	{
		Vector2 touchDifference = (_prevTouchPos - data.position) * MapMoveSensitivity;

		Vector3 camPos = _fullScreenMapCam.position;
		camPos.x += touchDifference.x;
		camPos.z += touchDifference.y;

		// Keep the camera in the boundaries of the map
		if (camPos.x > _camBoundsRight)
		{
			camPos.x = _camBoundsRight;
		} 
		else if(camPos.x < _camBoundsLeft)
		{
			camPos.x = _camBoundsLeft;
		}
		else if(camPos.z > _camBoundsUp)
		{
			camPos.z = _camBoundsUp;
		}
		else if(camPos.z < _camBoundsDown)
		{
			camPos.z = _camBoundsDown;
		}

		_fullScreenMapCam.position = camPos;
		_prevTouchPos = data.position;
	}

	public void CenterMapCamOnPlayer()
	{
		// Center the map camera over the player
		Vector3 camPos = _player.position;
		camPos.y = transform.position.y;
		_fullScreenMapCam.transform.position = camPos;
	}

	private void ComputeCameraBounds()
	{
		_vertFullCamExtent = (_fullScreenMapCam.GetComponent<Camera> ().orthographicSize);
		_horFullCamExtent = (_vertFullCamExtent * Screen.width / Screen.height);

		Vector3 mapCenterCoord = StreetMap.Instance.GetMapCenterUnityCoord ();
		float distFromMapCenterToEdge = StreetMap.Instance.GetDistanceFromMapCenterToEdge ();

		_camBoundsUp = (int)(mapCenterCoord.z + distFromMapCenterToEdge - _vertFullCamExtent);
		_camBoundsDown = (int)(mapCenterCoord.z - distFromMapCenterToEdge + _vertFullCamExtent);
		_camBoundsRight = (int)(mapCenterCoord.x + distFromMapCenterToEdge - _horFullCamExtent);
		_camBoundsLeft = (int)(mapCenterCoord.x - distFromMapCenterToEdge + _horFullCamExtent);
	}

}
