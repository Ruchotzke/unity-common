using System;
using System.Collections.Generic;
using System.Linq;
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
        /// The border points for this object.
        /// </summary>
        private BorderContainer borderContainer;

        public List<SurfacePoint> Points;
        
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
            
            borderContainer = new BorderContainer();
            
            Points = new List<SurfacePoint>();
            
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
        /// Use surface nets to voxelize this container.
        /// </summary>
        /// <param name="sdf"></param>
        /// <param name="qef"></param>
        /// <returns></returns>
        public (List<Mesh> meshes, List<Contour> contours) SurfaceNets(SdfOperator sdf, QEFSettings qef)
        {
            /* First initialize the data */
            SamplePoints(sdf);
            SampleEdges(sdf);
            
            /* Now compute the interior points of all voxel cells */
            GenerateSurfacePoints();
            
            /* Connect up neighboring surface points */
            ConnectSurfacePoints(sdf);
            
            /* Don't forget the border/new verts */
            var corners = borderContainer.GetCorners(Area, sdf);
            var cornerList = new List<SurfacePoint>();
            if (corners.bl != null) cornerList.Add(corners.bl);
            if (corners.br != null) cornerList.Add(corners.br);
            if (corners.tr != null) cornerList.Add(corners.tr);
            if (corners.tl != null) cornerList.Add(corners.tl);
            
            borderContainer.ConnectEdges();

            foreach (var cell in VoxelCells)
            {
                foreach (var point in cell.SurfacePoints)
                {
                    if(point != null) Points.Add(point);
                }
            }

            Points.AddRange(borderContainer.GetAll());
            Points.AddRange(cornerList);
            
            /* Generate contours based on surface points */
            
            /* Make meshes from contours */

            return (new List<Mesh>(), null);
        }

        /// <summary>
        /// Sample all points of this voxel space.
        /// </summary>
        /// <param name="sdf"></param>
        private void SamplePoints(SdfOperator sdf)
        {
            for (int x = 0; x < VoxelPoints.GetLength(0); x++)
            {
                for (int y = 0; y < VoxelPoints.GetLength(1); y++)
                {
                    /* Convert this voxel position into a world position */
                    var worldPos = Area.min + new Vector2(x * edgeSizes.x, y * edgeSizes.y);
                    
                    /* Sample the sdf */
                    var sample = sdf.SampleValue(worldPos);

                    if (sample == 0.0f)
                    {
                        /* Perturb! Just a bit... */
                        sample = (x + y % 2 == 0) ? float.Epsilon : -float.Epsilon;
                    }
                    
                    /* Save */
                    VoxelPoints[x,y] = new VoxelPoint(worldPos, sample);
                }
            }
        }

        /// <summary>
        /// Sample all edges of this voxel space.
        /// </summary>
        /// <param name="sdf"></param>
        private void SampleEdges(SdfOperator sdf)
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

        /// <summary>
        /// Compute the surface points for all voxel cells.
        /// </summary>
        private void GenerateSurfacePoints()
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    /* Determine the number of intersection points */
                    List<Vector3> intersectionPoints = new List<Vector3>();
                    var bot = VoxelCells[x, y].GetEdge(this, VoxelEdgeDirection.Bottom);
                    var top = VoxelCells[x, y].GetEdge(this, VoxelEdgeDirection.Top);
                    var left = VoxelCells[x, y].GetEdge(this, VoxelEdgeDirection.Left);
                    var right = VoxelCells[x, y].GetEdge(this, VoxelEdgeDirection.Right);
                    
                    if (bot.EdgeIntersection != null) intersectionPoints.Add(bot.EdgeIntersection.Value);
                    if (right.EdgeIntersection != null) intersectionPoints.Add(right.EdgeIntersection.Value);
                    if (top.EdgeIntersection != null) intersectionPoints.Add(top.EdgeIntersection.Value);
                    if (left.EdgeIntersection != null) intersectionPoints.Add(left.EdgeIntersection.Value);
                    
                    /* We also need to remove duplicates, as sometimes a corner has an intersection */
                    //TODO: Handle this more appropriately, as in singleton overlap points between edges.
                    var q = intersectionPoints.GroupBy(x => x).
                        Where(g => g.Count() > 1).Select(x => x.Key);
                    foreach (var dup in q)
                    {
                        intersectionPoints.Remove(dup);
                    }
                    
                    /* Compute surface points based on number of intersections (2 or 4) */
                    switch (intersectionPoints.Count)
                    {
                        case 0:
                            /* Nothing to do, fully internal/external */
                            continue;
                        case 1:
                            /* Technically impossible, but is possible if we remove a duplicate vertex */
                            /* Appears in one corner */
                            /* In this case, we just know our intersection point */
                            VoxelCells[x,y].SurfacePoints[0] = new SurfacePoint(intersectionPoints[0]);
                            break;
                        case 2:
                            /* Single point in this cell, normal case */
                            var avg = intersectionPoints[0] + intersectionPoints[1];
                            avg /= 2.0f;
                            VoxelCells[x, y].SurfacePoints[0] = new SurfacePoint(avg);
                            break;
                        case 4:
                            /* Saddle point, arbitrarily resolve */
                            /* Connect B/L and T/R */
                            var bl = (intersectionPoints[(int)VoxelEdgeDirection.Bottom] + intersectionPoints[(int)VoxelEdgeDirection.Left]) / 2.0f;
                            var tr = (intersectionPoints[(int)VoxelEdgeDirection.Top] + intersectionPoints[(int)VoxelEdgeDirection.Right]) / 2.0f;

                            VoxelCells[x, y].SurfacePoints[0] = new SurfacePoint(bl);
                            VoxelCells[x, y].SurfacePoints[1] = new SurfacePoint(tr);
                            break;
                        default:
                            /* Something WEIRD is happening */
                            Debug.LogError($"Unable to process voxel with {intersectionPoints.Count} intersections...");
                            continue;
                    }
                }
            }
        }

        /// <summary>
        /// Iterate cell by cell to connect up neighboring surface points.
        /// </summary>
        private void ConnectSurfacePoints(SdfOperator sdf)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    /* Only process this cell if it has a surface point */
                    if (VoxelCells[x, y].SurfacePoints[0] == null) continue;
                    
                    /* Connect this point to any neighbors via intersection edges */
                    foreach (var edgedir in VoxelEdgeDirectionExtensions.GetAll())
                    {
                        /* Determine if this edge is even relevant */
                        if (!VoxelCells[x, y].GetEdge(this, edgedir).EdgeData.HasValue) continue;
                        
                        /* Get the neighboring cell, or treat this as a border edge */
                        if (VoxelCells[x, y].TryGetNeighbor(this, edgedir, out var neighbor))
                        {
                            /* Connect the surface points */
                            /* Four possible cases because of saddle points */
                            if (VoxelCells[x, y].SurfacePoints[1] == null)
                            {
                                /* This cell has one surface point */
                                if (neighbor.SurfacePoints[1] == null)
                                {
                                    /* Neighbor has one surface point */
                                    VoxelCells[x,y].SurfacePoints[0].Adjacent.Add(neighbor.SurfacePoints[0]);
                                }
                                else
                                {
                                    /* Neighbor has two surface points (Saddle) */
                                    VoxelCells[x,y].SurfacePoints[0].Adjacent.Add(neighbor.SurfacePoints[
                                        edgedir is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 1 : 0]);
                                }
                            }
                            else
                            {
                                /* This cell has two surface points (Saddle) */
                                if (neighbor.SurfacePoints[1] == null)
                                {
                                    /* Neighbor has one surface point */
                                    VoxelCells[x,y].SurfacePoints[edgedir is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 0 : 1]
                                        .Adjacent.Add(neighbor.SurfacePoints[0]);
                                }
                                else
                                {
                                    /* Neighbor has two surface points (Saddle) */
                                    VoxelCells[x,y].SurfacePoints[edgedir is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 0 : 1]
                                        .Adjacent.Add(neighbor.SurfacePoints[edgedir is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 1 : 0]);
                                }
                            }
                        }
                        else
                        {
                            /* This is a border with an edge point. Take note. */
                            var borderPoint = new SurfacePoint()
                            {
                                Position = VoxelCells[x, y].GetEdge(this, edgedir).EdgeIntersection.Value,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            };
                            borderContainer.AddBorder(borderPoint, sdf.SampleNormal(borderPoint.Position), edgedir);
                            
                            /* Don't forget to link to neighbors too */
                            if (VoxelCells[x, y].SurfacePoints[1] == null)
                            {
                                /* Only one point in this cell */
                                borderPoint.Adjacent.Add(VoxelCells[x, y].SurfacePoints[0]);
                                VoxelCells[x, y].SurfacePoints[0].Adjacent.Add(borderPoint);
                            }
                            else
                            {
                                /* Multiple in this cell */
                                var index = edgedir is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 0 : 1;
                                borderPoint.Adjacent.Add(VoxelCells[x, y].SurfacePoints[index]);
                                VoxelCells[x, y].SurfacePoints[index].Adjacent.Add(borderPoint);
                            }
                        }
                    }
                }
            }
        }
    }
}