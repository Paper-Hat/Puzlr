using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public enum GameType
    {
        Endless,
        Speed,
        Fidget,
    }
    
    public static SettingsManager _instance;
    
    [Header("GameType Agnostic Settings")] 
    public float GameStartDelay = 5f;
    public int XDimensions { get; set; } = 10; //10-15
    public int YDimensions { get; set; } = 8; //6-12
    public int DistinctTiles { get; set; } = 4; // 4-7
    public int DefaultRowFillCount { get; set; } = 3; //3-5
    public int MatchRequirement { get; set; } = 3; //3-5
    public float SpeedUpCooldown { get; set; }= 2f; //0-4
    public GameType GameMode = GameType.Endless;
    
    [Header("Speed Gameplay Settings")]
    [SerializeField] [Range(.1f, .5f)] public float maxSpeed = 0.5f;
    [SerializeField] [Range(1.5f, 5f)] public float startSpeed = 3f;
    [SerializeField] [Range(10f, 100f)] public float speedFactor = 30f;
    [SerializeField] [Range(10, 100)] public int matchesUntilSpeedup = 10;

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Sets game mode via int with legend:
    /// 0: Endless
    /// 1: Speed
    /// 2: Fidget
    /// </summary>
    /// <param name="gameType"></param>
    public void SetGameMode(int gameType)
    {
        switch (gameType)
        {
            case 0:
                GameMode = GameType.Endless;
                break;
            case 1:
                GameMode = GameType.Speed;
                break;
            case 2:
                GameMode = GameType.Fidget;
                break;
            default:
                Debug.LogError("Invalid game mode selected");
                break;
        }
    }
    
}
