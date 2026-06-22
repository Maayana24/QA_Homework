/*using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Compatibility Tests — PlayMode.
///
/// Non-functional. Tests that the system works correctly across
/// DIFFERENT VALID CONFIGURATIONS — not just the "normal" setup.
///
/// In game terms: does the system work for every type of character build?
/// In dev terms:  does the system work regardless of how you configure it?
///
/// We test:
///   - All-minimum stats (glass cannon with no investment)
///   - All-maximum stats (fully maxed character)
///   - Every stat at zero independently (one stat missing at a time)
///   - Component enable/disable cycles (player gets stunned, component toggled)
///   - Multiple players coexisting (their events don't bleed into each other)
///   - Player with only HP investment (no stamina stats)
///   - Player with only Stamina investment (no HP stats)
///
/// Place in: Assets/Tests/PlayMode/
/// </summary>
public class CompatibilityTests
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

    // ---- Extreme stat builds ----

    [UnityTest]
    public IEnumerator AllStatsAtMinimum_SystemFunctionsWithoutError()
    {
        var player = TestSceneBuilder.CreatePlayer(0, 0, 0, 0, 0, 0, 0, 0);
        _players.Add(player);
        yield return null;

        Assert.DoesNotThrow(() =>
        {
            player.Health.TakeDamage(50f);
            player.Health.Heal(10f);
            player.Stamina.UseStamina(10f);
            player.Stamina.TickRegen(1f);
        });

        Assert.IsFalse(float.IsNaN(player.Health.CurrentHealth));
        Assert.IsFalse(float.IsNaN(player.Stamina.CurrentStamina));
    }

    [UnityTest]
    public IEnumerator AllStatsAtMaximum_SystemFunctionsWithoutError()
    {
        var player = TestSceneBuilder.CreatePlayer(100, 100, 100, 100, 100, 100, 100, 100);
        _players.Add(player);
        yield return null;

        Assert.DoesNotThrow(() =>
        {
            player.Health.TakeDamage(9999f);
            player.Health.Heal(9999f);
            player.Stamina.UseStamina(9999f);
            player.Stamina.TickRegen(9999f);
        });

        Assert.IsTrue(player.Health.CurrentHealth >= 0f);
        Assert.IsTrue(player.Health.CurrentHealth <= player.Health.MaxHealth);
    }

    // ---- Single-stat isolation (one stat set, all others zero) ----

    [UnityTest]
    public IEnumerator OnlyVitalitySet_HPScalesCorrectly_StaminaIsBase()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 10, endurance: 0);
        _players.Add(player);
        yield return null;

        Assert.AreEqual(200f, player.Health.MaxHealth,  0.001f);
        Assert.AreEqual(50f,  player.Stamina.MaxStamina, 0.001f);
    }

    [UnityTest]
    public IEnumerator OnlyEnduranceSet_StaminaScalesCorrectly_HPIsBase()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 0, endurance: 10);
        _players.Add(player);
        yield return null;

        Assert.AreEqual(100f, player.Health.MaxHealth,   0.001f);
        Assert.AreEqual(100f, player.Stamina.MaxStamina, 0.001f);
    }

    [UnityTest]
    public IEnumerator OnlyResistanceSet_DamageIsReducedByPercent_HPBase()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 50, armor: 0);
        _players.Add(player);
        yield return null;

        player.Health.TakeDamage(100f);
        Assert.AreEqual(50f, player.Health.CurrentHealth, 0.001f);
    }

    [UnityTest]
    public IEnumerator OnlyArmorSet_FlatReductionApplied_ResistanceIsZero()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 20);
        _players.Add(player);
        yield return null;

        player.Health.TakeDamage(50f);
        Assert.AreEqual(70f, player.Health.CurrentHealth, 0.001f); // 50 - 20 armor = 30 damage
    }

    // ---- Component enable/disable cycles ----

    [UnityTest]
    public IEnumerator DeathHandler_ReSubscribes_AfterBeingDisabledAndReEnabled()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0);
        _players.Add(player);
        yield return null;

        // Simulate player being stunned (component disabled then re-enabled)
        player.Death.enabled = false;
        yield return null;
        player.Death.enabled = true;
        yield return null;

        player.Health.TakeDamage(100f);
        yield return null;

        Assert.IsTrue(player.Death.PlayerHasDied,
            "DeathHandler should have re-subscribed after re-enabling.");
    }

    [UnityTest]
    public IEnumerator PlayerHealth_StillWorks_AfterGameObjectSetActiveToggle()
    {
        var player = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0);
        _players.Add(player);
        yield return null;

        player.GameObject.SetActive(false);
        yield return null;
        player.GameObject.SetActive(true);
        yield return null;

        Assert.DoesNotThrow(() => player.Health.TakeDamage(10f));
        Assert.IsTrue(player.Health.CurrentHealth < player.Health.MaxHealth);
    }

    // ---- Multiple players — no event bleed ----

    [UnityTest]
    public IEnumerator TwoPlayers_DeathEvents_DoNotCrossContaminate()
    {
        var playerA = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0);
        var playerB = TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0);
        _players.Add(playerA);
        _players.Add(playerB);
        yield return null;

        // Only kill player A
        playerA.Health.TakeDamage(100f);
        yield return null;

        Assert.IsTrue(playerA.Death.PlayerHasDied,  "Player A should be dead.");
        Assert.IsFalse(playerB.Death.PlayerHasDied, "Player B should NOT be affected.");
    }

    [UnityTest]
    public IEnumerator TenPlayers_IndependentHealthValues_NoSharedState()
    {
        for (int i = 0; i < 10; i++)
            _players.Add(TestSceneBuilder.CreatePlayer(vitality: 0, resistance: 0, armor: 0));

        yield return null;

        // Deal different damage to each player
        for (int i = 0; i < _players.Count; i++)
            _players[i].Health.TakeDamage((i + 1) * 5f); // 5, 10, 15 ... 50

        yield return null;

        // Each player should have a unique health value
        for (int i = 0; i < _players.Count; i++)
        {
            float expected = 100f - (i + 1) * 5f;
            Assert.AreEqual(expected, _players[i].Health.CurrentHealth, 0.001f,
                $"Player {i} has wrong health — possible shared state.");
        }
    }

    // ---- Preset compatibility ----

    [UnityTest]
    public IEnumerator GlassCannon_DiesToSmallHit_ThatTankySurvives()
    {
        var glass = TestSceneBuilder.CreateGlassCannonPlayer();
        var tank  = TestSceneBuilder.CreateTankyPlayer();
        _players.Add(glass);
        _players.Add(tank);
        yield return null;

        glass.Health.TakeDamage(120f);
        tank.Health.TakeDamage(120f);

        Assert.IsTrue(glass.Health.IsDead,  "Glass cannon should not survive 120 damage.");
        Assert.IsFalse(tank.Health.IsDead,  "Tanky player should survive 120 damage.");
    }

    [UnityTest]
    public IEnumerator EndurancePlayer_OutlastsDefault_InStaminaDrain()
    {
        var defaultPlayer   = TestSceneBuilder.CreatePlayer();
        var endurePlayer    = TestSceneBuilder.CreateEndurancePlayer();
        _players.Add(defaultPlayer);
        _players.Add(endurePlayer);
        yield return null;

        int defaultActions = 0;
        int endureActions  = 0;
        float cost = 10f;

        while (defaultPlayer.Stamina.UseStamina(cost)) defaultActions++;
        while (endurePlayer.Stamina.UseStamina(cost))  endureActions++;

        Assert.IsTrue(endureActions > defaultActions,
            $"Endurance player ({endureActions}) should outlast default ({defaultActions}).");
    }
}
*/