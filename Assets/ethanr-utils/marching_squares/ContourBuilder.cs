using System;
using System.Collections.Generic;
using UnityEngine;

namespace ethanr_utils.marching_squares
{
    /// <summary>
    /// A helper class used to convert contours into polygons.
    /// </summary>
    public class ContourBuilder
    {
        
        public List<Edge> edges;

        /// <summary>
        /// A link of vectors to the edges they originate.
        /// </summary>
        public Dictionary<Vector2, List<Edge>> edgeDict;
        
        public ContourBuilder()
        {
            edges = new List<Edge>();
            edgeDict = new Dictionary<Vector2, List<Edge>>();
        }

        public void AddEdge(Edge e)
        {
            edges.Add(e);
            AddEdgeToDict(e);
        }

        private void AddEdgeToDict(Edge e)
        {
            if (edgeDict.TryGetValue(e.From, out var arr))
            {
                arr.Add(e);
            }
            else
            {
                edgeDict.Add(e.From, new List<Edge>() { e });
            }
        }

        public void AddEdge(Vector2 v1, Vector2 v2)
        {
            AddEdge(new Edge(v1, v2));
        }

        /// <summary>
        /// Build up a set of contours from the provided edges.
        /// </summary>
        /// <returns></returns>
        public List<List<Vector2>> BuildContoursVectors()
        {
            List<List<Vector2>> contours = new List<List<Vector2>>();
            List<Edge> edges = new List<Edge>();
            edges.AddRange(this.edges);
            
            /* Continue to build individual contours at a time */
            while (edges.Count > 0)
            {
                /* Start the next contour */
                List<Vector2> contour = new List<Vector2>();
                Edge currentEdge = edges[0];
                Edge startEdge = edges[0];
                edges.RemoveAt(0);
                contour.Add(currentEdge.From);
                
                /* Reassemble this contour */
                for (;;)
                {
                    /* Attempt to find the next contour in the chain */
                    List<Edge> nextEdgeArr;
                    if (!edgeDict.TryGetValue(currentEdge.To, out nextEdgeArr))
                    {
                        throw new Exception("Unable to find edge for contour.");
                    }
                    if (nextEdgeArr.Count > 1)
                    {
                        Debug.LogWarning("Too many edges!");
                    }
                    var nextEdge = nextEdgeArr[0];
                    edges.Remove(nextEdge);
                    
                    /* If this edge was our starting edge, we're done with this contour */
                    if (nextEdge.Equals(startEdge))
                    {
                        break;
                    }
                    
                    /* Add this edge into the contour */
                    contour.Add(nextEdge.From);
                    
                    /* If we're still going, update the current edge and continue */
                    currentEdge = nextEdge;
                }
                
                /* We successfully reassembled this contour */
                contours.Add(contour);
            }
            
            
            return contours;
        }
    }

    /// <summary>
    /// An edge in a contour.
    /// </summary>
    public struct Edge : IEquatable<Edge>
    {
        public Vector2 From;
        public Vector2 To;

        public Edge(Vector2 from, Vector2 to)
        {
            this.From = from;
            this.To = to;
        }

        /// <summary>
        /// Reverse this edge.
        /// </summary>
        /// <returns></returns>
        public Edge Reverse()
        {
            return new Edge(To, From);
        }

        /// <summary>
        /// True if this edge is somehow connected to another edge.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsConnectedTo(Edge other)
        {
            return From.Equals(other.From) || From.Equals(other.To) || To.Equals(other.From) || To.Equals(other.To);   
        }

        public bool Equals(Edge other)
        {
            return From.Equals(other.From) && To.Equals(other.To);
        }

        public override bool Equals(object obj)
        {
            return obj is Edge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To);
        }

        public override string ToString()
        {
            return $"({From} -> {To})";
        }
    }
}