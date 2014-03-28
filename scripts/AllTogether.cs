using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ColorLerp{
	public Color c;
	public float timeElapsed, startTime, totalTime;
	public float amt{
		get{ 
			return Mathf.Max(0, 1.0f-(Time.time - startTime) / totalTime);
		}
	}
	public ColorLerp(Color _c, float _t){
		c = _c;
		totalTime = _t;
		startTime = Time.time;
	}
}

public class AllTogether : MonoBehaviour {
	public RLRender display, mapDisplay;
	// the player characters
	List<RLCharacter> characters = new List<RLCharacter>();
	List<RLCharacter> exitCharacters = new List<RLCharacter>();
	List<RLCharacter> charactersJoined = new List<RLCharacter>();
	List<RLCharacter> monsters;
	List<RLCharacter> pickups;

	RLCharacter player;
	FsmSystem fsm;
	RL.Map map;
	RL.Pathfinder pf = new RL.Pathfinder();
	RLCharacter[,] characterMap;
	RLCharacter[,] monsterMap;
	ColorLerp[,] mapColors;// = new ColorLerp[14,12];
	ColorLerp[,] mapColorsForeground;// = new ColorLerp[14,12];

	RLCharacter warrior, archer;
	public AudioSource archerAudio, mageAudio, WarriorAudio, priestAudio, minerAudio;
	public AudioClip archerActionAudio, mageActionAudio, WarriorActionAudio, priestActionAudio, minerActionAudio,
		playerExitAudio, meleeAudio, healthPickupAudio, weaponPickupAudio, playerHurtAudio, playerDeathAudio,
		playerAddAudio;
	public Color floorC, wallC, WallCTop;
	public int wallTopIndex;
	public GameOver gameOver;
	public DungeonCoalitionTitle titleScreen;
	public int goldCount = 0;
	public TextMesh console;
	public Color mageColor, warriorColor, minerColor, archerColor, priestColor, monsterColor;
	public HudParentComponent hud;
	static AllTogether _instance;
	int lastPlayerMove;
	public static AllTogether instance(){
		if (_instance == null)
			_instance = GameObject.Find ("AllTogetherGame").GetComponent<AllTogether>();
		return _instance;
	}
	static public int[,] levelMonsters = {
		{4, 4, 4, 5, 5},
		{6, 1, 6, 0, 1},
		{6, 9, 1, 7, 7},
		{1, 2, 2, 2, 1},
		{1, 2, 3, 9, 1},
		{3, 2, 5, 3, 9},

		{9, 9, 4, 3, 4},
		{2, 2, 4, 3, 4},
		{3, 2, 5, 3, 4},
		{3, 2, 5, 3, 4},
		{3, 2, 5, 3, 4},

	};
	int[,] wallRange = { 
		{18, 24},
		{14, 23},
		{15, 22},
		{16, 21},
		{17, 20},

		{14, 20},
		{14, 20},
		{14, 20},
		{14, 20},
		{14, 20},
	};
	int[,] monsterRange = {
		{3, 5},
		{3, 5},
		{3, 5},
		{3, 5},
		{3, 5},
		 
		{3, 5},
		{3, 5},
		{3, 5},
		{4, 5},
		{4, 5},
	};

