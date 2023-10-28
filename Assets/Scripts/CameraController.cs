using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// A basic orbit camera controller. Requires the camera this is attached to be parented to a GameObject representing the pivot.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [ExecuteInEditMode]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private bool requireClick = true;
        [SerializeField] private float deadZone = 10;
        [SerializeField] private float sensitivity = 0.01f;
        [SerializeField] [Range(0,1)] private float dampingRate = 0.1f;
        [SerializeField] private Vector2 zoomSpeed = new(.5f, 3);
        [SerializeField] private Vector2 zoomRange = new(50, 300);
        [SerializeField] private float safeDistance = 2f;

        private Camera myCamera;
        private Vector3 lastMouse;
        private Vector3 startMouse;
        private Vector3 vel;
        [SerializeField][Range(-Mathf.PI/2, Mathf.PI/2)] private float lattitude;
        [SerializeField][Range(-Mathf.PI, Mathf.PI)] private float longitude;
        private bool wasMoving = false;
        private bool inDeadzone = true;

        // Use this for initialization
        void Start()
        {
            myCamera = GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            // Handle input
            if(!requireClick || Input.GetMouseButton(0))
            {
                Vector3 currMouse = Input.mousePosition;
                if (!wasMoving)
                    startMouse = currMouse;

                Vector3 delta = currMouse - lastMouse;
                if (!inDeadzone || (currMouse - startMouse).magnitude > deadZone)
                {
                    vel += delta * sensitivity;
                    inDeadzone = false;
                }

                lastMouse = currMouse;
                wasMoving = true;
            } else
            {
                wasMoving = false;
                inDeadzone = true;
            }

            // Zooming
            vel.z += Input.mouseScrollDelta.y;

            // Apply the rotation
            vel *= dampingRate;

            longitude += -vel.x;
            lattitude += vel.y;
            lattitude = Mathf.Clamp(lattitude, -Mathf.PI/2.001f, Mathf.PI/2.001f);

            float coslat = Mathf.Cos(lattitude);
            float sinlat = Mathf.Sin(lattitude);
            float coslong = Mathf.Cos(longitude);
            float sinlong = Mathf.Sin(longitude);
            transform.parent.localRotation = Quaternion.LookRotation(new(coslat * coslong, sinlat, coslat * sinlong));

            // Check collisions
            float z = -transform.localPosition.z;
            float maxDist = 0;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit))
                maxDist = z - hit.distance + safeDistance;

            // Apply zoom
            float zoomSpeedComputed = Mathf.Lerp(zoomSpeed.x, zoomSpeed.y, Mathf.InverseLerp(zoomRange.x, zoomRange.y, z));
            float newZ = -Mathf.Clamp(z + vel.z * zoomSpeedComputed, Mathf.Max(zoomRange.x, maxDist), zoomRange.y);
            transform.localPosition = new(0, 0, newZ);
        }
    }
}