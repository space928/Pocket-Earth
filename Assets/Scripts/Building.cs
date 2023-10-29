using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [ExecuteInEditMode]
    public class Building : SelectablePlanetObject
    {
        [SerializeField] private BuildingType buildingType;
        [SerializeField] public RoadNode roadNode;
        [SerializeField] private float perCapitaPollutionRate;
        [SerializeField] private float basePollutionRate = 0.002f;

        private Material material;
        
        public float PerCapitaPollutionRate { get => perCapitaPollutionRate; set { perCapitaPollutionRate = value; } }

        public BuildingType BuildingType => buildingType;

        // Use this for initialization
        void Start()
        {
            // Sorry render batching...
            material = GetComponentInChildren<MeshRenderer>().sharedMaterial = new(GetComponentInChildren<MeshRenderer>().sharedMaterial);
            SelectableMaterial = material;
        }

        // Update is called once per frame
        void Update()
        {
            GameManager.GameManagerInst.RegisterEnvironmentalContribution(-Time.deltaTime * basePollutionRate);

            SelectionUpdate();
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            EditorUpdate();
#endif
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