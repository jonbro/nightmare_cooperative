using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MonsterDef {
	public string name = "";
	public int sx = 0;
	public int sy = 0;
	public RLCharacter.RLTypes moveType;
	public bool hasTurnCount;
}
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
	public RLRender display;
	// the player characters
	List<RLCharacter> characters = new List<RLCharacter>();
	List<RLCharacter> exitCharacters = new List<RLCharacter>();
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
	static public MonsterDef[] pickupDefs = new MonsterDef[]{
		new MonsterDef{
			name = "action",
			sx = 4,
			sy = 15,
			moveType = RLCharacter.RLTypes.ACTION_PICKUP
		},
		new MonsterDef{
			name = "health",
			sx = 4,
			sy = 16,
			moveType = RLCharacter.RLTypes.HEALTH_PICKUP
		},
		new MonsterDef{
			name = "actionempty",
			sx = 4,
			sy = 17,
			moveType = RLCharacter.RLTypes.ACTION_PICKUP
		},
		new MonsterDef{
			name = "healthempty",
			sx = 4,
			sy = 18,
			moveType = RLCharacter.RLTypes.HEALTH_PICKUP
		},
		new MonsterDef{
			name = "gold",
			sx = 4,
			sy = 20,
			moveType = RLCharacter.RLTypes.GOLD_PICKUP
		}
	};
	static public MonsterDef[] characterDefs = new MonsterDef[]{
		new MonsterDef{
			name = "warrior",
			sx = 4,
			sy = 13,
			moveType = RLCharacter.RLTypes.WARRIOR
		},
		new MonsterDef{
			name = "archer",
			sx = 4,
			sy = 10,
			moveType = RLCharacter.RLTypes.ARCHER
		},
		new MonsterDef{
			name = "priest",
			sx = 4,
			sy = 14,
			moveType = RLCharacter.RLTypes.PRIEST
		},
		new MonsterDef{
			name = "miner",
			sx = 4,
			sy = 12,
			moveType = RLCharacter.RLTypes.MINER
		},
		new MonsterDef{
			name = "mage",
			sx = 4,
			sy = 11,
			moveType = RLCharacter.RLTypes.MAGE,
			hasTurnCount = true
		},
	};

	static public MonsterDef[] monsterDefs = new MonsterDef[] {
		new MonsterDef {
			name = "wanderer",
			sx = 4,
			sy = 1,
			moveType = RLCharacter.RLTypes.MONSTER_WANDER
		},
		new MonsterDef {
			name = "random until LOS",
			sx = 4,
			sy = 2,
			moveType = RLCharacter.RLTypes.MONSTER_WANDER_LOS
		},
		new MonsterDef {
			name = "random until distance",
			sx = 4,
			sy = 6,
			moveType = RLCharacter.RLTypes.MONSTER_WANDER_CHASE
		},
		new MonsterDef {
			name = "chaser",
			sx = 4,
			sy = 7,
			moveType = RLCharacter.RLTypes.MONSTER_CHASE
		},
		new MonsterDef {
			name = "fire at distance",
			sx = 4,
			sy = 8,
			moveType = RLCharacter.RLTypes.MONSTER_DISTANCE_FIRE
		},
		new MonsterDef {
			name = "fire at distance diagonal",
			sx = 4,
			sy = 9,
			moveType = RLCharacter.RLTypes.MONSTER_DISTANCE_FIRE_DIAGONAL
		},
		new MonsterDef {
			name = "hit wall and bounce",
			sx = 4,
			sy = 5,
			moveType = RLCharacter.RLTypes.MONSTER_WALL_BOUNCE,
			hasTurnCount =  true
		},
		new MonsterDef {
			name = "hit wall and turn",
			sx = 4,
			sy = 4,
			moveType = RLCharacter.RLTypes.MONSTER_WALL_TURN,
			hasTurnCount =  true
		},
		new MonsterDef {
			name = "wait chaser",
			sx = 4,
			sy = 7,
			moveType = RLCharacter.RLTypes.MONSTER_WAIT_CHASE
		},

	};
	static public int[,] levelMonsters = {
		{6, 1, 6, 0, 1},
		{6, 1, 1, 7, 7},
		{1, 2, 2, 2, 1},
		{1, 2, 3, 2, 1},
		{3, 2, 5, 3, 1},

		{3, 2, 5, 3, 4},
		{3, 2, 5, 3, 4},
		{3, 2, 5, 3, 4},
		{3, 2, 5, 3, 4},
		{3, 2, 5, 3, 4},

	};
	int[,] wallRange = { 
		{8, 20},
		{8, 20},
		{8, 20},
		{8, 20},
		{8, 20},

		{8, 20},
		{8, 20},
		{8, 20},
		{8, 20},
		{8, 20},
	};
	int currentLevel = 0;
	int mapSizeX, mapSizeY;
	// try to get across the screen without having any of your characters dying
	void Awake(){
		mapSizeX = 12;
		mapSizeY = 12;
		mapColors = new ColorLerp[mapSizeX, mapSizeY];
		mapColorsForeground = new ColorLerp[mapSizeX, mapSizeY];

		for (int x = 0; x < mapSizeX; x++) {
			for (int y = 0; y < mapSizeY; y++) {
				mapColors [x, y] = new ColorLerp (Color.white, 0);
			}
		}
		fsm = new FsmSystem ();
		fsm.AddState (new FsmState (FsmStateId.Player)
			.WithUpdateAction (PlayerUpdate)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Monster)
			.WithTransition (FsmTransitionId.GameOver, FsmStateId.GameOver)
		);
		fsm.AddState (new FsmState (FsmStateId.GameOver)
			.WithBeforeEnteringAction (GameOverStart)
			.WithUpdateAction (GameOverUpdate)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Player)
		);
		fsm.AddState (new FsmState (FsmStateId.Monster)
			.WithBeforeEnteringAction (MonsterProcess)
			.WithTransition (FsmTransitionId.Complete, FsmStateId.Player)
		);
	}
	void Start () {
		display.Setup (20, 16, 58, 40);
		// set up the rendering layers

//		InitialGen ();
	}
	public void InitialGen(){
		display.Setup (20, 16, 58, 40);
		currentLevel = 0;
		goldCount = 0;
		// kill all characters
		foreach (RLCharacter c in exitCharacters) {
			c.Kill ();
		}
		foreach (RLCharacter c in characters) {
			c.Kill ();
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
		// should potentially have more character archetypes later on
		List<MonsterDef> characterDefShuffled = ShuffleArray<MonsterDef>(characterDefs);
		// only player types that aren't already in the party
		for (int i = 0; i < 2; i++) {
			MonsterDef cdef = characterDefShuffled [i];
			exitCharacters.Add (CreatePlayerCharacter(CreateCharacter(i,1,'W',RLCharacter.RLTypes.PLAYER), cdef));
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
	RLCharacter CreatePlayerCharacter(RLCharacter c, MonsterDef def){
		c.gameObject.AddComponent<TileSelector> ();
		c.GetComponent<TileSelector> ().tileX = def.sx;
		c.GetComponent<TileSelector> ().tileY = def.sy;
		c.gameObject.AddComponent<ActionCounter> ();
		c.gameObject.name = def.name;
		c.GetComponent<ActionCounter> ().actionsRemaining = 3;
		c.AddType (def.moveType);
		return c;
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
			if (!nearCharacter) {
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
			c.SetPosition (cpos.x, cpos.y);
			characterMap [cpos.x, cpos.y] = c;
			characterCount++;
		}
		exitCharacters.Clear ();
		int bailCount = 0;
		while (true && bailCount < 1000) {
			monsterMap = new RLCharacter[mapSizeX, mapSizeY];
			if (monsters != null) {
				foreach (RLCharacter c in monsters) {
					c.Kill ();
				}
				foreach (RLCharacter c in pickups) {
					c.Kill ();
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

			pickups = new List<RLCharacter> ();

			// add a few action point pickups around the map
			int actionPointCount = Random.Range (1, 3);
			for (int i = 0; i < actionPointCount; i++) {
				Vector2i app = GetPositionAtDistanceFromCharactersAndMonsters (mapSizeX-2, mapSizeY-2, 2);
				map.SetTile (app.x, app.y, RL.TileType.OPEN);
				pickups.Add(CreateCharacter (app.x, app.y, 'a', RLCharacter.RLTypes.ACTION_PICKUP));
			}
			// add some health pickups
			actionPointCount = Random.Range (1, 2);
			for (int i = 0; i < actionPointCount; i++) {
				Vector2i app = GetPositionAtDistanceFromCharactersAndMonsters (mapSizeX-2, mapSizeY-2, 2);
				map.SetTile (app.x, app.y, RL.TileType.OPEN);
				pickups.Add(CreateCharacter (app.x, app.y, 'h', RLCharacter.RLTypes.HEALTH_PICKUP));
			}
			// spawn some gold you can pickup
			actionPointCount = Random.Range (1, 4);
			for (int i = 0; i < actionPointCount; i++) {
				// spawn in upper left or lower right

				Vector2i app = Random.value>0.5f?new Vector2i(Random.Range(1, mapSizeX/3), Random.Range(mapSizeY/3*2, mapSizeY-1)):new Vector2i(Random.Range(mapSizeX/3*2, mapSizeX-1), Random.Range(1, mapSizeY/3));
				map.SetTile (app.x, app.y, RL.TileType.OPEN);
				pickups.Add(CreateCharacter (app.x, app.y, 'g', RLCharacter.RLTypes.GOLD_PICKUP));
			}

			// every three levels, except for the first level, spawn in an extra party member that you can add
			if ((currentLevel+1) % 2 == 0 && characters.Count < 4 && currentLevel != 0) {
//			if(currentLevel == 0){
				// shuffle the character defs
				List<MonsterDef> characterDefShuffled = ShuffleArray<MonsterDef>(characterDefs);
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

						newChar.GetComponent<ActionCounter> ().actionsRemaining--;
						newChar.health--;
						pickups.Add(newChar);

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
		monsters = new List<RLCharacter> ();
		int monsterCount = Random.Range (5, 7);
		for (int i = 0; i < monsterCount; i++) {
			bailCount = 0;
			while (true && bailCount<1000) {
				// look for an open position to put the monster in that has a path to the player character
				Vector2i monsterPosition = GetPositionAtDistanceFromCharactersAndMonsters (mapSizeX-2, mapSizeY-2, 2);
				List<Vector2i> path = pf.FindPath (new Vector2i (1, 1), monsterPosition, (x, y) => {
					return 1;
				}, map);
				if (path.Count > 1 && path [path.Count - 1].Equals (monsterPosition)){
					RLCharacter m = CreateCharacter (monsterPosition.x, monsterPosition.y, '3', RLCharacter.RLTypes.MONSTER);

					// pick a monster type to spawn
					int monsterLevel = (int)Mathf.Min (levelMonsters.GetLength(0)-1, currentLevel);
					MonsterDef mdef = monsterDefs [levelMonsters [monsterLevel, Random.Range (0, levelMonsters.GetLength(1))]];
//					MonsterDef mdef = monsterDefs [Random.Range (Mathf.Min(monsterDefs.Length, Mathf.Max(0, currentLevel-3)), Mathf.Min (monsterDefs.Length, currentLevel+1))];
					m.gameObject.name = mdef.name;
					m.health = 2;
					m.AddType (mdef.moveType);
					if (mdef.hasTurnCount) {
						ActionCounter ac = m.gameObject.AddComponent<ActionCounter> ();
						ac.actionsRemaining = Random.Range(0,4);
					}
					monsterMap [monsterPosition.x, monsterPosition.y] = m;

					TileSelector ts = m.gameObject.AddComponent<TileSelector> ();
					ts.tileX = mdef.sx;
					ts.tileY = mdef.sy;
					monsters.Add (m);
					break;
				}
				bailCount++;
			}
		}
		currentLevel++;
	}
	void PlayerUpdate(){
		bool performedAction = false;
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
		}
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			deltaD = new Vector2i(0,1);
		}
		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			deltaD = new Vector2i(1,0);
		}
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			deltaD = new Vector2i (-1, 0);
		}
		if (Input.GetKeyDown (KeyCode.R) || Input.GetKeyDown (KeyCode.P)) {
			InitialGen ();
		}
		if (Input.GetKeyDown (KeyCode.Space)) {
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
								m.health--;
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

									movedThisTurn.Add (p);
									movedThisTurn.Add (pc);
									Camera.main.audio.PlayOneShot (playerAddAudio);
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
							goldCount++;
							Camera.main.audio.PlayOneShot (healthPickupAudio);
						}
						pickups [j].Kill ();
						pickups.RemoveAt (j);
					}
				}
				if (map.GetTile (characters [i].positionI.x, characters [i].positionI.y) == RL.TileType.STAIRS_DOWN) {
					exitCharacters.Add (characters [i]);
					characters.RemoveAt (i);
					Camera.main.audio.PlayOneShot (playerExitAudio);
				}
			}
			if (movedThisTurn.Count > 0)
				performedAction = true;
		}
		// remove all the dead enemies
		if (monsters.Count > 0) {
			for (int i = monsters.Count - 1; i >= 0; i--) {
				if (monsters [i].health <= 0) {
					monsters [i].Kill ();
					monsters.RemoveAt (i);
				}
			}
		}
		// if there are no characters left, then gen a new level
		if (characters.Count == 0 && exitCharacters.Count!=0) {
			GenLevel ();
		}else if(characters.Count == 0 && exitCharacters.Count ==0 || Input.GetKeyDown(KeyCode.U)){
			fsm.PerformTransition (FsmTransitionId.GameOver);
		}else if(performedAction)
			fsm.PerformTransition (FsmTransitionId.Complete);

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
				}
			}
		}
		if (knockedThisTurn) {
			w.GetComponent<ActionCounter> ().actionsRemaining--;
			return true;
		}
		return false;
	}
	bool PriestAction(RLCharacter w){
		if (w.GetComponent<ActionCounter> ().actionsRemaining > 0) {

			for (int i = 0; i < 4; i++) {
				Vector2i np = w.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				// knock down a random wall
				if (characterMap[np.x, np.y]) {
					characterMap[np.x, np.y].health = (int)Mathf.Min(4, characterMap[np.x, np.y].health+2);
					w.GetComponent<ActionCounter> ().actionsRemaining--;
					MapBloodStain (characterMap[np.x, np.y].positionI.x, characterMap[np.x, np.y].positionI.y, Color.yellow);
					Camera.main.audio.PlayOneShot (priestActionAudio);
					return true;
				}
			}
			MapBloodStain (w.positionI.x, w.positionI.y, Color.yellow);
			w.health = (int)Mathf.Min(4, w.health+2);
			Camera.main.audio.PlayOneShot (priestActionAudio);
		}
		return false;
	}
	bool WarriorAction(RLCharacter w){
		if (w.GetComponent<ActionCounter> ().actionsRemaining > 0) {
			for (int i = 0; i < 4; i++) {
				Vector2i np = w.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
				foreach (RLCharacter m in monsters) {
					if (m.positionI.Equals (np)) {
						m.health--;
						m.health--;
						w.GetComponent<ActionCounter> ().actionsRemaining--;
						MapBloodStain (m.positionI.x, m.positionI.y);
						Camera.main.audio.PlayOneShot (WarriorActionAudio);
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
				Vector2i[] line = RL.Map.Line(a.positionI, nearest.positionI);
				float timeOffset = 0;
				foreach (Vector2i l in line) {
					mapColors[l.x, l.y] = new ColorLerp(Color.blue, 0.25f+timeOffset);
					timeOffset += 0.1f;
				}
				MapBloodStain (nearest.positionI.x, nearest.positionI.y);
				Camera.main.audio.PlayOneShot (mageActionAudio);
				a.GetComponent<ActionCounter> ().actionsRemaining--;
				nearest.health--;
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
				Vector2i[] line = RL.Map.Line(a.positionI, nearest.positionI);
				float timeOffset = 0;
				foreach (Vector2i l in line) {
					mapColors[l.x, l.y] = new ColorLerp(Color.green, 0.25f+timeOffset);
					timeOffset += 0.1f;
				}
				Camera.main.audio.PlayOneShot (archerActionAudio);
				MapBloodStain (nearest.positionI.x, nearest.positionI.y);
				a.GetComponent<ActionCounter> ().actionsRemaining--;
				nearest.health--;
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
				MonsterWanderUntilDistance (m, 5);
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

			if (m.hasTypes.Contains (RLCharacter.RLTypes.MONSTER_CHASE)) {
				// only move the enemy if the path is at least 2 steps long (the start and end positions)
				if (!MonsterPath (m)) {
					// check the adjacent spots for characters and attack
					MonsterAttack (m);
				}
			}
			monsterMap [m.positionI.x, m.positionI.y] = m;
		}
		fsm.PerformTransition (FsmTransitionId.Complete);
	}
	void MonsterHitWallTurn(RLCharacter m, int turnAmount){
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
		List<int[]> dirArrayEnd = ShuffledDirections (directionOffset);
		foreach (int[] dir in dirArrayEnd) {
			// check to see if the character is visible
			Vector2i ndir = new Vector2i (dir);
			Vector2i np = m.positionI;
			int distanceCount = 1;
			while (map.IsOpenTile (np.x, np.y)) {
				np += ndir;
				if (characterMap [np.x, np.y] != null) {
					// attack the character at the end of the line
					Vector2i[] line = RL.Map.Line(m.positionI, characterMap[np.x, np.y].positionI);
					float timeOffset = 0;
					for(int i=0;i<line.Count()-1;i++){
						mapColors[line[i].x, line[i].y] = new ColorLerp(Color.green, 0.25f+timeOffset);
						timeOffset += 0.1f;
					}
					MonsterAttackChar (m, characterMap [np.x, np.y]);
					return;
				}
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
	void MonsterAttack(RLCharacter m){
		for (int i = 0; i < 4; i++) {
			Vector2i np = m.positionI + new Vector2i (RL.Map.nDir [i, 0], RL.Map.nDir [i, 1]);
			if (characterMap [np.x, np.y] != null) {
				MonsterAttackChar (m, characterMap [np.x, np.y]);
				break;
			}
		}
	}
	void MonsterAttackChar(RLCharacter m, RLCharacter c){
		c.health--;
		// if the character is at 0, then remove it
		MapBloodStain (c.positionI.x, c.positionI.y);
		if (c.health <= 0) {
			characters.Remove (c);
			c.Kill ();
			Camera.main.audio.PlayOneShot (playerDeathAudio);
		} else {
			Camera.main.audio.PlayOneShot (playerHurtAudio);
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
	RLCharacter CreateCharacter(int px, int py, char display, RLCharacter.RLTypes t){
		GameObject go = new GameObject();
		RLCharacter rlc = go.AddComponent<RLCharacter> ();
		rlc.display = display;
		rlc.AddType (t);
		rlc.health = 3;
		rlc.SetPosition (px, py);
		return rlc;
	}
	float gameOverStartTime;
	void GameOverStart(){
		gameOverStartTime = Time.time;
		gameOver.info.text = "reached level " + (currentLevel + 1) + "\n";
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
			}
		}
	}
	// Update is called once per frame
	void Update () {
		display.SetOffset (4, 1);
		// render all the walls on the map
		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				if((x+y)%2 == 0)
					display.AssignTileFromChar (x, y, ' ', Color.black, Color.Lerp(floorC, mapColors[x,y].c, mapColors[x,y].amt));
				else
					display.AssignTileFromChar (x, y, ' ', Color.black, Color.Lerp(floorC*0.85f, mapColors[x,y].c, mapColors[x,y].amt));
				switch(map.GetTile (x, y)) {
				case RL.TileType.WALL:
					display.AssignTileFromChar (x, y, (char)wallTopIndex, Color.Lerp(WallCTop, mapColors[x,y].c, mapColors[x,y].amt), Color.Lerp(wallC, mapColors[x,y].c, mapColors[x,y].amt));
					break;
				case RL.TileType.HARD_WALL:
					display.AssignTileFromChar (x, y, (char)wallTopIndex, Color.Lerp(WallCTop, mapColors[x,y].c, mapColors[x,y].amt), Color.Lerp(wallC, mapColors[x,y].c, mapColors[x,y].amt));
					break;
				case RL.TileType.STAIRS_DOWN:
					display.AssignTileFromOffset (x, y, 4, 19, Color.Lerp(Color.white, mapColors[x,y].c, mapColors[x,y].amt), Color.clear);
					break;
				default:
					break;				
				}
			}
		}
		foreach (RLCharacter p in pickups) {
			if (p.hasTypes.Contains (RLCharacter.RLTypes.WAITING_PLAYER)) {
				display.AssignTileFromOffset (p.positionI.x, p.positionI.y,  p.GetComponent<TileSelector>().tileX, p.GetComponent<TileSelector>().tileY, Color.white*0.5f, Color.clear, 1);
			} else {
				switch (p.display) {
				case 'a':
					display.AssignTileFromOffset (p.positionI.x, p.positionI.y, pickupDefs [0].sx, pickupDefs [0].sy, Color.white, Color.clear);			
					break;
				case 'h':
					display.AssignTileFromOffset (p.positionI.x, p.positionI.y, pickupDefs [1].sx, pickupDefs [1].sy, Color.white, Color.clear);			
					break;
				case 'g':
					display.AssignTileFromOffset (p.positionI.x, p.positionI.y, pickupDefs [4].sx, pickupDefs [4].sy, Color.white, Color.clear);			
					break;
				}
			}
		}
		AudioSource[] characterAudio = { archerAudio, mageAudio, WarriorAudio, priestAudio, minerAudio };
		foreach(AudioSource aud in characterAudio){
			aud.volume -= Time.deltaTime * 0.1f;
		}
		foreach (RLCharacter c in characters) {
			display.AssignTileFromOffset (c.positionI.x, c.positionI.y,  c.GetComponent<TileSelector>().tileX,c.GetComponent<TileSelector>().tileY, Color.white, Color.clear);
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
		foreach (RLCharacter m in monsters) {
			display.AssignTileFromOffset (m.positionI.x, m.positionI.y, m.GetComponent<TileSelector>().tileX,m.GetComponent<TileSelector>().tileY, (m.health==1?Color.red:new Color(186/255f, 12/255f, 250/255f)), Color.clear);
		}
		display.SetOffset (0, 0);
		int offsetY = 0;
		int offsetX = 0;
		foreach (RLCharacter c in characters) {
			display.AssignTileFromOffset (offsetX, 14+offsetY,  c.GetComponent<TileSelector>().tileX,c.GetComponent<TileSelector>().tileY, Color.white, Color.clear);
			for (int i = 0; i < 5; i++) {
				if (c.GetComponent<ActionCounter> ().actionsRemaining > i) {
					display.AssignTileFromOffset (4 + i + offsetX, 14 + offsetY, pickupDefs [0].sx, pickupDefs [0].sy, Color.white, Color.clear);
				} else {
					display.AssignTileFromOffset (4 + i + offsetX, 14+offsetY,  pickupDefs[2].sx,pickupDefs[2].sy, Color.white, Color.clear);
				}
			}
			for (int i = 0; i < 4; i++) {
				if (c.health > i) {
					display.AssignTileFromOffset (1 + i + offsetX, 14 + offsetY, pickupDefs [1].sx, pickupDefs [1].sy, Color.white, Color.clear);
				} else {
					display.AssignTileFromOffset (1 + i + offsetX, 14 + offsetY,  pickupDefs[3].sx,pickupDefs[3].sy, Color.white, Color.clear);
				}
			}
			offsetY--;
			if(offsetY==-2){
				offsetY = 0;
				offsetX = 11;
			}
		}
		for (int x = 0; x < map.sx; x++) {
			for (int y = 0; y < map.sy; y++) {
				display.SetForegroundColor (x, y, Color.Lerp (display.GetForegroundColor (x, y), mapColorsForeground [x, y].c, mapColorsForeground [x, y].amt));
			}
		}
		fsm.CurrentState.Update ();
	}
}