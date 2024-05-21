using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(KnifeTracker))]
public class MeshCarver : MonoBehaviour
{
    KnifeTracker knifeTracker;
    //stick
    Mesh mesh;
    NativeArray<float3> nativeVertices;
    NativeList<int> hitIndices;
    NativeArray<float4> nativeColours;

    //shavings
    [SerializeField] GameObject shavingPrefab;
    Shaving shavingInstance;

    List<Vector3> shavingVertices, hitVertices;
    [Space]
    [SerializeField] bool debug = false;
    [SerializeField] bool showAllPoints;
    [SerializeField, Range(0, 30)] int shavingIndex;

    [SerializeField] bool distanceDebug;
    [SerializeField, Range(0, 30)] int fromIndex;
    [SerializeField, Range(0, 30)] int toIndex;
    public float distance;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        knifeTracker = GetComponent<KnifeTracker>();
        shavingVertices = new List<Vector3>();
        hitVertices = new List<Vector3>();


        nativeVertices = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent);
        nativeColours = new NativeArray<float4>(mesh.vertexCount, Allocator.Persistent);
        for (int i = 0; i < mesh.vertexCount; i++)
            nativeColours[i] = 1;
        mesh.SetColors(nativeColours);

        using (var dataArray = Mesh.AcquireReadOnlyMeshData(mesh))
        {
            dataArray[0].GetVertices(nativeVertices.Reinterpret<Vector3>());
        }
    }


    void Update()
    {
        if (knifeTracker.IsCarving)
            CarveMesh();
    }

    private void CarveMesh()
    {
        hitIndices = new NativeList<int>(100, Allocator.TempJob);
        CarveMeshJob filterJob = new CarveMeshJob()
        {
            knifeOrigin = knifeTracker.LocalKnifePosition, 
            knifeRotation = knifeTracker.LocalKnifeRotation,
            knifeSize = knifeTracker.KnifeScale,

            Vertices = nativeVertices, 
            colours = nativeColours,
        };

        JobHandle filterJobHandle = filterJob.ScheduleAppend(hitIndices, nativeVertices.Length);
        filterJobHandle.Complete();

        nativeVertices = filterJob.Vertices;
        nativeColours = filterJob.colours;

        AddShaving();
        mesh.SetVertices(nativeVertices);
        mesh.SetColors(nativeColours);

        hitIndices.Dispose();
    }

    private void AddShaving()
    {
        if (!GetEdgeVertices(out Vector3 l_vertex, out Vector3 r_vertex)) return;

        Vector3 leftPos = transform.TransformPoint(l_vertex);
        Vector3 rightPos = transform.TransformPoint(r_vertex);
        shavingInstance.AddEdgeVertices(leftPos, rightPos);

        shavingVertices.Add(l_vertex);
        shavingVertices.Add(r_vertex);
    }

    private bool GetEdgeVertices(out Vector3 l_vertex, out Vector3 r_vertex)
    {
        l_vertex = Vector3.zero; r_vertex = Vector3.zero;
        if (hitIndices.Length < 2) return false;
        Vector3[] oldVertices = mesh.vertices;
        int l_index = -1, r_index = -1;
        float left = float.PositiveInfinity, right = float.NegativeInfinity;
        foreach (int hitIndex in hitIndices)
        {
            Vector3 hit = transform.TransformPoint(nativeVertices[hitIndex]);
            //hitVertices.Clear();
            hitVertices.Add(transform.TransformPoint(oldVertices[hitIndex]));
            Vector3 coords = knifeTracker.KnifeCoord(hit);
            if (coords.x > right)
            {
                right = coords.x;
                r_index = hitIndex;
            }
            if (coords.x < left)
            {
                left = coords.x;
                l_index = hitIndex;
            }
        }

        if (l_index == -1 || r_index == -1) return false;
        if (l_index == r_index) return false;

        
        l_vertex = oldVertices[l_index]; 
        r_vertex = oldVertices[r_index];
        return true;
    }

    private void OnApplicationQuit()
    {
        nativeVertices.Dispose();
        nativeColours.Dispose();
    }

    public void StartCarving() 
    {
        shavingInstance = Instantiate(shavingPrefab, transform, false).GetComponent<Shaving>();
    }
    public void StopCarving() 
    {
        //shavingVertices.Clear();
        //if (shavingInstance != null)
        //shavingInstance.Disconnect();
    }


    private void OnDrawGizmos()
    {
        if (!debug) return;
        if (shavingVertices == null) return;

        if (showAllPoints)
        {
            Gizmos.color = Color.black;
            foreach (var vertex in shavingVertices)
                Gizmos.DrawSphere(transform.TransformPoint(vertex), 0.001f);
            Gizmos.color = Color.white;
            foreach (var vertex in hitVertices)
                Gizmos.DrawSphere(vertex, 0.001f);
        }

        if (shavingVertices.Count < 2) return;
        shavingIndex = Mathf.Clamp(shavingIndex, 0, shavingVertices.Count / 2 - 1);
        int pairIndex = 2 * shavingIndex;

        Gizmos.color = Color.magenta;
        Vector3 left = transform.TransformPoint(shavingVertices[pairIndex]);
        Gizmos.DrawSphere(left, 0.003f);
        Gizmos.color = Color.green;
        Vector3 right = transform.TransformPoint(shavingVertices[pairIndex + 1]);
        Gizmos.DrawSphere(right, 0.003f);

        if (distanceDebug)
        {
            fromIndex = Mathf.Clamp(fromIndex, 0, shavingVertices.Count - 1);
            toIndex = Mathf.Clamp(toIndex, 0, shavingVertices.Count - 1);

            Vector3 from = transform.TransformPoint(shavingVertices[fromIndex]);
            Vector3 to = transform.TransformPoint(shavingVertices[toIndex]);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(from, 0.0035f);
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(to, 0.0035f);

            distance = Vector3.Distance(from, to);
        }
    }
}
