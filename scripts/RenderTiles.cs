using UnityEngine;
using System.Collections;

public class RenderTiles : MonoBehaviour {
	public Vector2 gridSize;
	public Vector2 worldSize;
	public float perlinScale, timeScale;
	public int spriteRow;
	public Color hiColor;
	public Color lowColor;
	private Mesh mesh;
	private Vector2[] uv;
	private Color32[] cs;
	private Vector3[] vertices;
	// Use this for initialization
	void Start () {
		CreateMesh();
		for (int x = 0; x < gridSize.x; x++) {
			for (int y = 0; y < gridSize.y; y++) {
				AssignSprite(x,y,10, 10);
			}
		}
	}
	void Update(){
		/**/	
		for (int x = 0; x < gridSize.x; x++) {
			for (int y = 0; y < gridSize.y; y++) {
//				AssignSprite(x,y,10, 10);
//				AssignColor (x, y, Color.black);
				AssignSprite(x,y,spriteRow, (int)(Mathf.PerlinNoise(x/perlinScale+Time.time*timeScale, y/perlinScale)*12.0f));
				AssignColor(x,y,Color.Lerp(lowColor, hiColor, Mathf.PerlinNoise(x/perlinScale*2+Time.time*timeScale, y/perlinScale)));
			}
		}

	}	
	// Update is called once per frame
	void LateUpdate () {
		GetComponent<MeshFilter>().mesh.uv = uv;
		GetComponent<MeshFilter>().mesh.colors32 = cs;
		GetComponent<MeshFilter>().mesh.vertices = vertices;
	}
	static int[,,] rotations = {
		{{0,0}, {1,0}, {0,1}, {1,1}},
		{{0,1}, {0,0}, {1,1}, {1,0}},
		{{1,1}, {0,1}, {1,0}, {0,0}},
		{{1,0}, {1,1}, {0,0}, {0,1}},
	};
	public void AssignSprite(int mX, int mY, int sX, int sY, int rotate=0){
		// should extract the proper uv, then reassign, and assign back to mesh
		// might want to calculate this from the tile size eventually, but for now, hardcoding
		float tileSizeX = 8.0f/128.0f;
		float tileSizeY = 8.0f/256.0f;
		if (mX<0 || mX > gridSize.x-1 || mY > gridSize.y-1)
			return;
		int index = (mY*(int)gridSize.x+mX)*4;
		for (int i = 0; i < 4; i++) {
			uv[index+i] = new Vector2( (sX+rotations[rotate%4, i, 0]) * tileSizeX, (sY+rotations[rotate%4, i, 1])*tileSizeY);
		}
	}
	public void AddVertexOffset(int x, int y, Vector3 offset){
		if (x<0 || x > gridSize.x-1 || y > gridSize.y-1)
			return;
		int index = (y*(int)gridSize.x+x)*4;
		vertices[index+Random.Range(0,4)] += offset;
	}
	public Color GetColor(int x, int y){
		int index = (y*(int)gridSize.x+x)*4;
		return (Color)cs[index];
	}
	public void AssignColor(int x, int y, Color c){
		if (x<0 || x > gridSize.x-1 || y > gridSize.y-1)
			return;
		int index = (y*(int)gridSize.x+x)*4;
		cs[index+3] = cs[index+2] = cs[index+1] = cs[index] = (Color32)c;
	}
	public void clearAll(){
		for(int x=0;x<gridSize.x;x++){
			for(int y=0;y<gridSize.y;y++){
				AssignColor(x, y, Color.clear);
			}
		}
	}
	public void CreateMesh(){
		if(mesh == null){
			GetComponent<MeshFilter>().mesh = mesh = new Mesh();
			mesh.name = "Star Mesh";
			mesh.hideFlags = HideFlags.HideAndDontSave;
		}
        mesh.Clear();

		// feel free to waste triangles, I have plenty!
		vertices = new Vector3[(int)gridSize.x*(int)gridSize.y*4];
		int[] tri = new int[(int)gridSize.x*(int)gridSize.y*6];
		uv = new Vector2[(int)gridSize.x*(int)gridSize.y*4];
		cs = new Color32[(int)gridSize.x*(int)gridSize.y*4];
		float xSize = worldSize.x;
		float ySize = worldSize.y;
		Vector3 worldOffset = transform.localPosition;
		for(int x=0;x<gridSize.x;x++){
			for(int y=0;y<gridSize.y;y++){
				int index = (y*(int)gridSize.x+x)*4;
				int tindex = (y*(int)gridSize.x+x)*6;
				//Debug.Log(xSize*x);

				vertices[0+index] = new Vector3(xSize*x,ySize*y,0)+worldOffset;
				vertices[1+index] = new Vector3(xSize*(x+1), ySize*y, 0)+worldOffset;
				vertices[2+index] = new Vector3(xSize*x, ySize*(y+1), 0)+worldOffset;
				vertices[3+index] = new Vector3(xSize*(x+1), ySize*(y+1), 0)+worldOffset;

				tri[0+tindex] = 0+index;
				tri[1+tindex] = 2+index;
				tri[2+tindex] = 1+index;
			
				tri[3+tindex] = 2+index;
				tri[4+tindex] = 3+index;
				tri[5+tindex] = 1+index;
				
				uv[0+index] = new Vector2((float)x/(float)gridSize.x, y/(float)gridSize.y);
				uv[1+index] = new Vector2((x+1)/(float)gridSize.x, y/(float)gridSize.y);
				uv[2+index] = new Vector2(x/(float)gridSize.x, (y+1)/(float)gridSize.y);
				uv[3+index] = new Vector2((x+1)/(float)gridSize.x, (y+1)/(float)gridSize.y);
			}
		}
		mesh.vertices = vertices;
		mesh.triangles = tri;
		mesh.uv = uv;
		mesh.RecalculateNormals();
	}
}
