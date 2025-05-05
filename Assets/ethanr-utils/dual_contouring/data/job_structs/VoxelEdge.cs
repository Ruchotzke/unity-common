using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data.job_structs
{
    /// <summary>
    /// An edge connecting two voxel points.
    /// </summary>
    public struct VoxelEdge
    {
        /// <summary>
        /// The origin coordinate of this edge.
        /// </summary>
        public readonly Vector2Int Origin;
        
        /// <summary>
        /// The direction of this edge with respect to its origin.
        /// </summary>
        public readonly VoxelDirection Direction;

        /// <summary>
        /// The data for this edge packed into a single value, or null for no intersection.
        /// </summary>
        public readonly Vector3? EdgeData;

        /// <summary>
        /// The world position of this edge's intersection point.
        /// </summary>
        public readonly Vector2? EdgeIntersection;

        /// <summary>
        /// Generate a new voxel edge from the provided data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        public VoxelEdge(VoxelDataContainer data, SdfOperator sdf, Vector2Int origin, VoxelDirection direction)
        {
            /* Default sets */
            Origin = origin;
            Direction = direction;
            
            /* Get both points associated with this edge */
            VoxelPoint a = data.VoxelPoints[origin.x, origin.y];
            VoxelPoint b = (direction == VoxelDirection.Right) ? data.VoxelPoints[origin.x + 1, origin.y] : data.VoxelPoints[origin.x, origin.y + 1];
            
            /* Determine if this is an edge with an intersection point */
            if (CheckIntersection(a, b, out float t))
            {
                /* We have an edge */
                var worldPos = Vector2.Lerp(a.Position, b.Position, t);
                var normal = sdf.SampleNormal(worldPos);
                EdgeData = new Vector3(normal.x, normal.y, t);
                EdgeIntersection = worldPos;
            }
            else
            {
                /* This edge does not cross the isosurface */
                EdgeData = null;
                EdgeIntersection = null;
            }
        }
        
        /// <summary>
        /// Check the intersection of two voxel points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool CheckIntersection(VoxelPoint a, VoxelPoint b, out float t)
        {
            /* Initial case: no intersection */
            if (a.Value > 0 && b.Value > 0 || a.Value < 0 && b.Value < 0)
            {
                t = 0;
                return false;
            }
            
            /* Tangential case: Both samples are on the isoline (0) */
            /* For now (TODO) consider these non-valid */
            if (a.Value == 0.0f && b.Value == 0.0f)
            {
                t = 0.5f;
                return false;
            }
            
            /* We have an intersection point, compute it */
            t = ApproxLinearInterp(a.Value, b.Value);
            return true;
        }

        /// <summary>
        /// Use binary search to attempt to find the isosurface here (non-exact but quick and easy)
        /// </summary>
        /// <param name="aSample"></param>
        /// <param name="bSample"></param>
        /// <returns></returns>
        private static float ApproxLinearInterp(float aSample, float bSample)
        {
            const int iterations = 8; // Can be adjusted for more/less accuracy

            if (aSample == 0)
            {
                return 0.0f;
            }

            if (bSample == 0)
            {
                return 1.0f;
            }
            
            if (aSample > bSample)
            {
                return 1.0f - ApproxLinearInterp(bSample, aSample);
            }
            
            float t = 0.5f;
            for (int i = 0; i < iterations; i++)
            {
                var sample = Mathf.Lerp(aSample, bSample, t);
                var delta = 1.0f / Mathf.Pow(2.0f, i + 1);
                if (sample == 0.0f)
                {
                    return t;
                }
                else if (sample > 0.0f)
                {
                    /* Sample is too high */
                    t -= delta;
                }
                else
                {
                    /* Sample is too low */
                    t += delta;
                }
            }

            return t;
        }
    }
}