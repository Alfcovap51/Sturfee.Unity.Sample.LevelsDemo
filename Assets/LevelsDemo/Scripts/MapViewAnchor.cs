using UnityEngine;

// Keeps player map related items rotating only with player camera's left/right movement
public class MapViewAnchor : MonoBehaviour {

	private Vector3 _rotation;

	private void Update () {
		_rotation = transform.parent.rotation.eulerAngles;
		_rotation.x = 0;
		_rotation.z = 0;
		transform.rotation = Quaternion.Euler (_rotation);
	}
}
