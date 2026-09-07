using System.Collections;
using UnityEngine;

namespace Game.Enemies.Fighter
{
    public class FighterJetManager : MonoBehaviour
    {
        [Header("Jet Paths")]
        [SerializeField] private JetPathSpawner[] _pathSpawners;

        [Header("Spawn Settings")]
        [SerializeField] private float _spawnInterval = 5f;
        [SerializeField] private float _activeDuration = 15f;

        private Coroutine _spawnRoutine;

        private void OnEnable()
        {
            _spawnRoutine = StartCoroutine(SpawnRoutine());
        }

        private void OnDisable()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_spawnInterval);

                SpawnRandomPath();
            }
        }

        private void SpawnRandomPath()
        {
            if (_pathSpawners == null || _pathSpawners.Length == 0)
                return;

            JetPathSpawner spawner = GetRandomInactiveSpawner();

            if (spawner == null)
                return;

            spawner.gameObject.SetActive(true);

            StartCoroutine(DisableAfterDelay(spawner));
        }

        private JetPathSpawner GetRandomInactiveSpawner()
        {
            int availableCount = 0;

            foreach (JetPathSpawner spawner in _pathSpawners)
            {
                if (spawner != null && !spawner.gameObject.activeSelf)
                    availableCount++;
            }

            if (availableCount == 0)
                return null;

            int randomIndex = Random.Range(0, availableCount);

            foreach (JetPathSpawner spawner in _pathSpawners)
            {
                if (spawner == null || spawner.gameObject.activeSelf)
                    continue;

                if (randomIndex == 0)
                    return spawner;

                randomIndex--;
            }

            return null;
        }

        private IEnumerator DisableAfterDelay(JetPathSpawner spawner)
        {
            yield return new WaitForSeconds(_activeDuration);

            if (spawner != null)
            {
                spawner.ResetSpawner();
                spawner.gameObject.SetActive(false);
            }
        }
    }
}