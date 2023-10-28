using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class PlanetObject : MonoBehaviour
    {
        [SerializeField] private Planet planet;
        [Range(-Mathf.PI, Mathf.PI)][SerializeField] private float lattitude;
        [Range(0f, Mathf.PI * 2)][SerializeField] private float longitude;
        [SerializeField] private float heading;
        [SerializeField] private bool alignRotation = true;

        /// <summary>
        /// Lattitude in radians.
        /// </summary>
        public float Lattitude
        {
            get => lattitude;
            set
            {
                lattitude = value;
                UpdatePositionFromLatLong();
            }
        }

        /// <summary>
        /// Longitude in radians.
        /// </summary>
        public float Longitude
        {
            get => longitude;
            set
            {
                longitude = value;
                UpdatePositionFromLatLong();
            }
        }

        /// <summary>
        /// The object's heading (y-rotation) in radians.
        /// </summary>
        public float Heading
        {
            get => heading;
            set
            {
                heading = value;
                UpdatePositionFromLatLong();
            }
        }

        public bool AlignRotation
        {
            get => alignRotation;
            set 
            {
                alignRotation = value;
                UpdatePositionFromLatLong();
            }
        }

        public Vector3 Position 
        { 
            set
            {
                Vector3 latlong = CartesianToPolar(value);
                Lattitude = latlong.x;
                Longitude = latlong.y;
                UpdatePositionFromLatLong();
            } 
        }

        public Planet Planet { get => planet; set { planet = value; } }

        public static Vector3 CartesianToPolar(Vector3 point)
        {
            var point_n = point.normalized;
            return new (Mathf.Asin(point_n.y), Mathf.Atan2(point_n.z, point_n.x), point.magnitude);
        }

        public static Vector3 PolarToCartesian(Vector3 latLongRad)
        {
            float coslat = Mathf.Cos(latLongRad.x);
            float sinlat = Mathf.Sin(latLongRad.x);
            float coslong = Mathf.Cos(latLongRad.y);
            float sinlong = Mathf.Sin(latLongRad.y);
            return new(latLongRad.z * coslat * coslong, latLongRad.z * sinlat, latLongRad.z * coslat * sinlong);
        }

        public Vector3 GetNormal(Vector3 point)
        {
            var p = (point - planet.transform.position).normalized;
            return p;
        }

        internal void UpdatePositionFromLatLong()
        {
            float radius = planet.Radius /* + planet height*/;
            float coslat = Mathf.Cos(lattitude);
            float sinlat = Mathf.Sin(lattitude);
            float coslong = Mathf.Cos(longitude);
            float sinlong = Mathf.Sin(longitude);
            Vector3 localPos = new(coslat * coslong, sinlat, coslat * sinlong);
            transform.position = radius * localPos + planet.transform.position;
            if(alignRotation)
                transform.localRotation = Quaternion.AngleAxis(heading, Vector3.up) * Quaternion.LookRotation(localPos);
        }

        // Use this for initialization
        void Start()
        {
            UpdatePositionFromLatLong();
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                if (UnityEditor.SceneView.lastActiveSceneView != UnityEditor.EditorWindow.mouseOverWindow)
                    UpdatePositionFromLatLong();
                else
                    Position = transform.position;
            }
#endif
        }
    }
}