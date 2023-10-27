using System.Collections;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class PlanetObject : MonoBehaviour
    {
        [SerializeField] private Planet planet;
        [Range(-Mathf.PI, Mathf.PI)][SerializeField] private float lattitude;
        [Range(0f, Mathf.PI * 2)][SerializeField] private float longitude;
        [SerializeField] private float heading;

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

        internal void UpdatePositionFromLatLong()
        {
            float radius = planet.Radius /* + planet height*/;
            float coslat = Mathf.Cos(lattitude);
            float sinlat = Mathf.Sin(lattitude);
            float coslong = Mathf.Cos(longitude);
            float sinlong = Mathf.Sin(longitude);
            Vector3 localPos = new(coslat * coslong, sinlat, coslat * sinlong);
            transform.position = radius * localPos + planet.transform.position;
            transform.localRotation = Quaternion.LookRotation(localPos);
        }

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            UpdatePositionFromLatLong();
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.up);
        }
    }
}