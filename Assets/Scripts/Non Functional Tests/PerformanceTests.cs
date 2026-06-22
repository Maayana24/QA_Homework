using System.Diagnostics;
using NUnit.Framework;

/// <summary>
/// Performance Tests — EditMode.
///
/// Non-functional. Not asking "does it work?" but "does it work FAST ENOUGH?"
///
/// We use System.Diagnostics.Stopwatch to measure elapsed time.
/// For more advanced profiling install Unity's Performance Testing package
/// (com.unity.test-framework.performance) and use [Performance] + Measure.Method().
///
/// Defined performance budgets for this system:
///   - Single damage calculation  : under 1ms
///   - 10,000 damage calculations : under 50ms
///   - Stat block construction    : under 1ms
///   - 1,000 stat block builds    : under 10ms
///   - Stamina regen tick (x1000) : under 5ms
///
/// Place in: Assets/Tests/EditMode/
/// </summary>
public class PerformanceTests
{
    private const int SINGLE_OP_BUDGET_MS    = 1;
    private const int BULK_10K_BUDGET_MS     = 50;
    private const int STAT_BUILD_1K_BUDGET_MS = 10;
    private const int REGEN_1K_BUDGET_MS     = 5;

    // ---- Stat block construction ----

    [Test]
    public void StatBlockConstruction_SingleInstance_UnderBudget()
    {
        var sw = Stopwatch.StartNew();
        var stats = new RPGStatBlock(5, 5, 20, 10, 5, 5, 2, 5);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, SINGLE_OP_BUDGET_MS,
            $"Stat block construction took {sw.ElapsedMilliseconds}ms — budget is {SINGLE_OP_BUDGET_MS}ms.");
    }

    [Test]
    public void StatBlockConstruction_1000Instances_UnderBudget()
    {
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
            new RPGStatBlock(i % 100, i % 100, i % 75, i % 50, i % 100, i % 100, i % 100, i % 100);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, STAT_BUILD_1K_BUDGET_MS,
            $"1000 stat block constructions took {sw.ElapsedMilliseconds}ms — budget is {STAT_BUILD_1K_BUDGET_MS}ms.");
    }

    // ---- Damage calculation ----

    [Test]
    public void DamageCalculation_SingleHit_UnderBudget()
    {
        var stats  = new RPGStatBlock(resistance: 30, armor: 10);
        var health = new PlayerHealthSystem(stats);

        var sw = Stopwatch.StartNew();
        health.TakeDamage(50f);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, SINGLE_OP_BUDGET_MS,
            $"Single TakeDamage took {sw.ElapsedMilliseconds}ms — budget is {SINGLE_OP_BUDGET_MS}ms.");
    }

    [Test]
    public void DamageCalculation_10000Hits_UnderBudget()
    {
        var stats  = new RPGStatBlock(vitality: 100, resistance: 30, armor: 10);
        var health = new PlayerHealthSystem(stats);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
        {
            health.TakeDamage(1f);
            if (health.IsDead()) health.RestoreFullHealth(); // keep it alive
        }
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, BULK_10K_BUDGET_MS,
            $"10,000 TakeDamage calls took {sw.ElapsedMilliseconds}ms — budget is {BULK_10K_BUDGET_MS}ms.");
    }

    // ---- Stamina regen tick ----

    [Test]
    public void StaminaRegenTick_1000Ticks_UnderBudget()
    {
        var stats   = new RPGStatBlock(endurance: 10, agility: 10);
        var stamina = new StaminaSystem(stats);
        stamina.UseStamina(stamina.MaxStamina); // drain first

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
            stamina.RegenTick(0.016f); // simulate 1000 frames at 60fps
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, REGEN_1K_BUDGET_MS,
            $"1000 regen ticks took {sw.ElapsedMilliseconds}ms — budget is {REGEN_1K_BUDGET_MS}ms.");
    }

    // ---- PreviewDamage (called every frame for UI) ----

    [Test]
    public void PreviewDamage_10000Calls_UnderBudget()
    {
        var stats  = new RPGStatBlock(resistance: 45, armor: 15);
        var health = new PlayerHealthSystem(stats);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
            health.PreviewDamage(100f);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, BULK_10K_BUDGET_MS,
            $"10,000 PreviewDamage calls took {sw.ElapsedMilliseconds}ms — budget is {BULK_10K_BUDGET_MS}ms.");
    }

    // ---- StatValidator ----

    [Test]
    public void StatValidation_1000Validations_UnderBudget()
    {
        var validStats   = new RPGStatBlock(5, 5, 20, 10, 5, 5, 2, 5);
        var invalidStats = new RPGStatBlock(); // will be tested too

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
            StatValidator.Validate(i % 2 == 0 ? validStats : invalidStats);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, STAT_BUILD_1K_BUDGET_MS,
            $"1000 validations took {sw.ElapsedMilliseconds}ms — budget is {STAT_BUILD_1K_BUDGET_MS}ms.");
    }
}
