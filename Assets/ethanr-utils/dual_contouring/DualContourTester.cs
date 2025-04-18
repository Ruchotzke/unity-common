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

        private int size = 16;
        private Rect area = new Rect(0.0f, 0.0f, 4.0f, 4.0f);
        private SdfObject obj;
        
        private void Awake()
        {
            /* Generate a volume filled with the SDF of a circle */
            // chunk = new VolumeChunk(new Vector2Int(size, size), area);
            // var largerCircle = new CircleSdf(1.5f);
            // var smallerCircle = new CircleSdf(.75f);
            // var torus = new DifferenceSdf(largerCircle, smallerCircle);
            // obj = new SdfObject(torus);
            
            var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            var union = new UnionSdf(r1, r2);
            obj = new SdfObject(union);
            
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
        }

        private List<List<SurfacePoint>> edges;
        
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                if (chunk != null)
                {
                    for (int x = 0; x < chunk.Points.GetLength(0); x++)
                    {
                        for (int y = 0; y < chunk.Points.GetLength(1); y++)
                        {
                            /* Underlying sample */
                            Gizmos.color = chunk.Points[x,y].SampleValue <= 0.0f ? Color.red : Color.black;
                            Gizmos.DrawSphere(chunk.VoxelToWorld(new Vector2Int(x, y)), 0.1f);
                        }
                    }
                }
                
                /* Voxel */
                edges = SurfaceNets.Generate(chunk);
                
                if (edges != null)
                {
                    string surfaceIDs = "";
                    foreach (var surface in edges)
                    { 
                        surfaceIDs += surface[0].SurfaceID + ", ";
                        var surfaceColor = Color.HSVToRGB(Random.Range(0.0f, 1.0f), 1.0f, 1.0f);
                        foreach (var point in surface)
                        {
                            Gizmos.color = surfaceColor;
                            Gizmos.DrawSphere(point.Position.ToVector3WithZ(-0.05f), 0.05f);
                        }
                    }
                    Debug.Log(surfaceIDs);
                }
                
            }
        }
    }
}