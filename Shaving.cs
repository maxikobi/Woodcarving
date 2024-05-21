using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Shaving : MonoBehaviour
{
    [SerializeField] float angle = 5;
    [SerializeField] float disconnectForce = 1;
    [SerializeField, Range(0.015f, 0.045f)] float vertexDistance = 0.025f;

    [SerializeField] bool debug = false;
    [SerializeField, Range(0, 30)] int index;
    QueueData leftQueued = new(), rightQueued = new();
    Mesh mesh;

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "shaving";
    }

    #region Adding
    public void AddEdgeVertices(Vector3 left, Vector3 right)
    {
        if (mesh.vertexCount == 0)
        {
            transform.position = left;
            transform.right = right - left;
            mesh.vertices = new Vector3[] {
                Vector3.zero,
                transform.InverseTransformPoint(right)};
            return;
        }

        QueueVertices(left, right);

        if (!leftQueued.add || !rightQueued.add) return;

        AddVertexPair(left);
        AddTriangles();


    }



    private void QueueVertices(Vector3 newLeftWorld, Vector3 newRightWorld)
    {
        Vector3[] vertices = mesh.vertices;

        Vector3 prevLeftLocal = vertices[^2];
        Vector3 prevRightLocal = vertices[^1];

        TryQueueVertex(newLeftWorld, prevLeftLocal, leftQueued);
        TryQueueVertex(newRightWorld, prevRightLocal, rightQueued);
    }

    public void TryQueueVertex(Vector3 newWorld, Vector3 prevLocal, QueueData queued)
    {
        //is new vertex is in front of the previous?
        Vector3 newLocal = transform.InverseTransformPoint(newWorld);
        /*if (newLocal.z < 0) return;

        //is it far enough away?
        Vector3 prevWorld = transform.TransformPoint(prevLocal);
        float dist = Vector3.Distance(newWorld, prevWorld);
        if (dist < vertexDistance) return;

        //if there is another queued already, is it farther away?
        if (queued.add && dist < queued.distance) return;
        //queue vertex*/
        queued.add = true;
        queued.localTarget = newLocal;
        queued.worldTarget = newWorld;
        //queued.distance = dist;
        
    }

    private void AddVertexPair(Vector3 newOrigin)
    {
        List < Vector3 > vertices = mesh.vertices.ToList();
        vertices.Add(leftQueued.localTarget);
        vertices.Add(rightQueued.localTarget);

        /*transform.position = newOrigin;
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] -= leftQueued.localTarget;*/

        /*Quaternion angleRotation = Quaternion.AngleAxis(angle, Vector3.right);
        for (int i = 0; i < vertices.Count; i += 2)
        {
            vertices[i] = angleRotation * vertices[i];
            vertices[i + 1] = angleRotation * vertices[i + 1];
        }*/

        leftQueued.add = false;
        rightQueued.add = false;
        mesh.vertices = vertices.ToArray();
    }

    private void AddTriangles()
    {
        int quadIndex = mesh.vertexCount - 4;
        List<int> tris = mesh.triangles.ToList();
        tris.Add(quadIndex + 2);
        tris.Add(quadIndex + 1);
        tris.Add(quadIndex + 0);
        tris.Add(quadIndex + 1);
        tris.Add(quadIndex + 2);
        tris.Add(quadIndex + 3);
        mesh.triangles = tris.ToArray();
    }

    #endregion

    public void Disconnect()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null) return;

        if (mesh.vertexCount < 4)
        {
            Destroy(gameObject);
            return;
        }

        body.isKinematic = false;
        body.AddForce((transform.up + 2 * transform.forward) * disconnectForce, ForceMode.VelocityChange);
        Destroy(gameObject, 5);
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;
        if (mesh == null || mesh.vertexCount < 2) return;

        Vector3[] vertices = mesh.vertices;       
        index = Mathf.Clamp(index, 0, mesh.vertexCount / 2 - 1);
        int pairIndex = index * 2;

        Gizmos.color = Color.magenta;
        Vector3 left = transform.TransformPoint(vertices[pairIndex]);
        Gizmos.DrawSphere(left, 0.003f);
        Gizmos.color = Color.green;
        Vector3 right = transform.TransformPoint(vertices[pairIndex + 1]);
        Gizmos.DrawSphere(right, 0.003f);

    }
}

public class QueueData
{
    public bool add;
    public Vector3 localTarget;
    public Vector3 worldTarget;
    public float distance;

    public QueueData()
    {
        add = false;
        localTarget = Vector3.zero;
        worldTarget = Vector3.zero;
        distance = -1;
    }
}
