using System.Collections.Generic;
using UnityEngine;
using Sturfee.Unity.XR.Core.Session;
using Sturfee.Unity.XR.Core.Models.Location;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

// Saves and loads the state of AR item data
// Used to remove and add data to the save data
// Data is automatically saved after AR Items are added or removed from the scene
public class SaveLoadManager : MonoBehaviour {

	public static List<ArItemData> ArItemList = new List<ArItemData>();

	private static string filename = "/saveData.dat";

	public static bool HasSaveData()
	{
		return (File.Exists (Application.persistentDataPath + filename));
	}

	public static void LoadGameData()
	{
		for (int i = 0; i < ArItemList.Count; i++)
		{
			// Convert serialized data back into usable data objects

			ArItemData arItemData = ArItemList [i];

			Quaternion quat = arItemData.Orientation.GetQuaternion;

			GpsSerializable gpsPos = arItemData.GpsPos;
			var gps = new GpsPosition
			{
				Latitude = gpsPos.Latitude,
				Longitude = gpsPos.Longitude,
				Height = gpsPos.Height
			};
			Vector3 pos = XRSessionManager.GetSession ().GpsToLocalPosition (gps);

			GameObject spawnItem;
			if (arItemData.ItemType == (int)ArItemType.level1)
			{
				spawnItem = GameManager.Instance.Level1ItemPrefab;
			}
			else if (arItemData.ItemType == (int)ArItemType.level2)
			{
				spawnItem = GameManager.Instance.Level2ItemPrefab;
			}
			else
			{
				spawnItem = GameManager.Instance.Level3ItemPrefab;
			}

			GameObject arItem = Instantiate (spawnItem, pos, quat);
			arItem.GetComponent<ArItem> ().Id = arItemData.Id;
		}
	}

	public static void SaveArItem(Transform arItem)
	{
		// Convert the AR item's Unity data into serializable format that can be saved

		ArItemData arItemData = new ArItemData ();

		arItemData.ItemType = (int) arItem.GetComponent<ArItem>().ItemType;

		QuaternionSerializable newQuat = new QuaternionSerializable ();
		newQuat.Fill (arItem.rotation);
		arItemData.Orientation = newQuat;

		var rawGpsPos = XRSessionManager.GetSession ().LocalPositionToGps (arItem.position);
		GpsSerializable gpsPos = new GpsSerializable ();
		gpsPos.Height = rawGpsPos.Height;
		gpsPos.Latitude = rawGpsPos.Latitude;
		gpsPos.Longitude = rawGpsPos.Longitude;
		arItemData.GpsPos = gpsPos;

		// Create (almost) unique ID for this AR item
		string itemId = arItem.GetInstanceID().ToString() + "." + rawGpsPos.Latitude.ToString() + "." + rawGpsPos.Longitude.ToString();
		arItemData.Id = itemId;
		arItem.GetComponent<ArItem> ().Id = itemId;

		Debug.Log ("Added AR item to save data");
		ArItemList.Add (arItemData);
		Save ();
	}

	public static void RemoveArItem(string arItemId)
	{
		bool foundItem = false;
		int count = 0;
		while (!foundItem && count < ArItemList.Count)
		{
			if (ArItemList [count].Id.Equals(arItemId))
			{
				ArItemList.RemoveAt (count);
				foundItem = true;
				Debug.Log ("Removed AR item from save data");
			}
			count++;
		}

		if (!foundItem)
		{
			Debug.LogError ("Could not find removed AR item's ID in save data. Nothing was removed from save data.");
		}
		else
		{
			Save ();
		}
	}

	public static void Load() {
		if(File.Exists(Application.persistentDataPath + filename))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + filename, FileMode.Open);
			ArItemList = (List<ArItemData>)bf.Deserialize(file);
			file.Close();

			Debug.Log ("Loaded Game Data");
		}
	}

	public static void Unload()
	{
		ArItemList = new List<ArItemData>();
	}

	private static void Save() {
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create (Application.persistentDataPath + filename);
		bf.Serialize(file, ArItemList);
		file.Close();

		Debug.Log ("Saved Game Data");
	}
}

[Serializable]
public struct ArItemData
{
	public string Id;
	public int ItemType;
	public GpsSerializable GpsPos;
	public QuaternionSerializable Orientation;
}

[Serializable]
public struct GpsSerializable 
{
	public double Latitude;
	public double Longitude;
	public double Height;
}

[Serializable]
public struct QuaternionSerializable
{
	public float x;
	public float y;
	public float z;
	public float w;

	public void Fill(Quaternion q)
	{
		x = q.x;
		y = q.y;
		z = q.z;
		w = q.w;
	}

	public Quaternion GetQuaternion { get { return new Quaternion (x, y, z, w); } }	
}

