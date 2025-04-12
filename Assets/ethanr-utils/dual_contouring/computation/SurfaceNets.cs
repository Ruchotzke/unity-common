using System;
using System.Collections.Generic;
using ethanr_utils.dual_contouring.data;
using UnityEngine;

namespace ethanr_utils.dual_contouring.computation
{
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
                    // if (CheckIntersection(chunk.Points[x+1, y], chunk.Points[x + 1, y+1], out t))
                    // {
                    //     /* BR->TR */
                    //     chunk.Edges.SetEdge(new Vector2Int(x+1, y), EdgeContainer.EdgeDirection.Up,  new Vector3(0.0f, 0.0f, t));
                    // }
                    // if (CheckIntersection(chunk.Points[x+1, y+1], chunk.Points[x, y+1], out t))
                    // {
                    //     /* TR->TL */
                    //     chunk.Edges.SetEdge(new Vector2Int(x + 1, y+1), EdgeContainer.EdgeDirection.Left,  new Vector3(0.0f, 0.0f, t));
                    // }
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
            Dictionary<Vector2Int, Vector2> surfacePoints = new Dictionary<Vector2Int, Vector2>();
            
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
                    }

                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel, EdgeContainer.EdgeDirection.Up, chunk, out isct))
                    {
                        /* LEFT */
                        isctPoints.Add(isct);
                    }

                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel + Vector2Int.one,
                            EdgeContainer.EdgeDirection.Down, chunk, out isct))
                    {
                        /* RIGHT */
                        isctPoints.Add(isct);
                    }

                    if (chunk.Edges.TryGetEdgeIntersectionPoint(voxel + Vector2Int.one,
                            EdgeContainer.EdgeDirection.Left, chunk, out isct))
                    {
                        /* TOP */
                        isctPoints.Add(isct);
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
            
            /* Start by connecting voxels in the up/right direction */
            foreach (var voxel in surfacePoints.Keys)
            {
                /* Get the corresponding surface point */
                var point = surfacePoints[voxel];
                
                /* Attempt to connect to a neighbor or both */
                if (surfacePoints.TryGetValue(new Vector2Int(voxel.x + 1, voxel.y), out Vector2 neighbor))
                {
                    surface.Add((point, neighbor));
                }

                if (surfacePoints.TryGetValue(new Vector2Int(voxel.x, voxel.y + 1), out neighbor))
                {
                    surface.Add((point, neighbor));
                }
            }
            
            /* We also need to make extra edges for the voxels along the edge (we want complete meshes) */
            List<Vector2> mapBoundaryPoints = new List<Vector2>(); //also save these to finish loops later
            
            foreach (var voxel in surfacePoints.Keys)
            {
                /* We only want edges */
                if (!(voxel.x == 0 || voxel.y == 0 || voxel.x == width - 1 || voxel.y == height - 1)) continue;
                
                /* Check for an edge intersection on the edge of the map */
                if (voxel.x == 0 && chunk.Edges.TryGetEdgeIntersectionPoint(voxel, EdgeContainer.EdgeDirection.Up, chunk, out var isct))
                {
                    /* LEFT SIDE */
                    mapBoundaryPoints.Add(isct);
                    surface.Add((isct, surfacePoints[voxel]));
                }
                if (voxel.y == 0 && chunk.Edges.TryGetEdgeIntersectionPoint(voxel, EdgeContainer.EdgeDirection.Right, chunk, out isct))
                {
                    /* BOTTOM SIDE */
                    mapBoundaryPoints.Add(isct);
                    surface.Add((isct, surfacePoints[voxel]));
                }
                if (voxel.x == width-2 && chunk.Edges.TryGetEdgeIntersectionPoint(voxel + Vector2Int.one,
                             EdgeContainer.EdgeDirection.Down, chunk, out isct))
                {
                    /* RIGHT SIDE */
                    mapBoundaryPoints.Add(isct);
                    surface.Add((isct, surfacePoints[voxel]));
                }
                if (voxel.y == height-2 && chunk.Edges.TryGetEdgeIntersectionPoint(voxel + Vector2Int.one,
                             EdgeContainer.EdgeDirection.Left, chunk, out isct))
                {
                    /* TOP SIDE */
                    mapBoundaryPoints.Add(isct);
                    surface.Add((isct, surfacePoints[voxel]));
                }
            }
            
            /* SYNC POINT - All edges have been enumerated */
            /* GOAL: Put connected isosurfaces into their own polygons */
            
            /* SYNC POINT - All voxel data has been encoded into polygons */
            /* GOAL: Generate a mesh */

            return surface;
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
            const int ITERATIONS = 5; // Can be adjusted for more/less accuracy

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