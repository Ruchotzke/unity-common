using System.Collections.Generic;
using ethanr_utils.dual_contouring.csg_ops;
using UnityEngine;
using UnityUtilities.General;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A contour containing a set of surface points.
    /// </summary>
    public class Contour
    {
        /// <summary>
        /// The data contained within this contour (references to indices)
        /// </summary>
        public List<int> Data = new();

        /// <summary>
        /// Any contours which represent holes in this one
        /// </summary>
        public List<Contour> Holes = new();

        /// <summary>
        /// The contour which contains this contour (null implies no containing contour */
        /// </summary>
        public Contour Parent;

        /// <summary>
        /// Arrange the various points within this contour into order.
        /// </summary>
        public void AssembleContour(SdfOperator sdf, SurfacePointContainer points)
        {
            List<int> polygon = new List<int>();

            /* Use BFS to build an ordered loop */
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(Data[0]);
            Data.RemoveAt(0);

            /* BFS */
            while (queue.Count > 0)
            {
                /* Grab the next entry */
                var curr = queue.Dequeue();
                
                /* Add neighbors if they haven't been added yet */
                foreach (var neighbor in points.GetAdjacent(curr))
                {
                    if (!Data.Contains(neighbor)) continue;
                    queue.Enqueue(neighbor);
                    Data.Remove(neighbor);
                }
                
                /* Place this vertex into the loop appropriately. */
                if (polygon.Count == 0)
                {
                    polygon.Add(curr);
                }
                else
                {
                    if (points.GetAdjacent(polygon[0], curr)) // first entry
                    {
                        polygon.Insert(0, curr);
                    }
                    else if (points.GetAdjacent(polygon[^1], curr)) // insertion entry
                    {
                        polygon.Add(curr);
                    }
                    else // uh oh
                    {
                        /* Whoa! This isn't connected... */
                        Debug.LogError($"Missing connected vertex for {curr}");
                    }
                }
            }
            
            /* If this surface still has points, something went wrong */
            if (Data.Count > 0)
            {
                Debug.LogError("Unable to assemble polygon from surface.");
            }
            
            /* Make sure the winding order is correct with respect to the normals */
            if (polygon.Count > 2)
            {
                if (!DoesOrderWindClockwise(points.Points[polygon[1]].Position, sdf.SampleNormal(points.Points[polygon[1]].Position), points.Points[polygon[2]].Position))
                {
                    /* NOT CLOCKWISE WRT NORMAL */
                    /* Flip this polygon */
                    polygon.Reverse();
                }
            }
            
            Data = polygon;
        }
        
        /// <summary>
        /// Check the ordering of triangle ABC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool DoesOrderWindClockwise(Vector2 a, Vector2 b, Vector2 c)
        {
            /* We now have a->b->c */
            var area = ((a.x * (b.y - c.y)) + (b.x * (c.y - a.y)) + (c.x * (a.y - b.y)));

            return area < 0;
        }

        /// <summary>
        /// Test whether a supplied point is contained within this contour.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool ContainsPoint(Vector2 point, SurfacePointContainer points)
        {
            /* Check each edge: we're raycasting to the right. */
            var intersections = 0;
            for (var i = 0; i < Data.Count - 1; i++)
            {
                var a = points.Points[Data[i]].Position;
                var b = points.Points[Data[i + 1]].Position;
                
                // Check if the horizontal ray from point intersects edge (v1,v2)
                var cond1 = (a.y > point.y) != (b.y > point.y);
                if (!cond1) continue;
                var xIntersect = a.x + (point.y - a.y) * (b.x - a.x) / (b.y - a.y);
                if (xIntersect > point.x)
                {
                    intersections++;
                }
            }
            
            /* And the final edge */
            var aa = points.Points[Data[^1]].Position;
            var bb = points.Points[Data[0]].Position;
            var cond11 = (aa.y > point.y) != (bb.y > point.y);
            if (cond11)
            {
                var xIntersect = aa.x + (point.y - aa.y) * (bb.x - aa.x) / (bb.y - aa.y);
                if (xIntersect > point.x)
                {
                    intersections++;
                }
            }

            return intersections % 2 == 1;
        }

        /// <summary>
        /// Get the depth of this contour in terms of parental depth.
        /// </summary>
        /// <returns></returns>
        public int GetDepth()
        {
            return Parent == null ? 0 : Parent.GetDepth() + 1;
        }
    }
}