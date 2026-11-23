using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldChunk[] chunks;
    [SerializeField] private GameObject startChunk;

    [SerializeField] private Transform player;
    [SerializeField] private WorldMovementManager worldMovement;
    [SerializeField] private DifficultyManager difficultyManager;
    [SerializeField] private GameManager gameManager;
    
    public static ChunkManager Instance { get; private set; }

    public int InitialChunkCount =>
        chunks.Length;
    
    [Header("Recycle")]
    [SerializeField] private float recycleOffset = 50f;
    
    private bool startChunkActive;
    private bool finalGroundPending;
    private bool finalGroundActivated;
    
    private void Awake()
    {
        Instance = this;
        finalGroundPending = false;
        finalGroundActivated = false;

        if (startChunk != null)
        {
            startChunkActive = true;
        }
        
        SortChunks();
        AlignChunks();
        
        if (difficultyManager != null)
        {
            difficultyManager.UpdateProgress();
        }

        foreach (WorldChunk chunk in chunks)
        {
            chunk.ResetContent(
                difficultyManager,
                gameManager.CurrentGroundType
            );
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.GameTimeFinished +=
            HandleGameTimeFinished;
    }
    
    private void OnDisable()
    {
        GameEvents.GameTimeFinished -=
            HandleGameTimeFinished;
    }
    
    private void HandleGameTimeFinished()
    {
        finalGroundPending = true;
    }
    
    private void Update()
    {
        if (!worldMovement.IsMovementEnabled)
            return;
        
        MoveChunks();
        MoveStartChunk();
        CheckRecycle();
    }
    
    private void MoveChunks()
    {
        Vector3 movement =
            Vector3.back *
            worldMovement.WorldSpeed *
            Time.deltaTime;
        
        foreach (WorldChunk chunk in chunks)
        {
            if (chunk != null && chunk.gameObject.activeSelf)
            {
                chunk.Move(movement);
            }
        }
    }
    
    private void MoveStartChunk()
    {
        if (!startChunkActive ||
            startChunk == null)
            return;
        
        Vector3 movement =
            Vector3.back *
            worldMovement.WorldSpeed *
            Time.deltaTime;
        
        startChunk.transform.position +=
            movement;
        
        float startEndZ =
            startChunk.transform.position.z +
            GetStartChunkLength() * 0.5f;
        
        if (startEndZ <
            player.position.z - recycleOffset)
        {
            startChunk.SetActive(false);

            startChunkActive = false;
        }
    }
    
    private float GetStartChunkLength()
    {
        return 20f;
    }
    
    private void CheckRecycle()
    {
        if (finalGroundActivated)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                WorldChunk c = chunks[i];
                if (c == null || !c.gameObject.activeSelf)
                    continue;

                // Keep the final ground chunk active
                if (c == chunks[^1])
                    continue;

                float endZ =
                    c.transform.position.z +
                    c.Length * 0.5f;

                // Deactivate immediately once passed behind the camera
                if (endZ < player.position.z - 5f)
                {
                    c.gameObject.SetActive(false);
                }
            }
            return;
        }
        
        WorldChunk first =
            chunks[0];
        
        float firstEndZ =
            first.transform.position.z +
            first.Length * 0.5f;
        
        if (firstEndZ <
            player.position.z - recycleOffset)
        {
            RecycleChunk(first);
        }
    }
    
    private void RecycleChunk(
        WorldChunk chunk)
    {
        WorldChunk last =
            chunks[^1];
        
        Vector3 position =
            chunk.transform.position;
        
        position.z =
            last.transform.position.z +
            last.Length;
        
        chunk.MoveTo(position);
        
        float currentSpeed = worldMovement != null ? worldMovement.WorldSpeed : 10f;
        float travelTimeToPlayer = position.z / Mathf.Max(1f, currentSpeed);

        bool shouldActivateFinal = finalGroundPending;
        if (!shouldActivateFinal && gameManager != null && gameManager.State == GameState.Playing)
        {
            if (gameManager.RemainingTime <= travelTimeToPlayer)
            {
                shouldActivateFinal = true;
            }
        }

        if (shouldActivateFinal)
        {
            ActivateFinalChunk(chunk);

            finalGroundPending = false;
            finalGroundActivated = true;
        }
        else
        {
            if (difficultyManager != null)
            {
                difficultyManager.UpdateProgress();
            }

            chunk.ResetContent(
                difficultyManager,
                gameManager.CurrentGroundType
            );
        }
        ShiftChunks(chunk);
    }
    
    private void ActivateFinalChunk(WorldChunk chunk)
    {
        chunk.ActivateFinalGround();
    }
    
    private void ShiftChunks(WorldChunk recycledChunk)
    {
        for (int i = 0;
             i < chunks.Length - 1;
             i++)
        {
            chunks[i] = chunks[i + 1];
        }
        
        chunks[^1] = recycledChunk;
    }


    private void SortChunks()
    {
        System.Array.Sort(
            chunks,
            (a, b) =>
                a.transform.position.z.CompareTo(
                    b.transform.position.z
                )
        );
    }


    private void AlignChunks()
    {
        for (int i = 1;
             i < chunks.Length;
             i++)
        {
            WorldChunk previous =
                chunks[i - 1];

            WorldChunk current =
                chunks[i];
            
            float previousEndZ =
                previous.transform.position.z +
                previous.Length * 0.5f;
            
            Vector3 position =
                current.transform.position;
            
            position.z =
                previousEndZ +
                current.Length * 0.5f;
            
            current.MoveTo(
                position
            );
        }
    }
}