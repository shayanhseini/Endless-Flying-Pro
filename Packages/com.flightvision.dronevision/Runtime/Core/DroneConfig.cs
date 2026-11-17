// DroneVision.Core — pure C# (no UnityEngine), C# 7.3, netstandard2.x.
// 1:1 port of the PC app's DroneConfig (Mashk/mashk/drone.py + churn.py Config, the
// fields the drone and the crowd tracker read). Both are checked against
// golden/drone_vectors.json. Change them together.
using System;

namespace DroneVision.Core
{
    [Serializable]
    public class DroneConfig
    {
        // roll (left / right): wrist height difference, in the player's own shoulder widths.
        // Arms open, a 0.5 difference is about a 10-15 degree tilt.
        public double rollEnter = 0.5;
        /// <summary>Difference that maps to Roll = ±1 (analog output).</summary>
        public double rollFull = 1.5;
        // pitch (up / down): both wrists above / below the shoulder line by this much
        public double upMargin = 0.25;
        public double downMargin = 0.3;
        /// <summary>Mean wrist height that maps to Pitch = ±1 (analog output).</summary>
        public double pitchFull = 1.0;
        /// <summary>A zone is left only below enter × this (no flicker).</summary>
        public double releaseRatio = 0.7;
        /// <summary>A zone change must hold this many frames (exhibition light jitter).</summary>
        public int zoneFrames = 2;
        // wings: both wrists above this fraction of the shoulder → hip distance (hanging arms ≈ 1.12-1.19)
        public double holdLine = 0.9;
        public double handVisibility = 0.35;
        public double handGraceMs = 250;
        public double edgeMargin = 0.01;
        // smoothing + latency
        public double minCutoff = 1.5;
        public double beta = 1.5;
        public double dCutoff = 1.0;
        public double leadMs = 30;
        public double maxLead = 0.15;
        public double widthTauS = 1.0;
        // body quality gates
        public double minVisibility = 0.5;
        public double hipVisibility = 0.5;
        public double minFacing = 0.35;
        public double minScale = 0.06;
        // resting tilt (people and cameras are not level), learned after the lock
        public double restAdaptTauS = 3.0;
        public double restRollLimit = 0.35;
        /// <summary>Wings held this long → locked (green frame). No "hold still" step.</summary>
        public double lockMs = 450;
        // presence
        public double graceMs = 250;
        /// <summary>A LOCKED player may be hidden this long without losing the lock.</summary>
        public double lockGraceMs = 1000;
        /// <summary>Gone this long = a new visitor; until then a returning player is recognised.</summary>
        public double resetMs = 2500;
        public double filterResetGapMs = 200;
        public bool invert = false;
        // crowd: who becomes the player, and keeping that same person (OperatorTracker)
        public double stageZone = 0.8;
        public double minOperatorScale = 0.04;
        public double jumpBase = 0.8;
        public double jumpPerSec = 3.0;
        public double scaleMin = 0.6;
        public double scaleMax = 1.65;
        public double colorWeight = 3;
        public double colorVeto = 0.18;
        public double reacquireMs = 1000;

        public DroneConfig Clone()
        {
            return (DroneConfig)MemberwiseClone();
        }
    }
}
