using UnityEngine;
using UnityEngine.UI;

public class ScreenMessageController : MonoBehaviour {

	public static ScreenMessageController Instance;

	private Text _text;
	private int _numWaitingMessages = 0;
	private bool _continuousText = false;

	private void Awake () {
		Instance = this;
		_text = GetComponent<Text> ();
	}

	// Shows text at bottom of screen for designated time period, or infinitely if none specified
	public void SetText(string text, float time = 0)
	{
		_text.text = text;

		if (time > 0)
		{
			_continuousText = false;
			_numWaitingMessages++;
			Invoke ("TimedClear", time); 
		}
		else
		{
			_continuousText = true;
		}
	}

	public void ClearText()
	{
		_text.text = "";
	}	

	private void TimedClear()
	{
		if (_numWaitingMessages <= 1)
		{
			_numWaitingMessages = 0;

			if (!_continuousText)
			{
				_text.text = "";
			}
		}
		else
		{
			_numWaitingMessages--;
		}
	}
}
