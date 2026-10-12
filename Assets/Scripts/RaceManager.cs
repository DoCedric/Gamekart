using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Race progress of one racer. Owned and mutated by the RaceManager only.</summary>
public class RacerState
{
    public Racer Racer { get; }
    public int NextCheckpoint { get; internal set; }  // index into the manager's checkpoint list
    public int CurrentLap { get; internal set; }      // 0 = hasn't crossed the start line yet
    public bool Finished { get; internal set; }
    public float FinishTime { get; internal set; }    // race clock when the last lap was completed
    public float LapStartTime { get; internal set; }  // race clock when the current lap began
    public List<float> LapTimes { get; } = new List<float>();

    public RacerState(Racer racer) => Racer = racer;
}

/// <summary>
/// Put on an empty at the root of the track's checkpoints. Every Checkpoint that is a
/// child of this object is used, in hierarchy order: the FIRST child is the finish/start
/// line, the rest follow in driving direction. It tracks laps and times for every
/// registered racer, so player and AI go through exactly the same rules.
/// </summary>
public class RaceManager : MonoBehaviour
{
    [SerializeField, Min(1)] int totalLaps = 3;
    [SerializeField] bool logEvents;

    public int TotalLaps => totalLaps;
    public IReadOnlyList<Checkpoint> Checkpoints => checkpoints;
    public IReadOnlyList<RacerState> Racers => racers;
    public float RaceTime => Time.time - raceStartTime;

    // Events for the UI and other systems to bind to.
    public event Action<RacerState> LapStarted;     // lap number is racer.CurrentLap
    public event Action<RacerState, float> LapCompleted; // racer, lap time
    public event Action<RacerState> LapProgressReset;    // crossed the line without every checkpoint
    public event Action<RacerState> RacerFinished;
    public event Action AllRacersFinished;

    readonly List<RacerState> racers = new List<RacerState>();
    readonly Dictionary<Racer, RacerState> stateByRacer = new Dictionary<Racer, RacerState>();
    readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
    float raceStartTime;

    void Awake()
    {
        GetComponentsInChildren(checkpoints);
        if (checkpoints.Count == 0)
            Debug.LogError("RaceManager: no Checkpoint children found. Add at least the finish line as a child.", this);

        foreach (Checkpoint checkpoint in checkpoints)
            checkpoint.Crossed += OnCheckpointCrossed;
    }

    void Start() => raceStartTime = Time.time; // becomes the countdown go-moment in a later step

    void OnDestroy()
    {
        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint != null) checkpoint.Crossed -= OnCheckpointCrossed;
        }
    }

    public RacerState Register(Racer racer)
    {
        if (stateByRacer.TryGetValue(racer, out RacerState existing)) return existing;

        var state = new RacerState(racer);
        racers.Add(state);
        stateByRacer.Add(racer, state);
        return state;
    }

    public RacerState GetState(Racer racer) => stateByRacer.TryGetValue(racer, out RacerState s) ? s : null;

    /// <summary>Time in the current lap, or 0 before the first lap starts.</summary>
    public float CurrentLapTime(RacerState state) => state.CurrentLap > 0 && !state.Finished ? RaceTime - state.LapStartTime : 0f;

    void OnCheckpointCrossed(Checkpoint checkpoint, Rigidbody body)
    {
        Racer racer = body.GetComponentInParent<Racer>();
        if (racer == null || !stateByRacer.TryGetValue(racer, out RacerState state) || state.Finished) return;

        // Driving through a gate backwards never counts.
        if (Vector3.Dot(body.linearVelocity, checkpoint.transform.forward) <= 0f) return;

        int index = checkpoints.IndexOf(checkpoint);
        bool isFinishLine = index == 0;

        if (index == state.NextCheckpoint)
        {
            if (isFinishLine) CrossFinishLine(state);
            else AdvanceCheckpoint(state);
        }
        else if (isFinishLine && state.CurrentLap > 0)
        {
            // Crossed the line without every checkpoint: the lap's progress starts over.
            state.NextCheckpoint = checkpoints.Count > 1 ? 1 : 0;
            Log($"{racer.DisplayName} missed checkpoints, lap {state.CurrentLap} progress reset");
            LapProgressReset?.Invoke(state);
        }
        // Any other out-of-order gate is ignored.
    }

    void AdvanceCheckpoint(RacerState state)
    {
        state.NextCheckpoint = (state.NextCheckpoint + 1) % checkpoints.Count;
    }

    void CrossFinishLine(RacerState state)
    {
        float now = RaceTime;

        if (state.CurrentLap > 0)
        {
            float lapTime = now - state.LapStartTime;
            state.LapTimes.Add(lapTime);
            Log($"{state.Racer.DisplayName} completed lap {state.CurrentLap}/{totalLaps} in {lapTime:F2}s");
            LapCompleted?.Invoke(state, lapTime);

            if (state.CurrentLap >= totalLaps)
            {
                state.Finished = true;
                state.FinishTime = now;
                Log($"{state.Racer.DisplayName} finished the race in {now:F2}s");
                RacerFinished?.Invoke(state);
                if (racers.TrueForAll(r => r.Finished)) AllRacersFinished?.Invoke();
                return;
            }
        }

        state.CurrentLap++;
        state.LapStartTime = now;
        state.NextCheckpoint = checkpoints.Count > 1 ? 1 : 0;
        Log($"{state.Racer.DisplayName} started lap {state.CurrentLap}/{totalLaps}");
        LapStarted?.Invoke(state);
    }

    void Log(string message)
    {
        if (logEvents) Debug.Log($"[Race {RaceTime:F2}] {message}", this);
    }
}
