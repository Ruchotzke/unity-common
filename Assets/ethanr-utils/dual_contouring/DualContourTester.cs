using System;
using ethanr_utils.dual_contouring.computation;
using ethanr_utils.dual_contouring.data;
using ethanr_utils.dual_contouring.sdf;
using UnityEngine;
using UnityUtilities.General;

namespace ethanr_utils.dual_contouring
{
    /// <summary>
    /// A basic tester for dual contouring stuff.
    /// </summary>
    public class DualContourTester : MonoBehaviour
    {
        private VolumeChunk chunk;
        [SerializeField] private bool rotate;
        [SerializeField] private Vector2 position;
        [SerializeField] private float rotation = 4.6f;
        [SerializeField] private float scale = 1.0f;
        
        private SdfEvaluator evaluator;
        
        private void Awake()
        {
            /* Generate a volume filled with the SDF of a circle */
            chunk = new VolumeChunk(new Vector2Int(8, 8), new Rect(0.0f, 0.0f, 4.0f, 4.0f));
            evaluator = new RectEvaluator(new Vector2(3f, 1.5f)); 
            var trans = new TransformEvaluator(position, rotation, scale);
            evaluator.EvaluateAgainst(chunk, trans);
            
            Debug.Log($"{evaluator.SampleSDF(Vector2.zero)}");
        }

        private void Update()
        {
            if (rotate)
            {
                // rotation += 15 * Time.deltaTime;
                chunk = new VolumeChunk(new Vector2Int(8, 8), new Rect(0.0f, 0.0f, 4.0f, 4.0f));
                var trans = new TransformEvaluator(position, rotation, scale);
                evaluator.EvaluateAgainst(chunk, trans);
            }
        }

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
                            
                            /* Voxel */
                            var edges = SurfaceNets.Generate(chunk);
                            foreach (var edge in edges)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawLine(edge.a.ToVector3WithZ(-0.05f), edge.b.ToVector3WithZ(-0.05f));
                            }
                        }
                    }
                }
            }
        }
    }
}