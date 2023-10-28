using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RoadGen;

namespace Assets.Scripts
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class RoadSegment : MonoBehaviour
    {
        [SerializeField] [Range(1, 6)] private int roadWidth = 1;
        [SerializeField] private LaneType[] laneTypes = new LaneType[6];
        private RoadNode roadNodeA;
        private RoadNode roadNodeB;
        private Material roadMaterial;
        private Mesh roadMesh;
        private RoadGen roadGen;

        public RoadSegment(int roadWidth, LaneType[] laneTypes, RoadNode roadNodeA, RoadNode roadNodeB, RoadGen roadGen)
        {
            this.roadWidth = roadWidth;
            this.laneTypes = laneTypes;
            this.roadNodeA = roadNodeA;
            this.roadNodeB = roadNodeB;
            this.roadGen = roadGen;
        }

        private void Start()
        {
            //roadMesh = GetComponent<MeshFilter>().sharedMesh;
            //roadMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        }

        public void Update()
        {

        }

        Mesh CreateRoadMesh(Line[] inSpline)
        {
            Mesh rmesh = new();

            int rp = 0;
            int vertsPerSeg = roadGen.roadProfiles[rp].length;//TODO: will throw error if no road profiles and doesn't allow for more than one.

            int i = -1;
            foreach (Line segment in inSpline)
            {
                i++;

                //if (i >= debug)
                //	break;

                Vector3[] lastVerts = rmesh.vertices;

                List<Vector3> newVerts = new();
                for (int x = 0; x < vertsPerSeg; x++)
                {
                    Vector2 rpcoord = new(roadGen.roadProfiles[rp].keys[x].time, roadGen.roadProfiles[rp].keys[x].value);
                    rpcoord.Scale(new Vector2(roadWidth, roadGen.roadHeight));
                    rpcoord.x -= roadWidth / 2;
                    Vector3 point = segment.start;
                    Vector3 billboard = new(rpcoord.x, rpcoord.y, 0);
                    Quaternion dir = Quaternion.LookRotation((segment.end - segment.start).normalized, Vector3.up);
                    point += Matrix4x4.TRS(Vector3.zero, dir, Vector3.one).MultiplyPoint(billboard);

                    newVerts.Add(point);
                }
                rmesh.SetVertices(lastVerts.Concat(newVerts).ToList());
            }

            List<int> tris = new();
            int nverts = rmesh.vertexCount;
            for (int y = 0; y < nverts; y++)
            {
                if (y % vertsPerSeg == vertsPerSeg - 1 || inSpline[(y - vertsPerSeg + 1) / vertsPerSeg].edge)
                    continue;

                tris.Add(Clamp(y - vertsPerSeg, nverts));
                tris.Add(y);
                tris.Add(Clamp(y - vertsPerSeg + 1, nverts));

                tris.Add(y);
                tris.Add(Clamp(y + 1, nverts));
                tris.Add(Clamp(y - vertsPerSeg + 1, nverts));
            }

            rmesh.SetTriangles(tris, 0);

            List<Vector2> uvs = new();
            for (int z = 0; z < nverts; z++)
            {
                float x = roadGen.roadProfiles[rp].keys[(z % vertsPerSeg)].time;
                uvs.Add(new Vector2(Mathf.Floor(z / ((float)vertsPerSeg)), x));
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
            return rmesh;
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