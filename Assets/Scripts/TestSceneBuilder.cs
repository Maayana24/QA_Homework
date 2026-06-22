using UnityEngine;

/// <summary>
/// Static helper that creates a fully wired Player GameObject for use in PlayMode tests.
/// Keeps test setup DRY — one call gives you a ready-to-use player.
///
/// Usage:
///   var player = TestSceneBuilder.CreatePlayer();
///   var player = TestSceneBuilder.CreatePlayer(resistance: 50, armor: 10);
///   TestSceneBuilder.DestroyPlayer(player);
///
/// IMPORTANT: The stat block is injected BEFORE PlayerHealth/PlayerStamina Awake runs,
/// so the systems are initialized with the exact stats you pass in.
/// </summary>
public static class TestSceneBuilder
{
    /// <summary>
    /// All components wired on a single GameObject, mirroring the real scene hierarchy.
    /// </summary>
    public struct PlayerComponents
    {
        public GameObject   GameObject;
        public PlayerStats  Stats;
        public PlayerHealth Health;
        public PlayerStamina Stamina;
        public DeathHandler  Death;
    }

    /// <summary>
    /// Create a Player with fully configurable stats.
    /// All parameters are optional — defaults give a basic player with no resistance or armor.
    /// </summary>
    public static PlayerComponents CreatePlayer(
        int vitality     = 5,
        int endurance    = 5,
        int resistance   = 0,
        int armor        = 0,
        int strength     = 5,
        int agility      = 5,
        int regeneration = 0,
        int willpower    = 5)
    {
        var go = new GameObject("Player_Test");

        // 1. Add PlayerStats first, then immediately inject the stat block
        //    so it's ready before PlayerHealth/PlayerStamina Awake runs.
        var stats = go.AddComponent<PlayerStats>();
        stats.Initialize(new RPGStatBlock(
            vitality, endurance, resistance, armor,
            strength, agility, regeneration, willpower));

        // 2. These read from PlayerStats in their own Awake
        var health  = go.AddComponent<PlayerHealth>();
        var stamina = go.AddComponent<PlayerStamina>();
        var death   = go.AddComponent<DeathHandler>();

        return new PlayerComponents
        {
            GameObject = go,
            Stats      = stats,
            Health     = health,
            Stamina    = stamina,
            Death      = death
        };
    }

    /// <summary>Destroy the player GameObject. Call in [TearDown] or [UnityTearDown].</summary>
    public static void DestroyPlayer(PlayerComponents player)
    {
        if (player.GameObject != null)
            Object.Destroy(player.GameObject);
    }

    /// <summary>Create a player that is guaranteed to survive a single large hit.</summary>
    public static PlayerComponents CreateTankyPlayer() =>
        CreatePlayer(vitality: 20, resistance: 50, armor: 20);

    /// <summary>Create a player with max regen and high stamina pool.</summary>
    public static PlayerComponents CreateEndurancePlayer() =>
        CreatePlayer(endurance: 20, agility: 20, willpower: 15);

    /// <summary>Create a player with zero defensive stats — dies easily, for edge-case tests.</summary>
    public static PlayerComponents CreateGlassCannonPlayer() =>
        CreatePlayer(vitality: 0, resistance: 0, armor: 0, strength: 20);
}
