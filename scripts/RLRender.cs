using UnityEngine;
using System.Collections;

// used for getting graphics onto the screen
public class RLRender : MonoBehaviour {

	RenderTiles bgt;
	RenderTiles fgt;

	// Use this for initialization
	void Start () {
		// generate the foreground and background layers
		GameObject fgtgo = new GameObject ();
		GameObject bgtgo = new GameObject ();
		fgtgo.transform.parent = transform;
		bgtgo.transform.parent = transform;
		fgtgo.name = "foreground render";
		bgtgo.name = "background render";
		fgt = AddTileRenender (fgtgo);
		bgt = AddTileRenender (bgtgo);
	}
	RenderTiles AddTileRenender(GameObject g){
		g.AddComponent<MeshFilter> ();
		g.AddComponent<MeshRenderer> ();
		g.renderer.material = (Material)Resources.Load ("tileRenderingMat");
		return g.AddComponent<RenderTiles> ();
	}
	public void Setup(int mapSizeX, int mapSizeY, float screenSizeX, float screenSizeY){
		fgt.gridSize = new Vector2 (mapSizeX, mapSizeY);
		fgt.worldSize = new Vector2 (screenSizeX / mapSizeX, screenSizeY / mapSizeY);
		bgt.gridSize = new Vector2 (mapSizeX, mapSizeY);
		bgt.worldSize = new Vector2 (screenSizeX / mapSizeX, screenSizeY / mapSizeY);
		// offset both by half so it is centered on this object
		fgt.transform.localPosition = new Vector3 (-screenSizeX / 2, -screenSizeY / 2, 0);
		bgt.transform.localPosition = new Vector3 (-screenSizeX / 2, -screenSizeY / 2, 0);
		fgt.CreateMesh ();
		bgt.CreateMesh ();
	}
	public void AssignTileFromChar(int x, int y, char t, Color fgColor, Color bgColor){
		int c = (int)t;
		int fgx = 0, fgy = 0;
		if(c>=32&&c<=63){
			fgx = 2;
			fgy = 31-(c-32); // to account for the @ symbol at the beginning			
		}else if(c>=64&&c<=90){
			fgx = 0;
			fgy = 31-(c-64); // to account for the @ symbol at the beginning
		}else if(c>=97&&c<=122){
			fgx = 5;
			fgy = 31-(c-96); // to account for the - symbol at the beginning
		}
		AssignTileFromOffset (x, y, fgx, fgy, fgColor, bgColor);
	}
	public void AssignTileFromChar(int x, int y, char t){
		AssignTileFromChar(x, y, t, Color.white, Color.black);
	}
	public void AssignTileFromOffset(int x, int y, int sx, int sy, Color fgc, Color bgc, int rotate = 0){
		if (bgc != Color.clear) {	
			bgt.AssignSprite (x, y, 11, 31); // just force the fill color on the background
			bgt.AssignColor (x, y, bgc);
		}
		fgt.AssignSprite(x,y,sx,sy,rotate);
		fgt.AssignColor(x,y,fgc);
	}
	public void Console(string t, int startX, int startY){
		int penPosX = startX;
		int penPosY = startY;

		for(int i=0;i<t.Length;i++){
			if(t[i] == '\n'){
				penPosX = 0;
				penPosY--;
			}else{
				AssignTileFromChar(penPosX, penPosY, t[i]);
				penPosX++;
			}
		}
	}
}
