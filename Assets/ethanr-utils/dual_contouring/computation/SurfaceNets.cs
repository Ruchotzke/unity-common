using System.Collections.Generic;
using System.Linq;
using ethanr_utils.dual_contouring.csg_ops;
using ethanr_utils.dual_contouring.data;
using TriangleNet.Geometry;
using Unity.VisualScripting;
using UnityEngine;
using UnityUtilities.Meshing;
using Contour = ethanr_utils.dual_contouring.data.Contour;
using TriContour = TriangleNet.Geometry.Contour;

namespace ethanr_utils.dual_contouring.computation
{
    /* SO USEFUL:::::https://www.boristhebrave.com/2018/04/15/dual-contouring-tutorial/ */
    
    /// <summary>
    /// A set of functions used to generate mesh data from volumetric space
    /// using surface nets (non-normal preserving dual contouring).
    /// </summary>
    public static class SurfaceNets
    {
        /// <summary>
        /// Generate the surface.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public static (List<Mesh> meshes, List<Contour> contours) Generate(VolumeChunk chunk, SdfOperator sdf)
        {
            /* SYNC POINT: Sampling data provided */
            /* GOAL: Compute all intersections and normals */
            /* Start with right and up from source */
            for (int x = 0; x < chunk.Points.GetLength(0) - 1; x++)
            {
                for (int y = 0; y < chunk.Points.GetLength(1) - 1; y++)
                {
                    /* Find all edge intersection interpolations (and normals if applicable) */
                    if (CheckIntersection(chunk.Points[x, y], chunk.Points[x + 1, y], out float t))
                    {
                        /* BL->BR */
                        chunk.Edges.SetEdge(new Vector2Int(x,y), EdgeContainer.EdgeDirection.Right, new Vector3(0.0f, 0.0f, t));
                    }
                    if (CheckIntersection(chunk.Points[x, y+1], chunk.Points[x, y], out t))
                    {
                        /* TL->BL */
                        chunk.Edges.SetEdge(new Vector2Int(x, y+1), EdgeContainer.EdgeDirection.Down,  new Vector3(0.0f, 0.0f, t));
                    }
                }
            }
            
            /* We missed some edges: namely the far right and far top */
            var width = chunk.Points.GetLength(0);
            var height = chunk.Points.GetLength(1);
            for (int x = 0; x < width - 1; x++)
            {
                if (CheckIntersection(chunk.Points[x, height-1], chunk.Points[x + 1, height-1], out float t))
                {
                    /* L->R */
                    chunk.Edges.SetEdge(new Vector2Int(x,height-1), EdgeContainer.EdgeDirection.Right, new Vector3(0.0f, 0.0f, t));
                }
            }
            for (int y = 0; y < height - 1; y++)
            {
                if (CheckIntersection(chunk.Points[width - 1, y], chunk.Points[width - 1, y + 1], out float t))
                {
                    /* B->T */
                    chunk.Edges.SetEdge(new Vector2Int(width-1, y), EdgeContainer.EdgeDirection.Up, new Vector3(0.0f, 0.0f, t));
                }
            }
            
            /* SYNC POINT - All edge/intersection data computed */
            /* GOAL: Map voxels to singular edge point */
            var surfacePoints = new Dictionary<Vector2Int, SurfacePoint>(); // store all surface points computed
            var surfaceEdges = new List<(Vector2Int a, Vector2Int b, EdgeContainer.EdgeDirection dir)>(); // store all edges the surface passes through
            
            /* For each voxel, we can compute one singular internal point */
            for (int x = 0; x < chunk.Points.GetLength(0) - 1; x++)
            {
                for (int y = 0; y < chunk.Points.GetLength(1) - 1; y++)
                {
                    var voxel = new Vector2Int(x, y);
                    
                    /* Grab all intersection points */
                    var isctPoints = new List<Vector2>();
                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel, EdgeContainer.EdgeDirection.Right, chunk,
                            out var isct))
                    {
                        /* BOTTOM */
                        isctPoints.Add(isct);
                        surfaceEdges.Add((voxel, voxel + Vector2Int.right, EdgeContainer.EdgeDirection.Right));
                    }

                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel, EdgeContainer.EdgeDirection.Up, chunk, out isct))
                    {
                        /* LEFT */
                        isctPoints.Add(isct);
                        surfaceEdges.Add((voxel, voxel + Vector2Int.up, EdgeContainer.EdgeDirection.Up));
                    }

                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel + Vector2Int.one,
                            EdgeContainer.EdgeDirection.Down, chunk, out isct))
                    {
                        /* RIGHT */
                        isctPoints.Add(isct);
                        surfaceEdges.Add((voxel + Vector2Int.right, voxel + Vector2Int.one, EdgeContainer.EdgeDirection.Up));
                    }

                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel + Vector2Int.one,
                            EdgeContainer.EdgeDirection.Left, chunk, out isct))
                    {
                        /* TOP */
                        isctPoints.Add(isct);
                        surfaceEdges.Add((voxel + Vector2Int.up, voxel + Vector2Int.one, EdgeContainer.EdgeDirection.Right));
                    }
                    
                    /* Now, handle the voxel based on the number of intersection points */
                    switch(isctPoints.Count)
                    {
                        case 0:
                            /* Fully internal or external */
                            continue;
                        case 2:
                            /* Normal case - average */
                            surfacePoints.Add(voxel, new SurfacePoint
                            {
                                Position = (isctPoints[0] + isctPoints[1]) * 0.5f,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            } );
                            break;
                        case 4:
                            /* Saddle point - weird */
                            Debug.LogWarning($"Saddle Point...");
                            break;
                        default:
                            Debug.LogError($"Unable to process voxel with {isctPoints.Count} intersections...");
                            continue;
                    }
                }
            }

            /* SYNC POINT - All relevant voxels have a point */
            /* GOAL: Connect voxel points together into isosurface */
            var mapBoundaryPoints = new BorderContainer(); //also save these to finish loops later
            
            /* Traverse all surface voxel edges (identified earlier) to find neighboring points */
            foreach (var voxelEdge in surfaceEdges)
            {
                /* If this is a border edge, handle it differently */
                if (IsBorderEdge(voxelEdge, width, height))
                {
                    /* Connect one voxel point to one border edge point */
                    if (voxelEdge.dir == EdgeContainer.EdgeDirection.Up)
                    {
                        if (voxelEdge.a.x == 0)
                        {
                            /* Left border */
                            var innerPoint = surfacePoints[voxelEdge.a];
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out var isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }
                            
                            /* Generate a new surface point */
                            var edgePoint = new SurfacePoint()
                            {
                                Position = isct,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            };

                            mapBoundaryPoints.AddLeftBorder(edgePoint, sdf.SampleNormal(edgePoint.Position));
                            edgePoint.Adjacent.Add(innerPoint);
                            innerPoint.Adjacent.Add(edgePoint);
                        }
                        else
                        {
                            /* Right border */
                            var innerPoint = surfacePoints[voxelEdge.a + Vector2Int.left];
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out var isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            /* Generate a new surface point */
                            var edgePoint = new SurfacePoint()
                            {
                                Position = isct,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            };

                            mapBoundaryPoints.AddRightBorder(edgePoint, sdf.SampleNormal(edgePoint.Position));
                            edgePoint.Adjacent.Add(innerPoint);
                            innerPoint.Adjacent.Add(edgePoint);
                        }
                    }
                    else if (voxelEdge.dir == EdgeContainer.EdgeDirection.Right)
                    {
                        if (voxelEdge.a.y == 0)
                        {
                            /* Bottom border */
                            var innerPoint = surfacePoints[voxelEdge.a];
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out var isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            /* Generate a new surface point */
                            var edgePoint = new SurfacePoint()
                            {
                                Position = isct,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            };

                            mapBoundaryPoints.AddBottomBorder(edgePoint, sdf.SampleNormal(edgePoint.Position));
                            edgePoint.Adjacent.Add(innerPoint);
                            innerPoint.Adjacent.Add(edgePoint);
                        }
                        else
                        {
                            /* Top border */
                            var innerPoint = surfacePoints[voxelEdge.a + Vector2Int.down];
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out var isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            /* Generate a new surface point */
                            var edgePoint = new SurfacePoint()
                            {
                                Position = isct,
                                SurfaceID = 0,
                                Adjacent = new List<SurfacePoint>()
                            };

                            mapBoundaryPoints.AddTopBorder(edgePoint, sdf.SampleNormal(edgePoint.Position));
                            edgePoint.Adjacent.Add(innerPoint);
                            innerPoint.Adjacent.Add(edgePoint);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Unable to process voxel with {voxelEdge.dir} inside...");
                    }
                }
                else
                {
                    /* Connect two voxel points together */
                    if (voxelEdge.dir == EdgeContainer.EdgeDirection.Up)
                    {
                        var leftVoxel = voxelEdge.a + Vector2Int.left;
                        var a = surfacePoints[leftVoxel];
                        var b = surfacePoints[voxelEdge.a];
                        a.Adjacent.Add(b);
                        b.Adjacent.Add(a);
                    }
                    else if (voxelEdge.dir == EdgeContainer.EdgeDirection.Right)
                    {
                        var botVoxel = voxelEdge.a + Vector2Int.down;
                        var a = surfacePoints[botVoxel];
                        var b = surfacePoints[voxelEdge.a];
                        a.Adjacent.Add(b);
                        b.Adjacent.Add(a);
                    }
                    else
                    {
                        Debug.LogError($"Unable to process voxel with {voxelEdge.dir} inside...");
                    }
                }
            }
            
            /* Don't forget the potential corner/edge loops too */
            var corners = mapBoundaryPoints.GetCorners(chunk.Area, sdf);
            var cornerList = new List<SurfacePoint>();
            if (corners.bl != null) cornerList.Add(corners.bl);
            if (corners.br != null) cornerList.Add(corners.br);
            if (corners.tr != null) cornerList.Add(corners.tr);
            if (corners.tl != null) cornerList.Add(corners.tl);
            
            /* Also connect up adjacent edges on the borders */
            mapBoundaryPoints.ConnectEdges();
            
            /* Tag/fill in surface IDs of all points */
            List<SurfacePoint> allSurfacePoints = new List<SurfacePoint>();
            allSurfacePoints.AddRange(surfacePoints.Values);
            allSurfacePoints.AddRange(mapBoundaryPoints.GetAll());
            allSurfacePoints.AddRange(cornerList);
            FloodFillSurfaces(allSurfacePoints);
            
            /* SYNC POINT - All edges have been enumerated and points tagged */
            /* GOAL: Put connected isosurfaces into their own polygons */
            var contours = GenerateSurfaces(allSurfacePoints);
            
            /* Make sure contours are in order */
            foreach (var contour in contours)
            {
                contour.AssembleContour(sdf);
            }
            
            /* Compose inner contours into outer contours */
            var outerContours = ComposeContours(contours);
            
            /* SYNC POINT - All voxel data has been encoded into polygons */
            /* GOAL: Generate a mesh */

            return (GenerateMeshes(outerContours), outerContours);
        }

        /// <summary>
        /// Triangulate all of the provided meshes from their polygons.
        /// </summary>
        /// <param name="outerContours"></param>
        /// <returns></returns>
        private static List<Mesh> GenerateMeshes(List<Contour> outerContours)
        {
            List<Mesh> meshes = new List<Mesh>();
            
            foreach (var contour in outerContours)
            {
                /* Generate the triangulation */
                var tripoly = new Polygon();
                var vertices = GenerateVertices(contour);
                tripoly.Add(new TriContour(vertices));
                
                /* Add all holes */
                foreach (var hole in contour.Holes)
                {
                    var holeVerts = GenerateVertices(hole);
                    tripoly.Add(new TriContour(holeVerts), true);
                }
                
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
        /// Compose inner contours into outer contours.
        /// Output list only contains outer contours, and each outer contour references its own holes.
        /// </summary>
        /// <param name="allContours"></param>
        /// <returns></returns>
        private static List<Contour> ComposeContours(List<Contour> allContours)
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

            while (open.Count > 0)
            {
                /* Find the contours with only one resolved parent */
                var toClose = new List<Contour>();
                foreach (var contour in open)
                {
                    if (containment[contour].Count == 1 && closed.Contains(containment[contour][0]))
                    {
                        toClose.Add(contour);
                    }
                }
                
                /* Move these contours to closed */
                /* Mark
                foreach (var contour in toClose)
                {
                    open.Remove(contour);
                    closed.Add(contour);
                }
            }
            
        }

        /// <summary>
        /// Helper to convert our points into vertices for trianglenet
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

        private static List<Contour> GenerateSurfaces(List<SurfacePoint> surfacePoints)
        {
            /* Each only belongs to one surface */
            HashSet<SurfacePoint> open = new HashSet<SurfacePoint>();
            open.AddRange(surfacePoints);
            
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
        /// Flood fill surface identifiers to match surfaces across multiple points.
        /// </summary>
        /// <param name="surfacePoints"></param>
        private static void FloodFillSurfaces(List<SurfacePoint> surfacePoints)
        {
            /* No double reaching */
            HashSet<SurfacePoint> open = new HashSet<SurfacePoint>();
            open.AddRange(surfacePoints);
            
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
            
            // Debug.Log($"Finished flood filling mesh with {currID} surfaces.");
        }

        /// <summary>
        /// Determine if this edge is on the edge of the voxel space.
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static bool IsBorderEdge((Vector2Int a, Vector2Int b, EdgeContainer.EdgeDirection dir) edge, int width, int height)
        {
            if (edge.dir == EdgeContainer.EdgeDirection.Up)
            { 
                return (edge.a.x == 0 || edge.a.x == width - 1);
            }
            else if (edge.dir == EdgeContainer.EdgeDirection.Right)
            {
                return (edge.a.y == 0 || edge.a.y == height - 1);
            }

            Debug.LogError("Bad case encountered in IsBorderEdge.");
            return false;
        }

        /// <summary>
        /// Check for a voxel intersection across a and b's shared edge.
        /// If it exists, return it.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static bool CheckIntersection(Voxel a, Voxel b, out float t)
        {
            /* Initial case: no intersection */
            if (a.SampleValue > 0 && b.SampleValue > 0 || a.SampleValue < 0 && b.SampleValue < 0)
            {
                t = 0;
                return false;
            }
            
            /* We have an intersection point, compute it */
            t = ApproxLinearInterp(a.SampleValue, b.SampleValue);
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