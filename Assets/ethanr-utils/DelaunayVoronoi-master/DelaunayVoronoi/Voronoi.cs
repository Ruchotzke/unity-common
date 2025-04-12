using System.Collections.Generic;
using UnityEngine;

namespace UnityUtilities.DelaunayVoronoi
{
    public class Voronoi
    {
        public IEnumerable<Edge> GenerateEdgesFromDelaunay(IEnumerable<Triangle> triangulation)
        {
            var voronoiEdges = new HashSet<Edge>();
            foreach (var triangle in triangulation)
            {
                foreach (var neighbor in triangle.TrianglesWithSharedEdge)
                {
                    var edge = new Edge(triangle.Circumcenter, neighbor.Circumcenter);
                    voronoiEdges.Add(edge);
                }
            }

            return voronoiEdges;
        }
        
        /// <summary>
        /// Updated version which converts edges into faces.
        /// </summary>
        /// <param name="triangulation"></param>
        /// <returns></returns>
        public Dictionary<Point, List<Edge>> GenerateEdgesFromDelaunayFaces(IEnumerable<Triangle> triangulation)
        {
            var voronoiEdges = new Dictionary<Point, List<Edge>>();
            
            foreach (var triangle in triangulation)
            {
                foreach (var neighbor in triangle.TrianglesWithSharedEdge)
                {
                    /* Get two shared vertices */
                    List<Point> sharedVerts = new List<Point>();
                    foreach (var vertex in triangle.Vertices)
                    {
                        if (vertex == neighbor.Vertices[0])
                        {
                            sharedVerts.Add(vertex);
                        }
                        
                        if (vertex == neighbor.Vertices[1])
                        {
                            sharedVerts.Add(vertex);
                        }
                        
                        if (vertex == neighbor.Vertices[2])
                        {
                            sharedVerts.Add(vertex);
                        }
                    }

                    if (sharedVerts.Count != 2)
                    {
                        Debug.LogError("WRONG!");
                    }
                    
                    var edge = new Edge(triangle.Circumcenter, neighbor.Circumcenter);
                    
                    /* Categorize this edge into two faces */
                    if(voronoiEdges.ContainsKey(sharedVerts[0]))
                    {
                        voronoiEdges[sharedVerts[0]].Add(edge);
                    }
                    else
                    {
                        voronoiEdges[sharedVerts[0]] = new List<Edge>() { edge };
                    }
                    
                    if(voronoiEdges.ContainsKey(sharedVerts[1]))
                    {
                        voronoiEdges[sharedVerts[1]].Add(edge);
                    }
                    else
                    {
                        voronoiEdges[sharedVerts[1]] = new List<Edge>() { edge };
                    }
                }
            }

            return voronoiEdges;
        }
    }
}