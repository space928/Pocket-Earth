using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NaughtyAttributes;
using Assets.Scripts;

public class RoadNode : PlanetObject
{
	public List<RoadNode> connections = new();
    public int roadProfile;

    [HideInInspector] public bool[] visitedEdges;
	[HideInInspector] public RoadNode last;
	private RoadGen roadGen;
	private GameObject intersection;

	public void UpdateIntersection()
	{
		// Remove old intersection
		if(intersection)
			Destroy(intersection);

		// Create a new one
		var go = Instantiate(roadGen.intersectionPrefabs[connections.Count - 2]);
		go.transform.SetParent(transform, false);
		intersection = go;
    }

    #region Editor Tools
    new void OnDrawGizmos ()
	{
		Gizmos.color = new Color (.9f, .3f, .1f, .8f);
		Gizmos.DrawSphere (transform.position, .5f);
	}

	[Button]
	public void Extend()
	{
#if UNITY_EDITOR
        Undo.RegisterFullObjectHierarchyUndo(transform.parent.gameObject, "Extended Road Node");
#endif
		var prev = connections.FirstOrDefault(x => x != this);
        Vector3 prevPos = prev != null ? prev.transform.position : transform.position;
		Vector3 dir = prevPos - transform.position;

		var go = Instantiate (gameObject);
		go.name = $"Road Node {transform.parent.childCount}";
		go.transform.SetParent(transform.parent, true);
        var nRoadNode = go.GetComponent<RoadNode>();
		connections.Add(nRoadNode);
		nRoadNode.connections.Clear();
		nRoadNode.connections.Add(this);
		nRoadNode.Planet = Planet;
		var latlong = CartesianToPolar(transform.position - dir);
		nRoadNode.Lattitude = latlong.x;
		nRoadNode.Longitude = latlong.y;

#if UNITY_EDITOR
		Selection.activeGameObject = go;
#endif
	}

	[Button]
	public void Insert()
	{
#if UNITY_EDITOR
        Undo.RegisterFullObjectHierarchyUndo(transform.parent.gameObject, "Inserted Road Node");
#endif
		var prev = connections.FirstOrDefault(x => x != this);
        Vector3 prevPos = prev != null ? prev.transform.position : transform.position;
        Vector3 midPos = (prevPos + transform.position) / 2;

        var go = Instantiate(gameObject);
		go.transform.SetParent (transform.parent, true);
        var nRoadNode = go.GetComponent<RoadNode>();
        connections.Add(nRoadNode);
        nRoadNode.connections.Clear();
        nRoadNode.connections.Add(this);
        nRoadNode.Planet = Planet;
        var latlong = CartesianToPolar(midPos);
        nRoadNode.Lattitude = latlong.x;
        nRoadNode.Longitude = latlong.y;
        if (prev != null)
		{
			prev.connections.Remove(this);
			prev.connections.Add(nRoadNode);
			connections.Remove(prev);
			nRoadNode.connections.Add(prev);
		}

#if UNITY_EDITOR
        Selection.activeGameObject = go;
#endif
    }

    [Button]
	public void Remove()
	{
#if UNITY_EDITOR
        Undo.RegisterFullObjectHierarchyUndo(transform.parent.gameObject, "Removed Road Node");
#endif
		var prev = connections.FirstOrDefault(x => x != this);
		var others = connections.Where(x => x != this && x != prev);

		if(prev)
		{
			prev.connections.Remove(this);
		}

		foreach(var other in others)
		{
			other.connections.Remove(this);
			other.connections.Add(prev);
		}

		
#if UNITY_EDITOR
		Selection.activeGameObject = prev.gameObject;
		DestroyImmediate(this);
#else
		Destroy(this);
#endif
    }

    [Button]
	public void Break()
	{
#if UNITY_EDITOR
        Undo.RegisterFullObjectHierarchyUndo(transform.parent.gameObject, "Broke Road Node");
#endif
		var prev = connections.FirstOrDefault(x => x != this);
        var others = connections.Where(x => x != this && x != prev);

        if (prev)
        {
            prev.connections.Remove(this);
        }

        foreach (var other in others)
        {
            other.connections.Remove(this);
            other.connections.Add(prev);
        }
    }
    #endregion
}
