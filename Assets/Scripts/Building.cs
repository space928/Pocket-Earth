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
                if(renderer.sharedMaterial == null) continue;
                switch(buildingType)
                {
                    case BuildingType.AppartmentBlock:
                        renderer.sharedMaterial.color = Color.white;
                        break;
                    case BuildingType.Powerplant:
                        renderer.sharedMaterial.color = Color.red;
                        break;
                    case BuildingType.House:
                        renderer.sharedMaterial.color = Color.green;
                        break;
                    case BuildingType.Supermarket:
                        renderer.sharedMaterial.color = Color.blue;
                        break;
                    case BuildingType.Farm:
                        renderer.sharedMaterial.color = Color.yellow;
                        break;
                    case BuildingType.Office:
                        renderer.sharedMaterial.color = Color.cyan;
                        break;
                    default:
                        renderer.sharedMaterial.color = Color.grey;
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