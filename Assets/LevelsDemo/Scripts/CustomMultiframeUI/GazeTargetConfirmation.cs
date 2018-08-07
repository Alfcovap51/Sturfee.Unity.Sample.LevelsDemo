using UnityEngine;
using UnityEngine.UI;

// Handles visuals of gaze target alignment confirmation
public class GazeTargetConfirmation : MonoBehaviour {

	[SerializeField]
	private GameObject _alignmentRing;
	[SerializeField]
	private GameObject _confirmationCircle;
	[SerializeField]
	private GameObject _checkmark;

	private Image _confirmationCircleImage;
	private Color _confirmationCircleColor;

	void Start () {
		_confirmationCircleImage = _confirmationCircle.GetComponent<Image> ();
		_confirmationCircleColor = _confirmationCircleImage.color;
	}

	public void TargetAlignedSuccess()
	{
		GetComponent<Collider> ().enabled = false;
		_alignmentRing.SetActive (false);
		_confirmationCircle.SetActive (true);
		_checkmark.SetActive (true);

		Fade ();
	}
		
	private void Fade()
	{
		_confirmationCircleColor.a -= 0.02f;
		_confirmationCircleImage.color = _confirmationCircleColor;

		if (_confirmationCircleColor.a > 0)
		{
			Invoke ("Fade", 0.02f);
		}
		else
		{
			Destroy (this.gameObject);
		}
	}
}
