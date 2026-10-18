using UnityEngine;

namespace Game.Enemies.Fighter
{
    public class JetPathSpawner : MonoBehaviour
    {
        [SerializeField] private FighterJetController _jetPrefab;
        [SerializeField] private FlightPath _path;
        [SerializeField] private bool _spawnOnEnable = true;
        [SerializeField] private bool _singleUse = false;

        private bool _hasSpawned;
        private FighterJetController _currentJet;

        private void OnEnable()
        {
            if (_spawnOnEnable)
            {
                _hasSpawned = false;
                Enable();
            }
        }

        private void OnDisable()
        {
            if (_currentJet != null)
            {
                _currentJet.gameObject.SetActive(false);
            }
        }

        public void Enable()
        {
            if (_singleUse && _hasSpawned)
                return;

            if (_currentJet != null)
                return;

            if (_jetPrefab == null)
            {
                Debug.LogError(
                    $"{name}: Fighter Jet Prefab is not assigned.",
                    this
                );
                return;
            }

            if (_path == null || _path.StartPoint == null)
            {
                Debug.LogError(
                    $"{name}: Flight Path or Start Point is not assigned.",
                    this
                );
                return;
            }

            FighterJetController jet = Instantiate(
                _jetPrefab,
                _path.StartPoint.position,
                _path.StartPoint.rotation
            );

            _currentJet = jet;
            _hasSpawned = true;

            jet.OnDespawned += OnJetDespawned;

            if (!jet.Fly(_path))
            {
                Debug.LogError(
                    $"{name}: Failed to start fighter flight.",
                    this
                );

                jet.OnDespawned -= OnJetDespawned;

                _currentJet = null;

                Destroy(jet.gameObject);
            }
        }

        private void OnJetDespawned(
            FighterJetController jet
        )
        {
            if (_currentJet == jet)
            {
                jet.OnDespawned -= OnJetDespawned;
                _currentJet = null;
            }
        }

        public void ResetSpawner()
        {
            _hasSpawned = false;
            _currentJet = null;
        }
    }
}