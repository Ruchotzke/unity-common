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
        public Vector2?[] SurfacePoints;

        /// <summary>
        /// Construct a new, empty voxel cell.
        /// </summary>
        /// <param name="position"></param>
        public VoxelCell(Vector2Int position)
        {
            Position = position;
            SurfacePoints = new Vector2?[2]; //support at most 2.
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
        /// <param name="data"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public VoxelEdge GetEdge(VoxelDataContainer data, VoxelEdgeDirection edge)
        {
            return edge switch
            {
                VoxelEdgeDirection.Bottom => data.RightVoxelEdges[Position.x, Position.y],
                VoxelEdgeDirection.Right => data.UpVoxelEdges[Position.x + 1, Position.y],
                VoxelEdgeDirection.Top => data.RightVoxelEdges[Position.x, Position.y + 1],
                VoxelEdgeDirection.Left => data.UpVoxelEdges[Position.x, Position.y],
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null)
            };
        }

        /// <summary>
        /// Determine whether this edge is crossed by the isosurface.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool IsEdgeIntersected(VoxelDataContainer data, VoxelEdgeDirection edge)
        {
            return edge switch
            {
                VoxelEdgeDirection.Bottom => data.RightVoxelEdges[Position.x, Position.y].EdgeData.HasValue,
                VoxelEdgeDirection.Right => data.UpVoxelEdges[Position.x + 1, Position.y].EdgeData.HasValue,
                VoxelEdgeDirection.Top => data.RightVoxelEdges[Position.x, Position.y + 1].EdgeData.HasValue,
                VoxelEdgeDirection.Left => data.UpVoxelEdges[Position.x, Position.y].EdgeData.HasValue,
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null)
            };
        }

        /// <summary>
        /// Get the neighbor
        /// </summary>
        /// <param name="data"></param>
        /// <param name="edge"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool GetNeighbor(VoxelDataContainer data, VoxelEdgeDirection edge, out VoxelCell neighbor)
        {
            switch (edge)
            {
                case VoxelEdgeDirection.Bottom:
                    if (Position.y == 0)
                    {
                        neighbor = default;
                        return false;
                    }
                    else
                    {
                        neighbor = data.VoxelCells[Position.x, Position.y - 1];
                        return true;
                    }
                case VoxelEdgeDirection.Right:
                    if (Position.x == data.Size.x - 1)
                    {
                        neighbor = default;
                        return false;
                    }
                    else
                    {
                        neighbor = data.VoxelCells[Position.x + 1, Position.y];
                        return true;
                    }
                case VoxelEdgeDirection.Top:
                    if (Position.y == data.Size.y - 1)
                    {
                        neighbor = default;
                        return false;
                    }
                    else
                    {
                        neighbor = data.VoxelCells[Position.x, Position.y + 1];
                        return true;
                    }
                case VoxelEdgeDirection.Left:
                    if (Position.x == 0)
                    {
                        neighbor = default;
                        return false;
                    }
                    else
                    {
                        neighbor = data.VoxelCells[Position.x-1, Position.y];
                        return true;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }
    }
}