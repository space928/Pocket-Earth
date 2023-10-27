using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    [RequireComponent (typeof (MeshRenderer))]
    public class Building : PlanetObject
    {
        [SerializeField] private BuildingType buildingType;

        private MeshRenderer[] renderers = null;

        // Use this for initialization
        void Start()
        {
            renderers = GetComponents<MeshRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
            if(!UnityEditor.EditorApplication.isPlaying)
            {
                // Basic behaviour in edit mode
                UpdatePositionFromLatLong();
                DBG_UpdateBuildingMat();
                return;
            }
#endif
        }

        internal void DBG_UpdateBuildingMat()
        {
            if (renderers == null)
                renderers = GetComponents<MeshRenderer>();
            foreach (var renderer in renderers) 
            {
                switch(buildingType)
                {
                    case BuildingType.AppartmentBlock:
                        renderer.material.color = Color.white;
                        break;
                    case BuildingType.Powerplant:
                        renderer.material.color = Color.red;
                        break;
                    case BuildingType.House:
                        renderer.material.color = Color.green;
                        break;
                    case BuildingType.Supermarket:
                        renderer.material.color = Color.blue;
                        break;
                    case BuildingType.Farm:
                        renderer.material.color = Color.yellow;
                        break;
                    case BuildingType.Office:
                        renderer.material.color = Color.cyan;
                        break;
                    default:
                        renderer.material.color = Color.grey;
                        break;
                }
            }
        }
    }

    public enum BuildingType
    {
        House,
        AppartmentBlock,
        Office,
        Supermarket,
        Farm,
        Factory,
        Powerplant
    }
}