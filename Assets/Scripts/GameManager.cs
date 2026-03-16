using UnityEngine;

public enum GameState
{
    PreRun,
    Riding,
    RiderEjected,
    DodgingFallingBike,
    RunComplete,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [SerializeField] MotorcycleController motorcycleController;

    public GameState CurrentState { get; private set; } = GameState.PreRun;

    void Start()
    {
        ResetAndStartRun();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetAndStartRun();
    }

    public void ResetAndStartRun()
    {
        SetState(GameState.PreRun);
        motorcycleController?.ResetForNewRun();
        StartRun();
    }

    public void StartRun()
    {
        if (CurrentState != GameState.PreRun)
            return;

        SetState(GameState.Riding);
    }

    public void OnRiderEjected()
    {
        if (CurrentState != GameState.Riding)
            return;

        SetState(GameState.RiderEjected);
        motorcycleController?.HandleRiderEjected();
    }

    public void OnDodgingFallingBike()
    {
        if (CurrentState != GameState.RiderEjected)
            return;

        SetState(GameState.DodgingFallingBike);
    }

    public void OnRunComplete()
    {
        if (CurrentState != GameState.Riding && CurrentState != GameState.DodgingFallingBike)
            return;

        SetState(GameState.RunComplete);
    }

    public void OnGameOver()
    {
        if (CurrentState == GameState.GameOver)
            return;

        SetState(GameState.GameOver);
    }

    void SetState(GameState nextState)
    {
        CurrentState = nextState;
    }
}
