using System;
using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

public class Scores{
	[XmlArray("scores"), XmlArrayItem("ScoreItem")]
	public List<ScoreItem> scores = new List<ScoreItem>();
	public static int version = 1;
	public int _version;

	public void Save(){
		var serializer = new XmlSerializer(typeof(Scores));
		this._version = version;
		StringWriter textWriter = new StringWriter();
		serializer.Serialize(textWriter, this);
		PlayerPrefs.SetString("HighScores", textWriter.ToString());
	}
	public static Scores Load()
	{
//		string path = Application.persistentDataPath + "/HighScores.xml";
		if (!PlayerPrefs.HasKey("HighScores")) {
			return new Scores ();
		} else {
			var serializer = new XmlSerializer (typeof(Scores));
			using (StringReader reader = new StringReader(PlayerPrefs.GetString("HighScores")))
			{
				Scores iMap = serializer.Deserialize (reader) as Scores;
				if (iMap._version == null || iMap._version != version) {
					iMap = new Scores ();
					iMap.Save ();
				}
				return iMap;
			}
		}
		return new Scores ();
	}
	public void Add(ScoreItem si){
		scores.Add (si);
	}
	public void SortTable(){
		scores.Sort ();
	}
}
public class ScoreItem : IComparable
{
	public bool won;
	public int levelReached;
	public int gold;
	public DateTime time;

	[XmlArray("charactersJoined"), XmlArrayItem("RLCharacter.RLTypes")]
	public List<RLCharacter.RLTypes> charactersJoined;

	public ScoreItem ()
	{

	}
	public int CompareTo(object obj)
	{
		ScoreItem other = obj as ScoreItem;

		if (other.won && !won)
			return 1;
		else if (won && !other.won)
			return -1;
		else if (other.levelReached > levelReached)
			return 1;
		else if (levelReached > other.levelReached)
			return -1;
		else if (other.gold > gold)
			return 1;
		else if (gold > other.gold)
			return -1;

		return time.CompareTo (other.time);
	}
}

