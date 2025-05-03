using System;
using System.Collections.Generic;
using ethanr_utils.dual_contouring.computation;
using ethanr_utils.dual_contouring.csg_ops;
using ethanr_utils.dual_contouring.data;
using ethanr_utils.dual_contouring.sdf;
using UnityEngine;

namespace ethanr_utils.dual_contouring
{
    /// <summary>
    /// The manager used to handle painting to a voxel area.
    /// </summary>
    public class DualContourPaintingManager : MonoBehaviour
    {
        [SerializeField] private float VoxelsPerUnit;
        [SerializeField] private Rect Area;

        /// <summary>
        /// The singleton instance for this manager.
        /// </summary>
        public static DualContourPaintingManager Instance;

        /// <summary>
        /// The chunk containing the painting data.
        /// </summary>
        private VolumeChunk chunk;

        /// <summary>
        /// The SDFs making up this painted chunk.
        /// </summary>
        private SdfOperator operations; 

        /// <summary>
        /// The currently rendered meshes.
        /// </summary>
        private List<MeshFilter> currFilters = new List<MeshFilter>();


        private void Awake()
        {
            /* Singleton */
            if (Instance != null) Destroy(gameObject);
            else Instance = this;
            
            /* Set up the initial painting */
            chunk = new VolumeChunk(new Vector2Int(Mathf.CeilToInt(Area.size.x * VoxelsPerUnit), Mathf.CeilToInt(Area.size.y * VoxelsPerUnit)),
                Area);
            operations = new SdfObject(new RectSdf(Area.size * 1.5f))
            {
                Position = Area.center
            };
            // operations = new RectSdf(Area.size * 1.5f);
        }

        private void Update()
        {
            /* Remove any old meshes */
            while (currFilters.Count > 0)
            {
                MeshPool.Instance.MeshFilterPool.Release(currFilters[0]);
                currFilters.RemoveAt(0);
            }

            /* Apply the operations to the chunk */
            chunk.Sample(operations);
            
            /* Render new meshes */
            var output = SurfaceNets.Generate(chunk, operations, new QEFSettings(true, false, 0.1f));
            foreach (var mesh in output.meshes)
            {
                /* Gather a pooled mesh */
                var mf = MeshPool.Instance.MeshFilterPool.Get();
                mf.mesh = mesh;
                currFilters.Add(mf);
            }
        }

        /// <summary>
        /// Add a new SDF to this tree.
        /// </summary>
        /// <param name="sdf"></param>
        public void AddSdf(SdfOperator sdf)
        {
            Debug.Log("AddSdf: " + sdf.ToString());
            operations = new UnionSdf(sdf, operations);
        }
        
        public void RemoveSdf(SdfOperator sdf)
        {
            Debug.Log("RemoveSdf: " + sdf.ToString());
            operations = new DifferenceSdf(operations, sdf);
        }
    }
}