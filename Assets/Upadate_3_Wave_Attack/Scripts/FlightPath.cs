using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies.Fighter
{
    /// <summary>
    /// One complete flight path option: where the jet starts, the waypoints
    /// it flies through, and where it exits. A FighterJetController can hold
    /// several of these and pick one when activated.
    /// </summary>
    [Serializable]
    public class FlightPath
    {
        [Tooltip("Just a label to keep things readable in the Inspector, e.g. 'Left Approach'.")]
        public string PathName = "Path";

        [Tooltip("Where the jet is placed when this path is chosen. Should be outside camera view.")]
        public Transform StartPoint;

        [Tooltip("Ordered points the jet flies between, each with its own wait time.")]
        public List<FlightWaypoint> Waypoints = new List<FlightWaypoint>();

        [Tooltip("Final point the jet flies to before being destroyed. Should be outside camera view.")]
        public Transform ExitPoint;
    }
}