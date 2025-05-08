using System;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace ethanr_utils.dual_contouring
{
    /// <summary>
    /// A helper used to manage pools of mesh objects
    /// </summary>
    public class MeshPool : MonoBehaviour
    {
        public static MeshPool Instance;
        
        public IObjectPool<MeshFilter> MeshFilterPool;
        [SerializeField] private Material meshMaterial;

        private void Awake()
        {
            /* Singleton */
            if (Instance != null) Destroy(gameObject);
            Instance = this;
            
            MeshFilterPool = new ObjectPool<MeshFilter>(CreatePoolItem, OnTakeFromPool, OnReturnedToPool,
                OnDestroyPoolObject, true);
        }

        private MeshFilter CreatePoolItem()
        {
            var go = new GameObject("Pooled Mesh");
            go.transform.SetParent(transform);
            
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = meshMaterial;

            return mf;
        }

        private void OnReturnedToPool(MeshFilter meshFilter)
        {
            meshFilter.gameObject.SetActive(false);
        }

        private void OnTakeFromPool(MeshFilter meshFilter)
        {
            meshFilter.gameObject.SetActive(true);
        }

        private void OnDestroyPoolObject(MeshFilter meshFilter)
        {
            Destroy(meshFilter.gameObject);
        }
    }
}