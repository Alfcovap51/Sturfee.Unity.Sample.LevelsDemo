using UnityEngine;

// Holds data for saving, removing, and loading this AR item.
public class ArItem: MonoBehaviour{

	[HideInInspector]
	public string Id;
	public ArItemType ItemType; 
}

public enum ArItemType
{
	level1,
	level2,
	level3
}
