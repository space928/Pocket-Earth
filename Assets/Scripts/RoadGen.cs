using Assets.Scripts;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Old dogy code incoming...
public class RoadGen : MonoBehaviour
{
	[Header("Dependencies")]
	public Planet planet;
    [Tooltip("These prefabs are in order based on number of intersections (3, 4, 5...)")] public List<GameObject> intersectionPrefabs;
	public Material roadMaterialTemplate;
	public Transform roadSegmentsParent;
    [Header("Update")]
    public bool autoUpdateMesh = false;

	[Header("Profile Definitions")]
    public AnimationCurve[] roadProfiles;
    public float roadWidth;
    public float roadHeight;
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
        UpdateMesh();
	}

	[Button]
	public void UpdateMesh()
	{
		GenerateRoadSegments();
		foreach(var segment in segments)
		{
			segment.UpdateRoadMesh();
		}
	}

	[Button]
	public void GenerateRoadSegments()
	{
        RoadNode[] roadNodes = GetComponentsInChildren<RoadNode>();
		HashSet<RoadSegment> segmentHashes = new(segments);

		// Search for existing road segments
		for (int i = 0; i < roadSegmentsParent.childCount; i++)
		{
			var rs = roadSegmentsParent.GetChild(i).GetComponent<RoadSegment>();
			if (!segmentHashes.Contains(rs))
			{
				segments.Add(rs);
				segmentHashes.Add(rs);
			}
		}

        // Destroyed orphaned segments
        for (int i = segments.Count-1; i >= 0; i--)
			if (segments[i] && (!segments[i].StartNode || !segments[i].EndNode))
			{
#if UNITY_EDITOR
				if (Application.isPlaying)
					Destroy(segments[i]);
				else
					DestroyImmediate(segments[i]);
#else
				Destroy(segments[i]);
#endif
				segments[i] = null;
            }
        segments.RemoveAll(seg => seg == null);
        if (roadNodes.Length < 1)
            return;

		Dictionary<int, RoadSegment> segmentCache = new(segments.Select(x => new KeyValuePair<int, RoadSegment>(
			x.StartNode.GetHashCode() ^ x.EndNode.GetHashCode(), x)));

		HashSet<RoadNode> visited = new();
        Stack<RoadNode> currNodes = new();
		currNodes.Push(roadNodes[0]);
		while(currNodes.Count > 0)
		{
			RoadNode curr = currNodes.Pop();
			visited.Add(curr);
			// Clean up dead children
			curr.connections.RemoveAll(x => x == null);
			foreach(RoadNode child in curr.connections)
			{
				if(visited.Contains(child)) continue;

				currNodes.Push(child);

				var nodeStart = curr;
				var nodeEnd = child;

				RoadNode nodePreStart = null;
				RoadNode nodePostEnd = null;
				if (curr.connections.Count == 2)
					nodePreStart = curr.connections[0] == child ? curr.connections[1] : curr.connections[0];

                if (child.connections.Count == 2)
                    nodePostEnd = child.connections[0] == curr ? child.connections[1] : child.connections[0];

				if (segmentCache.ContainsKey(nodeStart.GetHashCode() ^ nodeEnd.GetHashCode()))
				{
					segmentCache[nodeStart.GetHashCode() ^ nodeEnd.GetHashCode()].Construct(roadWidth, nodePreStart, nodeStart, nodeEnd, nodePostEnd, this);
					continue;
				}

                // Generate a new road segment between this child and it's parent
                GameObject segmentGo = new($"Road Segment {nodeStart.GetHashCode() ^ nodeEnd.GetHashCode()}");
				segmentGo.transform.SetParent(roadSegmentsParent, false);
				var segment = segmentGo.AddComponent<RoadSegment>();
				segment.Construct(roadWidth, nodePreStart, nodeStart, nodeEnd, nodePostEnd, this);
				segment.Planet = planet;
				segment.AlignRotation = false;
				segment.Position = (nodeStart.transform.position + nodeEnd.transform.position) / 2;
				segments.Add(segment);
			}
		}
    }

    public void OnDrawGizmos()
    {
        if(autoUpdateMesh)
		{
			UpdateMesh();
		}
    }

    public static int Clamp (int x, int count)
	{
		int ret = x >= 0 ? x : 0;
		if (ret > count - 1)
			ret = count - 1;
		return  ret;
	}
}
