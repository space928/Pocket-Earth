using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Citizen;
using static RoadGen;

namespace Assets.Scripts
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider))]
    public class RoadSegment : SelectablePlanetObject
    {
        [SerializeField][Range(1, 6)] private int roadLanes = 1;
        [SerializeField] private LaneType[] laneTypes = new LaneType[6];
        private float laneWidth;
        private RoadNode roadNodeA;
        private RoadNode roadNodeB;
        private RoadNode roadNodeC;
        private RoadNode roadNodeD;
        private Material roadMaterial;
        private Mesh roadMesh;
        private RoadGen roadGen;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        public RoadNode StartNode => roadNodeB;
        public RoadNode EndNode => roadNodeC;
        public LaneType[] LaneTypes => laneTypes;

        public float GetLengthByMode(ModeOfTransport mode)
        {
            float best = 0;
            foreach(LaneType laneType in laneTypes)
                best = Mathf.Max(best, CompatibleModeFactor(mode, laneType));

            if (best == 0)
                return float.PositiveInfinity;

            float dist = (roadNodeB.transform.position - roadNodeC.transform.position).magnitude;
            dist /= best;

            return dist;
        }

        public float CompatibleModeFactor(ModeOfTransport mode, LaneType type)
        {
            switch (mode)
            {
                case ModeOfTransport.Walking:
                    return type switch
                    {
                        LaneType.Path => .75f,
                        LaneType.Cycle => 0.5f,
                        LaneType.RoadR or LaneType.RoadL => 0.25f,
                        _ => 0,
                    };
                case ModeOfTransport.Cycling:
                    return type switch
                    {
                        LaneType.Cycle => 1.0f,
                        LaneType.Path => 0.25f,
                        LaneType.RoadR or LaneType.RoadL => 0.5f,
                        LaneType.BusR or LaneType.BusL => 0.5f,
                        _ => 0,
                    };
                case ModeOfTransport.Driving:
                    return type switch
                    {
                        LaneType.Cycle => 0,
                        LaneType.Path => 0,
                        LaneType.RoadR or LaneType.RoadL => 1.1f,
                        LaneType.BusR or LaneType.BusL => 0,
                        _ => 0,
                    };
                case ModeOfTransport.Bussing:
                    return type switch
                    {
                        LaneType.Cycle => 0,
                        LaneType.Path => 0,
                        LaneType.RoadR or LaneType.RoadL => 1,
                        LaneType.BusR or LaneType.BusL => 1.3f,
                        _ => 0,
                    };
                default:
                    return 0;
            }
        }

        public void Construct(float laneWidth, RoadNode roadNodeA, RoadNode roadNodeB, RoadNode roadNodeC, RoadNode roadNodeD, RoadGen roadGen)
        {
            this.laneWidth = laneWidth;
            this.roadNodeA = roadNodeA;
            this.roadNodeB = roadNodeB;
            this.roadNodeC = roadNodeC;
            this.roadNodeD = roadNodeD;
            this.roadGen = roadGen;
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            if (roadMaterial == null)
                roadMaterial = new(roadGen.roadMaterialTemplate);
            meshRenderer.sharedMaterial = roadMaterial;
            SelectableMaterial = roadMaterial;
        }

        private void Start()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = roadMaterial;
            meshCollider = GetComponent<MeshCollider>();
            SelectableMaterial = roadMaterial;
        }

        void Update()
        {
            SelectionUpdate();
        }

#if UNITY_EDITOR
        public void OnDrawGizmosSelected()
        {
            EditorUpdate();
        }
