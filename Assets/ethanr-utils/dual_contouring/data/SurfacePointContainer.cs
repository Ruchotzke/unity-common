using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ethanr_utils.dual_contouring.data.job_structs;
using Unity.Collections;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A container for surface point information.
    /// </summary>
    public class SurfacePointContainer
    {

        /// <summary>
        /// The points of this voxel data.
        /// </summary>
        public NativeArray<SurfacePoint> Points;

        /// <summary>
        /// The current count of surface points.
        /// </summary>
        public int PointCount;

        /// <summary>
        /// The adjacency matrix for this container.
        /// </summary>
        public NativeArray<bool> Adjacency;

        /// <summary>
        /// The maximum number of surface points allowed in this container.
        /// </summary>
        public int MaxEntries;

        /// <summary>
        /// Generate a new empty surface point container.
        /// </summary>
        /// <param name="maxEntries"></param>
        public SurfacePointContainer(int maxEntries)
        {
            /* Initialize point storage */
            MaxEntries = maxEntries;
            Points = new NativeArray<SurfacePoint>(maxEntries, Allocator.Domain, NativeArrayOptions.UninitializedMemory);
            PointCount = 0;
            
            /* Also initialize a 2D adjacency matrix */
            Adjacency = new NativeArray<bool>(maxEntries * maxEntries, Allocator.Domain, NativeArrayOptions.ClearMemory);
        }

        /// <summary>
        /// Generate a new surface point and return its index.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="ConstraintException"></exception>
        public int GenerateSurfacePoint(Vector2 position)
        {
            /* First make sure we have room */
            if(PointCount >= MaxEntries) throw new ConstraintException($"Maximum entries ({MaxEntries}) exceeded.");
            
            /* Generate and store a new point */
            Points[PointCount] = new SurfacePoint(position);
            PointCount++;
            
            return PointCount - 1;
        }

        /// <summary>
        /// Make the two indices provided neighbors.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public void MakeAdjacent(int a, int b)
        {
            /* Bounds check */
            if(a < 0 || a >= PointCount) throw new ArgumentException($"First index ({a}) is out of range ({PointCount}).");
            if(b < 0 || b >= PointCount) throw new ArgumentException($"Second index ({b}) is out of range ({PointCount}).");
            
            /* Mark the corresponding entries to true */
            Adjacency[a + MaxEntries * b] = true;
            Adjacency[b + MaxEntries * a] = true;
        }

        /// <summary>
        /// Determine if two indices are adjacent.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool GetAdjacent(int a, int b)
        {
            /* Bounds check */
            if(a < 0 || a >= PointCount) throw new ArgumentException($"First index ({a}) is out of range ({PointCount}).");
            if(b < 0 || b >= PointCount) throw new ArgumentException($"Second index ({b}) is out of range ({PointCount}).");
            
            return Adjacency[a + MaxEntries * b];
        }

        /// <summary>
        /// Get all adjacent points to the provided point index.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public List<int> GetAdjacent(int a)
        {
            List<int> result = new();
            var baseIndex = MaxEntries * a;
            for (int i = 0; i < MaxEntries; i++)
            {
                /* look for any true indices */
                var index = i + baseIndex;
                if(Adjacency[index]) result.Add(i);
            }
            
            return result;
        }

        /// <summary>
        /// Use a flood fill algorithm to identify connected surfaces.
        /// </summary>
        public void FloodFillSurfaces()
        {
            /* No double reaching */
            HashSet<int> open = new HashSet<int>();
            for (int i = 0; i < PointCount; i++)
            {
                open.Add(i);
            }
            
            /* Iterate until we find a starting point */
            uint currID = 1;

            while (open.Count > 0)
            {
                /* Find this meshes starting point */
                var start = open.First();
                
                /* Flood fill the current mesh */
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(start);
                open.Remove(start);

                while (queue.Count > 0)
                {
                    var curr = queue.Dequeue();
                    
                    /* Read, alter, write */
                    var surfacePoint = Points[curr];
                    surfacePoint.SurfaceID = currID;
                    Points[curr] = surfacePoint;

                    foreach (var neighbor in GetAdjacent(curr))
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
        public List<Contour> GenerateContours()
        {
            /* Each only belongs to one surface */
            HashSet<int> open = new HashSet<int>();
            for (int i = 0; i < PointCount; i++)
            {
                open.Add(i);
            }
            
            var surfaces = new List<Contour>();
            uint id = 1;    
            
            while (open.Count > 0)
            {
                var currSurface = new Contour();
                foreach (var point in open)
                {
                    if (Points[point].SurfaceID == id)
                    {
                        currSurface.Data.Add(point);
                    }
                }
                open.RemoveWhere(s => currSurface.Data.Contains(s));
                
                surfaces.Add(currSurface);
                if(open.Count > 0) id = Points[open.First()].SurfaceID;
            }

            return surfaces;
        }

        public void Dispose()
        {
            Points.Dispose();
            Adjacency.Dispose();
        }
    }
}