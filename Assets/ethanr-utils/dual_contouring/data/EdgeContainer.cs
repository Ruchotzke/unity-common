using System;
using System.Collections.Generic;
using UnityEngine;

namespace ethanr_utils.dual_contouring.data
{
    /// <summary>
    /// A container for voxel edge information (in contrast to points)
    /// </summary>
    public class EdgeContainer
    {
        public enum EdgeDirection
        {
            Left,
            Right,
            Up,
            Down
        }
        
        /// <summary>
        /// A map from a point to a horizontal (+X) edge.
        /// </summary>
        private Dictionary<Vector2Int, Vector3> horizontalEdges;
        
        /// <summary>
        /// A map from a point to a vertical (+Y) edge.
        /// </summary>
        private Dictionary<Vector2Int, Vector3> verticalEdges;
        
        public EdgeContainer()
        {
            horizontalEdges = new Dictionary<Vector2Int, Vector3>();
            verticalEdges = new Dictionary<Vector2Int, Vector3>();
        }

        /// <summary>
        /// Set the provided edge.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="direction"></param>
        /// <param name="edge"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetEdge(Vector2Int source, EdgeDirection direction, Vector3 edge)
        {
            switch (direction)
            {
                case EdgeDirection.Left:
                    edge.z = 1.0f - edge.z; //invert due to flip of direction
                    horizontalEdges[new Vector2Int(source.x - 1, source.y)] = edge;
                    break;
                case EdgeDirection.Right:
                    horizontalEdges[source] = edge;
                    break;
                case EdgeDirection.Up:
                    verticalEdges[source] = edge;
                    break;
                case EdgeDirection.Down:
                    edge.z = 1.0f - edge.z; //invert due to flip of direction
                    verticalEdges[new Vector2Int(source.x, source.y - 1)] = edge;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>
        /// Try and get the edge for the given source and direction
        /// </summary>
        /// <param name="source"></param>
        /// <param name="direction"></param>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool TryGetEdge(Vector2Int source, EdgeDirection direction, out Vector3 edge)
        {
            /* First normalize the direction to up or right */
            if (direction == EdgeDirection.Left)
            {
                direction = EdgeDirection.Right;
                source.x--;
            }
            else if (direction == EdgeDirection.Down)
            {
                direction = EdgeDirection.Up;
                source.y--;
            }
            
            /* Attempt a fetch */
            if (direction == EdgeDirection.Right)
            {
                return horizontalEdges.TryGetValue(source, out edge);
            }
            else
            {
                /* Only other option is UP */
                return verticalEdges.TryGetValue(source, out edge);
            }
        }
        
        /// <summary>
        /// Find an edge and it's intersection point in world space.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="direction"></param>
        /// <param name="chunk"></param>
        /// <param name="intersection"></param>
        /// <returns></returns>
        public bool TryGetEdgeIntersectionPoint(Vector2Int source, EdgeDirection direction, VolumeChunk chunk, out Vector2 intersection)
        {
            /* First normalize the direction to up or right */
            if (direction == EdgeDirection.Left)
            {
                direction = EdgeDirection.Right;
                source.x--;
            }
            else if (direction == EdgeDirection.Down)
            {
                direction = EdgeDirection.Up;
                source.y--;
            }
            
            /* Now get the edge */
            if (TryGetEdge(source, direction, out var edge))
            {
                if (direction == EdgeDirection.Right)
                {
                    intersection = Vector2.Lerp(chunk.VoxelToWorld(source.x, source.y), chunk.VoxelToWorld(source.x+1, source.y), edge.z);
                    return true;
                }
                if (direction == EdgeDirection.Up)
                {
                    intersection = Vector2.Lerp(chunk.VoxelToWorld(source.x, source.y), chunk.VoxelToWorld(source.x, source.y+1), edge.z);
                    return true;
                }

                intersection = Vector2.zero;
                return false;
            }
            else
            {
                intersection = Vector2.zero;
                return false;
            }
        }
    }
}