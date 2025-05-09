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
        /// The data contained within this contour
        /// </summary>
        public List<SurfacePoint> Data = new();

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
        public void AssembleContour(SdfOperator sdf)
        {
            List<SurfacePoint> polygon = new List<SurfacePoint>();

            /* Use BFS to build an ordered loop */
            Queue<SurfacePoint> queue = new Queue<SurfacePoint>();
            queue.Enqueue(Data[0]);
            Data.RemoveAt(0);

            /* BFS */
            while (queue.Count > 0)
            {
                /* Grab the next entry */
                var curr = queue.Dequeue();
                
                /* Add neighbors if they haven't been added yet */
                foreach (var neighbor in curr.Adjacent)
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
                    if (polygon[0].Adjacent.Contains(curr)) // first entry
                    {
                        polygon.Insert(0, curr);
                    }
                    else if (polygon[^1].Adjacent.Contains(curr)) // insertion entry
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
                if (!DoesOrderWindClockwise(polygon[1], sdf.SampleNormal(polygon[1].Position), polygon[2]))
                {
                    /* NOT CLOCKWISE WRT NORMAL */
                    /* Flip this polygon */
                    polygon.Reverse();
                }
            }
            
            Data = polygon;
        }

        /// <summary>
        /// Determine if this is a hole or outer polygon using winding order.
        /// </summary>
        /// <returns></returns>
        public bool IsInnerContour()
        {
            /* Sample the first three points to determine winding */
            return !DoesOrderWindClockwise(Data[0], Data[1], Data[2]);
        }

        /// <summary>
        /// Get the box bounding this contour.
        /// </summary>
        /// <returns></returns>
        public Rect GetBoundingBox()
        {
            Vector2 min = Data[0].Position;
            Vector2 max = Data[0].Position;

            foreach (var point in Data)
            {
                if(point.Position.x < min.x) min.x = point.Position.x;
                if(point.Position.y < min.y) min.y = point.Position.y;
                if(point.Position.x > max.x) max.x = point.Position.x;
                if(point.Position.y > max.y) max.y = point.Position.y;
            }
            
            return new Rect(min, max - min);
        }

        /// <summary>
        /// Determine whether this contour is ordered clockwise.
        /// </summary>
        /// <returns></returns>
        public bool OrderIsClockwise()
        {
            return SignedArea.ComputeSignedArea(this) < 0;
        }
        
        /// <summary>
        /// Check the ordering from a to b with respect to the normal.
        /// Returns true if this will build a clockwise ordering.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="aNorm"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool DoesOrderWindClockwise(SurfacePoint a, Vector2 aNorm, SurfacePoint b)
        {
            var n = a.Position + aNorm;
            
            /* We now have a->n->b */
            var area = ((a.Position.x * (n.y - b.Position.y)) + (n.x * (b.Position.y - a.Position.y)) + (b.Position.x * (a.Position.y - n.y)));

            return area < 0;
        }
        
        /// <summary>
        /// Check the ordering of triangle ABC
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool DoesOrderWindClockwise(SurfacePoint a, SurfacePoint b, SurfacePoint c)
        {
            /* We now have a->b->c */
            var area = ((a.Position.x * (b.Position.y - c.Position.y)) + (b.Position.x * (c.Position.y - a.Position.y)) + (c.Position.x * (a.Position.y - b.Position.y)));

            return area < 0;
        }

        /// <summary>
        /// Test whether a supplied point is contained within this contour.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool ContainsPoint(Vector2 point)
        {
            /* Check each edge: we're raycasting to the right. */
            var intersections = 0;
            for (var i = 0; i < Data.Count - 1; i++)
            {
                var a = Data[i].Position;
                var b = Data[i + 1].Position;
                
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
            var aa = Data[^1].Position;
            var bb = Data[0].Position;
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

        public string DumpPoints()
        {
            var ret = "";
            foreach (var point in Data)
            {
                ret += $"({point.Position.x}, {point.Position.y})\n";
            }

            return ret;
        }
    }
}