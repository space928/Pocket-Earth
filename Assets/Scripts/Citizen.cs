using Assets.Scripts;
using ESarkis;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class Citizen : PlanetObject
{
    [SerializeField] private Building home;
    [SerializeField] private Building work;
    [SerializeField] private GameManager gameManager;

    [ReadOnly][SerializeField] private TimeTableEntry[] timetable = new TimeTableEntry[48];
    [ReadOnly][SerializeField] private CitizenState state = CitizenState.Activity;
    [ReadOnly][SerializeField] private Goal currentGoal = Goal.Sleep;
    [ReadOnly][SerializeField] private Goal lastGoal = Goal.Sleep;
    [ReadOnly][SerializeField] private Building currBuilding;
    [ReadOnly][SerializeField] private Building goalBuilding;
    [ReadOnly][SerializeField] private float excentricity;
    [ReadOnly][SerializeField] private float timetableOffset;
    [ReadOnly][SerializeField] private Stack<((RoadSegment seg, bool reverse), float distance)> travelPath = new();
    [ReadOnly][SerializeField] private float minCyclePathProportion;
    [ReadOnly][SerializeField] private ((RoadSegment seg, bool reverse), float distance) currentPathSegment;
    [ReadOnly][SerializeField] private float currentPathSegmentProgress = 1;
    [ReadOnly][SerializeField] private float currentPathTotalLength = 0;
    [ReadOnly][SerializeField] private ModeOfTransport currentModeOfTransport = ModeOfTransport.Walking;
    [ReadOnly][SerializeField] private float baseMovementSpeed = 1;

    private Material myMat;

    private readonly float maxWalkDist = 150;
    private readonly float maxCycleDist = 500;

    public ModeOfTransport CurrentModeOfTransport => currentModeOfTransport;
    public CitizenState CurrentState => state;

    public void Construct(Building home, Building work, GameManager gameManager, float baseMovementSpeed)
    {
        this.home = home;
        this.work = work;
        this.gameManager = gameManager;
        this.baseMovementSpeed = baseMovementSpeed;
    }

    public void Start()
    {
        // Construct a time table
        excentricity = Random.Range(0.05f, 0.4f);
        Goal lastGoal = Goal.Sleep;
        for (int i = 0; i < timetable.Length; i++)
        {
            timetable[i] = new TimeTableEntry()
            {
                goal = lastGoal,
            };
            if (Random.value < excentricity)
                lastGoal = (Goal)Random.Range(0, (int)Goal._MaxVal - 1);
        }
        minCyclePathProportion = Random.Range(0f, 1f);
        currBuilding = home;

        var mr = GetComponentInChildren<MeshRenderer>();
        myMat = new(mr.sharedMaterial);
        mr.sharedMaterial = myMat;
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying)
            return;
#endif

        if (state == CitizenState.Activity)
        {
            currentGoal = timetable[(int)(gameManager.GlobalTime + timetableOffset) % timetable.Length].goal;
            if (currentGoal != lastGoal)
            {
                timetableOffset = Random.value * excentricity;
                state = CitizenState.Travelling;

                // Determine the goal building...
                goalBuilding = currentGoal switch
                {
                    Goal.Shopping => gameManager.Shops[Random.Range(0, gameManager.Shops.Count)],// Pick a random shop, our citizens like variety
                    Goal.Recreation => gameManager.Buildings[Random.Range(0, gameManager.Buildings.Count)],// Pick any building at random; fun can be found anywhere
                    Goal.Work => work,
                    _ => home,
                };

                // Now work out how to get there...
                if (currBuilding == goalBuilding || currBuilding.roadNode == goalBuilding.roadNode)
                {
                    // We're already here, no need to travel
                    state = CitizenState.Activity;
                    currBuilding = goalBuilding;
                }
                else
                {
                    FindBestRoute();
                }
            }

            myMat.color = Color.white;
        }

        if (state == CitizenState.Travelling)
        {
            HandleStateTravelling();

            switch(currentModeOfTransport)
            {
                case ModeOfTransport.Walking:
                    myMat.color = Color.green; 
                    break;
                case ModeOfTransport.Cycling:
                    myMat.color = Color.cyan;
                    break;
                case ModeOfTransport.Driving:
                    myMat.color = Color.red;
                    break;
                case ModeOfTransport.Bussing:
                    myMat.color = Color.yellow;
                    break;
            }
        }
    }

    private void HandleStateTravelling()
    {
        if (currentPathSegmentProgress >= currentPathSegment.distance)
        {
            if (travelPath == null || travelPath.Count == 0)
            {
                currBuilding = goalBuilding;
                state = CitizenState.Activity;
                return;
            }

            currentPathSegmentProgress = 0;
            currentPathSegment = travelPath.Pop();
        }

        // Advance along path
        float progress = currentPathSegmentProgress / currentPathSegment.distance;
        Vector3 npos = currentPathSegment.Item1.seg.Interpolate(currentPathSegment.Item1.reverse ? progress : 1 - progress);
        Position = npos;

        // TODO: Tune these numbers, I pulled them out of my butt.
        (GameManager.ContributionSource source, float rate) contribution = currentModeOfTransport switch
        {
            ModeOfTransport.Walking => (GameManager.ContributionSource.Respiration, 0.00001f),
            ModeOfTransport.Cycling => (GameManager.ContributionSource.Respiration, 0.00001f),
            ModeOfTransport.Driving => (GameManager.ContributionSource.FossilFuel, 0.01f), // TODO: Both of these CAN and SHOULD be upgradable to grid.
            ModeOfTransport.Bussing => (GameManager.ContributionSource.FossilFuel, 0.001f),
            _ => (GameManager.ContributionSource.FossilFuel, 1)
        };
        gameManager.RegisterEnvironmentalContribution(-contribution.rate * Time.deltaTime, contribution.source);

        currentPathSegmentProgress += Time.deltaTime * baseMovementSpeed;
    }

    private void FindBestRoute()
    {
        // Consider all routes and pick the most desirable
        ModeOfTransport[] modes = (ModeOfTransport[])System.Enum.GetValues(typeof(ModeOfTransport));
        (float distance, Stack<((RoadSegment seg, bool reverse), float distance)> path, ModeOfTransport mode) best = (float.PositiveInfinity, null, ModeOfTransport.Walking);
        foreach (var mode in modes)
        {
            var (distance, path) = PathFind(currBuilding.roadNode, goalBuilding.roadNode, mode);
            if (mode == ModeOfTransport.Walking && distance > maxWalkDist)
                continue;
            if (mode == ModeOfTransport.Cycling && distance > maxCycleDist)
                continue;
            if (mode == ModeOfTransport.Driving || mode == ModeOfTransport.Bussing)
                distance += 60; // For short journeys there's some overhead to taking a car/bus

            // Citizens are not very good at estimating journeys
            distance *= Random.Range(0.7f, 1.5f);

            if (distance < best.distance)
                best = (distance, path, mode);
        }

        if (best.distance == float.PositiveInfinity)
        {
            Debug.Log($"{name} oh no! I can't get from {currBuilding} to {goalBuilding}!");
            travelPath = null;
            state = CitizenState.Activity;
        }

        currentPathTotalLength = best.distance;

        travelPath = best.path;
        currentModeOfTransport = best.mode;
    }

    private (float distance, Stack<((RoadSegment seg, bool reverse), float distance)> path) PathFind(RoadNode start, RoadNode end, ModeOfTransport mode)
    {
        // Use Dijkstra's algorithm to find an appropriate route
        // Derived from wikipedia
        PriorityQueue<RoadNode> nodeQ = new();
        Dictionary<RoadNode, float> distances = new();
        Dictionary<RoadNode, RoadNode> prev = new();

        foreach (var node in gameManager.RoadGen.RoadNodes)
        {
            prev.Add(node, null);
            if (node != start)
            {
                distances.Add(node, float.PositiveInfinity);
            }
            else
            {
                distances.Add(node, 0);
                nodeQ.Enqueue(node, 0);
            }
        }

        while (nodeQ.Count > 0)
        {
            var u = nodeQ.Dequeue();

            if (u == end)
            {
                // Target found!
                Stack<((RoadSegment seg, bool reverse), float distance)> path = new();
                if (prev[u] != null || u == start) // The edge cases aren't important here...
                    while (u != null && prev[u] != null)
                    {
                        path.Push((u.segments[prev[u]], distances[u]-distances[prev[u]]));
                        u = prev[u];
                    }
                return (distances[end], path);
            }

            foreach (var child in u.connections)
            {
                var alt = distances[u] + u.segments[child].seg.GetLengthByMode(mode);
                if (alt < distances[child])
                {
                    distances[child] = alt;
                    prev[child] = u;
                    nodeQ.Enqueue(child, alt);
                }
            }
        }

        return (float.PositiveInfinity, null);
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        if(travelPath != null && travelPath.Count > 0)
            foreach (var seg in travelPath)
                Gizmos.DrawLine(seg.Item1.seg.StartNode.transform.position, seg.Item1.seg.EndNode.transform.position);
    }

    [System.Serializable]
    public struct TimeTableEntry
    {
        public Goal goal;
    }

    public enum Goal
    {
        Sleep,
        Home,
        Shopping,
        Recreation,
        Work,

        _MaxVal
    }

    public enum CitizenState
    {
        Travelling,
        Activity
    }

    public enum ModeOfTransport
    {
        Walking,
        Cycling,
        Driving,
        Bussing
    }
}
