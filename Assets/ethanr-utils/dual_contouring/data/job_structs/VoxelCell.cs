using System;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data.job_structs
{
    /// <summary>
    /// A voxel cell (four points/values turning into one surface point)
    /// </summary>
    public struct VoxelCell
    {
        /// <summary>
        /// The position of this voxel cell (lower left index).
        /// </summary>
        public readonly Vector2Int Position;

        /// <summary>
        /// The surface points contained within this cell.
        /// </summary>
        public Vector2Int?[] SurfacePoints;

        /// <summary>
        /// Construct a new, empty voxel cell.
        /// </summary>
        /// <param name="position"></param>
        public VoxelCell(Vector2Int position)
        {
            Position = position;
            SurfacePoints = new Vector2Int?[2]; //support at most 2.
        }

        /// <summary>
        /// Get the given corner of this cell
        /// </summary>
        /// <param name="corner"></param>
        /// <returns></returns>
        public VoxelPoint GetCorner(VoxelCorner corner)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the given edge of this cell.
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public VoxelEdge GetEdge(VoxelEdgeDirection edge)
        {
            throw new NotImplementedException();
        }
    }
}