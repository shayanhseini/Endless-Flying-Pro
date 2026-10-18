using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Serializable]
    private class PoolConfig
    {
        public PoolType type;
        public PooledObject prefab;
        public int amountPerChunk;
    }
    
    [Header("References")]
    [SerializeField] private ChunkManager chunkManager;


    [Header("Pools")]
    [SerializeField] private PoolConfig[] poolConfigs;


    private Dictionary<PoolType, ObjectPool> pools;


    public int InitialChunkCount =>
        chunkManager.InitialChunkCount;


    private void Awake()
    {
        InitializePools();
    }


    private void InitializePools()
    {
        int chunkCount =
            chunkManager.InitialChunkCount;


        pools = new Dictionary<PoolType, ObjectPool>();


        for (int i = 0; i < poolConfigs.Length; i++)
        {
            PoolConfig config = poolConfigs[i];

            if (config.type != PoolType.Obstacle)
                continue;


            int poolSize =
                chunkCount * config.amountPerChunk;


            pools[config.type] = new ObjectPool(
                config.prefab,
                poolSize,
                transform
            );
        }
    }


    public PooledObject Get(PoolType type)
    {
        if (!pools.TryGetValue(type, out ObjectPool pool))
        {
            Debug.LogError(
                $"Pool with type {type} was not found."
            );

            return null;
        }


        return pool.Get();
    }


    public void Release(PooledObject pooledObject)
    {
        if (pooledObject == null)
            return;


        if (!pools.TryGetValue(pooledObject.PoolType, out ObjectPool pool))
        {
            Debug.LogError(
                $"Pool with type {pooledObject.PoolType} was not found."
            );

            return;
        }


        pool.Release(pooledObject);
    }
}