	int currentLevel = 0;
	int turnCount = 0;
	int mapSizeX, mapSizeY;
	bool wonGame = false;
	// try to get across the screen without having any of your characters dying
	void Awake(){
//		hud = transform.Find ("hudParent").GetComponent<HudParentComponent>();
		mapSizeX = 11;
		mapSizeY = 11;
		Camera.main.orthographicSize = (mapSizeX+1) / 2.0f;
		Camera.main.transform.position = new Vector3(((mapSizeX-1)*1.7f)/2.0f, (mapSizeY-1)/2.0f, -10);
		mapColors = new ColorLerp[mapSizeX, mapSizeY];
		mapColorsForeground = new ColorLerp[mapSizeX, mapSizeY];

		for (int x = 0; x < mapSizeX; x++) {
			for (int y = 0; y < mapSizeY; y++) {
				mapColors [x, y] = new ColorLerp (Color.white, 0);
			}
		}
		fsm = new FsmSystem ();
		fsm.AddState(new FsmState (FsmStateId.InitialGen)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Player)
		);
		fsm.AddState (new FsmState (FsmStateId.Player)
			.WithUpdateAction (PlayerUpdate)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Monster)
			.WithTransition (FsmTransitionId.Rebuild, FsmStateId.InitialGen)
			.WithTransition (FsmTransitionId.GameOver, FsmStateId.GameOver)
		);
		fsm.AddState (new FsmState (FsmStateId.GameOver)
			.WithBeforeEnteringAction (GameOverStart)
			.WithUpdateAction (GameOverUpdate)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.InitialGen)
		);
		fsm.AddState (new FsmState (FsmStateId.Monster)
			.WithBeforeEnteringAction (MonsterProcess)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Player)
		);

	}
	void Start () {

	}
	public void InitialGen(){
		hud.gameObject.SetActive (true);
		turnCount = Random.Range (10, 100);
		console.gameObject.SetActive (true);
		consoleArray.Clear ();
		ConsoleAdd ("");
		display.Setup (21, 16, 58, 40);
		mapDisplay.Setup (21, 16, 58, 40);
		currentLevel = 0;
		goldCount = 0;
		wonGame = false;
		charactersJoined.Clear ();
		// kill all characters
		foreach (RLCharacter c in exitCharacters) {
			c.FinalKillNoAudio ();
		}
		foreach (RLCharacter c in characters) {
			c.FinalKillNoAudio();
		}
		if (monsters != null) {
			foreach (RLCharacter c in monsters) {
				Destroy(c.gameObject);
			}
			foreach (RLCharacter c in pickups) {
				Destroy (c.gameObject);
			}
		}
		exitCharacters.Clear ();
		characters.Clear ();
		// setup the reference from the hud
		hud.characters = characters;

		// should potentially have more character archetypes later on
		List<MonsterDef> characterDefShuffled = ShuffleArray<MonsterDef>(MonsterDef.characterDefs);
		// only player types that aren't already in the party
		for (int i = 0; i < 2; i++) {
			MonsterDef cdef = characterDefShuffled [i];
			RLCharacter c = CreatePlayerCharacter (CreateCharacter (i, 1, 'W', RLCharacter.RLTypes.PLAYER), cdef);
			exitCharacters.Add (c);
			charactersJoined.Add (c);
		} 

		GenLevel ();
		// clear the lerp colors
		for (int x = 0; x < mapSizeX; x++) {
			for (int y = 0; y < mapSizeY; y++) {
				mapColors [x, y] = new ColorLerp (Color.black, (Random.value+1.0f)*1.75f);
			}
		}
		// clear the lerp colors
		for (int x = 0; x < mapSizeX; x++) {
			for (int y = 0; y < mapSizeY; y++) {
				mapColorsForeground [x, y] = new ColorLerp (Color.clear, (Random.value+1.0f)*1.75f);
			}
		}
		fsm.PerformTransition(FsmTransitionId.Complete);
	}
	static List<T> ShuffleArray<T>(T[] Input){
		// shuffle the list of directions
		List<T> ArrayStart = new List<T>();
		List<T> ArrayEnd = new List<T>();
		for (int i = 0; i < Input.Length; i++) {
			ArrayStart.Add(Input[i]);
		}
		while (ArrayStart.Count > 0) {
			int counter = Random.Range (0, ArrayStart.Count);
			ArrayEnd.Add (ArrayStart [counter]);
			ArrayStart.RemoveAt (counter);
		}
		return ArrayEnd;
	}

	Vector2i GetPositionAtDistanceFromCharactersAndMonsters(int maxX, int maxY, int distanceFromCharacter, int distanceFromMonster=1){
		int bailcount = 0;
		while (true && bailcount < 200) {
			int apx = Random.Range (1, maxX);
			int apy = Random.Range (1, maxY);
			bool nearCharacter = false;
			for (int x = apx - distanceFromCharacter; x < apx + distanceFromCharacter; x++) {
				for (int y = apy - distanceFromCharacter; y < apy + distanceFromCharacter; y++) {
					if (map.IsValidTile(x, y) && characterMap [x, y] != null) {
						nearCharacter = true;
					}
				}
			}
			for (int x = apx - distanceFromMonster; x < apx + distanceFromMonster; x++) {
				for (int y = apy - distanceFromMonster; y < apy + distanceFromMonster; y++) {
					if (map.IsValidTile(x, y) && monsterMap [x, y] != null) {
						nearCharacter = true;
					}
				}
			}
			if (!nearCharacter && apx != mapSizeX-2 && apy != mapSizeY-2) {
				return new Vector2i (apx, apy);
			}
			bailcount++;
		}
		return new Vector2i (1, 1);
	}

	void GenLevel(){
		map = new RL.Map (mapSizeX, mapSizeY);
		int characterCount = 1;
		characters.AddRange (exitCharacters);
		characterMap = new RLCharacter[mapSizeX, mapSizeY];
		monsterMap = new RLCharacter[mapSizeX, mapSizeY];

		// clear the lerp colors
		for (int x = 0; x < mapSizeX; x++) {
			for (int y = 0; y < mapSizeY; y++) {
				mapColors [x, y] = new ColorLerp (Color.white, 0);
			}
		}

		foreach (RLCharacter c in characters) {
			Vector2i cpos = GetPositionAtDistanceFromCharactersAndMonsters (5, 5, 1);
			c.SetPositionImmediate (cpos.x, cpos.y);
			characterMap [cpos.x, cpos.y] = c;
			c.Hide ();
			characterCount++;
		}
		exitCharacters.Clear ();
		int bailCount = 0;
		while (true && bailCount < 1000) {
			monsterMap = new RLCharacter[mapSizeX, mapSizeY];
			if (monsters != null) {
				foreach (RLCharacter c in monsters) {
					c.FinalKill ();
				}
				foreach (RLCharacter c in pickups) {
					c.FinalKill ();
				}
			}
			// gen map, add some random walls, and check for walkability
			map = new RL.Map (mapSizeX, mapSizeY);
			int wallCount = Random.Range (wallRange[currentLevel, 0], wallRange[currentLevel, 1]);
			for (int i = 0; i < wallCount; i++) {
				while (true) {
					int wx = Random.Range (1, map.sx-1);
					int wy = Random.Range (1, map.sy-1);
					if(characterMap[wx, wy] == null){
						map.SetTile (wx, wy, RL.TileType.WALL);
						break;
					}
				}
			}
			// clear the two player positions
			map.SetTile (1, 1, RL.TileType.OPEN);
			map.SetTile (2, 1, RL.TileType.OPEN);
			map.SetTile (mapSizeX-2, mapSizeY-2, RL.TileType.STAIRS_DOWN);

			if (currentLevel == 0) {
				map.SetTile (mapSizeX-2, mapSizeY-2, RL.TileType.GOBLET);
			}

			pickups = new List<RLCharacter> ();
			monsters = new List<RLCharacter> ();

			// add a few action point pickups around the map
			int actionPointCount = Random.Range (1, 3);
			for (int i = 0; i < actionPointCount; i++) {
				Vector2i app = GetPositionAtDistanceFromCharactersAndMonsters (mapSizeX-2, mapSizeY-2, 2);
				map.SetTile (app.x, app.y, RL.TileType.OPEN);
				pickups.Add(CreateCharacter (app.x, app.y, 'a', RLCharacter.RLTypes.ACTION_PICKUP, MonsterDef.pickupDefs[0]));
				pickups [pickups.Count - 1].HideImmediate ();
			}
			// add some health pickups
			actionPointCount = Random.Range (1, 2);
			for (int i = 0; i < actionPointCount; i++) {
				Vector2i app = GetPositionAtDistanceFromCharactersAndMonsters (mapSizeX-2, mapSizeY-2, 2);
				map.SetTile (app.x, app.y, RL.TileType.OPEN);
				pickups.Add(CreateCharacter (app.x, app.y, 'h', RLCharacter.RLTypes.HEALTH_PICKUP,  MonsterDef.pickupDefs[1]));
				pickups [pickups.Count - 1].HideImmediate ();
			}
			// spawn some gold you can pickup
			actionPointCount = Random.Range (1, 4);
			for (int i = 0; i < actionPointCount; i++) {
				// spawn in upper left or lower right

				Vector2i app = Random.value>0.5f?new Vector2i(Random.Range(1, mapSizeX/3), Random.Range(mapSizeY/3*2, mapSizeY-1)):new Vector2i(Random.Range(mapSizeX/3*2, mapSizeX-1), Random.Range(1, mapSizeY/3));
				map.SetTile (app.x, app.y, RL.TileType.OPEN);
				pickups.Add(CreateCharacter (app.x, app.y, 'g', RLCharacter.RLTypes.GOLD_PICKUP,  MonsterDef.pickupDefs[4]));
				pickups [pickups.Count - 1].HideImmediate ();
				// chance of spawning a guard moster
				if (Random.Range (0, 2) == 0) {
					// pick a random direction, and spawn the guard in that direction if it is within the level
					List<int[]> dirArrayEnd = ShuffledDirections ();
					app += new Vector2i(dirArrayEnd[0]);

					if (map.IsValidTile (app.x, app.y) && map.GetTile(app.x, app.y)!=RL.TileType.HARD_WALL) {
						AddMonsterAtPosition (app.x, app.y, MonsterDef.monsterDefs[8]);
						map.SetTile (app.x, app.y, RL.TileType.OPEN);
					}
				}
			}

			// every three levels, except for the first level, spawn in an extra party member that you can add
			if ((currentLevel+1) % 2 == 0 && characters.Count < 4 && currentLevel != 0) {
				// shuffle the character defs
				List<MonsterDef> characterDefShuffled = ShuffleArray<MonsterDef>(MonsterDef.characterDefs);
				// only player types that aren't already in the party
				foreach (MonsterDef cdef in characterDefShuffled) {
					bool foundType = false;
					foreach (RLCharacter c in characters) {
						if (c.hasTypes.Contains (cdef.moveType)) {
							foundType = true;
						}
					}
					if (!foundType) {
						int apx = Random.Range (1, map.sx-1);
						int apy = Random.Range (1, map.sy-1);
						map.SetTile (apx, apy, RL.TileType.OPEN);
						RLCharacter newChar = CreatePlayerCharacter (CreateCharacter (apx, apy, 'W', RLCharacter.RLTypes.WAITING_PLAYER), cdef);
						// start the new characters out a little bit lower than the older characters
						newChar.SetWating ();
						newChar.GetComponent<ActionCounter> ().actionsRemaining--;
						newChar.health--;
						pickups.Add(newChar);
						newChar.HideImmediate ();
						break;
					}
				}
			}
			bool hasPath = true;
			foreach (RLCharacter c in characters) {
				List<Vector2i> path = pf.FindPath (c.positionI, new Vector2i (mapSizeX-2, mapSizeY-2), (x, y) => {
					return 1;
				}, map);
				hasPath = hasPath && path.Count > 0 && path [path.Count - 1].Equals (new Vector2i (mapSizeX-2, mapSizeY-2));
			}
			// check paths from both of the player characters to the exit
			if (hasPath) {
				break;
			}
			bailCount++;
		}
		// add some random monsters to the map
		int monsterCount = Random.Range (LevelDef.levels[currentLevel].spawnCountLow, LevelDef.levels[currentLevel].spawnCountHigh);
		monsterCount -= monsters.Count;
		for (int i = 0; i < monsterCount; i++) {
			SpawnMonster ();
		}
		map.SetTile (mapSizeX-2, mapSizeY-2, RL.TileType.STAIRS_DOWN);
		if (currentLevel == 9) {
			map.SetTile (mapSizeX-2, mapSizeY-2, RL.TileType.GOBLET);
		}

		currentLevel++;
		StartCoroutine (FinishGen ());
	}
	IEnumerator FinishGen(){
		yield return StartCoroutine(GetComponent<MapDisplay> ().BuildInternal (map));
		// fade in all the characters
//		Debug.Break ();
		Debug.Log ("should be fading in characters");
		foreach (RLCharacter c in characters) {
			c.Show ();
		}
		foreach (RLCharacter c in monsters) {
			c.Show ();
		}
		foreach (RLCharacter c in pickups) {
			c.Show ();
		}
	}
	RLCharacter CreateCharacter(int px, int py, char display, RLCharacter.RLTypes t, MonsterDef m=null){
		GameObject go = new GameObject();
		RLCharacter rlc = go.AddComponent<RLCharacter> ();
		go.transform.parent = transform;
		if (m != null) {
			rlc.AddSprite (m.name);
			go.name = m.name;
		}
		rlc.display = display;
		rlc.AddType (t);
		rlc.health = 3;
		rlc.SetPositionImmediate (px, py);
		return rlc;
	}
	void SpawnMonster(bool fadeInImmediate = false){
		int bailCount = 0;
		// pick a monster type to spawn
		Debug.Log (currentLevel);
		MonsterDef mdef = LevelDef.levels[Mathf.Min(currentLevel, LevelDef.levels.Length-1)].GetMonsterForLevel();
		while (true && bailCount<1000) {
			// look for an open position to put the monster in that has a path to the player character
			Vector2i monsterPosition = GetPositionAtDistanceFromCharactersAndMonsters (mapSizeX-2, mapSizeY-2, 2);
			List<Vector2i> path = pf.FindPath (new Vector2i (1, 1), monsterPosition, (x, y) => {
				return 1;
			}, map);
			if ((mdef.isPassable && map.IsOpenTile(monsterPosition.x, monsterPosition.y)) || path.Count > 1 && path [path.Count - 1].Equals (monsterPosition)){
				RLCharacter m = AddMonsterAtPosition (monsterPosition.x, monsterPosition.y, mdef);
				if (fadeInImmediate && m != null)
					m.Show ();
				break;
			}
			bailCount++;
		}
	}
	RLCharacter AddMonsterAtPosition(int x, int y, MonsterDef mdef){
		if (mdef.isTile) {
			// add the tile type to the map at the position
			map.SetTile (x, y, mdef.tile);
			return null;
		}
		RLCharacter m = CreateCharacter (x, y, '3', RLCharacter.RLTypes.MONSTER);
		m.gameObject.name = mdef.name;
		m.health = 2;
		m.name = mdef.name;
		m.def = mdef;
		m.AddSprite (m.name);
		m.HideImmediate ();
		m.AddType (mdef.moveType);

		if (mdef.hasTurnCount) {
			ActionCounter ac = m.gameObject.AddComponent<ActionCounter> ();
			ac.actionsRemaining = Random.Range (0, 4);
		} else if (mdef.hasFireCount) {			
			ActionCounter ac = m.gameObject.AddComponent<ActionCounter> ();
			ac.actionsRemaining = 0;
		}
		if (mdef.isDistance) {
			TrapDisplay td = m.gameObject.AddComponent<TrapDisplay> ();
			td.Setup (map, mdef.directions);
			if(m.GetComponent<ActionCounter>() && m.GetComponent<ActionCounter>().actionsRemaining != 0)
				m.AddSprite (m.name + " disable");
		}
		monsterMap [x, y] = m;

		TileSelector ts = m.gameObject.AddComponent<TileSelector> ();
		ts.tileX = mdef.sx;
		ts.tileY = mdef.sy;

		monsters.Add (m);
		return m;
	}
	RLCharacter CreatePlayerCharacter(RLCharacter c, MonsterDef def){
		c.gameObject.AddComponent<TileSelector> ();
		c.GetComponent<TileSelector> ().tileX = def.sx;
		c.GetComponent<TileSelector> ().tileY = def.sy;
		c.gameObject.AddComponent<ActionCounter> ();
		c.gameObject.name = def.name;
		c.name = def.name;
		c.offset = new Vector3 (-mapSizeX/2-1, -mapSizeY/2, 0);
		c.mapScale = mapDisplay.worldSize;

		c.AddSprite (c.name);
		c.HideImmediate ();
		switch (def.moveType) {
		case RLCharacter.RLTypes.WARRIOR:
			c.color = warriorColor;
			break;
		case RLCharacter.RLTypes.ARCHER:
			c.color = archerColor;
			break;
		case RLCharacter.RLTypes.MINER:
			c.color = minerColor;
			break;
		case RLCharacter.RLTypes.MAGE:
			c.color = mageColor;
			break;
		case RLCharacter.RLTypes.PRIEST:
			c.color = priestColor;
			break;
		}
		c.GetComponent<ActionCounter> ().actionsRemaining = 3;
		c.AddType (def.moveType);
		return c;
	}

	GoTweenChain currentFlow;
	void PlayerUpdate(){
		if(currentFlow == null)
			currentFlow = new GoTweenChain();
		bool performedAction = false;
		bool performedMove = false;
		// put the characters into the character map
		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				characterMap [x, y] = null;
			}
		}
		foreach (RLCharacter pc in characters) {
			characterMap [pc.positionI.x, pc.positionI.y] = pc;
		}
		Vector2i deltaD = new Vector2i(0,0);
		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			deltaD = new Vector2i (0, -1);
			lastPlayerMove = 0;
		}
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			deltaD = new Vector2i(0,1);
			lastPlayerMove = 3;
		}
		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			deltaD = new Vector2i(1,0);
			lastPlayerMove = 2;
		}
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			deltaD = new Vector2i (-1, 0);
			lastPlayerMove = 1;
		}
		if (Input.GetKeyDown (KeyCode.R) || Input.GetKeyDown (KeyCode.P)) {
			fsm.PerformTransition (FsmTransitionId.Rebuild);
			InitialGen ();
		}
		if (Input.GetKeyDown (KeyCode.Space)) {
			ConsoleAdd ("-");
			lastPlayerMove = -1;
			// do the player actions
			// check around the warrior, and see if there is an enemy on one of the surrounding tiles
			foreach (RLCharacter c in characters) {
				if (c.hasTypes.Contains (RLCharacter.RLTypes.WARRIOR)) {
					performedAction = WarriorAction (c) || performedAction;
				}
				if (c.hasTypes.Contains (RLCharacter.RLTypes.ARCHER)) {
					performedAction = ArcherAction (c) || performedAction;
				}
				if (c.hasTypes.Contains (RLCharacter.RLTypes.MINER)) {
					performedAction = MinerAction (c) || performedAction;
				}
				if (c.hasTypes.Contains (RLCharacter.RLTypes.MAGE)) {
					performedAction = MageAction (c) || performedAction;
				}
				if (c.hasTypes.Contains (RLCharacter.RLTypes.PRIEST)) {
					performedAction = PriestAction (c) || performedAction;
				}
			}
		}
		if (!deltaD.Equals (new Vector2i (0, 0))) {
			ConsoleAdd ("-");
			// check each of the player characters to see if this is a valid move
			bool setPositon = true;
			List<RLCharacter> movedThisTurn = new List<RLCharacter> ();
			while (setPositon) {
				setPositon = false;
				for (int i=0;i<characters.Count;i++) {
					RLCharacter pc = characters [i];
					if (!movedThisTurn.Contains (pc)) {
						Vector2i np = pc.positionI + deltaD;
						bool hasMonster = false;
						foreach (RLCharacter m in monsters) {
							if (m.positionI.Equals (np)) {
								// attack the monster for one damage
								ConsoleAdd (setColor (pc.name, pc.color) + " hits " + setColor(m.name, monsterColor) + " for 1hp");
								pc.Attack (m.positionI);

								m.Hit();
								MapBloodStain (m.positionI.x, m.positionI.y);
								hasMonster = true;
								movedThisTurn.Add (pc);
								setPositon = true;
								Camera.main.audio.PlayOneShot (meleeAudio);
							}
						}
						if(ContainsType(np.x, np.y, RLCharacter.RLTypes.WAITING_PLAYER)){
							Debug.Log ("player on spot");
							bool addedWaitingChar = false;
							foreach (RLCharacter p in pickups) {
								if (p.hasTypes.Contains (RLCharacter.RLTypes.WAITING_PLAYER) && p.positionI.Equals(np)) {
									p.hasTypes.Remove (RLCharacter.RLTypes.WAITING_PLAYER);
									p.AddType (RLCharacter.RLTypes.PLAYER);
									pickups.Remove (p);
									characters.Add (p);
									p.WakeUp ();
									charactersJoined.Add (p);
									movedThisTurn.Add (p);
									movedThisTurn.Add (pc);
									Camera.main.audio.PlayOneShot (playerAddAudio);
									ConsoleAdd (setColor (p.name, p.color) + " joins cooperative");
									characterMap [np.x, np.y] = p;
									setPositon = true;
									addedWaitingChar = true;
									Debug.Log ("added char");
									break;
								}
								if (addedWaitingChar)
									break;
							}
						} else if (!hasMonster && map.IsOpenTile (np.x, np.y) && characterMap [np.x, np.y] == null) {
							// check to see if there is an unactivated character on this position, and activate if so
							// move the character into the new position
							characterMap [pc.positionI.x, pc.positionI.y] = null;
							characterMap [np.x, np.y] = pc;
							pc.SetPosition (np.x, np.y);
							movedThisTurn.Add (pc);
							setPositon = true;
						}
					}
				}
			}
			// if the character has landed on a stairs then remove them from the level
			for (int i = characters.Count - 1; i >= 0; i--) {
				for (int j = pickups.Count - 1; j >= 0; j--) {
					if (pickups[j].positionI.Equals (characters [i].positionI)) {
						if (pickups [j].hasTypes.Contains (RLCharacter.RLTypes.ACTION_PICKUP)) {
							characters [i].GetComponent<ActionCounter> ().actionsRemaining = (int)Mathf.Min(6, ((int)characters [i].GetComponent<ActionCounter> ().actionsRemaining)+2);
							Camera.main.audio.PlayOneShot (weaponPickupAudio);
						}
						if (pickups [j].hasTypes.Contains (RLCharacter.RLTypes.HEALTH_PICKUP)) {
							characters [i].health = (int)Mathf.Min(4, characters [i].health+2);
							Camera.main.audio.PlayOneShot (healthPickupAudio);
						}
						if (pickups [j].hasTypes.Contains (RLCharacter.RLTypes.GOLD_PICKUP)) {
							ConsoleAdd (setColor (characters[i].name, characters[i].color) + " found some gold");
							goldCount++;
							Camera.main.audio.PlayOneShot (healthPickupAudio);
						}
						pickups [j].Kill ();
						pickups.RemoveAt (j);
					}
				}
				// deal with environmental hazards
				switch (map.GetTile (characters [i].positionI.x, characters [i].positionI.y)) {
					case RL.TileType.ACID:
						characters [i].Hit ();
						break;
					case RL.TileType.LAVA:
						characters [i].Hit (10);
						break;
				}
				// these are two things that modify character list so either one or the other needs to run
				if (characters [i].health <= 0) {
					characters [i].Kill ();
					characters.Remove (characters [i]);
				} else if (map.GetTile (characters [i].positionI.x, characters [i].positionI.y) == RL.TileType.STAIRS_DOWN) {
					if (currentLevel == 10) {
						wonGame = true;
					} else {
						exitCharacters.Add (characters [i]);
						characters [i].Hide ();
						characters.RemoveAt (i);
						Camera.main.audio.PlayOneShot (playerExitAudio);
					}
				} else if (map.GetTile (characters [i].positionI.x, characters [i].positionI.y) == RL.TileType.GOBLET) {
					wonGame = true;

				}
			}
			if (movedThisTurn.Count > 0)
				performedMove = true;
		}
		// remove all the dead enemies
		if (monsters.Count > 0) {
			for (int i = monsters.Count - 1; i >= 0; i--) {
				if (monsters [i].health <= 0) {
					ConsoleAdd (setColor(monsters[i].name, monsterColor) + " dies");
					monsters [i].Kill ();
					monsters.RemoveAt (i);
				}
			}
		}
		// if there are no characters left, then gen a new level
		if (characters.Count == 0 && exitCharacters.Count != 0) {
			TweenManager.instance ().FinishAll ();
			TweenManager.instance ().FinishAll ();
			TweenManager.instance ().StartPlay ();
			TweenManager.instance ().NextTurn ();
			GenLevel ();
			TweenManager.instance ().NextTurn ();
		} else if (wonGame || characters.Count == 0 && exitCharacters.Count == 0 || Input.GetKeyDown (KeyCode.U)) {
			fsm.PerformTransition (FsmTransitionId.GameOver);
		} else if (performedAction || performedMove) {
			if(!performedAction)
				TweenManager.instance ().FinishAll ();
			TweenManager.instance ().StartPlay ();
			TweenManager.instance ().NextTurn ();
			fsm.PerformTransition (FsmTransitionId.Complete);
		}
	}

	#region characterActions
	bool MinerAction(RLCharacter w){
		bool knockedThisTurn = false;
		if (w.GetComponent<ActionCounter> ().actionsRemaining > 0) {
			for (int i = 0; i < 4; i++) {
				Vector2i np = w.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				// knock down a random wall
				if (map.GetTile (np.x, np.y) == RL.TileType.WALL) {
					map.SetTile (np.x, np.y, RL.TileType.OPEN);
					MapBloodStain (np.x, np.y, floorC*0.5f);
					Camera.main.audio.PlayOneShot (minerActionAudio);
					knockedThisTurn = true;

					pickups.Add(CreateCharacter (np.x, np.y, 'g', RLCharacter.RLTypes.GOLD_PICKUP, MonsterDef.pickupDefs[4]));
				
				}
			}
		}
		if (knockedThisTurn) {
			ConsoleAdd (setColor ("miner", minerColor) + " knocks down wall");
			w.GetComponent<ActionCounter> ().actionsRemaining--;
			GetComponent<MapDisplay> ().Build (map, false);

			return true;
		}
		return false;
	}
	bool PriestAction(RLCharacter w){
		if (w.GetComponent<ActionCounter> ().actionsRemaining > 0) {

			for (int i = 0; i < 4; i++) {
				Vector2i np = w.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				// knock down a random wall
				if (characterMap[np.x, np.y] && characterMap[np.x, np.y].health < 4) {
					characterMap[np.x, np.y].health = (int)Mathf.Min(4, characterMap[np.x, np.y].health+2);
					w.GetComponent<ActionCounter> ().actionsRemaining--;
					MapBloodStain (characterMap[np.x, np.y].positionI.x, characterMap[np.x, np.y].positionI.y, Color.yellow);
					Camera.main.audio.PlayOneShot (priestActionAudio);
					ConsoleAdd (setColor ("priest", priestColor) + " heals "+setColor(characterMap[np.x,np.y].name, characterMap[np.x,np.y].color));
					AddPriestEffect (characterMap[np.x, np.y].gameObject);
					return true;
				}
			}
			if (w.health < 4) {
				MapBloodStain (w.positionI.x, w.positionI.y, Color.yellow);
				w.health = (int)Mathf.Min (4, w.health + 2);
				w.GetComponent<ActionCounter> ().actionsRemaining--;
				Camera.main.audio.PlayOneShot (priestActionAudio);
				AddPriestEffect (w.gameObject);
				ConsoleAdd (setColor ("priest", priestColor) + " heals self");
				return true;
			}
		}
		return false;
	}
	void AddPriestEffect(GameObject a){
		// get an arrow and shoot it at the target
		GameObject arrow = (GameObject)Instantiate(Resources.Load("health"), a.transform.position, Quaternion.identity);
		arrow.transform.localScale = Vector3.one * 0.5f;
		Go.to (arrow.transform, 0.4f, TweenManager.TweenConfigCurrent ().scale(2));
		TweenManager.instance ().NextTurn ();
		Go.to (arrow.transform, 0.2f, TweenManager.TweenConfigCurrent ().materialColor(new Color(1,1,1,0)).onIterationEnd((x) => { Destroy(arrow); }));
	}
	bool WarriorAction(RLCharacter w){
		if (w.GetComponent<ActionCounter> ().actionsRemaining > 0) {
			for (int i = 0; i < 4; i++) {
				Vector2i np = w.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				foreach (RLCharacter m in monsters) {
					if (m.positionI.Equals (np)) {
						m.Hit (1);
						w.Attack (m.positionI);
						if (m.health > 0) {
							TweenManager.instance ().NextTurn ();
							m.Hit (1);
							w.Attack (m.positionI);
						}
						w.GetComponent<ActionCounter> ().actionsRemaining--;
						MapBloodStain (m.positionI.x, m.positionI.y);
						Camera.main.audio.PlayOneShot (WarriorActionAudio);
						ConsoleAdd (setColor ("warrior", warriorColor) + " hits "+setColor(m.name, monsterColor)+ " for 2hp");
						// just hit one monster and return
						return true;
					}
				}
				// only hit one monster on the warriors turn
			}
		}
		return false;
	}
	bool MageAction(RLCharacter a){
		if (a.GetComponent<ActionCounter> ().actionsRemaining > 0) {
			// the archer fires at the nearest monster on each of the cardinal directions
			List<RLCharacter> archerTargets = new List<RLCharacter> ();
			for (int i = 4; i < 8; i++) {
				Vector2i ndir = new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				Vector2i np = a.positionI + ndir;
				while (map.IsOpenTile (np.x, np.y) && characterMap [np.x, np.y] == null) {
					foreach (RLCharacter m in monsters) {
						if (m.positionI.Equals (np)) {
							archerTargets.Add (m);
						}
					}
					np += ndir;
				}
			}
			if (archerTargets.Count > 0) {
				RLCharacter nearest = archerTargets [0];
				foreach (RLCharacter m in archerTargets) {
					if (a.positionI.Distance (m.positionI) < a.positionI.Distance (nearest.positionI)) {
						nearest = m;
					}
				}
				// get an arrow and shoot it at the target
				GameObject arrow = (GameObject)Instantiate(Resources.Load("mageBolt"), a.transform.position, Quaternion.identity);
				Vector3 positionDiff = nearest.transform.position - a.transform.position;
				Go.to (arrow.transform, positionDiff.magnitude*0.5f*0.1f, TweenManager.TweenConfigCurrent ().positionPath(new GoSpline(new List<Vector3>{a.transform.position, nearest.transform.position})));
				positionDiff.Normalize ();
				float angle = Mathf.Atan2 (positionDiff.y, positionDiff.x);
				arrow.transform.rotation = Quaternion.Euler (0, 0, Mathf.Rad2Deg*angle);
				TweenManager.instance ().NextTurn ();
				Go.to (arrow.transform, 0.2f, TweenManager.TweenConfigCurrent ().onIterationEnd((x) => { Destroy(arrow); }));

				ConsoleAdd (setColor (a.name, a.color) + " hits " + setColor(nearest.name, monsterColor));
				MapBloodStain (nearest.positionI.x, nearest.positionI.y);
				Camera.main.audio.PlayOneShot (mageActionAudio);
				a.GetComponent<ActionCounter> ().actionsRemaining--;
				nearest.Hit();
				return true;
			}
		}
		return false;
	}
	bool ArcherAction(RLCharacter a){
		if (a.GetComponent<ActionCounter> ().actionsRemaining > 0) {
			// the archer fires at the nearest monster on each of the cardinal directions
			List<RLCharacter> archerTargets = new List<RLCharacter> ();
			for (int i = 0; i < 4; i++) {
				Vector2i ndir = new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				Vector2i np = a.positionI + ndir;
				while (map.IsOpenTile (np.x, np.y) && characterMap [np.x, np.y] == null) {
					foreach (RLCharacter m in monsters) {
						if (m.positionI.Equals (np)) {
							archerTargets.Add (m);
						}
					}
					np += ndir;
				}
			}
			if (archerTargets.Count > 0) {
				RLCharacter nearest = archerTargets [0];
				foreach (RLCharacter m in archerTargets) {
					if (a.positionI.Distance (m.positionI) < a.positionI.Distance (nearest.positionI)) {
						nearest = m;
					}
				}

				// get an arrow and shoot it at the target
				GameObject arrow = (GameObject)Instantiate(Resources.Load("Arrow"), a.transform.position, Quaternion.identity);
				Vector3 positionDiff = nearest.transform.position - a.transform.position;

				Go.to (arrow.transform, positionDiff.magnitude*0.5f*0.15f, TweenManager.TweenConfigCurrent ().positionPath(new GoSpline(new List<Vector3>{a.transform.position, nearest.transform.position})));
				positionDiff.Normalize ();
				float angle = Mathf.Atan2 (positionDiff.y, positionDiff.x);
				arrow.transform.rotation = Quaternion.Euler (0, 0, Mathf.Rad2Deg*angle);
				TweenManager.instance ().NextTurn ();
				Go.to (arrow.transform, 0.2f, TweenManager.TweenConfigCurrent ().onIterationEnd((x) => { Destroy(arrow); }));

				Camera.main.audio.PlayOneShot (archerActionAudio);
				MapBloodStain (nearest.positionI.x, nearest.positionI.y);
				a.GetComponent<ActionCounter> ().actionsRemaining--;
				ConsoleAdd (setColor (a.name, a.color) + " hits " + setColor(nearest.name, monsterColor));
				nearest.Hit();
				return true;
			}
		}
		return false;
	}
	#endregion

	void MapBloodStain(int x, int y){
		MapBloodStain (x, y, Color.red);
	}
	void MapBloodStain(int x, int y, Color baseColor){
		mapColors[x, y] = new ColorLerp(baseColor, 2f);
	}
	List<int[]> ShuffledDirections(int offset=0){
		// shuffle the list of directions
		List<int[]> dirArrayStart = new List<int[]>();
		List<int[]> dirArrayEnd = new List<int[]>();
		for (int i = 0+offset; i < 4+offset; i++) {
			dirArrayStart.Add(new int[]{RL.Map.nDir[i,0], RL.Map.nDir[i,1]});
		}
		while (dirArrayStart.Count > 0) {
			int counter = Random.Range (0, dirArrayStart.Count);
			dirArrayEnd.Add (dirArrayStart [counter]);
			dirArrayStart.RemoveAt (counter);
		}
		return dirArrayEnd;
	}
	#region monsterActions
	void MonsterProcess(){
		// update all monster positions / attack players etc.
		foreach (RLCharacter m in monsters) {
			monsterMap [m.positionI.x, m.positionI.y] = null;
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_CHASE)) {
				// only move the enemy if the path is at least 2 steps long (the start and end positions)
				if (!MonsterPath (m)) {
					// check the adjacent spots for characters and attack
					MonsterAttack (m);
				}
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_PLAYER_COPY)) {
				MonsterPlayerCopy (m);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WANDER_LOS)) {
				// if the monster is wandering
				MonsterWanderAndChase (m);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WANDER)) {
				// if the monster is wandering
				MonsterWander (m);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WANDER_CHASE)) {
				// if the monster is wandering
				MonsterWanderUntilDistance (m, 4);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_DISTANCE_FIRE)) {
				// if the monster is wandering
				MonsterDistanceFire(m);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_DISTANCE_FIRE_DIAGONAL)) {
				// if the monster is wandering
				MonsterDistanceFire(m, 4);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WALL_BOUNCE)) {
				// if the monster is wandering
				MonsterHitWallTurn(m, 2);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WALL_TURN)) {
				// if the monster is wandering
				MonsterHitWallTurn(m, 1);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WALL_TURN_CHASE)) {
				// if the monster is wandering
				MonsterHitWallTurn(m, 1, true);
			}
			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_WAIT_CHASE)) {
				// if the monster is wandering
				MonsterWaitUntilDistance(m, 3);
			}
			monsterMap [m.positionI.x, m.positionI.y] = m;
		}
		// spawn a monster if we are on a monster spawning turn
		// we want the spawning to start at a low rate and increase
		if (turnCount%LevelDef.levels[currentLevel-1].spawnRate == 0 ){
			SpawnMonster(true);
		}
		// regen the map, since we might have spawned some lava
		GetComponent<MapDisplay> ().Build (map, false);
		turnCount++;

		while(consoleArray.Count>0 &&consoleArray [consoleArray.Count - 1] == "-") {
			consoleArray.RemoveAt (consoleArray.Count - 1);
		}

		TweenManager.instance ().NextTurn ();
		fsm.PerformTransition (FsmTransitionId.Complete);
	}
	void MonsterPlayerCopy(RLCharacter m){
		// check to see if we can attack
		// if not, try to copy the players movement
		if(!MonsterAttack(m) && lastPlayerMove >= 0){
			Vector2i np = m.positionI + new Vector2i (RL.Map.nDir[lastPlayerMove, 0], RL.Map.nDir[lastPlayerMove, 1]);
			if (map.IsOpenTile (np.x, np.y) && monsterMap [np.x, np.y] == null) {
				m.SetPosition (np.x, np.y);
			}
		}
	}
	void MonsterHitWallTurn(RLCharacter m, int turnAmount, bool convertToChase=false){
		if (convertToChase) {
			// if there is a nearby character, then start chasing them
			if (MonsterWaitUntilDistance (m, 3, RLCharacter.RLTypes.MONSTER_WALL_TURN_CHASE))
				return;
		}
		// attempt to move in the current direction, if we are going to hit a wall or a character do so
		List<int[]> dirArrayEnd = ShuffledDirections ();
		foreach (int[] dir in dirArrayEnd) {
			//attempt to move in each direction
			Vector2i npa = m.positionI + new Vector2i (dir [0], dir [1]);
			if (characterMap [npa.x, npa.y] != null) {
				MonsterAttack (m);
				return;
			}
		}
		for (int i = 0; i < 2; i++) {
			int cdir = m.GetComponent<ActionCounter> ().actionsRemaining;
			Vector2i currentDir = new Vector2i (RL.Map.nDirOrdered [cdir, 0], RL.Map.nDirOrdered [cdir, 1]);
			Vector2i np = m.positionI + currentDir;
			if (map.IsOpenTile (np.x, np.y) && monsterMap [np.x, np.y] == null) {
				m.SetPosition (np.x, np.y);
				break;
			} else {
				// turn around
				m.GetComponent<ActionCounter> ().actionsRemaining = (m.GetComponent<ActionCounter> ().actionsRemaining + turnAmount) % 4;
			}
		}
	}
	void MonsterDistanceFire(RLCharacter m, int directionOffset=0){
		// check to see if we are able to fire
		if (m.GetComponent<ActionCounter> ().actionsRemaining > 0) {
			m.GetComponent<ActionCounter> ().actionsRemaining--;
			if (m.GetComponent<ActionCounter> ().actionsRemaining == 0) {
				m.AddSprite (m.def.name);
				m.GetComponent<TrapDisplay> ().EnableChildren ();
			}
			return;	
		}
		List<int[]> dirArrayEnd = ShuffledDirections (directionOffset);
		foreach (int[] dir in dirArrayEnd) {
			// check to see if the character is visible
			Vector2i ndir = new Vector2i (dir);
			Vector2i np = m.positionI;
			np += ndir;
			while (map.IsOpenTile (np.x, np.y) && monsterMap[np.x, np.y] == null) {
				if (characterMap [np.x, np.y] != null) {
					// attack the character at the end of the line
					Vector2i[] line = RL.Map.Line(m.positionI, characterMap[np.x, np.y].positionI);
					float timeOffset = 0;
					for(int i=0;i<line.Count()-1;i++){
						mapColors[line[i].x, line[i].y] = new ColorLerp(Color.green, 0.25f+timeOffset);
						timeOffset += 0.1f;
					}
					MonsterAttackChar (m, characterMap [np.x, np.y]);
					m.GetComponent<ActionCounter> ().actionsRemaining = 4;
					m.GetComponent<TileSelector> ().tileX = 4+9;
					m.AddSprite (m.def.name + " disable");
					m.GetComponent<TrapDisplay> ().DisableChildren ();
					return;
				}
				np += ndir;
			}
		}
	}
	void MonsterWanderAndChase(RLCharacter m){
		List<int[]> dirArrayEnd = ShuffledDirections ();
		foreach (int[] dir in dirArrayEnd) {
			//attempt to move in each direction
			Vector2i np = m.positionI + new Vector2i (dir [0], dir [1]);
			if (characterMap [np.x, np.y] != null) {
				MonsterAttack (m);
				return;
			}
		}
		foreach (int[] dir in dirArrayEnd) {
			// check to see if the character is visible
			Vector2i ndir = new Vector2i (dir);
			Vector2i np = m.positionI;
			int distanceCount = 1;
			while (map.IsOpenTile (np.x, np.y) && monsterMap[np.x, np.y] == null) {
				np += ndir;
				if (characterMap [np.x, np.y] != null) {
					// move one towards the character we see
					m.SetPosition (m.positionI.x+ndir.x, m.positionI.y+ndir.y);
					return;
				}
				distanceCount++;
			}
		}
		MonsterWander (m);
	}
	void MonsterWander(RLCharacter m){
		List<int[]> dirArrayEnd = ShuffledDirections ();
		foreach (int[] dir in dirArrayEnd) {
			//attempt to move in each direction
			Vector2i np = m.positionI + new Vector2i (dir [0], dir [1]);
			if (characterMap [np.x, np.y] != null) {
				MonsterAttack (m);
				return;
			}
		}
		foreach (int[] dir in dirArrayEnd) {
			//attempt to move in each direction
			Vector2i np = m.positionI + new Vector2i (dir [0], dir [1]);
			if (map.IsOpenTile (np.x, np.y) && !ContainsType (np.x, np.y, RLCharacter.RLTypes.MONSTER)) {
				m.SetPosition (np.x, np.y);
				break;
			}
		}
	}
	bool MonsterWaitUntilDistance(RLCharacter m, int distanceTrigger, RLCharacter.RLTypes toRemove = RLCharacter.RLTypes.MONSTER_WAIT_CHASE){
		List<List<Vector2i>> paths = new List<List<Vector2i>> ();
		foreach(RLCharacter c in characters){
			List<Vector2i> foundPath = pf.FindPath (m.positionI, c.positionI, (x,y) => {
				if(ContainsType(x, y, RLCharacter.RLTypes.MONSTER))
					return 1000;
				if(ContainsType(x, y, RLCharacter.RLTypes.STAIRS_DOWN))
					return 5;
				return 1;
			}, map);
			// if the path is less than 4, then home in on the character
			if (foundPath.Count <= distanceTrigger) {
				m.hasTypes.Remove (toRemove);
				m.AddType (RLCharacter.RLTypes.MONSTER_CHASE);
				// immediately chase
				if (!MonsterPath (m)) {
					// check the adjacent spots for characters and attack
					MonsterAttack (m);
				}
				return true;
			}
		}
		return false;
	}

	void MonsterWanderUntilDistance(RLCharacter m, int distanceTrigger){
		List<int[]> dirArrayEnd = ShuffledDirections ();
		foreach (int[] dir in dirArrayEnd) {
			//attempt to move in each direction
			Vector2i np = m.positionI + new Vector2i (dir [0], dir [1]);
			if (characterMap [np.x, np.y] != null) {
				m.hasTypes.Remove (RLCharacter.RLTypes.MONSTER_WANDER_CHASE);
				m.AddType (RLCharacter.RLTypes.MONSTER_CHASE);
				MonsterAttack (m);
				break;
			} else if (map.IsOpenTile (np.x, np.y) && !ContainsType (np.x, np.y, RLCharacter.RLTypes.MONSTER)) {
				m.SetPosition (np.x, np.y);
				break;
			}
		}
		List<List<Vector2i>> paths = new List<List<Vector2i>> ();
		foreach(RLCharacter c in characters){
			List<Vector2i> foundPath = pf.FindPath (m.positionI, c.positionI, (x,y) => {
				if(ContainsType(x, y, RLCharacter.RLTypes.MONSTER))
					return 1000;
				if(ContainsType(x, y, RLCharacter.RLTypes.STAIRS_DOWN))
					return 5;
				return 1;
			}, map);
			// if the path is less than 4, then home in on the character
			if (foundPath.Count <= distanceTrigger) {
				m.hasTypes.Remove (RLCharacter.RLTypes.MONSTER_WANDER_CHASE);
				m.AddType (RLCharacter.RLTypes.MONSTER_CHASE);
			}
		}
	}
	bool MonsterAttack(RLCharacter m){
		for (int i = 0; i < 4; i++) {
			Vector2i np = m.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
			if (characterMap [np.x, np.y] != null) {
				MonsterAttackChar (m, characterMap [np.x, np.y]);
				return true;
			}
		}
		return false;
	}
	void MonsterAttackChar(RLCharacter m, RLCharacter c){
		ConsoleAdd (setColor(m.name, monsterColor) + " hits " + setColor (c.name, c.color) + " for 1hp");
		m.Attack (c.positionI);
		c.Hit ();
		// if the character is at 0, then remove it
		MapBloodStain (c.positionI.x, c.positionI.y);
		if (c.health <= 0) {
			ConsoleAdd (setColor (c.name, c.color) + " dies");
			characters.Remove (c);
			c.Kill ();
		}
	}
	// if the monster moved, then don't attack
	bool MonsterPath(RLCharacter m){
		// if the monster is pathing towards an enemy
		List<List<Vector2i>> paths = new List<List<Vector2i>> ();
		foreach(RLCharacter c in characters){
			List<Vector2i> foundPath = pf.FindPath (m.positionI, c.positionI, (x,y) => {
				if(ContainsType(x, y, RLCharacter.RLTypes.MONSTER))
					return 1000;
				if(ContainsType(x, y, RLCharacter.RLTypes.STAIRS_DOWN))
					return 5;
				return 1;
			}, map);
			if (foundPath.Count >= 2) {
				paths.Add (foundPath);
			}
		}
		if (paths.Count > 0) {
			// get shortest list
			List<Vector2i> shortestPath = paths [0];
			foreach (List<Vector2i> p in paths) {
				if (p.Count < shortestPath.Count) {
					shortestPath = p;
				}
			}
			Vector2i np = shortestPath[1];
			if (characterMap [np.x, np.y] == null) {
				m.SetPosition (np.x, np.y);
				return true;
			}
		}
		return false;
	}
	#endregion

	bool ContainsType(int x, int y, RLCharacter.RLTypes t){
		// make an intersection of characters and monsters
		List<RLCharacter> objects = new List<RLCharacter> ();
		objects.AddRange (characters);
		objects.AddRange (monsters);
		objects.AddRange (pickups);
		for (int i = objects.Count - 1; i >= 0; i--) {
			if (objects[i].positionI.x == x && objects[i].positionI.y == y && objects [i].hasTypes.Contains (t)) {
				return true;
			}
		}
		return false;
	}
	float gameOverStartTime;
	void GameOverStart(){
		hud.gameObject.SetActive (false);
		GetComponent<MapDisplay> ().HideMap (map);
		// disable all the monsters, characters, and the map
		foreach (RLCharacter c in exitCharacters) {
			c.FinalKillNoAudio();
		}
		foreach (RLCharacter c in characters) {
			c.FinalKillNoAudio();
		}
		if (monsters != null) {
			foreach (RLCharacter c in monsters) {
				Destroy(c.gameObject);
			}
			foreach (RLCharacter c in pickups) {
				Destroy (c.gameObject);
			}
		}
		monsters.Clear ();
		characters.Clear ();
		pickups.Clear ();
		exitCharacters.Clear ();
		console.gameObject.SetActive (false);

		// store the gameover info in the score table
		Scores scores = Scores.Load ();
		// add a new score
		ScoreItem si = new ScoreItem {
			won = wonGame,
			gold = goldCount,
			levelReached = currentLevel,
			time = System.DateTime.Now
		};
		List<RLCharacter.RLTypes> charactersSeen = new List<RLCharacter.RLTypes>();
		foreach (RLCharacter c in charactersJoined) {
			foreach (RLCharacter.RLTypes t in c.hasTypes) {
				if (t == RLCharacter.RLTypes.ARCHER ||
				    t == RLCharacter.RLTypes.MAGE ||
				    t == RLCharacter.RLTypes.PRIEST ||
				    t == RLCharacter.RLTypes.MINER ||
					t == RLCharacter.RLTypes.WARRIOR ) {
					if(!charactersSeen.Contains(t))
						charactersSeen.Add (t);
				}
			}
		}
		si.charactersJoined = charactersSeen;
		scores.Add (si);
		scores.Save ();
		gameOverStartTime = Time.time;
		scores.SortTable ();
		string scoreTable = "High Scores\n";
		// build the score table
		for (int i = 0; i < 7; i++) {
			if (scores.scores.Count == i)
				break;
			ScoreItem item = scores.scores [i];
			scoreTable += (item.won ? "WIN" : "   ") + " ";
			scoreTable += "Level " +item.levelReached+ " ";
			scoreTable += "Gold " +item.gold+ "\n";
		}
		gameOver.scoreTable.text = scoreTable;
		if (wonGame) {
			gameOver.title.text = "YOU WON";
			gameOver.info.text = "";
		} else {
			gameOver.title.text = "YOU HAVE DIED";
			gameOver.info.text = "reached level " + (currentLevel) + "\n";
		}
		gameOver.info.text += "found " + goldCount + " gold\n";
	}
	void GameOverUpdate(){
		// mess with the render matrices
		for (int x = 0; x < 20; x++) {
			for (int y = 0; y < 16; y++) {
				Vector3 offVec = new Vector3 ((Random.value * 0.2f) - 0.1f, (Random.value * 0.2f) - 0.1f, 0);
				display.fgt.AddVertexOffset (x, y, offVec);
				display.bgt.AddVertexOffset (x, y, offVec);
				display.fgt.AssignColor(x,y,Color.Lerp(display.fgt.GetColor(x,y), Color.black, (Time.time-gameOverStartTime)*0.5f));
				display.bgt.AssignColor(x,y,Color.Lerp(display.bgt.GetColor(x,y), Color.black, (Time.time-gameOverStartTime)*0.5f));
			}
		}
		if (Time.time - gameOverStartTime > 2) {
			gameOver.gameObject.SetActive (true);
			if (Input.GetKey (KeyCode.Space)) {
				gameObject.SetActive (false);
				gameOver.gameObject.SetActive (false);
				titleScreen.gameObject.SetActive (true);
				titleScreen.StartMenu ();
				fsm.PerformTransition (FsmTransitionId.Complete);
			}
		}
	}
	// get the lookup table index for the sprite, based on its neighbors
	int calculateIndex(int x, int y){
		// calculate the binary index of the layer

		int i1 = (!map.IsValidTile(x+1, y)||map.IsOpenTile(x+1,y))?0:1;
		int i2 = (!map.IsValidTile(x,y-1)||map.IsOpenTile(x,y-1))?0:1;
		int i3 = (!map.IsValidTile(x-1,y)||map.IsOpenTile(x-1,y))?0:1;
		int i4 = (!map.IsValidTile(x,y+1)||map.IsOpenTile(x,y+1))?0:1;

		return (i4<<3) | (i3<<2) | (i2<<1) | (i1);
	}
	int calcIndexOryx(int x, int y){
		int o = calculateIndex (x, y);
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
	// Update is called once per frame
	void Update () {
		AudioSource[] characterAudio = { archerAudio, mageAudio, WarriorAudio, priestAudio, minerAudio };
		foreach(AudioSource aud in characterAudio){
			aud.volume -= Time.deltaTime * 0.1f;
		}
		foreach (RLCharacter c in characters) {
			if (c.hasTypes.Contains (RLCharacter.RLTypes.ARCHER)) {
				archerAudio.volume += Time.deltaTime * 0.2f;
			}
			if (c.hasTypes.Contains (RLCharacter.RLTypes.WARRIOR)) {
				WarriorAudio.volume += Time.deltaTime * 0.2f;
			}
			if (c.hasTypes.Contains (RLCharacter.RLTypes.MAGE)) {
				mageAudio.volume += Time.deltaTime * 0.2f;
			}
			if (c.hasTypes.Contains (RLCharacter.RLTypes.MINER)) {
				minerAudio.volume += Time.deltaTime * 0.2f;
			}
			if (c.hasTypes.Contains (RLCharacter.RLTypes.PRIEST)) {
				priestAudio.volume += Time.deltaTime * 0.2f;
			}
		}

		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				display.SetForegroundColor (x+3, y+1, Color.Lerp (display.GetForegroundColor (x+3, y+1), mapColorsForeground [x, y].c, mapColorsForeground [x, y].amt));
			}
		}
		hud.levelDisplay.text = "Level " + (currentLevel) + "/10";
		fsm.CurrentState.Update ();
	}
	string setColor(string input, Color c){
//		Debug.Log (ColorToHex (c));
		return "<color=\"#"+ColorToHex(c)+"ff\">"+input+"</color>";
	}
	// Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
	string ColorToHex(Color32 color)
	{
		string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
		return hex.ToLower();
	}

	Color HexToColor(string hex)
	{
		byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
		return new Color32(r,g,b, 255);
	}
	List<string> consoleArray = new List<string>();
	void ConsoleAdd(string input){
		/*
		consoleArray.Add (input);
		while (consoleArray.Count > 20) {
			consoleArray.RemoveAt (0);
		}
		console.text = "<material=1>";
		foreach (string t in consoleArray) {
			if (t.Equals("-")) {
				console.text += "</material><quad material=0 size=30 x=0 y=0 width=1 height=1 />\n<material=1>";
			} else {
				console.text += t + "\n";		
			}
		}
		console.text += "</material>";
		*/
		console.gameObject.SetActive (false);
	}
}