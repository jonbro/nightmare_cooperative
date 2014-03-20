using UnityEngine;
using System.Collections;

public class HudComponent : MonoBehaviour {
	public GameObject heart;
	public GameObject potion;
	public GameObject portrait;
	public GameObject[] hearts;
	public GameObject[] potions;
	RLCharacter c;
	// Use this for initialization
	void Start () {
		int nHearts = 4;
		int nPotions = 5;
		hearts = new GameObject[nHearts];
		for (int i = 0; i < nHearts; i++) {
			if (i == 0)
				hearts [i] = heart;
			else{
				hearts [i] = (GameObject)Instantiate (heart, Vector3.zero, Quaternion.identity);
				hearts [i].transform.parent = transform;
				hearts [i].transform.position = heart.transform.position + Vector3.right * i;
				hearts [i].transform.localScale = heart.transform.localScale;
			}
		}
		potions = new GameObject[nPotions];
		for (int i = 0; i < nPotions; i++) {
			if (i == 0)
				potions [i] = potion;
			else{
				potions [i] = (GameObject)Instantiate (potion, Vector3.zero, Quaternion.identity);
				potions [i].transform.parent = transform;
				potions [i].transform.position = potion.transform.position + Vector3.right * i;
				potions [i].transform.localScale = potion.transform.localScale;
			}
		}
	}
	// Update is called once per frame
	void Update () {
	
	}
}
