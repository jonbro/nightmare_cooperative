using UnityEngine;
using System.Collections;

public class DungeonCoalitionTitle : MonoBehaviour {
	public RLRender display;
	// Use this for initialization
	float counter;
	public GameObject gameRunner;
	public AudioSource menuMusic;
	void Start () {
		display.Setup (20, 16, 58, 40);
		StartMenu ();
	}
	public void StartMenu(){
		counter = 0;
		menuMusic.GetComponent<FadeLoop> ().FadeTo (1, 2);
	}
	// Update is called once per frame
	void Update () {
		counter += Time.deltaTime;
//		display.Console ("The Nightmare\nCooperative", 0, 14);
//		display.Console ("Arrows to Move\n and Melee", 0, 9);
//
//		display.Console ("Space to Special\n and Start", 0, 6);
//
//		int totalLength = 0;
//		for (int i = 0; i < AllTogether.characterDefs.Length; i++) {
//			totalLength += AllTogether.characterDefs [i].name.Length + 5;
//		}
//		int lastLength = 0;
//		for (int i = 0; i < AllTogether.characterDefs.Length; i++) {
//			int startPos = -(int)((counter * 4f)+lastLength);
//			while (startPos+AllTogether.characterDefs[i].name.Length+1 < 0) {
//				startPos += totalLength+4;
//			}
//			display.AssignTileFromOffset (startPos, 11, AllTogether.characterDefs [i].sx, AllTogether.characterDefs [i].sy, Color.white, Color.clear);
//			display.Console (AllTogether.characterDefs [i].name, startPos+2, 11);
//			lastLength += AllTogether.characterDefs [i].name.Length + 5;
//		}
		if (Input.GetKeyDown (KeyCode.Space)) {
			menuMusic.GetComponent<FadeLoop> ().FadeTo (0, 2);
			gameObject.SetActive (false);
			gameRunner.SetActive (true);
			gameRunner.GetComponent<AllTogether> ().InitialGen ();
		}
	}
}
