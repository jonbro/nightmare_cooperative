using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Facing
{
	DOWN,
	LEFT,
	RIGHT,
	UP,
}

public class GameController : MonoBehaviour
{
	enum floorType
	{
		OPEN,
		WALL,
		LAVA,
		CHANGE_LEFT,
		CHANGE_RIGHT,
		CHANGE_UP,
		CHANGE_DOWN
	}

	public Facing currentFacing;
	public Sprite playerU, playerL, playerR, playerD, stairsDown;
	public Sprite tl, tr, bl, br, l, r, t, b;
	public Sprite bulletH, bulletV, heart;
	public GameObject spriteHolder;
	RLCharacter player;
	public GameObject monster;

	delegate void EachObjectCallback (RLCharacter c);

	delegate void OverlapCallback (RLCharacter a,RLCharacter b);

	public List<RLCharacter> monsters = new List<RLCharacter> ();
	public List<RLCharacter> bullets = new List<RLCharacter> ();
	public List<RLCharacter> objects = new List<RLCharacter> ();
	RL.Map map;
	FsmSystem fsm;

	GameObject[] hearts;
	int currentHealth = 3;

	GameObject[] bulletDisplay;
	int currentBullets = 3;

	int currentLevel = 0;
	// Use this for initialization
	void Start ()
	{
		hearts = new GameObject[3];
		bulletDisplay = new GameObject[3];

		fsm = new FsmSystem ();
		fsm.AddState (new FsmState (FsmStateId.Player)
			.WithUpdateAction (PlayerUpdate)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Bullets)
		);
		fsm.AddState (new FsmState (FsmStateId.Bullets)
			.WithBeforeEnteringAction (BulletProcess)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Monster)
		);
		fsm.AddState (new FsmState (FsmStateId.Monster)
			.WithBeforeEnteringAction (MonsterProcess)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Player)
		);

		GenLevel ();

		// add the health display
		for (int i = 0; i < 3; i++) {
			bulletDisplay [i] = (GameObject)Instantiate (spriteHolder, new Vector3 (i+3, map.sy, 0), Quaternion.identity);
			bulletDisplay [i].GetComponent<SpriteRenderer> ().sprite = bulletV;
			bulletDisplay [i].GetComponent<SpriteRenderer> ().color = Color.grey;
			hearts [i] = (GameObject)Instantiate (spriteHolder, new Vector3 (i, map.sy, 0), Quaternion.identity);
			hearts [i].GetComponent<SpriteRenderer> ().sprite = heart;
			hearts [i].GetComponent<SpriteRenderer> ().color = Color.red;
		}

		// center the camera on the game
		Camera.main.transform.position = new Vector3 (map.sx / 2, map.sy / 2, -10);
	}
	void GenLevel(){
		currentLevel++;
		// clear the map and all current objects
		foreach (RLCharacter c in objects) {
			Destroy (c.gameObject);
		}
		objects.Clear ();

		// rebuild the map
		// build the map
		map = new RL.Map (14, 12);

		// build the walls based on the underlying tiles
		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				/*				
				 * assuming there is a sprite in the middle, here are the potential fills
				 * ...
				 * .x.
				 * ...
				 * 
				 * ...
				 * xxx
				 * ...
				 *
				 * .x.
				 * .x.
				 * .x.
				 *
				 * oxi // note, need to do two for each, depending on the direction of internal / external
				 * oxx
				 * ooo
				 * 
				 * ...
				 * .xx
				 * .x.
				 * 
				 * .x.
				 * xx.
				 * ...
				 * 
				 * what a pain in the butt. I am not going to fuck with this for right now, because it makes tons of assumptions
				 * about the map structure and room layout and stuff
				*/ 

				Sprite toInsert = tl;
				bool needsInsert = false;
				if (!map.IsOpenTile (x, y)) {
					toInsert = t;
					needsInsert = true;
				}
				if (needsInsert) {
					GameObject s = (GameObject)Instantiate (spriteHolder, new Vector3 (x, y, 0), Quaternion.identity);
					s.GetComponent<SpriteRenderer> ().sprite = toInsert;
					s.AddComponent<RLCharacter> ();
					objects.Add (s.GetComponent<RLCharacter>());
				}
			}
		}

		GameObject playerGO = (GameObject)Instantiate (spriteHolder, new Vector3 (4, 3, 0), Quaternion.identity);
		player = playerGO.AddComponent<RLCharacter> ();
