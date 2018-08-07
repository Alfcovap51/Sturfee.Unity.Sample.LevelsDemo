using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Sturfee.Unity.XR.Core.Session;

// Used to initialize the map, and set boundaries for the full screen map camera
public class StreetMap : MonoBehaviour {

	public static StreetMap Instance;

	public AbstractMap Map;

	private int _numTilesFromCenter;	// # of tiles from center map tile to an edge tile. Assumes there is an equal amount of map tiles in every direction.

	private void Awake()
	{
		Instance = this;
		Map = GetComponent<AbstractMap> ();
	}
		
	private void Start () {
		_numTilesFromCenter = Map.Options.extentOptions.rangeAroundCenterOptions.north;
	}

	public void InitializeMap()
	{
		transform.position = XRSessionManager.GetSession ().GetXRCameraPosition ();
		transform.position += Vector3.down * 100;
	
		var gpsPos = XRSessionManager.GetSession ().GetLocationCorrection ();
		Vector2d gpsLatLongPos = new Vector2d (gpsPos.Latitude, gpsPos.Longitude);	

		Map.Initialize (gpsLatLongPos, 17);
	}

	public Vector3 GetMapCenterUnityCoord()
	{
		return transform.GetChild (0).position;
	}

	public float GetDistanceFromMapCenterToEdge()
	{
		// Child 0 is the center map square, Child 1 is the top left map square
		float mapSquareSize = Mathf.Abs ((transform.GetChild (0).position.x - transform.GetChild (1).position.x) / _numTilesFromCenter);
		return mapSquareSize * (_numTilesFromCenter + 0.5f);
	}
}
