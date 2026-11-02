using System;

namespace SaucerSiege.Core
{
    public enum Screen
    {
        Title,
        LevelSelect,
        Playing,
        Paused,
        Result,
    }

    /// <summary>
    /// The game's screens around a battle: title, level select, play, pause and results, the
    /// game speed, and the short beat between the end of a battle and its result screen. Any
    /// screen change cancels that beat, so an old result can never pop up over a new battle.
    /// </summary>
    public sealed class Session
    {
        /// <summary>Real seconds between a battle ending and its result screen.</summary>
        public const float ResultDelay = 1.6f;
        static readonly int[] Speeds = { 1, 2, 3 };

        float resultTimer = -1f;
        int speedIndex;
        LevelDefinition trial;

        public Session(IRecordStore store)
        {
            Progress = new Progress(store);
        }

        public Progress Progress { get; }
        public Screen Screen { get; private set; } = Screen.Title;
        public Battle Battle { get; private set; }
        public int LevelIndex { get; private set; }
        public BattleResult LastResult { get; private set; }

        /// <summary>How many battle seconds pass per real second: 1, 2 or 3.</summary>
        public int Speed => Speeds[speedIndex];

        /// <summary>True between the end of a battle and its result screen.</summary>
        public bool ResultPending => resultTimer >= 0f;

        public void ToTitle()
        {
            Battle = null;
            resultTimer = -1f;
            Screen = Screen.Title;
        }

        public void ToLevelSelect()
        {
            Battle = null;
            resultTimer = -1f;
            Screen = Screen.LevelSelect;
        }

        /// <summary>Starts a level if it is unlocked; the speed goes back to 1x.</summary>
        public bool StartLevel(int index)
        {
            if (index < 0 || index >= Levels.All.Count || !Progress.IsUnlocked(index))
            {
                return false;
            }
            LevelIndex = index;
            trial = null;
            Begin(Levels.All[index]);
            return true;
        }

        /// <summary>
        /// Plays a level outside the campaign (the PlayMode tests and the screenshot hook use it):
        /// nothing is recorded and no level is unlocked.
        /// </summary>
        public void StartTrial(LevelDefinition level)
        {
            trial = level ?? throw new ArgumentNullException(nameof(level));
            LevelIndex = -1;
            Begin(level);
        }

        public bool IsTrial => trial != null;

        public bool Restart()
        {
            if (trial != null)
            {
                StartTrial(trial);
                return true;
            }
            return StartLevel(LevelIndex);
        }

        void Begin(LevelDefinition level)
        {
            Battle = new Battle(level);
            resultTimer = -1f;
            speedIndex = 0;
            Screen = Screen.Playing;
        }

        /// <summary>The next level after this one, if it exists.</summary>
        public bool HasNextLevel => trial == null && LevelIndex + 1 < Levels.All.Count;

        public bool StartNextLevel() => HasNextLevel && StartLevel(LevelIndex + 1);

        public void TogglePause()
        {
            if (Screen == Screen.Playing)
            {
                Screen = Screen.Paused;
            }
            else if (Screen == Screen.Paused)
            {
                Screen = Screen.Playing;
            }
        }

        public void CycleSpeed()
        {
            speedIndex = (speedIndex + 1) % Speeds.Length;
        }

        /// <summary>
        /// Advances the battle by <paramref name="realSeconds"/> times the speed. When the battle
        /// ends, its record is saved at once and the result screen follows after <see cref="ResultDelay"/>.
        /// </summary>
        public void Tick(float realSeconds)
        {
            if (Screen != Screen.Playing || Battle == null)
            {
                return;
            }
            if (!Battle.IsOver)
            {
                Battle.Advance(Math.Min(realSeconds, 0.1f) * Speed);
                if (Battle.IsOver)
                {
                    LastResult = trial != null
                        ? new BattleResult(-1, Battle.State == BattleState.Won, Battle.Score, Battle.Stars, Battle.WavesStarted, false)
                        : Progress.Submit(LevelIndex, Battle);
                    resultTimer = ResultDelay;
                }
                return;
            }
            if (resultTimer >= 0f)
            {
                resultTimer -= realSeconds;
                if (resultTimer < 0f)
                {
                    resultTimer = -1f;
                    Screen = Screen.Result;
                }
            }
        }
    }
}
