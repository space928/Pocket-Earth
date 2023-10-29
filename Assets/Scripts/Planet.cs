using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(MeshFilter))]
    public class Planet : MonoBehaviour
    {
        [SerializeField] private float radius;
        [SerializeField] private Texture2D terrainMap;
        [SerializeField] private float terrainMapHeight;
        [SerializeField] private Mesh mesh;
        [SerializeField][Range(-Mathf.PI, Mathf.PI)] private float a,b,c,d;
        [SerializeField][Range(-4, 4)]private int e,f;

        public float Radius => radius;

        private static float Fract(float x) => x - Mathf.Floor(x);// x>=0?x - Mathf.Floor(x) : x - Mathf.Ceil(x);

        public float GetRadiusAtPoint(float lattitude, float longitude)
        {
            // Don't even ask where this formula came from...
            float lawrap = Fract(lattitude / Mathf.PI / 2 + .25f);
            float s = Mathf.Sign(Mathf.Abs(lawrap - 0.25f) - 0.25f);
            return radius + terrainMap.GetPixelBilinear(Fract(longitude / Mathf.PI / 2 + 0.25f * s), -2 * Mathf.Abs(lawrap - 0.5f)).r * terrainMapHeight;
        }

        public float GetRadiusAtPoint(Vector3 point)
        {
            var latlong = PlanetObject.CartesianToPolar(point);
            // Don't even ask where this formula came from...
            float lawrap = Fract(latlong.x / Mathf.PI / 2 + .25f);
            float s = Mathf.Sign(Mathf.Abs(lawrap - 0.25f) - 0.25f);
            return radius + terrainMap.GetPixelBilinear(Fract(latlong.y / Mathf.PI / 2 + 0.25f * s), -2 * Mathf.Abs(lawrap - 0.5f)).r * terrainMapHeight;
        }

        // Use this for initialization
        void Start()
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
        }

        // Update is called once per frame
        void Update()
        {

        }

        /*public void OnDrawGizmosSelected()
        {
            for(float lat = -Mathf.PI; lat <  a; lat+= Mathf.PI/20) 
            {
                for (float lon = -Mathf.PI; lon < b; lon += Mathf.PI / 20)
                {
                    float la = lat + e * Mathf.PI*2;
                    float lo = lon + f * Mathf.PI;
                    float lawrap = Fract(la / Mathf.PI / 2 + .25f);
                    float s = Mathf.Sign(Mathf.Abs(lawrap-0.25f)-0.25f);
                    Gizmos.color = new Color(Fract(lo / Mathf.PI/2 + 0.25f*s), 2*Mathf.Abs(lawrap - 0.5f), 0);
                    Vector3 v = PlanetObject.PolarToCartesian(new(la, lo, GetRadiusAtPoint(la, lo)));
                    //Gizmos.DrawRay(v, v.normalized);
                    Gizmos.DrawSphere(v, 1);
                }
            }
        }*/
    }
}