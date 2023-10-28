using Assets.Scripts;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Old dogy code incoming...
[RequireComponent (typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
public class RoadGen : MonoBehaviour
{
	[Header("Update")]
	public bool draw = false;
    public bool autoDraw = false;

	[Header("Profile Definitions")]
    public AnimationCurve[] roadProfiles;
    public float roadWidth;
    public float roadHeight;
	[Tooltip("These prefabs are in order based on number of intersections (3, 4, 5...)")]public List<GameObject> intersectionPrefabs;
	[Tooltip("Segments per m")]
    public float resolution;
    public int maxIterations = 2000;
		
	public float debug;

	private List<Line> spline = new();
	[SerializeField] [ReadOnly] private List<RoadSegment> segments = new();
	private Mesh dbgmesh;

	public struct Line
	{
		public Vector3 start;
		public Vector3 end;
		public bool edge;
		public float intersectionDist;

		public Line (Vector3 start, Vector3 end, bool edge, float intersectionDist)
		{
			this.start = start;
			this.end = end;
			this.edge = edge;
			this.intersectionDist = intersectionDist;
		}
	}

	// Use this for initialization
	void Start ()
	{
		GenerateRoadSegments ();
	}

	public void GenerateRoadSegments ()
	{
		spline.Clear ();
		RoadNode[] roadNodes = GetComponentsInChildren<RoadNode>();
		foreach (RoadNode n in roadNodes)
		{
			n.visitedEdges = new bool[n.connections.Count];
		}
		if (roadNodes.Length < 1)
			return;

		//TODO: this will hang if a road loops back on itself
		Stack<RoadNode> currNodes = new();
		currNodes.Push (roadNodes [0]);
		RoadNode last = roadNodes [0];
		while (currNodes.Count > 0)
		{
			if (spline.Count > maxIterations)
				break;
			
			RoadNode rn = currNodes.Pop ();

			//nodes.Add (rn);
			//rn.parent = last;
			// Update the visited edges for where we're coming from
			for (int x = 0; x < rn.visitedEdges.Length; x++)
				if (rn.connections [x] == rn.last)
					rn.visitedEdges [x] = true;
			
			int i = -1;
			foreach (RoadNode n in rn.connections)
			{
				i++;
				if (rn.visitedEdges [i])//Can't branch back to oneself
						continue;
				RoadNode nc = n;
				nc.last = rn;

				// Make sure we update the visited edges for where we're going
                for (int x = 0; x < rn.visitedEdges.Length; x++)
                    if (rn.connections[x] == nc)
                        rn.visitedEdges[x] = true;

                currNodes.Push(nc);
            }

			RoadNode[] child = rn.connections.Where (y => y != rn.last).ToArray ();//This only looks at one connection
			if (child.Length < 1)
				continue;
			
			foreach (RoadNode ch in child)
			{
				RoadNode childchild = ch.connections.FirstOrDefault (z => z != rn);
				if (childchild == null)
					childchild = ch;
				InterpSpline (new RoadNode[]{ last, rn, ch, childchild });

				//Mark end splines to prevent creation of extra triangles
				if (childchild == ch)
				{
					int sn = spline.FindLastIndex (x => true);
					spline [sn] = new Line (spline [sn].start, spline [sn].end, true, spline [sn].intersectionDist);
				}
			}

			last = rn;
		}
		//debug = spline.Count;

		if (draw)
		{
			if (!autoDraw)
				draw = false;
			
			//Mesh nroad = CreateRoadMesh (spline.ToArray ());
			/*dbgmesh = nroad;
			GetComponent<MeshFilter> ().mesh = nroad;
			GetComponent<MeshCollider> ().sharedMesh = nroad;*/
		}
	}

	Line[] InterpSpline (RoadNode[] nodes)
	{
		RoadNode curr = nodes [1];

		Vector3 p0 = nodes [0].transform.localPosition;
		Vector3 p1 = curr.transform.localPosition;
		Vector3 p2 = nodes [2].transform.localPosition;
		Vector3 p3 = nodes [3].transform.localPosition;

		//HACK:Naive distance
		float d = Mathf.Sqrt (Mathf.Pow (p1.x - p2.x, 2) + Mathf.Pow (p1.y - p2.y, 2) + Mathf.Pow (p1.z - p2.z, 2));

		int iters = (int)(resolution * d);
		Vector3 lastP = p1;
		for (int x = 1; x <= iters; x++)
		{
			Vector3 n = GetCatmullRomPosition (x / (float)iters, p0, p1, p2, p3);
			spline.Add (new Line (lastP, n, false, 0));
			lastP = n;
		}
		return spline.ToArray ();
	}

	public static int Clamp (int x, int count)
	{
		int ret = x >= 0 ? x : 0;
		if (ret > count - 1)
			ret = count - 1;
		return  ret;
	}

	//Returns a position between 4 Vector3 with Catmull-Rom spline algorithm
	//http://www.iquilezles.org/www/articles/minispline/minispline.htm
	Vector3 GetCatmullRomPosition (float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		//The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
		Vector3 a = 2f * p1;
		Vector3 b = p2 - p0;
		Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
		Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

		//The cubic polynomial: a + b * t + c * t^2 + d * t^3
		Vector3 pos = 0.5f * (a + (b * t) + (t * t * c) + (t * t * t * d));

		return pos;
	}

	void OnDrawGizmos ()
	{
		GenerateRoadSegments ();
		Gizmos.color = new Color (1, 1, 0, 0.75F);
		foreach (Line l in spline)
			Gizmos.DrawLine (l.start, l.end);

		/*Gizmos.color = Color.white;
		for (int i = 0; i < dbgmesh.vertices.Length - 1; i++)
		{
			//Gizmos.DrawRay (p, Vector3.up);
			int vertsPerSeg = roadProfiles [0].length;
			if (i % vertsPerSeg == vertsPerSeg - 1)
				continue;
			Gizmos.DrawLine (dbgmesh.vertices [i], dbgmesh.vertices [i + 1]);
		}*/
	}
}
