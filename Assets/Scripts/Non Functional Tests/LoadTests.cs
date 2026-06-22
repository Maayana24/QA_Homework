using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Load Tests — PlayMode.
///
/// Non-functional. Tests the system under HIGH but REALISTIC load.
/// Not trying to break it — testing it at the upper bound of expected usage.
///
/// Scenarios:
///   - 100 players all taking damage simultaneously (large multiplayer battle)
///   - Rapid damage events on a single player (machine-gun fire)
///   - Many event subscribers listening to one player
///   - Many stamina drains happening in the same frame
///
/// Load budget:
///   - 100 players, 1 hit each     : under 100ms total
///   - 1 player, 500 hits          : under 100ms total
///   - 50 event subscribers        : no missed events
///
/// Place in: Assets/Tests/PlayMode/
/// </summary>
public class LoadTests
{
    private readonly List<TestSceneBuilder.PlayerComponents> _players = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var p in _players)
            TestSceneBuilder.DestroyPlayer(p);
        _players.Clear();
        yield return null;
    }

    // ---- Many players simultaneously ----

    [UnityTest]
    public IEnumerator HundredPlayers_AllTakeDamage_WithinTimeBudget()
    {
        for (int i = 0; i < 100; i++)
            _players.Add(TestSceneBuilder.CreatePlayer(resistance: 20, armor: 5));

        yield return null; // let all Awakes run

        var sw = Stopwatch.StartNew();
        foreach (var p in _players)
            p.Health.TakeDamage(50f);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, 100,
            $"100 players taking damage took {sw.ElapsedMilliseconds}ms.");

        // All should still be alive (100 base HP, 50 damage with 20 resist + 5 armor = 35)
        foreach (var p in _players)
            Assert.IsFalse(p.Health.IsDead, "Player died unexpectedly under load.");
    }

    [UnityTest]
    public IEnumerator HundredPlayers_AllDie_DeathHandlerFires_ForEach()
    {
        int totalDeaths = 0;

        for (int i = 0; i < 100; i++)
        {
            var p = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0);
            p.Health.OnDied += () => totalDeaths++;
            _players.Add(p);
        }

        yield return null;

        foreach (var p in _players)
            p.Health.TakeDamage(100f);

        yield return null;

        Assert.AreEqual(100, totalDeaths,
            $"Expected 100 death events, got {totalDeaths}.");
    }

    // ---- Rapid fire damage on single player ----

    [UnityTest]
    public IEnumerator SinglePlayer_500RapidHits_AllProcessed_WithinBudget()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 100, resistance: 0, armor: 0);
        _players.Add(player);
        yield return null;

        int healthChangedCount = 0;
        player.Health.OnHealthChanged += _ => healthChangedCount++;

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 500; i++)
        {
            if (player.Health.IsDead)
                player.Health.RestoreFullHealth();
            player.Health.TakeDamage(1f);
        }
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, 100,
            $"500 rapid hits took {sw.ElapsedMilliseconds}ms.");
        Assert.AreEqual(500, healthChangedCount,
            "Not all hits fired OnHealthChanged.");
    }

    // ---- Many event subscribers ----

    [UnityTest]
    public IEnumerator FiftySubscribers_AllReceiveOnHealthChanged()
    {
        var player = TestSceneBuilder.CreatePlayer();
        _players.Add(player);
        yield return null;

        int receivedCount = 0;
        for (int i = 0; i < 50; i++)
            player.Health.OnHealthChanged += _ => receivedCount++;

        player.Health.TakeDamage(10f);
        yield return null;

        Assert.AreEqual(50, receivedCount,
            $"Expected 50 subscribers to receive event, only {receivedCount} did.");
    }

    // ---- Stamina drain under load ----

    [UnityTest]
    public IEnumerator SinglePlayer_200StaminaDrains_InOneFrame_AllProcessed()
    {
        var player = TestSceneBuilder.CreatePlayer(endurance: 100, willpower: 0);
        _players.Add(player);
        yield return null;

        int successCount = 0;
        int failCount    = 0;

        for (int i = 0; i < 200; i++)
        {
            bool success = player.Stamina.UseStamina(1f);
            if (success) successCount++;
            else         failCount++;
        }

        // MaxStamina = 50 + 100*5 = 550, cost 1 each = expect 200 successes
        Assert.AreEqual(200, successCount,
            $"Expected 200 successful stamina uses, got {successCount}. Failures: {failCount}.");
    }

    // ---- Mixed operations load ----

    [UnityTest]
    public IEnumerator FiftyPlayers_MixedDamageHealStamina_SystemRemainsStable()
    {
        for (int i = 0; i < 50; i++)
            _players.Add(TestSceneBuilder.CreatePlayer(vitality: 10, resistance: 10, armor: 5));

        yield return null;

        // Simulate a chaotic game loop frame
        foreach (var p in _players)
        {
            p.Health.TakeDamage(30f);
            p.Stamina.UseStamina(10f);
            p.Health.Heal(10f);
            p.Stamina.TickRegen(0.016f);
        }

        yield return null;

        foreach (var p in _players)
        {
            Assert.IsFalse(float.IsNaN(p.Health.CurrentHealth),  "Health is NaN.");
            Assert.IsFalse(float.IsNaN(p.Stamina.CurrentStamina), "Stamina is NaN.");
            Assert.IsTrue(p.Health.CurrentHealth >= 0f,           "Health went negative.");
            Assert.IsTrue(p.Stamina.CurrentStamina >= 0f,         "Stamina went negative.");
        }
    }
}
