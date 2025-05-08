using System.Collections.Generic;
using UnityEngine;
using UnityUtilities.General;

namespace UnityUtilities.Meshing
{
    /// <summary>
    /// A helper object used to make procedural meshing easier.
    /// </summary>
    public class Mesher
    {

        /// <summary>
        /// A set of vertex attributes.
        /// </summary>
        public struct VertexAttr
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV;
            public Color Color;
        }
        
        /// <summary>
        /// The attributes/vertices of this mesh.
        /// </summary>
        private List<VertexAttr> vertexAttributes;
        
        /// <summary>
        /// The indices of this mesh.
        /// </summary>
        private List<int> indices;

        /// <summary>
        /// Whether or not duplicate vertices should be merged.
        /// </summary>
        private bool mergeVerts;
        
        /// <summary>
        /// Create a new mesher object.
        /// </summary>
        public Mesher(bool mergeVerts)
        {
            vertexAttributes = new List<VertexAttr>();
            indices = new List<int>();
            this.mergeVerts = mergeVerts;
        }

        /// <summary>
        /// Check for a duplicate vertex attribute
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private int CheckForDuplicate(Vector3 position)
        {
            if (!mergeVerts) return -1;
            
            for (int i = 0; i < vertexAttributes.Count; i++)
            {
                if (vertexAttributes[i].Position == position) return i;
            }

            return -1;
        }

        /// <summary>
        /// Generate a mesh from this mesher's state.
        /// </summary>
        /// <returns></returns>
        public Mesh GenerateMesh()
        {
            Mesh mesh = new Mesh();
            
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();

            foreach (var attr in vertexAttributes)
            {
                vertices.Add(attr.Position);
                uvs.Add(attr.UV);
                normals.Add(attr.Normal);
                colors.Add(attr.Color);
            }
            
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        /// <summary>
        /// Add a trriangleNET triangle to this mesh
        /// </summary>
        /// <param name="triangle"></param>
        public void AddTriangle(TriangleNet.Topology.Triangle triangle)
        {
            Vector3 a = triangle.vertices[0].ToVector2();
            Vector3 b = triangle.vertices[1].ToVector2();
            Vector3 c = triangle.vertices[2].ToVector2();
            
            AddTriangle(a, c, b);
        }

        /// <summary>
        /// Add a new triangle to this mesh.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            /* Assign indices (or find them) for these verts */
            int v1Index = CheckForDuplicate(v1);
            int v2Index = CheckForDuplicate(v2);
            int v3Index = CheckForDuplicate(v3);

            if (v1Index == -1)
            {
                vertexAttributes.Add(new VertexAttr() { Position = v1 });
                v1Index = vertexAttributes.Count - 1;
            }
            if (v2Index == -1)
            {
                vertexAttributes.Add(new VertexAttr() { Position = v2 });
                v2Index = vertexAttributes.Count - 1;
            }
            if (v3Index == -1)
            {
                vertexAttributes.Add(new VertexAttr() { Position = v3 });
                v3Index = vertexAttributes.Count - 1;
            }
            
            /* Add the triangle */
            indices.Add(v1Index);
            indices.Add(v2Index);
            indices.Add(v3Index);
        }

        /// <summary>
        /// Add a new triangle to this mesh with a solid color.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="color"></param>
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            /* Assign indices (or find them) for these verts */
            int v1Index = CheckForDuplicate(v1);
            int v2Index = CheckForDuplicate(v2);
            int v3Index = CheckForDuplicate(v3);

            if (v1Index == -1)
            {
                vertexAttributes.Add(new VertexAttr() { Position = v1, Color = color });
                v1Index = vertexAttributes.Count - 1;
            }
            if (v2Index == -1)
            {
                vertexAttributes.Add(new VertexAttr() { Position = v2, Color = color });
                v2Index = vertexAttributes.Count - 1;
            }
            if (v3Index == -1)
            {
                vertexAttributes.Add(new VertexAttr() { Position = v3, Color = color });
                v3Index = vertexAttributes.Count - 1;
            }
            
            /* Add the triangle */
            indices.Add(v1Index);
            indices.Add(v2Index);
            indices.Add(v3Index);
        }

        /// <summary>
        /// Add a quad to this mesh.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="v4"></param>
        /// <param name="color"></param>
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color)
        {
            AddTriangle(v1, v2, v4, color);
            AddTriangle(v2, v3, v4, color);
        }

        /// <summary>
        /// Add a fan of triangles (provided in CW order) to this mesh.
        /// </summary>
        /// <param name="vertices"></param>
        public void AddFan(Vector3[] vertices)
        {
            for (int i = 1; i < vertices.Length - 1; i++)
            {
                AddTriangle(vertices[0], vertices[i], vertices[i + 1]);
            }
        }
    }
}