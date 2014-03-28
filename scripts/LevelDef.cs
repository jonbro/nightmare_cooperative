using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PossibleMonster{
	public string name;
	public float percent;
	public float min, max;
}
public class LevelDef
{
	float totalWeight;
	bool needsRebuildPercent = true;
	public PossibleMonster[] possibleMonsters;
	public int spawnRate = 10;
	public int spawnCountLow = 3;
	public int spawnCountHigh = 5;
	void RebuildPercent(){
		foreach (PossibleMonster p in possibleMonsters) {
			totalWeight += p.percent;
		}
		needsRebuildPercent = false;
	}
	public MonsterDef GetMonsterForLevel(){

		// returns the monster def for this level
		if (needsRebuildPercent)
			RebuildPercent ();
		float rand = Random.value * totalWeight;
		MonsterDef selectedMonster = null;
		foreach (PossibleMonster p in possibleMonsters) {
			if (rand < p.percent) {
				// found our monster
				// find it in the monster defs
				Debug.Log ("found monster! " + p.name);
				foreach (MonsterDef m in MonsterDef.monsterDefs) {
					if (m.name == p.name)
						selectedMonster = m;
				}
				break;
			}
			rand -= p.percent;
		}
		if(selectedMonster == null)
			Debug.LogError ("couldn't find requested monster: "+ rand);
		return selectedMonster;
	}
	/*
	 * non random enemies
	 * 
	 * patrol
	 * good patrol
	 * chase
	 * golden guard
	 * lava
	 * acid
	 * trap
	 * twisted trap
	 * 
	 * */
	public static LevelDef[] levels = new LevelDef[]{
		// 01x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "player copy",
					percent = 0.2f
				},
				new PossibleMonster{
					name = "patrol", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.1f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.05f
				}
			},
			spawnRate = 15
		},
		// 02x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "patrol", 
					percent = 0.1f
				},
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.05f
				}
			},
			spawnRate = 15
		},
		// 03x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "patrol", 
					percent = 0.1f
				},
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.4f
				}
			},
			spawnRate = 12
		},
		// 04x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "patrol", 
					percent = 0.1f
				},
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.2f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.5f
				}
			},
			spawnRate = 12
		},

		// 05x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "good patrol until distance", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.1f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.5f
				}
			},
		},
		// 06x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "good patrol until distance", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "twisted trap", 
					percent = 0.3f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.1f
				}
			}
		},
		// 07x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "twisted trap", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "acid", 
					percent = 0.4f
				},
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.2f
				},
			}
		},
		// 08x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "chase", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "twisted trap", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.5f
				}
			},
			spawnRate = 9
		},
		// 09x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "chase", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "twisted trap", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "lava", 
					percent = 0.1f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.5f
				}
			},
			spawnRate = 9
		},
		// 10x
		new LevelDef{
			possibleMonsters = new PossibleMonster[]{
				new PossibleMonster{
					name = "good patrol", 
					percent = 0.25f
				},
				new PossibleMonster{
					name = "chase", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "lava", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "trap", 
					percent = 0.5f
				},
				new PossibleMonster{
					name = "twisted trap", 
					percent = 0.5f
				},
			},
			spawnRate = 8
		},
	};
}