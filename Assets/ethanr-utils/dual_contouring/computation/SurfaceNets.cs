using System;
using System.Collections.Generic;
using System.Linq;
using ethanr_utils.dual_contouring.data;
using UnityEngine;

namespace ethanr_utils.dual_contouring.computation
{
    /* SO USEFUL:::::https://www.boristhebrave.com/2018/04/15/dual-contouring-tutorial/ */
    
    /// <summary>
    /// A set of functions used to generate mesh data from volumetric space
    /// using surface nets (non-normal preserving dual contouring).
    /// </summary>
    public class SurfaceNets
    {
        /// <summary>
        /// Generate the surface.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public static List<(Vector2 a, Vector2 b)> Generate(VolumeChunk chunk)
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
            var surfacePoints = new Dictionary<Vector2Int, Vector2>(); // store all surface points computed
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
                            surfacePoints.Add(voxel, (isctPoints[0] + isctPoints[1]) * 0.5f);
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
            var surface = new List<(Vector2 a, Vector2 b)>();
            List<Vector2> mapBoundaryPoints = new List<Vector2>(); //also save these to finish loops later

            
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
                            Vector2 isct;
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            surface.Add((isct, innerPoint));
                            mapBoundaryPoints.Add(isct);
                        }
                        else
                        {
                            /* Right border */
                            var innerPoint = surfacePoints[voxelEdge.a + Vector2Int.left];
                            Vector2 isct;
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            surface.Add((isct, innerPoint));
                            mapBoundaryPoints.Add(isct);
                        }
                    }
                    else if (voxelEdge.dir == EdgeContainer.EdgeDirection.Right)
                    {
                        if (voxelEdge.a.y == 0)
                        {
                            /* Bottom border */
                            var innerPoint = surfacePoints[voxelEdge.a];
                            Vector2 isct;
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            surface.Add((isct, innerPoint));
                            mapBoundaryPoints.Add(isct);
                        }
                        else
                        {
                            /* Top border */
                            var innerPoint = surfacePoints[voxelEdge.a + Vector2Int.down];
                            Vector2 isct;
                            if (!chunk.Edges.TryGetEdgeIntersectionPoint(voxelEdge.a, voxelEdge.dir, chunk, out isct))
                            {
                                Debug.LogError($"Unable to find intersection for voxel {voxelEdge.a}");
                            }

                            surface.Add((innerPoint, isct));
                            mapBoundaryPoints.Add(isct);
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
                        surface.Add((a,b));
                    }
                    else if (voxelEdge.dir == EdgeContainer.EdgeDirection.Right)
                    {
                        var botVoxel = voxelEdge.a + Vector2Int.down;
                        var a = surfacePoints[botVoxel];
                        var b = surfacePoints[voxelEdge.a];
                        surface.Add((a,b));
                    }
                    else
                    {
                        Debug.LogError($"Unable to process voxel with {voxelEdge.dir} inside...");
                    }
                }
            }
            
            /* SYNC POINT - All edges have been enumerated */
            /* GOAL: Put connected isosurfaces into their own polygons */
            /* Step 1: Assemble surfaces as sets of connected edges */
            var edgeMap = new Dictionary<Vector2, List<Vector2>>();
            foreach (var edge in surface)
            {
                if(!edgeMap.ContainsKey(edge.a)) edgeMap.Add(edge.a, new List<Vector2>());
                if(!edgeMap.ContainsKey(edge.b)) edgeMap.Add(edge.b, new List<Vector2>());
                edgeMap[edge.a].Add(edge.b);
                edgeMap[edge.b].Add(edge.a);
            }
            
            /* SYNC POINT - All voxel data has been encoded into polygons */
            /* GOAL: Generate a mesh */

            return surface;
        }

        /// <summary>
        /// Attempt to assemble a set of polygons from the provided map.
        /// </summary>
        /// <param name="edgeMap"></param>
        /// <param name="polygons"></param>
        /// <returns></returns>
        private bool AssembleContours(Dictionary<Vector2, List<Vector2>> edgeMap, out List<List<Vector2>> polygons)
        {
            polygons = new List<List<Vector2>>();
            var visited = new HashSet<Vector2>();
            
            /* Loop through each potential contour ring */
            foreach (var start in edgeMap.Keys)
            {
                /* Don't start a new ring from a vertex that already was used elsewhere */
                if (visited.Contains(start)) continue;

                List<Vector2> loop = new();
                Vector2 current = start;
                Vector2? previous = null;

                do
                {
                    /* Save this vertex into the current loop */
                    loop.Add(current);
                    visited.Add(current);

                    /* Get the next neighbor */
                    var neighbors = edgeMap[current];
                    Vector2 next = neighbors.FirstOrDefault(n => n != previous);

                    if (next == default)
                        break; // dead end

                    previous = current;
                    current = next;

                } while (current != start && !visited.Contains(current));

                if (loop.Count > 2 && current == start)
                {
                    loops.Add(loop);
                }
            }
        }

        /// <summary>
        /// Determine if this edge is on the edge of the voxel space.
        /// </summary>
        /// <param name="edge"></param>
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
            const int ITERATIONS = 8; // Can be adjusted for more/less accuracy

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
            for (int i = 0; i < ITERATIONS; i++)
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