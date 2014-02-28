using UnityEngine;
using System.Collections;

public class DungeonCoalitionTitle : MonoBehaviour {
	public RLRender display;
	// Use this for initialization
	float counter;
	public GameObject gameRunner;
	void Start () {
		display.Setup (20, 16, 40, 32);
		counter = 0;
	}
	
	// Update is called once per frame
	void Update () {
		counter += Time.deltaTime;
		display.Console ("Dungeon Coalition", 0, 14);
		display.Console ("Arrows to Move\n and Melee", 0, 10);
		display.Console ("Space to Special\n and Start", 0, 7);

		int totalLength = 0;
		for (int i = 0; i < AllTogether.characterDefs.Length; i++) {
			totalLength += AllTogether.characterDefs [i].name.Length + 5;
		}
		int lastLength = 0;
		for (int i = 0; i < AllTogether.characterDefs.Length; i++) {
			int startPos = -(int)((counter * 4f)+lastLength);
			while (startPos+AllTogether.characterDefs[i].name.Length+1 < 0) {
				startPos += totalLength+4;
			}
			display.AssignTileFromOffset (startPos, 12, AllTogether.characterDefs [i].sx, AllTogether.characterDefs [i].sy, Color.white, Color.clear);
			display.Console (AllTogether.characterDefs [i].name, startPos+2, 12);
			lastLength += AllTogether.characterDefs [i].name.Length + 5;
		}
		if (Input.GetKeyDown (KeyCode.Space)) {
			gameObject.SetActive (false);
			gameRunner.SetActive (true);
		}
	}
}
