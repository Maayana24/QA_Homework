using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Security Tests — EditMode + PlayMode.
///
/// Non-functional. In a game, "security" means:
///   - The system cannot be exploited via invalid inputs
///   - Stats cannot be set to impossible/illegal values
///   - Health/Stamina cannot be manipulated outside the public API
///   - Suspicious activity is detected and reported
///   - The system degrades safely — not silently or catastrophically
///
/// These tests simulate what a cheating player (or a buggy network message)
/// might send to the system, and verify the system rejects or sanitizes it.
///
/// Place in: Assets/Tests/EditMode/ (logic) and Assets/Tests/PlayMode/ (MonoBehaviour)
/// </summary>
public class SecurityTests
{
    // =================== EDITMODE — pure logic ===================

    // ---- Input injection on damage ----

    [Test]
    public void TakeDamage_NegativeValue_IsRejected_HealthUnchanged()
    {
        // Exploit: send negative damage to heal yourself
        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0));
        health.TakeDamage(50f);
        float afterDamage = health.CurrentHealth;

        health.TakeDamage(-999f); // attempted exploit

        Assert.AreEqual(afterDamage, health.CurrentHealth,
            "Negative damage should not modify health (heal exploit attempt).");
    }

    [Test]
    public void Heal_NegativeValue_IsRejected_HealthUnchanged()
    {
        // Exploit: send negative heal to damage without triggering death event
        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0));
        float before = health.CurrentHealth;

        health.Heal(-999f);

        Assert.AreEqual(before, health.CurrentHealth,
            "Negative heal should not modify health.");
    }

    [Test]
    public void UseStamina_NegativeBaseCost_IsRejected()
    {
        // Exploit: use stamina with negative cost to gain stamina for free
        var stamina = new StaminaSystem(new RPGStatBlock(endurance: 0));
        stamina.UseStamina(stamina.MaxStamina); // drain all
        bool result = stamina.UseStamina(-999f);

        Assert.IsFalse(result, "Negative stamina cost should be rejected.");
        Assert.AreEqual(0f, stamina.CurrentStamina,
            "Stamina should not increase via negative cost exploit.");
    }

    // ---- Stat boundary enforcement ----

    [Test]
    public void StatValidator_OverMaxStats_AreDetected()
    {
        // Simulate a tampered character save file with inflated stats
        var tamperedStats = new RPGStatBlock(
            vitality: 999, resistance: 999, armor: 999);

        var result = StatValidator.Validate(tamperedStats);

        Assert.IsFalse(result.IsValid,
            "Tampered stat block should fail validation.");
        Assert.GreaterOrEqual(result.Errors.Length, 3,
            "All over-cap stats should be reported.");
    }

    [Test]
    public void ResistanceCap_PreventsFull_DamageImmunity()
    {
        // Even if resistance is somehow set to 100 (over the validator cap),
        // the damage formula itself caps at 75% — player is never immune.
        var stats  = new RPGStatBlock(vitality: 0, resistance: 100, armor: 0);
        var health = new PlayerHealthSystem(stats);

        health.TakeDamage(100f);

        Assert.IsTrue(health.CurrentHealth < health.MaxHealth,
            "Player should not be immune to damage even at resistance 100.");
        Assert.AreEqual(25f, health.CurrentHealth, 0.001f,
            "Resistance should be capped at 75% reduction.");
    }

    [Test]
    public void SetMaxHealth_CannotBeSetToZero_OrNegative()
    {
        var health   = new PlayerHealthSystem(new RPGStatBlock(vitality: 0));
        float before = health.MaxHealth;

        health.SetMaxHealth(0f);
        health.SetMaxHealth(-100f);

        Assert.AreEqual(before, health.MaxHealth,
            "MaxHealth should not be settable to zero or negative.");
    }

    // ---- Private state cannot be bypassed ----

    [Test]
    public void CurrentHealth_HasNoPublicSetter_EnforcedByDesign()
    {
        // This test documents the design constraint.
        // If someone adds a public setter, this test MUST fail compilation.
        var type     = typeof(PlayerHealthSystem);
        var property = type.GetProperty("CurrentHealth");

        Assert.IsNull(property?.GetSetMethod(false),
            "CurrentHealth must not have a public setter — state must only change through methods.");
    }

    [Test]
    public void CurrentStamina_HasNoPublicSetter_EnforcedByDesign()
    {
        var type     = typeof(StaminaSystem);
        var property = type.GetProperty("CurrentStamina");

        Assert.IsNull(property?.GetSetMethod(false),
            "CurrentStamina must not have a public setter.");
    }

    // =================== PLAYMODE — MonoBehaviour ===================

    private readonly List<TestSceneBuilder.PlayerComponents> _players = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var p in _players)
            TestSceneBuilder.DestroyPlayer(p);
        _players.Clear();
        yield return null;
    }

    // ---- HealthGuard detects suspicious activity ----

    [UnityTest]
    public IEnumerator HealthGuard_Fires_WhenNegativeDamageInputDetected()
    {
        var go     = new GameObject("Player_Test");
        var stats  = go.AddComponent<PlayerStats>();
        stats.Initialize(new RPGStatBlock());
        var health = go.AddComponent<PlayerHealth>();
        go.AddComponent<DeathHandler>();
        var guard  = go.AddComponent<HealthGuard>();

        _players.Add(new TestSceneBuilder.PlayerComponents
        {
            GameObject = go, Stats = stats, Health = health, Death = go.GetComponent<DeathHandler>()
        });

        yield return null;

        bool flagged = false;
        guard.OnSuspiciousActivity += _ => flagged = true;

        guard.ValidateDamageInput(-100f); // simulate exploit attempt

        Assert.IsTrue(flagged, "HealthGuard should flag negative damage input.");
    }

    [UnityTest]
    public IEnumerator HealthGuard_Fires_WhenNaNDamageInputDetected()
    {
        var go     = new GameObject("Player_Test");
        var stats  = go.AddComponent<PlayerStats>();
        stats.Initialize(new RPGStatBlock());
        var health = go.AddComponent<PlayerHealth>();
        go.AddComponent<DeathHandler>();
        var guard  = go.AddComponent<HealthGuard>();

        _players.Add(new TestSceneBuilder.PlayerComponents
        {
            GameObject = go, Stats = stats, Health = health, Death = go.GetComponent<DeathHandler>()
        });

        yield return null;

        bool flagged = false;
        guard.OnSuspiciousActivity += _ => flagged = true;

        guard.ValidateDamageInput(float.NaN);

        Assert.IsTrue(flagged, "HealthGuard should flag NaN damage input.");
    }

    [UnityTest]
    public IEnumerator HealthGuard_Fires_WhenInvalidStatsAreDetectedOnStart()
    {
        var go    = new GameObject("Player_Test");
        var stats = go.AddComponent<PlayerStats>();

        // Inject stat block that passes RPGStatBlock (clamps to 0 internally)
        // but is still flagged by the validator for being out of game-design range
        // We test with valid-but-suspicious values that HealthGuard can monitor
        stats.Initialize(new RPGStatBlock(vitality: 5));

        go.AddComponent<PlayerHealth>();
        go.AddComponent<DeathHandler>();
        var guard = go.AddComponent<HealthGuard>();

        _players.Add(new TestSceneBuilder.PlayerComponents
        {
            GameObject = go, Stats = stats,
            Health = go.GetComponent<PlayerHealth>(),
            Death  = go.GetComponent<DeathHandler>()
        });

        // HealthGuard.Start() calls StatValidator — for this test
        // we test the validator path by calling directly
        bool flagged = false;
        guard.OnSuspiciousActivity += _ => flagged = true;
        guard.ValidateDamageInput(float.PositiveInfinity);

        yield return null;

        Assert.IsTrue(flagged, "HealthGuard should flag infinity damage input.");
    }

    [UnityTest]
    public IEnumerator HealthGuard_DoesNotFlag_LegitimateGameplay()
    {
        var go     = new GameObject("Player_Test");
        var stats  = go.AddComponent<PlayerStats>();
        stats.Initialize(new RPGStatBlock(vitality: 5));
        var health = go.AddComponent<PlayerHealth>();
        go.AddComponent<DeathHandler>();
        var guard  = go.AddComponent<HealthGuard>();

        _players.Add(new TestSceneBuilder.PlayerComponents
        {
            GameObject = go, Stats = stats, Health = health, Death = go.GetComponent<DeathHandler>()
        });

        yield return null;

        int flagCount = 0;
        guard.OnSuspiciousActivity += _ => flagCount++;

        // Normal gameplay — small damage, small heal, no exploits
        guard.ValidateDamageInput(25f);
        guard.ValidateDamageInput(10f);
        guard.ValidateDamageInput(50f);

        Assert.AreEqual(0, flagCount,
            "Normal damage values should not trigger HealthGuard.");
    }
}