//		player = playerGO.GetComponent<RLCharacter> ();
		player.GetComponent<SpriteRenderer> ().sprite = playerR;
		player.name = "player";
		objects.Add (player);
		currentFacing = Facing.RIGHT;

		// add a random number of monsters
		int nMonsters = currentLevel;
		for (int i = 0; i < nMonsters; i++) {
			GameObject m = (GameObject)Instantiate (monster, new Vector3 (Random.Range (1, map.sx - 1), Random.Range (1, map.sy - 1)), Quaternion.identity);
			m.GetComponent<RLCharacter> ().AddType (RLCharacter.RLTypes.MONSTER);
			objects.Add (m.GetComponent<RLCharacter> ());
		}

		// add some downward facing stairs
		GameObject stair = (GameObject)Instantiate (spriteHolder, new Vector3 (Random.Range (1, map.sx - 1), Random.Range (1, map.sy - 1)), Quaternion.identity);
		stair.GetComponent<SpriteRenderer> ().sprite = stairsDown;
		stair.AddComponent<RLCharacter> ();
		stair.GetComponent<RLCharacter> ().AddType (RLCharacter.RLTypes.STAIRS_DOWN);
		objects.Add (stair.GetComponent<RLCharacter> ());

	}
	void BulletProcess ()
	{
		EachObject ((b) => {
			b.SetPosition (b.positionI.x + RL.Map.nDir [(int)b.direction, 0], b.positionI.y + RL.Map.nDir [(int)b.direction, 1]);
		}, RLCharacter.RLTypes.BULLET);

		OverlapCheck ((b, m) => {
			objects.Remove (m);
			objects.Remove (b);
			Destroy (m.gameObject);
			Destroy (b.gameObject);
		}, RLCharacter.RLTypes.BULLET, RLCharacter.RLTypes.MONSTER);

		EachObject ((b) => {
			if (!map.IsOpenTile (b.positionI.x, b.positionI.y)) {
				objects.Remove (b);
				Destroy (b.gameObject);
			}
		}, RLCharacter.RLTypes.BULLET);

		fsm.PerformTransition (FsmTransitionId.Complete);
	}
	RL.Pathfinder pf = new RL.Pathfinder ();
	void MonsterProcess ()
	{
		// move the monsters at random
		//

		// move the monster towards the player
		EachObject ((m) => {
			List<Vector2i> foundPath = pf.FindPath (m.positionI, player.positionI, (x,y) => {
				return ContainsType(x, y, RLCharacter.RLTypes.MONSTER) || ContainsType(x, y, RLCharacter.RLTypes.STAIRS_DOWN)?10000:1;
			}, map);
			// only move the enemy if the path is at least 2 steps long (the start and end positions)
			if(foundPath.Count >= 2){
				Vector2i nextPosition = foundPath[foundPath.Count-2];
				if (map.IsValidTile (nextPosition.x, nextPosition.y) && !ContainsType(nextPosition.x, nextPosition.y, RLCharacter.RLTypes.MONSTER)) {
					if (player.positionI.x == nextPosition.x && player.positionI.y == nextPosition.y) {
						currentHealth--;
						if (currentHealth == 0) {
							// end game
							Debug.Log("Game Over");
						}
						if(currentHealth>=0)
							hearts [currentHealth].gameObject.SetActive (false);
					} else {
						m.SetPosition (nextPosition.x, nextPosition.y);
					}
				}
			}
		}, RLCharacter.RLTypes.MONSTER);

		// check for monster bullet overlap again
		OverlapCheck ((b, m) => {
			objects.Remove (m);
			objects.Remove (b);
			Destroy (m.gameObject);
			Destroy (b.gameObject);
		}, RLCharacter.RLTypes.BULLET, RLCharacter.RLTypes.MONSTER);

		fsm.PerformTransition (FsmTransitionId.Complete);
	}
	// Update is called once per frame
	void PlayerUpdate ()
	{
		bool shouldTransition = false;
		// should stop the player from making moves into enemies
		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			player.SetPosition (player.positionI.x, player.positionI.y - 1);
			currentFacing = Facing.DOWN;
			player.GetComponent<SpriteRenderer> ().sprite = playerD;
			shouldTransition = true;
		}
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			player.SetPosition (player.positionI.x, player.positionI.y + 1);
			currentFacing = Facing.UP;
			player.GetComponent<SpriteRenderer> ().sprite = playerU;
			shouldTransition = true;
		}
		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			player.SetPosition (player.positionI.x + 1, player.positionI.y);
			currentFacing = Facing.RIGHT;
			player.GetComponent<SpriteRenderer> ().sprite = playerR;
			shouldTransition = true;
		}
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			player.SetPosition (player.positionI.x - 1, player.positionI.y);
			currentFacing = Facing.LEFT;
			player.GetComponent<SpriteRenderer> ().sprite = playerL;
			shouldTransition = true;
		}
		if (Input.GetKeyDown (KeyCode.Space)) {
			if (currentBullets <= 0)
				return;
			currentBullets--;
			bulletDisplay [currentBullets].gameObject.SetActive (false);
			// fire current weapon
			GameObject bulletGO = (GameObject)Instantiate (spriteHolder, new Vector3 (player.positionI.x, player.positionI.y, 0), Quaternion.identity);
			RLCharacter bullet = bulletGO.AddComponent<RLCharacter> ();
			bullet.direction = currentFacing;
			// move the bullet out of the player
			if (bullet.direction == Facing.LEFT || bullet.direction == Facing.RIGHT)
				bullet.GetComponent<SpriteRenderer> ().sprite = bulletH;
			else
				bullet.GetComponent<SpriteRenderer> ().sprite = bulletV;
			bullet.name = "bullet";
			bullet.AddType (RLCharacter.RLTypes.BULLET);
			bullet.GetComponent<SpriteRenderer> ().color = Color.grey;
			objects.Add (bullet);
			// push the player away from the facing direction if possible
			Facing opposite = oppositeDirection (currentFacing);
			if (map.IsOpenTile (player.positionI.x + RL.Map.nDir [(int)opposite, 0], player.positionI.y + RL.Map.nDir [(int)opposite, 1]) &&
				!ContainsType(player.positionI.x + RL.Map.nDir [(int)opposite, 0], player.positionI.y + RL.Map.nDir [(int)opposite, 1], RLCharacter.RLTypes.MONSTER)) {
				player.SetPosition (player.positionI.x + RL.Map.nDir [(int)opposite, 0], player.positionI.y + RL.Map.nDir [(int)opposite, 1]);
			}
			shouldTransition = true;
		}
		// check to see if the player is on the stairs, and if so, regen the level
		if (ContainsType (player.positionI.x, player.positionI.y, RLCharacter.RLTypes.STAIRS_DOWN)) {
			GenLevel ();
		} else if (shouldTransition) {
			fsm.PerformTransition (FsmTransitionId.Complete);
		}
	}
	Facing oppositeDirection(Facing current){
		switch (current) {
		case Facing.DOWN:
			return Facing.UP;
		case Facing.UP:
			return Facing.DOWN;
		case Facing.RIGHT:
			return Facing.LEFT;
		case Facing.LEFT:
			return Facing.RIGHT;
		}
		return Facing.DOWN;
	}
	void Update ()
	{
		fsm.CurrentState.Update ();
	}
	bool ContainsType(int x, int y, RLCharacter.RLTypes t){
		for (int i = objects.Count - 1; i >= 0; i--) {
			if (objects[i].positionI.x == x && objects[i].positionI.y == y && objects [i].hasTypes.Contains (t)) {
				return true;
			}
		}
		return false;
	}
	// loop through the objects with a certain set of types, and process them
	// through a helper function
	void EachObject (EachObjectCallback fn, params RLCharacter.RLTypes[] validTypes)
	{
		// 
		if (objects.Count > 0) {
			for (int i = objects.Count - 1; i >= 0; i--) {
				if (validTypes.Length > 0) {
					foreach (RLCharacter.RLTypes t in validTypes) {
						if (objects.Count > i && objects [i].hasTypes.Contains (t)) {
							fn (objects [i]);
							break;
						}
					}
				} else {
					fn (objects [i]);
				}
			}
		}
	}
	// should add support for muliple types eventually
	// perhaps it would be better for types to nest themselves?
	// also, the map should really contain multiple layers,
	// all of which are expressed / exposed through these interfaces
	void OverlapCheck (OverlapCallback fn, RLCharacter.RLTypes ta, RLCharacter.RLTypes tb)
	{
		if (objects.Count > 0) {
			for (int i = objects.Count - 1; i >= 0; i--) {
				if (objects.Count > i && objects [i].hasTypes.Contains (ta)) {
					for (int j = i; j >= 0; j--) {
						if (objects.Count >= j && objects.Count >= i) {
							if (i != j && objects.Count > j && objects [j].hasTypes.Contains (tb)
							     && objects [i].positionI.Equals (objects [j].positionI)) {
								fn (objects [i], objects [j]);
							}
						}
					}
				}
			}
		}
	}
}