#endif

        public override int GetHashCode()
        {
            return HashCode.Combine(roadNodeA, roadNodeB, roadNodeC, roadNodeD);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 0, 1, 0.75F);
            // This is approximate, and will always be slightly too short.
            float segmentLength = (roadNodeB.transform.position - this.roadNodeC.transform.position).magnitude;
            int iters = (int)(roadGen.resolution * segmentLength);
            Vector3 lastPoint = Interpolate(0);
            for (int i = 1; i <= iters; i++)
            {
                Vector3 point = Interpolate(i / (float)iters);
                Gizmos.DrawLine(lastPoint, point);
                lastPoint = point;
            }
        }

        public Vector3 Interpolate(float t)
        {
            if (!this.roadNodeA && !this.roadNodeD)
            {
                Vector3 pos = Vector3.LerpUnclamped(roadNodeB.transform.position, roadNodeC.transform.position, t);
                pos *= Planet.GetRadiusAtPoint(pos) / pos.magnitude;
                return pos;
            }

            if (!this.roadNodeA)
                return GetCatmullRomPosition(t+2/3, 
                    this.roadNodeB.transform.position, this.roadNodeB.transform.position, 
                    this.roadNodeC.transform.position, this.roadNodeD.transform.position);

            if (!this.roadNodeD)
                return GetCatmullRomPosition(t,
                    this.roadNodeA.transform.position, this.roadNodeB.transform.position,
                    this.roadNodeC.transform.position, this.roadNodeC.transform.position);

            return GetCatmullRomPosition(t + 1/3,
                    this.roadNodeA.transform.position, this.roadNodeB.transform.position,
                    this.roadNodeC.transform.position, this.roadNodeD.transform.position);
        }

        //Returns a position between 4 Vector3 with Catmull-Rom spline algorithm
        //http://www.iquilezles.org/www/articles/minispline/minispline.htm
        Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            //The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            //The cubic polynomial: a + b * t + c * t^2 + d * t^3
            Vector3 pos = 0.5f * (a + (b * t) + (t * t * c) + (t * t * t * d));

            // Now project that position back onto the sphere
            pos *= Planet.GetRadiusAtPoint(pos) / pos.magnitude;

            return pos;
        }

        public void UpdateRoadMesh()
        {
            Mesh rmesh = new();

            int rp = 0;
            int vertsPerSlice = roadGen.roadProfiles[rp].length;//TODO: will throw error if no road profiles and doesn't allow for more than one.

            List<Vector3> newVerts = new();
            // This is approximate, and will always be slightly too short.
            float segmentLength = (roadNodeB.transform.position - this.roadNodeC.transform.position).magnitude;
            int iters = Mathf.Max((int)(roadGen.resolution * segmentLength), 1);
            Vector3 lastPoint = Interpolate(0);
            for (int i = 1; i <= iters+1; i++)
            {
                Vector3 point = Interpolate(i / (float)iters);

                for (int x = 0; x < vertsPerSlice; x++)
                {
                    Vector2 rpcoord = new(roadGen.roadProfiles[rp].keys[x].time, roadGen.roadProfiles[rp].keys[x].value);
                    rpcoord.Scale(new Vector2(roadLanes, roadGen.roadHeight));
                    rpcoord.x -= roadLanes / (float)2;
                    Vector3 newVert = lastPoint;
                    Vector3 slice = new(rpcoord.x, rpcoord.y, 0);
                    if ((point - lastPoint).normalized.magnitude == 0)
                        Debug.Log($"fwd is 0: p0={lastPoint} p1={point}");
                    if (GetNormal(lastPoint).magnitude == 0)
                        Debug.Log($"nrm is 0: p0={lastPoint} p1={point}");
                    Quaternion dir = Quaternion.LookRotation((point - lastPoint).normalized, GetNormal(lastPoint));
                    newVert += Matrix4x4.TRS(-transform.position, dir /* Quaternion.Euler(-transform.localEulerAngles)*/, Vector3.one).MultiplyPoint(slice);

                    newVerts.Add(newVert);
                }

                lastPoint = point;
            }
            rmesh.SetVertices(newVerts);

            List<int> tris = new();
            int nverts = rmesh.vertexCount;
            for (int y = 0; y < nverts; y++)
            {
                if (y % vertsPerSlice == vertsPerSlice - 1)
                    continue;

                tris.Add(Clamp(y - vertsPerSlice, nverts));
                tris.Add(y);
                tris.Add(Clamp(y - vertsPerSlice + 1, nverts));

                tris.Add(y);
                tris.Add(Clamp(y + 1, nverts));
                tris.Add(Clamp(y - vertsPerSlice + 1, nverts));
            }

            rmesh.SetTriangles(tris, 0);

            List<Vector2> uvs = new();
            for (int z = 0; z < nverts; z++)
            {
                float x = roadGen.roadProfiles[rp].keys[(z % vertsPerSlice)].time;
                uvs.Add(new Vector2(Mathf.Floor(z / ((float)vertsPerSlice)), x));
            }
            rmesh.SetUVs(0, uvs);

            /*List<Vector3> normals = new List<Vector3> ();
            for (int z = 0; z < nverts; z++)
            {
                normals.Add (Vector3.up);
            }

            rmesh.SetNormals (normals);*/

            rmesh.RecalculateBounds();
            rmesh.RecalculateNormals();
            rmesh.RecalculateTangents();
            //rmesh.UploadMeshData (false);
            
            meshFilter.sharedMesh = rmesh;
            meshCollider.sharedMesh = rmesh;
        }
    }

    public enum LaneType
    {
        RoadL,
        RoadR,
        Path,
        Cycle,
        BusL,
        BusR
    }
}