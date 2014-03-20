using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class HudParentComponent : MonoBehaviour {

	public List<RLCharacter> characters;

	public TextMesh levelDisplay;
	public GameObject hud;
	GameObject[] huds;
	// Use this for initialization
	void Start () {
		// spawn the four subhuds
		huds = new GameObject [4];
		for (int i = 0; i < 4; i++) {
			huds[i] = (GameObject)Instantiate (hud, new Vector3 (0, i*2, 0), Quaternion.identity);
			huds [i].transform.parent = transform;
			huds [i].transform.localPosition = new Vector3 (0, -i * 2, 0);
		}

	}

	Dictionary<string, Sprite> sprites;
	public Sprite GetSprite(string spriteName){
		if (sprites == null) {
			Sprite[] spriteArray = Resources.LoadAll<Sprite> ("sprites");
			sprites = new Dictionary<string, Sprite> ();
			foreach (Sprite s in spriteArray) {
				sprites [s.name] = s;
			}
		}
		if(sprites.ContainsKey(spriteName))
			return sprites[spriteName];
		return null;
	}

	// Update is called once per frame
	void Update () {
		for(int i=0;i<4;i++){
			if (i > characters.Count - 1)
				huds [i].SetActive (false);
			else {
				huds [i].SetActive (true);

				RLCharacter c = characters [i];
				HudComponent hc = huds [i].GetComponent<HudComponent> ();
				hc.portrait.GetComponent<SpriteRenderer> ().sprite = GetSprite (c.name);
				for (int h = 0; h < 4; h++) {
					if (c.health > h)
						hc.hearts [h].GetComponent<SpriteRenderer> ().sprite = GetSprite ("health");
					else
						hc.hearts [h].GetComponent<SpriteRenderer> ().sprite = GetSprite ("health_empty");
				}
				for (int h = 0; h < 5; h++) {
					if (c != null && c.GetComponent<ActionCounter>().actionsRemaining > h)
						hc.potions [h].GetComponent<SpriteRenderer> ().sprite = GetSprite ("action");
					else
						hc.potions [h].GetComponent<SpriteRenderer> ().sprite = GetSprite ("action_empty");
				}
			}
		}
	}
}
