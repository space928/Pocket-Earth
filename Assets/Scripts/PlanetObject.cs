using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using Unity.VisualScripting;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class PlanetObject : MonoBehaviour
    {
        [SerializeField] private Planet planet;
        [Range(-Mathf.PI, Mathf.PI)][SerializeField][OnValueChanged("UpdatePositionFromLatLong")] private float lattitude;
        [Range(0f, Mathf.PI * 2)][SerializeField][OnValueChanged("UpdatePositionFromLatLong")] private float longitude;
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
            if(!planet)
                return;
            float radius = planet.GetRadiusAtPoint(lattitude, longitude) /* + planet height*/;
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

#if UNITY_EDITOR
        internal void EditorUpdate()
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                if (planet)
                {
                    if (UnityEditor.SceneView.lastActiveSceneView == UnityEditor.EditorWindow.mouseOverWindow)
                        Position = transform.position;
                }
            }
        }
#endif

        // Update is called once per frame
        void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            EditorUpdate();
#endif
        }
    }

    public abstract class SelectablePlanetObject : PlanetObject
    {
        private bool isMouseOver;
        private bool wasMouseOver;
        private bool isHovered;
        private bool isSelected;
        private Material selectableMaterial;

        public Material SelectableMaterial { get => selectableMaterial; set => selectableMaterial = value; }

        public void SelectionUpdate()
        { 
            if (!isMouseOver && wasMouseOver)
            {
                wasMouseOver = false;
                OnMouseExit();
            }

            isMouseOver = false;
        }

        public void MouseOver(RaycastHit hit)
        {
            isMouseOver = true;
            if (!wasMouseOver)
                OnMouseEnter();

            wasMouseOver = true;
        }

        public void OnMouseEnter()
        {
            isHovered = true;
            selectableMaterial.SetColor("_EmissionColor", GameManager.GameManagerInst.HighlightColour);
        }

        public void OnMouseExit()
        {
            isHovered = false;
            selectableMaterial.SetColor("_EmissionColor", isSelected ? GameManager.GameManagerInst.SelectedColour : Color.black);
        }

        public void Select()
        {
            isSelected = true;
            selectableMaterial.SetColor("_EmissionColor", GameManager.GameManagerInst.SelectedColour);
        }

        public void DeSelect()
        {
            isSelected = false;
            selectableMaterial.SetColor("_EmissionColor", isHovered ? GameManager.GameManagerInst.HighlightColour : Color.black);
        }
    }
}