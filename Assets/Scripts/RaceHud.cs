using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shows the player's race data in the PanelRenderer UI on the same GameObject.
/// Read-only: it never changes the race. Looks up labels by the names used in UID_Race.uxml.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class RaceHud : MonoBehaviour
{
    [Tooltip("Found automatically if left empty.")]
    [SerializeField] RaceManager raceManager;

    const string CountdownStartClass = "countDownStart"; // see UID_Style.uss
    [Tooltip("How long 'START!' and 'FINISH!!' stay on screen before the big centre label disappears.")]
    [SerializeField] float startMessageSeconds = 1f;

    PanelRenderer panelRenderer;
    Label timeLabel;
    Label lapLabel;
    Label countdownLabel;
    Coroutine countdownAnimation;
    RacerState player;
    int uiVersion = -1;

    // Last shown values, so label text is only rewritten when it actually changes.
    int shownCentiseconds = -1;
    int shownLap = -1;
    int shownTotalLaps = -1;

    void OnEnable()
    {
        panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);

        if (raceManager == null) raceManager = FindFirstObjectByType<RaceManager>();
        if (raceManager == null)
        {
            Debug.LogWarning("RaceHud: no RaceManager in the scene.", this);
            return;
        }

        raceManager.RacerRegistered += OnRacerRegistered;
        raceManager.CountdownChanged += OnCountdownChanged;
        raceManager.RacerFinished += OnRacerFinished;
        foreach (RacerState state in raceManager.Racers) OnRacerRegistered(state); // player may already be registered
    }

    void OnDisable()
    {
        if (panelRenderer != null) panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        if (raceManager != null)
        {
            raceManager.RacerRegistered -= OnRacerRegistered;
            raceManager.CountdownChanged -= OnCountdownChanged;
            raceManager.RacerFinished -= OnRacerFinished;
        }
        player = null;
        countdownAnimation = null; // coroutines stop with the disabled component
    }

    // Called when the UI is (re)built, including when the UXML is edited during play.
    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        if (version == uiVersion) return;
        uiVersion = version;

        timeLabel = root.Q<Label>("time");
        lapLabel = root.Q<Label>("lap");
        countdownLabel = root.Q<Label>("countdown");

        // Fresh labels: force the next Update to write their text.
        shownCentiseconds = -1;
        shownLap = -1;
        shownTotalLaps = -1;

        // Hidden unless a countdown is running; catches up if the UI was built after it began.
        if (countdownLabel != null) countdownLabel.style.display = DisplayStyle.None;
        if (raceManager != null && raceManager.State == RaceState.Countdown && raceManager.CountdownValue > 0)
            OnCountdownChanged(raceManager.CountdownValue);
    }

    void OnCountdownChanged(int value) => ShowMessage(value > 0 ? value.ToString() : "START!", hideAfterwards: value == 0);

    void OnRacerFinished(RacerState state)
    {
        if (state.Racer.IsPlayer) ShowMessage("FINISH!!", hideAfterwards: true);
    }

    // The big centre label doubles as the countdown and the finish message.
    void ShowMessage(string text, bool hideAfterwards)
    {
        if (countdownLabel == null) return;

        countdownLabel.text = text;
        countdownLabel.style.display = DisplayStyle.Flex;

        if (countdownAnimation != null) StopCoroutine(countdownAnimation);
        countdownAnimation = StartCoroutine(AnimateCountdown(hideAfterwards));
    }

    // Start class on for one frame, then off: the label jumps to the start look and the
    // USS transition animates it back to its resting look.
    IEnumerator AnimateCountdown(bool hideAfterwards)
    {
        countdownLabel.AddToClassList(CountdownStartClass);
        yield return null;
        countdownLabel.RemoveFromClassList(CountdownStartClass);

        if (hideAfterwards)
        {
            yield return new WaitForSeconds(startMessageSeconds);
            countdownLabel.style.display = DisplayStyle.None;
        }
    }

    void OnRacerRegistered(RacerState state)
    {
        if (state.Racer.IsPlayer) player = state;
    }

    void Update()
    {
        if (player == null) return;

        // Shows the running lap time; after finishing it holds the final lap's time.
        float lapTime = player.Finished && player.LapTimes.Count > 0
            ? player.LapTimes[player.LapTimes.Count - 1]
            : raceManager.CurrentLapTime(player);
        UpdateTime(lapTime);
        UpdateLap(player.CurrentLap, raceManager.TotalLaps);
    }

    void UpdateTime(float seconds)
    {
        int centiseconds = Mathf.FloorToInt(seconds * 100f);
        if (centiseconds == shownCentiseconds || timeLabel == null) return;

        shownCentiseconds = centiseconds;
        int minutes = centiseconds / 6000;
        int wholeSeconds = centiseconds / 100 % 60;
        timeLabel.text = $"{minutes:00}:{wholeSeconds:00}:{centiseconds % 100:00}";
    }

    void UpdateLap(int lap, int totalLaps)
    {
        if ((lap == shownLap && totalLaps == shownTotalLaps) || lapLabel == null) return;

        shownLap = lap;
        shownTotalLaps = totalLaps;
        lapLabel.text = $"LAP {lap}/{totalLaps}";
    }
}
