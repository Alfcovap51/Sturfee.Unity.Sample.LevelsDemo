using UnityEngine;

public class Rotation : MonoBehaviour {

	public float Speed = 0.85f;

	void FixedUpdate () {
		transform.Rotate (Vector3.forward * Speed);
	}
}
