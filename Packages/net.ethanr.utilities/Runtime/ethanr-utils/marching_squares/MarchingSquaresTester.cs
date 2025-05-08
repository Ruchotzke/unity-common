using System;
using System.Collections.Generic;
using TriangleNet.Geometry;
using UnityEditor;
using UnityEngine;
using UnityUtilities.General;
using UnityUtilities.Meshing;

namespace ethanr_utils.marching_squares
{
    public class MarchingSquaresTester : MonoBehaviour
    {
        public float isoValue = 1.0f;
        public ContourBuilder contourBuilder;

        public List<Vector2> vertices = new List<Vector2>()
        {
            new Vector2(0, 0),
            new Vector2(2, 5),
            new Vector2(4, 4),
            new Vector2(6, 3),
        };

        public float[,] testData = new float[5, 5]
        {
            { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, },
            { 0.0f, 2.0f, 3.0f, 0.3f, 0.0f, },
            { 0.0f, 1.0f, 3.0f, 1.5f, 0.0f, },
            { 0.0f, 1.0f, 0.6f, 0.8f, 0.0f, },
            { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, },
        };

        // private void Awake()
        // {
        //     /* Generate a polygon */
        //     var p = new Polygon();
        //     var newVerts = new List<Vertex>();
        //     foreach (var v in vertices)
        //     {
        //         newVerts.Add(v.ToVertex());
        //     }
        //     var contour = new Contour(newVerts);
        //     p.Add(contour);
        //     
        //     /* Generate the mesh */
        //     var mesh = p.Triangulate();
        //     var mesher = new Mesher(false);
        //     foreach (var tri in mesh.Triangles)
        //     {
        //         Debug.Log($"TRI: {tri.vertices[0].ToVector2()}, {tri.vertices[2].ToVector2()}, {tri.vertices[1].ToVector2()}");
        //         mesher.AddTriangle(tri.vertices[0].ToVector2(), tri.vertices[2].ToVector2(), tri.vertices[1].ToVector2());
        //     }
        //
        //     var genMesh = mesher.GenerateMesh();
        //     GetComponent<MeshFilter>().mesh = genMesh;
        // }

        private void Update()
        {
            /* Create some voxel data */
            float[,] voxels = new float[100, 100];
            for (int x = 0; x < voxels.GetLength(0); x++)
            {
                for (int y = 0; y < voxels.GetLength(1); y++)
                {
                    voxels[x,y] = Mathf.PerlinNoise(Time.time * 0.25f + x/20.0f, Time.time * 0.25f + y/20.0f);
                }
            }
            
            /* Generate a mesh */
            // var mesh = MarchingSquares.GenerateMesh(voxels, isoValue, new Rect(0.0f, 0.0f, 100.0f, 100.0f));
            // GetComponent<MeshFilter>().mesh = mesh;
            contourBuilder = MarchingSquares.GenerateMeshContours(voxels, isoValue, new Rect(0.0f, 0.0f, 100.0f, 100.0f));
            var contours = contourBuilder.BuildContoursVectors();
            Gizmos.color = Color.green;
            foreach (var contour in contours)
            {
                for (int i = 0; i < contour.Count - 1; i++)
                {
                    Gizmos.DrawLine(contour[i], contour[i + 1]);
                }
                Gizmos.DrawLine(contour[^1], contour[0]);
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                // if (contourBuilder != null)
                // {
                //     Debug.Log($"Drawing {contourBuilder.edges.Count} edges.");
                //     foreach (var edge in contourBuilder.edges)
                //     {
                //         Gizmos.DrawLine(edge.From, edge.To);
                //     }
                // }
            
                // ContourBuilder contourBuilder = MarchingSquares.GenerateMeshContours(testData, isoValue, new Rect(0.0f, 0.0f, 10.0f, 10.0f));
                // Debug.Log($"Drawing {contourBuilder.edges.Count} edges.");
                // foreach (var edge in contourBuilder.edges)
                // {
                //     Gizmos.DrawLine(edge.From, edge.To);
                // }
                // var contours = contourBuilder.BuildContoursVectors();
                // Gizmos.color = Color.green;
                // foreach (var contour in contours)
                // {
                //     for (int i = 0; i < contour.Count - 1; i++)
                //     {
                //         Gizmos.DrawLine(contour[i], contour[i + 1]);
                //     }
                //     Gizmos.DrawLine(contour[^1], contour[0]);
                // }
            }
            
            
        }
    }
}