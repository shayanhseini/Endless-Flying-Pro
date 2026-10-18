using System;
using UnityEngine;

namespace Game.Enemies.Fighter
{
    [Serializable]
    public class FlightWaypoint
    {
        public Transform Point;

        [Tooltip("Checked (default): the jet turns to face and fly straight at this point - use for points that are genuinely ahead of the jet's current heading.\n\nUnchecked: the jet keeps flying along whatever heading it already has and completely ignores this point's direction - no turning at all. Use this for a point placed directly to the left/right of the jet (a lane marker / side dodge) so it never spins to face it.")]
        public bool RotateTowardTarget = true;

        [Min(0f)]
        public float WaitTime = 1f;
    }
}