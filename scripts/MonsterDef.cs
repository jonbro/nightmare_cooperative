using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterDef {
	public string name = "";
	public int sx = 0;
	public int sy = 0;
	public RLCharacter.RLTypes moveType;
	public bool hasTurnCount, hasFireCount;
	public Color color = Color.white;
	public string description;
	public bool isTile;
	public bool isPassable = true;
	public bool isDistance;
	public int[] directions;
	public RL.TileType tile;

	// all the defs that are used by the game
	static public MonsterDef[] monsterDefs = new MonsterDef[] {
		new MonsterDef {
			description = "random walk",
			name = "wanderer",
			sx = 4,
			sy = 1,
			moveType = RLCharacter.RLTypes.MONSTER_WANDER
		},
		new MonsterDef {
			description = "random until LOS",
			name = "bad guard",
			sx = 4,
			sy = 2,
			moveType = RLCharacter.RLTypes.MONSTER_WANDER_LOS
		},
		new MonsterDef {
			description = "random until distance",
			name = "alert guard",
			sx = 4,
			sy = 6,
			moveType = RLCharacter.RLTypes.MONSTER_WANDER_CHASE
		},
		new MonsterDef {
			description = "chaser",
			name = "chase",
			sx = 4,
			sy = 7,
			moveType = RLCharacter.RLTypes.MONSTER_CHASE
		},
		new MonsterDef {
			description = "fire at distance",
			name = "trap",
			sx = 4,
			sy = 8,
			moveType = RLCharacter.RLTypes.MONSTER_DISTANCE_FIRE,
			hasTurnCount =  true,
			isDistance = true,
			directions = new int[]{0,1,2,3}
		},
		new MonsterDef {
			description = "fire at distance diagonal",
			name = "twisted trap",
			sx = 4,
			sy = 9,
			moveType = RLCharacter.RLTypes.MONSTER_DISTANCE_FIRE_DIAGONAL,
			isDistance = true,
			directions = new int[]{4,5,6,7},
			hasTurnCount =  true
		},
		new MonsterDef {
			description = "hit wall and bounce",
			name = "patrol",
			sx = 4,
			sy = 5,
			moveType = RLCharacter.RLTypes.MONSTER_WALL_BOUNCE,
			hasTurnCount =  true
		},
		new MonsterDef {
			description = "hit wall and turn",
			name = "good patrol",
			sx = 4,
			sy = 4,
			moveType = RLCharacter.RLTypes.MONSTER_WALL_TURN,
			hasTurnCount =  true
		},
		new MonsterDef {
			description = "hit wall and turn",
			name = "good patrol until distance",
			sx = 4,
			sy = 4,
			moveType = RLCharacter.RLTypes.MONSTER_WALL_TURN_CHASE,
			hasTurnCount =  true
		},
		new MonsterDef {
			description = "wait chaser",
			name = "golden guard",
			sx = 4,
			sy = 3,
			moveType = RLCharacter.RLTypes.MONSTER_WAIT_CHASE
		},
		new MonsterDef {
			description = "player copy",
			name = "player copy",
			sx = 4,
			sy = 3,
			moveType = RLCharacter.RLTypes.MONSTER_PLAYER_COPY
		},
		new MonsterDef {
			description = "acid pool",
			name = "acid",
			sx = 4,
			sy = 3,
			moveType = RLCharacter.RLTypes.MONSTER_NO_MOVE,
			isTile = true,
			tile = RL.TileType.ACID
		},
		new MonsterDef {
			description = "lava",
			name = "lava",
			sx = 4,
			sy = 3,
			moveType = RLCharacter.RLTypes.MONSTER_NO_MOVE,
			isTile = true,
			isPassable = false,
			tile = RL.TileType.LAVA
		},
	};
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
		},
	};
}
