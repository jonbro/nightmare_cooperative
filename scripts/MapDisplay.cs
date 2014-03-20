using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RL;
// builds out a display of the map using the unity sprite system
public class MapDisplay : MonoBehaviour {
	GameObject[,] displayObjects;
	GameObject mapParent;
	public void HideMap(Map map){
		StartCoroutine (HideMapInternal (map));
	}
	IEnumerator HideMapInternal(Map map){
		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				StartCoroutine (ScaleInTime ((x + y) * 0.05f, displayObjects [x, y], 0));
			}
		}
		yield return new WaitForSeconds (1);
	}
	public void Build(Map map, bool animate = true){
		StartCoroutine (BuildInternal (map, animate));
	}
	public IEnumerator BuildInternal(Map map, bool animate = true){
		if (displayObjects == null) {
			displayObjects = new GameObject[map.sx, map.sy];
			mapParent = new GameObject ("map parent");
			mapParent.transform.parent = transform;
		} else if(animate) {
			for (int x = 0; x < map.sx; x++) {
				for (int y = 0; y < map.sy; y++) {
					StartCoroutine (ScaleInTime ((x + y) * 0.05f, displayObjects [x, y], 0));
				}
			}
			yield return new WaitForSeconds (1);
		}

		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				// if the map doesn't exist yet
				if (displayObjects [x, y] == null) {
					displayObjects [x, y] = new GameObject ();
					displayObjects [x, y].AddComponent<SpriteRenderer> ();
					displayObjects[x,y].transform.localScale = Vector3.zero;
					displayObjects[x,y].transform.localPosition = new Vector3 (x, y, 0);
					displayObjects[x,y].transform.parent = mapParent.transform;
				}
				// assign the shortcut and the order
				SpriteRenderer s = displayObjects [x, y].GetComponent<SpriteRenderer> ();
				s.sortingOrder = 0;
				s.color = Color.white;
				//put in the correct tiles
				switch(map.GetTile (x, y)) {
				case RL.TileType.WALL:
				case RL.TileType.HARD_WALL:
					s.sprite = GetSpriteWithName ("og_wallset_"+(calcIndexOryx(x, y, map)+6));
					break;
				case RL.TileType.STAIRS_DOWN:
					s.sprite = GetSpriteWithName ("stairsdown");
					break;
				case RL.TileType.GOBLET:
					s.sprite = GetSpriteWithName ("goblet");
					break;
				case RL.TileType.ACID:
					s.sprite = GetSpriteWithName ("acid");
					break;
				case RL.TileType.LAVA:
					s.sprite = GetSpriteWithName ("lava");
					break;
				default:
					s.sprite = GetSpriteWithName ("floor_0");
					if ((x + y) % 2 == 0)
						s.color = Color.white * 0.9f;
					break;
				}
				// make the screen layout good, and animate everything in
				if (animate) {
					StartCoroutine (ScaleInTime ((x + y) * 0.05f, displayObjects [x, y], 1));
				}else{
					displayObjects [x, y].transform.localScale = Vector3.one;
				}
			}
		}
		if (animate) {
			yield return new WaitForSeconds((map.sx+map.sy)*0.05f);
		}
	}
	IEnumerator ScaleInTime(float t, GameObject o, float scaleTo){
		yield return new WaitForSeconds (t);
		o.transform.scaleTo (0.25f, scaleTo);
	}
	Sprite[] sprites;
	Sprite GetSpriteWithName(string name){

		if(sprites == null)
			sprites = Resources.LoadAll<Sprite>("og_wallset");
		foreach (Sprite s in sprites) {
			if (s.name == name) {
				return s;
			}
		}
		return null;
	}
			
	// get the lookup table index for the sprite, based on its neighbors
	int calculateIndex(int x, int y, Map map){
		// calculate the binary index of the layer

		int i1 = (!map.IsValidTile(x+1, y)||map.IsOpenTile(x+1,y))?0:1;
		int i2 = (!map.IsValidTile(x,y-1)||map.IsOpenTile(x,y-1))?0:1;
		int i3 = (!map.IsValidTile(x-1,y)||map.IsOpenTile(x-1,y))?0:1;
		int i4 = (!map.IsValidTile(x,y+1)||map.IsOpenTile(x,y+1))?0:1;

		return (i4<<3) | (i3<<2) | (i2<<1) | (i1);
	}
	int calcIndexOryx(int x, int y, Map map){
		int o = calculateIndex (x, y, map);
		int count = 0;
		foreach (int i in oryxTranslation) {
			if (i == o)
				return count;
			count++;
		}
		return 0;
	}
	int[] oryxTranslation = {
		0,
		1,
		5,
		4,
		2,
		10,
		8,
		3,
		6,
		9,
		12,
		15,
		7,
		14,
		11,
		13
	};
}
