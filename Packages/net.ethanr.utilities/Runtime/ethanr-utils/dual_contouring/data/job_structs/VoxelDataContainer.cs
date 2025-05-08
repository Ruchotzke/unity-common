using System.Collections.Generic;
using System.Linq;
using ethanr_utils.dual_contouring.csg_ops;
using TriangleNet.Geometry;
using Unity.VisualScripting;
using UnityEngine;
using UnityUtilities.Meshing;

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

        /// <summary>
        /// All the surface points for this object.
        /// </summary>
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
            
            /* Use a flood fill to assign points to contours based on their neighbors */
            FloodFillSurfaces();
            
            /* Generate contours based on surface points */
            var contours = GenerateContours();
            foreach (var contour in contours)
            {
                contour.AssembleContour(sdf);
            }
            
            /* Categorize contours as holes/surfaces based on their positioning/composition */
            ComposeContours(contours);
            
            /* Make meshes from contours */
            var meshes = GenerateMeshes(contours);

            return (meshes, contours);
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
                    var q = intersectionPoints.GroupBy(v => v).
                        Where(g => g.Count() > 1).Select(v => v.Key);
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
                    foreach (var direction in VoxelEdgeDirectionExtensions.GetAll())
                    {
                        /* Determine if this edge is even relevant */
                        if (!VoxelCells[x, y].GetEdge(this, direction).EdgeData.HasValue) continue;
                        
                        /* Get the neighboring cell, or treat this as a border edge */
                        if (VoxelCells[x, y].TryGetNeighbor(this, direction, out var neighbor))
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
                                        direction is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 1 : 0]);
                                }
                            }
                            else
                            {
                                /* This cell has two surface points (Saddle) */
                                if (neighbor.SurfacePoints[1] == null)
                                {
                                    /* Neighbor has one surface point */
                                    VoxelCells[x,y].SurfacePoints[direction is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 0 : 1]
                                        .Adjacent.Add(neighbor.SurfacePoints[0]);
                                }
                                else
                                {
                                    /* Neighbor has two surface points (Saddle) */
                                    VoxelCells[x,y].SurfacePoints[direction is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 0 : 1]
                                        .Adjacent.Add(neighbor.SurfacePoints[direction is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 1 : 0]);
                                }
                            }
                        }
                        else
                        {
                            /* This is a border with an edge point. Take note. */
                            var borderPoint = new SurfacePoint()
                            {
                                // ReSharper disable once PossibleInvalidOperationException (THIS IS OK!)
                                Position = VoxelCells[x, y].GetEdge(this, direction).EdgeIntersection.Value,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            };
                            borderContainer.AddBorder(borderPoint, sdf.SampleNormal(borderPoint.Position), direction);
                            
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
                                var index = direction is VoxelEdgeDirection.Left or VoxelEdgeDirection.Bottom ? 0 : 1;
                                borderPoint.Adjacent.Add(VoxelCells[x, y].SurfacePoints[index]);
                                VoxelCells[x, y].SurfacePoints[index].Adjacent.Add(borderPoint);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Use a flood fill algorithm to separate connected vertices into single contours.
        /// </summary>
        private void FloodFillSurfaces()
        {
            /* No double reaching */
            HashSet<SurfacePoint> open = new HashSet<SurfacePoint>();
            open.AddRange(Points);
            
            /* Iterate until we find a starting point */
            uint currID = 1;

            while (open.Count > 0)
            {
                /* Find this meshes starting point */
                SurfacePoint start = open.First();
                
                /* Flood fill the current mesh */
                Queue<SurfacePoint> queue = new Queue<SurfacePoint>();
                queue.Enqueue(start);
                open.Remove(start);

                while (queue.Count > 0)
                {
                    var curr = queue.Dequeue();
                    curr.SurfaceID = currID;

                    foreach (var neighbor in curr.Adjacent)
                    {
                        if (open.Contains(neighbor))
                        {
                            open.Remove(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                
                /* Move on to the next mesh */
                currID++;
            }
        }
        
        /// <summary>
        /// Use surface IDs to generate actual ordered contours.
        /// </summary>
        /// <returns></returns>
        private List<Contour> GenerateContours()
        {
            /* Each only belongs to one surface */
            HashSet<SurfacePoint> open = new HashSet<SurfacePoint>();
            open.AddRange(Points);
            
            var surfaces = new List<Contour>();
            uint id = 1;    
            
            while (open.Count > 0)
            {
                var currSurface = new Contour();
                foreach (var point in open)
                {
                    if (point.SurfaceID == id)
                    {
                        currSurface.Data.Add(point);
                    }
                }
                open.RemoveWhere(s => currSurface.Data.Contains(s));
                
                surfaces.Add(currSurface);
                if(open.Count > 0) id = open.First().SurfaceID;
            }

            return surfaces;
        }
        
        /// <summary>
        /// Compose inner contours into outer contours.
        /// </summary>
        /// <param name="allContours"></param>
        /// <returns></returns>
        private void ComposeContours(List<Contour> allContours)
        {
            /* First, determine containment lists for all contours */
            Dictionary<Contour, List<Contour>> containment = new Dictionary<Contour, List<Contour>>();
            foreach (var a in allContours)
            {
                containment[a] = new List<Contour>();
                
                foreach (var b in allContours)
                {
                    /* No dual case */
                    if (a == b) continue;
                    
                    /* Check confinement */
                    if (b.ContainsPoint(a.Data[0].Position))
                    {
                        containment[a].Add(b);
                    }
                }
            }
            
            /* Now, iteratively resolve the hierarchy of surfaces */
            HashSet<Contour> open = new HashSet<Contour>();
            HashSet<Contour> closed = new HashSet<Contour>();
            
            /* Start by putting contours into open/closed categories, with outer contours closed immediately */
            foreach (var contour in containment.Keys)
            {
                if (containment[contour].Count == 0)
                {
                    closed.Add(contour);
                    contour.Parent = null;
                }
                else
                {
                    open.Add(contour);
                }
            }
            
            /* Continue to resolve until all contours are taken care of */
            while (open.Count > 0)
            {
                /* Iterate through all open contours */
                /* Determine which can be closed (any whose parents are all resolved) */
                HashSet<Contour> canBeClosed = new HashSet<Contour>();
                foreach (var contour in open)
                {
                    var resolved = containment[contour].All(container => closed.Contains(container));

                    if (resolved) canBeClosed.Add(contour);
                }
                
                /* For all contours to close, update their parent based on maximum depth */
                /* Close them */
                foreach (var contour in canBeClosed)
                {
                    /* Update open/closed */
                    open.Remove(contour);
                    closed.Add(contour);
                    
                    /* Find the maximum depth potential parent */
                    var maxDepthParent = containment[contour][0];
                    foreach (var parent in containment[contour])
                    {
                        if (parent.GetDepth() > maxDepthParent.GetDepth())
                        {
                            maxDepthParent = parent;
                        }
                    }
                    contour.Parent = maxDepthParent;
                    maxDepthParent.Holes.Add(contour);
                }

                /* Safety case to avoid infinite loops */
                if (canBeClosed.Count == 0)
                {
                    Debug.LogError("Composition of contours halted; infinite loop detected.");
                    break;
                }
            }
        }
        
        /// <summary>
        /// Triangulate all the provided meshes from their polygons.
        /// </summary>
        /// <param name="contours"></param>
        /// <returns></returns>
        private List<Mesh> GenerateMeshes(List<Contour> contours)
        {
            List<Mesh> meshes = new List<Mesh>();
            
            /* We need to make a mesh for each even depth contour */
            List<Contour> evenDepth = contours.Where(contour => contour.GetDepth() % 2 == 0).ToList();
            
            /* Now make a mesh for each */
            foreach (var contour in evenDepth)
            {
                /* Generate a parent polygon */
                var tripoly = new Polygon();
                var vertices = GenerateVertices(contour);
                tripoly.Add(new TriangleNet.Geometry.Contour(vertices));
                
                /* Add any holes */
                foreach (var hole in contour.Holes)
                {
                    var holeVerts = GenerateVertices(hole);
                    tripoly.Add(new TriangleNet.Geometry.Contour(holeVerts), true);
                }
                
                /* Triangulate */
                var triangulation = tripoly.Triangulate();
                
                /* Generate and save the mesh */
                Mesher mesher = new Mesher(false);
                foreach (var tri in triangulation.Triangles)
                {
                    mesher.AddTriangle(tri);
                }
                
                meshes.Add(mesher.GenerateMesh());
            }

            return meshes;
        }
        
        /// <summary>
        /// Helper to convert our points into vertices for triangle-NET
        /// </summary>
        /// <param name="contour"></param>
        /// <returns></returns>
        private static List<Vertex> GenerateVertices(Contour contour)
        {
            List<Vertex> vertices = new List<Vertex>();

            foreach (var contourPoint in contour.Data)
            {
                vertices.Add(new Vertex(contourPoint.Position.x, contourPoint.Position.y));
            }
            
            return vertices;
        }
    }
}