using System.Collections.Generic;
using ethanr_utils.dual_contouring.computation;
using ethanr_utils.dual_contouring.data;
using ethanr_utils.dual_contouring.sdf;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities.General;
using Random = UnityEngine.Random;

namespace ethanr_utils.dual_contouring
{
    /// <summary>
    /// A basic tester for dual contouring stuff.
    /// </summary>
    public class DualContourTester : MonoBehaviour
    {
        private VolumeChunk chunk;
        [FormerlySerializedAs("rotate")] [SerializeField] private bool update;
        [SerializeField] private Vector2 position;
        [SerializeField] private float rotation = 4.6f;
        [SerializeField] private float scale = 1.0f;

        private int size = 20;
        private Rect area = new Rect(0.0f, 0.0f, 4.0f, 4.0f);
        private SdfObject obj;
        
        private List<MeshFilter> currFilters = new List<MeshFilter>();
        private List<List<SurfacePoint>> polygons = new List<List<SurfacePoint>>();
        
        private void Awake()
        {
            chunk = new VolumeChunk(new Vector2Int(size, size), area);
            var largerCircle = new CircleSdf(1.5f);
            var smallerCircle = new CircleSdf(1f);
            var evenSmallerCircle = new CircleSdf(0.6f);
            var theSmallestCircle = new CircleSdf(0.3f);
            var torus = new DifferenceSdf(largerCircle, smallerCircle);
            var innerTorus = new DifferenceSdf(evenSmallerCircle, theSmallestCircle);
            obj = new SdfObject(new UnionSdf(torus, innerTorus));
            
            // var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            // var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            // var union = new UnionSdf(r1, r2);
            // obj = new SdfObject(union);

            // var rect = new RectSdf(new Vector2(3.0f, 2.0f));
            // var rectObj = new SdfObject(rect);
            // var circ = new CircleSdf(1.0f);
            // var circObj = new SdfObject(circ);
            // rectObj.Position += new Vector2(1.0f, -1.0f);
            // circObj.Position += new Vector2(-1.0f, 3.0f);
            // var finalUnion = new UnionSdf(rectObj, circObj);
            // obj = new SdfObject(finalUnion);

            // var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            // var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            // var diff = new DifferenceSdf(r1, r2);
            // obj = new SdfObject(diff);

            // var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            // var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            // var isct = new IntersectionSdf(r1, r2);
            // obj = new SdfObject(isct);

            // obj = new SdfObject(new RectSdf(new Vector2(2.0f, 1.0f)));
        }

        private void Update()
        {
            if (update)
            {
                // rotation += 15 * Time.deltaTime;
                chunk = new VolumeChunk(new Vector2Int(size, size), area);
                obj.Position = position;
                obj.Rotation = rotation;
                obj.Scale = scale;
                chunk.Update(obj);
                // evaluator.EvaluateAgainst(chunk, trans);
            }
            
            /* Remove the old meshes */
            while (currFilters.Count > 0)
            {
                MeshPool.Instance.MeshFilterPool.Release(currFilters[0]);
                currFilters.RemoveAt(0);
            }
            
            /* Render the meshes */
            var output = SurfaceNets.Generate(chunk, obj);
            foreach (var mesh in output.meshes)
            {
                /* Gather a pooled mesh */
                var mf = MeshPool.Instance.MeshFilterPool.Get();
                mf.mesh = mesh;
                currFilters.Add(mf);
            }
            // polygons = output.polygons;
        }

        private List<List<SurfacePoint>> edges;
        
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                /* Render the underlying data */
                if (chunk != null)
                {
                    for (int x = 0; x < chunk.Points.GetLength(0); x++)
                    {
                        for (int y = 0; y < chunk.Points.GetLength(1); y++)
                        {
                            /* Underlying sample */
                            Gizmos.color = chunk.Points[x,y].SampleValue <= 0.0f ? Color.red : Color.black;
                            Gizmos.DrawSphere(chunk.VoxelToWorld(new Vector2Int(x, y)), 0.05f);
                        }
                    }
                }
                
                /* Render the polygons */
                foreach (var polygon in polygons)
                {
                    for (int i = 0; i < polygon.Count-1; i++)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(polygon[i].Position, polygon[i + 1].Position);
                        var lerpPoint = Vector2.Lerp(polygon[i].Position, polygon[i + 1].Position, 0.8f);
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawCube(lerpPoint, Vector3.one * 0.05f);
                    }
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(polygon[0].Position, polygon[^1].Position);
                    Gizmos.color = Color.magenta;
                    var finalLerp = Vector2.Lerp(polygon[^1].Position, polygon[0].Position, 0.8f);
                    Gizmos.DrawCube(finalLerp, Vector3.one * 0.05f);
                }

                // /* Voxel */
                // edges = SurfaceNets.Generate(chunk, obj);
                //
                // if (edges != null)
                // {
                //     Gizmos.color = Color.green;
                //     foreach (var polygon in edges)
                //     {
                //         for (int i = 0; i < polygon.Count-1; i++)
                //         {
                //             Gizmos.DrawLine(polygon[i].Position, polygon[i + 1].Position);
                //         }
                //         Gizmos.DrawLine(polygon[0].Position, polygon[^1].Position);
                //     }
                //     // string surfaceIDs = "";
                //     // foreach (var surface in edges)
                //     // { 
                //     //     surfaceIDs += surface[0].SurfaceID + ", ";
                //     //     var surfaceColor = Color.HSVToRGB(Random.Range(0.0f, 1.0f), 1.0f, 1.0f);
                //     //     foreach (var point in surface)
                //     //     {
                //     //         Gizmos.color = surfaceColor;
                //     //         Gizmos.DrawSphere(point.Position.ToVector3WithZ(-0.05f), 0.05f);
                //     //     }
                //     // }
                //     // Debug.Log(surfaceIDs);
                // }

            }
        }
    }
}