using System;
using UnityEngine;

namespace TrafficTown2D.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Quiz,
        LevelComplete,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameState startingState = GameState.MainMenu;

        public GameState CurrentState { get; private set; }

        public event Action<GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetState(startingState);
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;
            StateChanged?.Invoke(CurrentState);
        }

        public void TogglePause()
        {
            SetState(CurrentState == GameState.Paused ? GameState.Playing : GameState.Paused);
        }
    }
}