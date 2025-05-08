using System.Collections.Generic;
using ethanr_utils.dual_contouring.data;
using ethanr_utils.dual_contouring.data.job_structs;
using ethanr_utils.dual_contouring.sdf;
using UnityEngine;
using DifferenceSdf = ethanr_utils.dual_contouring.sdf.DifferenceSdf;

namespace ethanr_utils.dual_contouring
{
    /// <summary>
    /// A basic tester for dual contouring stuff.
    /// </summary>
    public class DualContourTester : MonoBehaviour
    {
        [Header("Rendering")] 
        [SerializeField] private bool RenderSamplePoints = false;
        [SerializeField] private bool RenderSurfacePoints = false;
        [SerializeField] private bool RenderContours = false;
        [SerializeField] private bool RenderMeshes = false;

        [Header("Dual Contouring Object")]
        [SerializeField] private int size = 16;
        [SerializeField] private bool update;
        [SerializeField] private Vector2 position;
        [SerializeField] private float rotation = 4.6f;
        [SerializeField] private float scale = 1.0f;
        
        [Header("QEF Settings")]
        [SerializeField] private bool clip = false;
        [SerializeField] private bool bias = false;
        [SerializeField, Range(0, 3.0f)] private float biasAmount = 0.0f;

        private Rect area = new Rect(0.0f, 0.0f, 4.0f, 4.0f);
        private SdfObject obj;
        
        private List<MeshFilter> currFilters = new List<MeshFilter>();
        private List<List<SurfacePoint>> polygons = new List<List<SurfacePoint>>();

        private VoxelDataContainer voxelData;
        private List<Contour> contours;
        
        private void Awake()
        {
            // chunk = new VolumeChunk(new Vector2Int(size, size), area);
            
            // var largerCircle = new CircleSdf(1.5f);
            // var smallerCircle = new CircleSdf(1f);
            // var torus = new DifferenceSdf(largerCircle, smallerCircle);
            // obj = new SdfObject(torus);
            
            var largerCircle = new CircleSdf(1.5f);
            var smallerCircle = new CircleSdf(1f);
            var evenSmallerCircle = new CircleSdf(0.6f);
            var theSmallestCircle = new CircleSdf(0.3f);
            var torus = new DifferenceSdf(largerCircle, smallerCircle);
            var innerTorus = new DifferenceSdf(evenSmallerCircle, theSmallestCircle);
            obj = new SdfObject(new UnionSdf(torus, innerTorus));
            
            // var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            // var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            // var union = new UnionSdf(r1, r2);
            // obj = new SdfObject(union);

            // var rect = new RectSdf(new Vector2(3.0f, 2.0f));
            // var rectObj = new SdfObject(rect);
            // var circ = new CircleSdf(1.0f);
            // var circObj = new SdfObject(circ);
            // rectObj.Position += new Vector2(1.0f, -1.0f);
            // circObj.Position += new Vector2(-1.0f, 3.0f);
            // var finalUnion = new UnionSdf(rectObj, circObj);
            // obj = new SdfObject(finalUnion);

            // var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            // var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            // var diff = new DifferenceSdf(r1, r2);
            // obj = new SdfObject(diff);

            // var r1 = new RectSdf(new Vector2(2.0f, 1.0f));
            // var r2 = new RectSdf(new Vector2(1.0f, 2.0f));
            // var isct = new IntersectionSdf(r1, r2);
            // obj = new SdfObject(isct);

            // obj = new SdfObject(new RectSdf(new Vector2(2.0f, 1.0f)));
        }

        private void Update()
        {
            if (update)
            {
                // rotation += 15 * Time.deltaTime;
                // chunk = new VolumeChunk(new Vector2Int(size, size), area);
                obj.Position = position;
                obj.Rotation = rotation;
                obj.Scale = scale;
                // chunk.Sample(obj);
                // evaluator.EvaluateAgainst(chunk, trans);
            }
            
            voxelData = new VoxelDataContainer(Vector2Int.one * size, area);
            
            /* Remove the old meshes */
            while (currFilters.Count > 0)
            {
                MeshPool.Instance.MeshFilterPool.Release(currFilters[0]);
                currFilters.RemoveAt(0);
            }
            
            /* Render the meshes */
            var output = voxelData.SurfaceNets(obj, new QEFSettings(bias, clip, biasAmount));
            contours = output.contours;

            if (RenderMeshes)
            {
                foreach (var mesh in output.meshes)
                {
                    /* Gather a pooled mesh */
                    var mf = MeshPool.Instance.MeshFilterPool.Get();
                    mf.mesh = mesh;
                    currFilters.Add(mf);
                }
            }
            
        }

        private List<List<SurfacePoint>> edges;
        
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                if (voxelData != null)
                {
                    /* Points */
                    if (RenderSamplePoints)
                    {
                        foreach (var point in voxelData.VoxelPoints)
                        {
                            Gizmos.color = point.Value <= 0.0f ? Color.red : Color.green;
                            Gizmos.DrawSphere(point.Position, 0.02f);
                        }
                    }
                    
                    /* Surface Points */
                    if (RenderSurfacePoints)
                    {
                        Gizmos.color = Color.cyan;
                        foreach (var point in voxelData.Points)
                        {
                            Gizmos.DrawSphere(point.Position, 0.02f);
                        }
                    }
                    
                    /* Contours */
                    if (RenderContours)
                    {
                        Gizmos.color = Color.yellow;
                        foreach (var contour in contours)
                        {
                            for (int i = 0; i < contour.Data.Count - 1; i++)
                            {
                                Gizmos.DrawLine(contour.Data[i].Position, contour.Data[i + 1].Position);
                            }
                            Gizmos.DrawLine(contour.Data[^1].Position, contour.Data[0].Position);
                        }
                    }
                }
            }
        }
    }
}