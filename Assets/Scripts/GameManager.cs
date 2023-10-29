using Assets.Scripts;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Material healthBarMat;
    [SerializeField] private DisasterManager disasterGenerator;
    [SerializeField] private Light sun;
    [SerializeField] private GameObject citizenPrefab;
    [SerializeField] private Transform citizensParent;
    [SerializeField] private RoadGen roadGen;
    [SerializeField] private Planet planet;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshProUGUI selectionText;

    [Header("Parameters")]
    [SerializeField] [Range(0, 0.1f)] private float baseDisasterProbability = 0.01f;
    [SerializeField] [Range(0, 1f)] private float healthMultiplier = 0.5f;
    [SerializeField] [Range(0, 1f)] private float baseBuildingOccupancy = 0.5f;
    [SerializeField] private List<BuildingCapacity> buildingCapacities = new();
    [SerializeField] private List<BuildingCapacity> workplaceCapacities = new();
    // [SerializeField] private List<(BuildingType type, GameObject prefab)> buildingPrefabs = new();
    [SerializeField] private float workAtHomeProbability = 0.2f;
    [SerializeField] private float timeScale = 1000;
    [SerializeField] private float baseCitizenMovementSpeed = 5;
    [SerializeField] private Color highlightColour = Color.white;
    [SerializeField] private Color selectedColour = Color.white;

    [Header("Stats")]
    [SerializeField][ProgressBar("Percentage Walking", 100)] private float percentageWalking;
    [SerializeField][ProgressBar("Percentage Cycling", 100)] private float percentageCycling;
    [SerializeField][ProgressBar("Percentage Driving", 100)] private float percentageDriving;
    [SerializeField][ProgressBar("Percentage Bussing", 100)] private float percentageBussing;
    [SerializeField][ProgressBar("Percentage Travelling", 100, EColor.Indigo)] private float percentageTravelling;

    private static GameManager gameManagerInst;

    private Dictionary<BuildingType, int> buildingCapacitiesDict = new();
    private Dictionary<BuildingType, int> workplaceCapacitiesDict = new();
    //private Dictionary<BuildingType, GameObject> buildingPrefabsDict = new();
    private float environmentHealth = 0.6f;
    private float lastCheck = 0;
    private List<Citizen> citizens = new();
    private List<Building> buildings = new();
    private List<Building> shops = new();
    [ShowNonSerializedField] private float globalTime = 0;
    private float electricityPollutionRate = 0;
    [ShowNonSerializedField] private SelectablePlanetObject selectedObject = null;

    public SelectablePlanetObject SelectedObject => selectedObject;

    [ShowNativeProperty]
    public float EnvironmentalHealth => System.MathF.Tanh(environmentHealth)*0.5f+0.5f;
    [ShowNativeProperty]
    public float EnvironmentalHealthRaw { get => environmentHealth; set => environmentHealth = value; }
    /// <summary>
    /// The time of day in decimal hours.
    /// </summary>
    public float GlobalTime => globalTime;

    public float ElectricityPollutionRate => electricityPollutionRate;
    public RoadGen RoadGen => roadGen;
    public List<Building> Buildings => buildings;
    public List<Building> Shops => shops;
    public Color HighlightColour => highlightColour;
    public Color SelectedColour => selectedColour;

    public static GameManager GameManagerInst
    {
        get 
        { 
            if(gameManagerInst)
                return gameManagerInst; 
            else
                return gameManagerInst = GameObject.FindObjectOfType<GameManager>();
        }
    }

    /*public Dictionary<BuildingType, GameObject> BuildingPrefabs
    {
        get
        {
            if((buildingPrefabsDict?.Count ?? 0) > 0)
                return buildingPrefabsDict;
            foreach (var item in buildingPrefabs)
                buildingPrefabsDict.Add(item.type, item.prefab);
            return buildingPrefabsDict;
        }
    }*/

    public void RegisterSelection(SelectablePlanetObject selected)
    {
        if(selectedObject != null)
            selectedObject.DeSelect();
        selected.Select();
        selectedObject = selected;

        selectionText.text = $"Selected: {selected.name}";
    }

    public void DeSelect()
    {
        if (selectedObject != null)
        {
            selectedObject.DeSelect();
            selectedObject = null;
        }

        selectionText.text = "";
    }

    [System.Serializable]
    public struct BuildingCapacity
    {
        public BuildingType buildingType;
        public int capacity;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Build dicts from serialised lists
        foreach (var item in buildingCapacities)
            buildingCapacitiesDict.Add(item.buildingType, item.capacity);
        foreach (var item in workplaceCapacities)
            workplaceCapacitiesDict.Add(item.buildingType, item.capacity);
        //foreach (var item in buildingPrefabs)
        //    buildingPrefabsDict.Add(item.type, item.prefab);

        // Find buildingss
        buildings.AddRange(FindObjectsOfType<Building>());
        List<Building> workplacesWeighted = new();
        foreach(Building b in buildings)
            for(int i = 0; i < workplaceCapacitiesDict[b.BuildingType]; i++)
                workplacesWeighted.Add(b);
        workplacesWeighted.Shuffle();
        shops = buildings.Where(x => x.BuildingType == BuildingType.Supermarket).ToList();

        // Now create new citizens in each of the housing buildings
        int worker = 0;
        foreach (Building b in buildings)
        {
            switch(b.BuildingType)
            {
                case BuildingType.House:
                case BuildingType.AppartmentBlock:
                    for(int i = 0; i < buildingCapacitiesDict[b.BuildingType] * baseBuildingOccupancy; i++)
                    {
                        // Create citizens
                        var citizenGo = Instantiate(citizenPrefab);
                        var citizen = citizenGo.GetComponent<Citizen>();
                        citizen.Planet = planet;
                        citizen.Position = b.transform.position;
                        citizenGo.transform.SetParent(citizensParent, false);
                        var workplace = workplacesWeighted[(worker++) % workplacesWeighted.Count];
                        if (Random.value < workAtHomeProbability)
                            workplace = b;
                        citizen.Construct(b, workplace, this, baseCitizenMovementSpeed);
                        citizens.Add(citizen);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(healthBarMat)
        {
            healthBarMat.SetFloat("_Value", EnvironmentalHealth);
        }

        if (Time.time > lastCheck + 1)
        {
            lastCheck = Time.time;
            if (Random.value < baseDisasterProbability * EnvironmentalHealth)
            {
                // Trigger a disaster
            }
        }

        globalTime += (Time.deltaTime/3600) * timeScale;
        var sunAng = sun.transform.localRotation.eulerAngles;
        sun.transform.localRotation = Quaternion.Euler(sunAng.x, globalTime/24*360, sunAng.z);

        if(Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
        {
            // Slow...
            var selectable = hit.collider.GetComponentInParent<SelectablePlanetObject>();
            if (selectable  != null)
            {
                selectable.MouseOver(hit);

                if (Input.GetMouseButton(0))
                    RegisterSelection(selectable);
            } else if(Input.GetMouseButton(0))
            {
                DeSelect();
            }
        } else if (Input.GetMouseButton(0))
        {
            DeSelect();
        }

#if UNITY_EDITOR
        CalculateStats();
#endif
    }

    public void CalculateStats()
    {
        int[] counts = new int[4];
        int travelling = 0;
        foreach(Citizen c in citizens)
        {
            if (c.CurrentState == Citizen.CitizenState.Travelling)
            {
                counts[(int)c.CurrentModeOfTransport]++;
                travelling++;
            }
        }

        percentageWalking = counts[0]/(float)travelling*100;
        percentageCycling = counts[1]/(float)travelling*100;
        percentageDriving = counts[2]/(float)travelling*100;
        percentageBussing = counts[3]/(float)travelling*100;

        percentageTravelling = travelling / (float)citizens.Count * 100;
    }

    [Button]
    [Tooltip("Executes in O(N^2), it's not meant to be fast!")]
    public void AutoAttachBuildingsToRoadNodes()
    {
        Building[] buildings = FindObjectsOfType<Building>();
        RoadNode[] roadNodes = FindObjectsOfType<RoadNode>();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayProgressBar("Connecting buildings...", "Connecting buildings with road nodes.", 0);
#endif
        int i = 0;
        foreach (Building building in buildings)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayProgressBar("Connecting buildings...", "Connecting buildings with road nodes.", i/(float)buildings.Length);
#endif
            Vector3 bpos = building.transform.position;
            RoadNode best = null;
            float dist = float.MaxValue;
            foreach(RoadNode node in roadNodes) 
            {
                float ndist = (bpos - node.transform.position).sqrMagnitude;
                if (ndist < dist)
                {
                    dist = ndist;
                    best = node;
                }
            }

            if (best)
            {
                building.roadNode = best;
            }
            // While we're at it
            building.Planet = planet;

            i++;
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.ClearProgressBar();
#endif
    }

    public void RegisterEnvironmentalContribution(float x, ContributionSource contribution = ContributionSource.Grid)
    {
        environmentHealth += x * healthMultiplier;
    }

    public enum ContributionSource
    {
        Grid,
        FossilFuel,
        Waste,
        Respiration
    }
}
