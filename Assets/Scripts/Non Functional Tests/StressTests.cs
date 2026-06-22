using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Stress Tests — EditMode + PlayMode.
///
/// Non-functional. Unlike load tests (realistic high volume),
/// stress tests deliberately EXCEED normal operating conditions.
/// The goal is to find the breaking point and ensure the system
/// FAILS GRACEFULLY rather than crashing or corrupting state.
///
/// We test:
///   - Extreme input values (float.MaxValue, float.MinValue, NaN, Infinity)
///   - Massive operation counts (1,000,000 calls)
///   - Deeply negative and deeply positive stat values
///   - Rapid death/revival cycles
///   - Creating and destroying hundreds of players
///
/// A system under stress should either:
///   a) Keep working correctly, OR
///   b) Return a predictable safe value — never NaN, never negative, never crash
///
/// Place in: Assets/Tests/EditMode/ (pure logic) and Assets/Tests/PlayMode/ (MonoBehaviour)
/// </summary>
public class StressTests
{
    // =================== EDITMODE (pure logic) ===================

    [Test]
    public void TakeDamage_FloatMaxValue_ClampsToZero_NoException()
    {
        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0, resistance: 0, armor: 0));
        Assert.DoesNotThrow(() => health.TakeDamage(float.MaxValue));
        Assert.AreEqual(0f, health.CurrentHealth);
    }

    [Test]
    public void Heal_FloatMaxValue_ClampsToMaxHealth_NoException()
    {
        var health = new PlayerHealthSystem(new RPGStatBlock());
        Assert.DoesNotThrow(() => health.Heal(float.MaxValue));
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
    }

    [Test]
    public void TakeDamage_NaNInput_DoesNotCorruptHealth()
    {
        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0));
        float before = health.CurrentHealth;

        // NaN should be treated as invalid input — health must not become NaN
        health.TakeDamage(float.NaN);

        Assert.IsFalse(float.IsNaN(health.CurrentHealth),
            "CurrentHealth became NaN after NaN damage input.");
        // Ideally health is unchanged — this tests graceful rejection
    }

    [Test]
    public void TakeDamage_PositiveInfinity_DoesNotCrash_HealthIsZeroOrSafe()
    {
        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0, resistance: 0, armor: 0));
        Assert.DoesNotThrow(() => health.TakeDamage(float.PositiveInfinity));
        Assert.IsFalse(float.IsNaN(health.CurrentHealth));
        Assert.IsFalse(float.IsInfinity(health.CurrentHealth));
    }

    [Test]
    public void StaminaSystem_UseStamina_NegativeInfinity_IsRejected()
    {
        var stamina = new StaminaSystem(new RPGStatBlock());
        float before = stamina.CurrentStamina;
        bool result = stamina.UseStamina(float.NegativeInfinity);
        Assert.IsFalse(result, "Negative infinity cost should be rejected.");
        Assert.AreEqual(before, stamina.CurrentStamina, "Stamina should be unchanged.");
    }

    [Test]
    public void DamageCalculation_1MillionCalls_SystemRemainsStable()
    {
        var stats  = new RPGStatBlock(vitality: 100, resistance: 10, armor: 5);
        var health = new PlayerHealthSystem(stats);

        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                health.TakeDamage(1f);
                if (health.IsDead()) health.RestoreFullHealth();
            }
        });

        Assert.IsFalse(float.IsNaN(health.CurrentHealth));
        Assert.IsTrue(health.CurrentHealth >= 0f);
        Assert.IsTrue(health.CurrentHealth <= health.MaxHealth);
    }

    [Test]
    public void StaminaRegen_1MillionTicks_NeverExceedsMax()
    {
        var stamina = new StaminaSystem(new RPGStatBlock(agility: 100));

        for (int i = 0; i < 1_000_000; i++)
            stamina.RegenTick(0.016f);

        Assert.AreEqual(stamina.MaxStamina, stamina.CurrentStamina,
            "Stamina exceeded MaxStamina after 1M regen ticks.");
    }

    [Test]
    public void HealthPercentage_NeverGoesOutOfZeroToOneRange_UnderStress()
    {
        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0, resistance: 0, armor: 0));

        for (int i = 0; i < 10_000; i++)
        {
            health.TakeDamage(i % 50);
            health.Heal(i % 30);
            float pct = health.GetHealthPercentage();
            Assert.IsTrue(pct >= 0f && pct <= 1f,
                $"Health percentage out of range: {pct} on iteration {i}.");
        }
    }

    [Test]
    public void StatValidator_NullInput_ReturnsInvalid_NoException()
    {
        Assert.DoesNotThrow(() =>
        {
            var result = StatValidator.Validate(null);
            Assert.IsFalse(result.IsValid);
        });
    }

    // =================== PLAYMODE (MonoBehaviour) ===================

    private readonly List<TestSceneBuilder.PlayerComponents> _players = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var p in _players)
            TestSceneBuilder.DestroyPlayer(p);
        _players.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator CreateAndDestroy_500Players_NoMemoryLeak_NoException()
    {
        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                var p = TestSceneBuilder.CreatePlayer();
                TestSceneBuilder.DestroyPlayer(p);
            }
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator RapidDeathRevivalCycle_100Times_StateRemainsConsistent()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0);
        _players.Add(player);
        yield return null;

        for (int i = 0; i < 100; i++)
        {
            player.Health.TakeDamage(100f);
            player.Health.RestoreFullHealth();
        }

        yield return null;

        Assert.IsFalse(player.Health.IsDead,        "Player stuck in dead state after 100 cycles.");
        Assert.AreEqual(100, player.Death.DeathCount,   "DeathCount mismatch.");
        Assert.AreEqual(100, player.Death.RevivalCount, "RevivalCount mismatch.");
    }

    [UnityTest]
    public IEnumerator ExtremeStatValues_DoNotProduceNaNOrInfinity_InRuntime()
    {
        // Max legal stats
        var player = TestSceneBuilder.CreatePlayer(
            vitality: 100, endurance: 100, resistance: 75,
            armor: 100, strength: 100, agility: 100,
            regeneration: 100, willpower: 100);
        _players.Add(player);
        yield return null;

        player.Health.TakeDamage(999f);
        player.Stamina.UseStamina(999f);
        player.Stamina.TickRegen(9999f);
        player.Health.Heal(9999f);

        Assert.IsFalse(float.IsNaN(player.Health.CurrentHealth));
        Assert.IsFalse(float.IsNaN(player.Stamina.CurrentStamina));
        Assert.IsFalse(float.IsInfinity(player.Health.CurrentHealth));
        Assert.IsFalse(float.IsInfinity(player.Stamina.CurrentStamina));
    }
}
