using ethanr_utils.dual_contouring.data;
using UnityEngine;

namespace ethanr_utils.dual_contouring.sdf
{
    /// <summary>
    /// An evaluator for a given SDF.
    /// </summary>
    public abstract class SdfEvaluator
    {
        /// <summary>
        /// Evaluate this SDF against a provided arbitrary point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public abstract float SampleSDF(Vector2 point);

        /// <summary>
        /// Evaluate this SDF against the provided volume chunk, updating it.
        /// </summary>
        /// <param name="chunk"></param>
        public virtual void EvaluateAgainst(VolumeChunk chunk)
        {
            /* Iterate over and sample each piece of the chunk */
            for (int x = 0; x < chunk.Points.GetLength(0); x++)
            {
                for (int y = 0; y < chunk.Points.GetLength(1); y++)
                {
                    var sample = chunk.VoxelToWorld(new Vector2Int(x, y));
                    chunk.Points[x, y] = new Voxel(0, SampleSDF(sample));
                }
            }
        }

        /// <summary>
        /// Evaluate the provided SDF against the provided chunk, updating it.
        /// Uses a provided transform to transform the SDF.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="transform"></param>
        public virtual void EvaluateAgainst(VolumeChunk chunk, TransformEvaluator transform)
        {
            /* Iterate over and sample each piece of the chunk */
            for (int x = 0; x < chunk.Points.GetLength(0); x++)
            {
                for (int y = 0; y < chunk.Points.GetLength(1); y++)
                {
                    var sample = chunk.VoxelToWorld(new Vector2Int(x, y));
                    chunk.Points[x, y] = new Voxel(0, transform.Evaluate(this, sample));
                }
            }
        }
    }
}