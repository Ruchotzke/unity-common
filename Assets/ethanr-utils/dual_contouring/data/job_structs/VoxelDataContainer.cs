using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data.job_structs
{
    /// <summary>
    /// A single container used to contain all the voxel data.
    /// </summary>
    public class VoxelDataContainer
    {
        /// <summary>
        /// The number of VOXEL CELLS contained in the x and y directions.
        /// </summary>
        public readonly Vector2Int Size;

        /// <summary>
        /// The area containing all of these voxels.
        /// </summary>
        public readonly Rect Area;

        /// <summary>
        /// The length of an edge.
        /// </summary>
        private readonly Vector2 edgeSizes;

        /// <summary>
        /// All contained voxel cells.
        /// </summary>
        public VoxelCell[,] VoxelCells;
        
        /// <summary>
        /// All contained voxel points.
        /// </summary>
        public VoxelPoint[,] VoxelPoints;
        
        /// <summary>
        /// All contained rightward voxel edges.
        /// </summary>
        public VoxelEdge[,] RightVoxelEdges;

        /// <summary>
        /// All contained upward voxel edges.
        /// </summary>
        public VoxelEdge[,] UpVoxelEdges;
        
        /// <summary>
        /// A container used to hold a SINGLE COPY of all voxel information.
        /// </summary>
        public VoxelDataContainer(Vector2Int size, Rect area)
        {
            Size = size;
            Area = area;
            edgeSizes = new Vector2(area.width/size.x, area.height/size.y); 
            
            VoxelCells = new VoxelCell[size.x, size.y];
            VoxelPoints = new VoxelPoint[size.x+1, size.y+1];
            RightVoxelEdges = new VoxelEdge[size.x+1, size.y+1];
            UpVoxelEdges = new VoxelEdge[size.x+1, size.y+1];
            
            /* Initialize all cells */
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    VoxelCells[x, y] = new VoxelCell(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>
        /// Sample all points of this voxel space.
        /// </summary>
        /// <param name="sdf"></param>
        public void SamplePoints(SdfOperator sdf)
        {
            for (int x = 0; x < VoxelPoints.GetLength(0); x++)
            {
                for (int y = 0; y < VoxelPoints.GetLength(1); y++)
                {
                    /* Convert this voxel position into a world position */
                    var worldPos = Area.min + new Vector2(x * edgeSizes.x, y * edgeSizes.y);
                    
                    /* Sample the sdf */
                    var sample = sdf.SampleValue(worldPos);
                    
                    /* Save */
                    VoxelPoints[x,y] = new VoxelPoint(worldPos, sample);
                }
            }
        }

        /// <summary>
        /// Sample all edges of this voxel space.
        /// </summary>
        /// <param name="sdf"></param>
        public void SampleEdges(SdfOperator sdf)
        {
            /* First handle all baseline edges (have two edges per point) */
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    /* Tackle the right edge for this point */
                    RightVoxelEdges[x, y] = new VoxelEdge(this, sdf, new Vector2Int(x, y), VoxelDirection.Right);

                    /* And the upward edge */
                    UpVoxelEdges[x, y] = new VoxelEdge(this, sdf, new Vector2Int(x, y), VoxelDirection.Up);
                }
            }
            
            /* Also handle all right/top edges */
            for (int x = 0; x < Size.x; x++)
            {
                RightVoxelEdges[x, Size.y] = new VoxelEdge(this, sdf, new Vector2Int(x, Size.y), VoxelDirection.Right);
            }

            for (int y = 0; y < Size.y; y++)
            {
                UpVoxelEdges[Size.x, y] = new VoxelEdge(this, sdf, new Vector2Int(Size.x, y), VoxelDirection.Up); 
            }
        }
    }
}