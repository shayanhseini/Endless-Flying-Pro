using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldChunk[] chunks;

    [SerializeField] private GameObject startChunk;

    [SerializeField] private Transform player;
    [SerializeField] private WorldMovementManager worldMovement;
    [SerializeField] private DifficultyManager difficultyManager;


    public int InitialChunkCount => chunks.Length;


    [Header("Recycle")]
    [SerializeField] private float recycleOffset = 50f;


    private bool startChunkActive;


    private void Awake()
    {
        // Start Chunk فقط زمین خالی است
        if (startChunk != null)
        {
            startChunkActive = true;
        }


        // Main Chunks
        SortChunks();
        AlignChunks();


        foreach (WorldChunk chunk in chunks)
        {
            chunk.ResetContent(
                difficultyManager
            );
        }
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
            chunk.Move(movement);
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


        startChunk.transform.position += movement;


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
        // طول زمین استارت را از Scale/اندازه‌اش می‌توانی
        // به صورت دستی تعیین کنی.
        return 20f;
    }


    private void CheckRecycle()
    {
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


        chunk.ResetContent(
            difficultyManager
        );


        ShiftChunks(chunk);
    }


    private void ShiftChunks(
        WorldChunk recycledChunk)
    {
        for (int i = 0;
             i < chunks.Length - 1;
             i++)
        {
            chunks[i] =
                chunks[i + 1];
        }


        chunks[^1] =
            recycledChunk;
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


            current.MoveTo(position);
        }
    }
}