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
        private float rotation = 0;
        
        private SdfEvaluator evaluator;
        
        private void Awake()
        {
            /* Generate a volume filled with the SDF of a circle */
            chunk = new VolumeChunk(new Vector2Int(15, 15), new Rect(0.0f, 0.0f, 3.0f, 3.0f));
            evaluator = new RectEvaluator(new Vector2(0.5f, 1.0f)); 
            var trans = new TransformEvaluator(Vector2.one, rotation, 1.0f);
            evaluator.EvaluateAgainst(chunk, trans);
        }

        private void Update()
        {
            if (rotate)
            {
                rotation += 15 * Time.deltaTime;
                var trans = new TransformEvaluator(0.25f * Mathf.Cos(Time.time) * Vector2.one + Vector2.one * 1.5f, rotation, 0.5f * Mathf.Sin(Time.time) + 1f); 
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
                            Gizmos.color = chunk.Points[x,y].SampleValue <= 0.0f ? Color.red : Color.white;
                            Gizmos.DrawSphere(chunk.VoxelToWorld(new Vector2Int(x, y)), 0.1f);
                            
                            /* Voxel */
                            var points = SurfaceNets.Generate(chunk);
                            foreach (var point in points)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawSphere(point.ToVector3WithZ(-0.2f), 0.1f);
                            }
                        }
                    }
                }
            }
        }
    }
}