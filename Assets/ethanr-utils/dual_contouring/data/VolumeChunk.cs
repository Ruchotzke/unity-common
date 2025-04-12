using System.Collections.Generic;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A chunk of data dedicated to storage of hermite data.
    /// </summary>
    public class VolumeChunk
    {
        /// <summary>
        /// The number of points contained in this chunk.
        /// </summary>
        public Vector2Int Size;

        /// <summary>
        /// The world space area represented by this chunk.
        /// </summary>
        public Rect Area;

        /// <summary>
        /// All the sample points in this chunk.
        /// </summary>
        public Voxel[,] Points;

        /// <summary>
        /// The edges within this chunk.
        /// Vector3 component represents a normal, and z component represents intersection point.
        /// </summary>
        public EdgeContainer Edges;

        /// <summary>
        /// The length of a given x or y edge.
        /// </summary>
        private Vector2 EdgeSizes;

        /// <summary>
        /// Construct a new, empty volume chunk.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="area"></param>
        public VolumeChunk(Vector2Int size, Rect area)
        {
            Size = size;
            Area = area;
            EdgeSizes = new Vector2(area.width/(size.x-1), area.height/(size.y-1)); 
            Points = new Voxel[size.x, size.y];
            Edges = new EdgeContainer();
        }

        /// <summary>
        /// Convert a provided voxel coordinate into a world coordinate.
        /// </summary>
        /// <param name="voxel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Vector2 VoxelToWorld(Vector2Int voxel)
        {
            /* First ensure the provided voxel is within bounds */
            if (voxel.x >= Points.GetLength(0) || voxel.y >= Points.GetLength(1))
            {
                throw new System.ArgumentException("Invalid voxel");
            }
            
            /* Simply index */
            return Area.min + voxel * EdgeSizes;
        }

        /// <summary>
        /// Convert a provided voxel coordinate into a world coordinate.
        /// </summary>
        /// <returns></returns>
        public Vector2 VoxelToWorld(int x, int y)
        {
            return VoxelToWorld(new Vector2Int(x, y));
        }

        /// <summary>
        /// Get the indices of any voxels contained within the given rect.
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        public List<Vector2Int> GetVoxelsInArea(Rect area)
        {
            List<Vector2Int> voxels = new List<Vector2Int>();
            
            /* There is definitely a better way */
            for (int x = 0; x < Points.GetLength(0); x++)
            {
                for (int y = 0; y < Points.GetLength(1); y++)
                {
                    var worldPos = VoxelToWorld(new Vector2Int(x, y));
                    if (area.Contains(worldPos))
                    {
                        voxels.Add(new Vector2Int(x, y));            
                    }
                }
            }
            
            return voxels;
        }
    }   
}

