//using System;
//using NUnit.Framework;

///// <summary>
///// Usability Tests — EditMode.
/////
///// Non-functional. Usability testing in a game system context means:
///// "Is this API pleasant and safe for OTHER DEVELOPERS to use?"
/////
///// We are not testing the player's experience here.
///// We are testing the DEVELOPER's experience of working with this code.
/////
///// We ask:
/////   - Does the API fail with CLEAR, HELPFUL errors on bad input?
/////   - Do read operations (Preview, GetPercentage) have ZERO side effects?
/////   - Is the initialization order forgiving, or does it crash silently?
/////   - Does the validator give enough info to fix the problem?
/////   - Are edge cases (zero stats, max stats, null) handled without surprises?
/////
///// If a new developer uses this API wrong, do they get a crash with a mystery NullRef,
///// or a clear exception that tells them exactly what went wrong?
/////
///// Place in: Assets/Tests/EditMode/
///// </summary>
//public class UsabilityTests
//{
//    // ---- Null safety ----

//    [Test]
//    public void PlayerHealthSystem_NullStatBlock_ThrowsClear_ArgumentException()
//    {
//        var ex = Assert.Throws<ArgumentNullException>(() =>
//            new PlayerHealthSystem(null));

//        // The message should tell the developer WHAT was null
//        StringAssert.Contains("stats", ex.ParamName,
//            "Exception should name the offending parameter.");
//    }

//    [Test]
//    public void StaminaSystem_NullStatBlock_ThrowsClear_ArgumentException()
//    {
//        var ex = Assert.Throws<ArgumentNullException>(() =>
//            new StaminaSystem(null));

//        StringAssert.Contains("stats", ex.ParamName);
//    }

//    // ---- Read operations have no side effects ----

//    [Test]
//    public void PreviewDamage_DoesNotModifyHealth()
//    {
//        var health = new PlayerHealthSystem(new RPGStatBlock(resistance: 30));
//        float before = health.CurrentHealth;

//        health.PreviewDamage(100f);
//        health.PreviewDamage(100f);
//        health.PreviewDamage(100f);

//        Assert.AreEqual(before, health.CurrentHealth,
//            "PreviewDamage should never modify CurrentHealth.");
//    }

//    [Test]
//    public void PreviewCost_DoesNotModifyStamina()
//    {
//        var stamina = new StaminaSystem(new RPGStatBlock(willpower: 10));
//        float before = stamina.CurrentStamina;

//        stamina.PreviewCost(20f);
//        stamina.PreviewCost(20f);

//        Assert.AreEqual(before, stamina.CurrentStamina,
//            "PreviewCost should never modify CurrentStamina.");
//    }

//    [Test]
//    public void GetHealthPercentage_DoesNotModifyHealth()
//    {
//        var health = new PlayerHealthSystem(new RPGStatBlock());
//        health.TakeDamage(30f);
//        float hpBefore = health.CurrentHealth;

//        health.GetHealthPercentage();
//        health.GetHealthPercentage();

//        Assert.AreEqual(hpBefore, health.CurrentHealth);
//    }

//    // ---- Validator gives actionable error messages ----

//    [Test]
//    public void StatValidator_OverMaxStat_ErrorNamesTheOffendingStat()
//    {
//        var badStats = new RPGStatBlock(vitality: 999); // over MAX_STAT
//        var result   = StatValidator.Validate(badStats);

//        Assert.IsFalse(result.IsValid);
//        Assert.IsTrue(result.Errors.Length > 0);
//        StringAssert.Contains("Vitality", result.Errors[0],
//            "Error message should name which stat is out of range.");
//    }

//    [Test]
//    public void StatValidator_MultipleInvalidStats_ReportsAllErrors()
//    {
//        // RPGStatBlock clamps negatives to 0 internally — use values above MAX_STAT to trigger
//        var badStats = new RPGStatBlock(vitality: 200, endurance: 200, resistance: 200);
//        var result   = StatValidator.Validate(badStats);

//        Assert.IsFalse(result.IsValid);
//        Assert.GreaterOrEqual(result.Errors.Length, 3,
//            "Validator should report ALL invalid stats, not just the first.");
//    }

//    [Test]
//    public void StatValidator_ValidBlock_ReturnsNoErrors()
//    {
//        var goodStats = new RPGStatBlock(5, 5, 20, 10, 5, 5, 2, 5);
//        var result    = StatValidator.Validate(goodStats);

//        Assert.IsTrue(result.IsValid);
//        Assert.AreEqual(0, result.Errors.Length);
//    }

//    [Test]
//    public void StatValidator_ValidateOrThrow_GivesReadableMessage_OnFailure()
//    {
//        var badStats = new RPGStatBlock(vitality: 999);
//        var ex = Assert.Throws<ArgumentException>(() =>
//            StatValidator.ValidateOrThrow(badStats));

//        Assert.IsNotNull(ex.Message);
//        Assert.IsNotEmpty(ex.Message,
//            "ValidateOrThrow exception message should not be empty.");
//        StringAssert.Contains("Vitality", ex.Message);
//    }

//    // ---- Forgiving initialization ----

//    [Test]
//    public void RPGStatBlock_NegativeInputs_SilentlyClampToZero_NoException()
//    {
//        // A new developer might pass -1 by accident.
//        // The constructor should clamp, not throw.
//        Assert.DoesNotThrow(() => new RPGStatBlock(vitality: -1, resistance: -10));
//    }

//    [Test]
//    public void PlayerHealthSystem_ZeroVitality_StillHasBaseHP()
//    {
//        var health = new PlayerHealthSystem(new RPGStatBlock(vitality: 0));
//        Assert.AreEqual(100f, health.MaxHealth,
//            "Zero vitality should still give base 100 HP — not zero.");
//    }

//    [Test]
//    public void StaminaSystem_ZeroEndurance_StillHasBaseStamina()
//    {
//        var stamina = new StaminaSystem(new RPGStatBlock(endurance: 0));
//        Assert.AreEqual(50f, stamina.MaxStamina,
//            "Zero endurance should still give base 50 stamina — not zero.");
//    }

//    // ---- Repeated/idempotent operations ----

//    [Test]
//    public void RestoreFullHealth_CalledTwice_BehavesIdentically()
//    {
//        var health = new PlayerHealthSystem(new RPGStatBlock());
//        health.TakeDamage(50f);
//        health.RestoreFullHealth();
//        float afterFirst = health.CurrentHealth;
//        health.RestoreFullHealth();
//        float afterSecond = health.CurrentHealth;

//        Assert.AreEqual(afterFirst, afterSecond,
//            "RestoreFullHealth called twice should produce the same result.");
//    }

//    [Test]
//    public void UseStamina_ZeroCost_ReturnsTrueAndChangesNothing()
//    {
//        var stamina = new StaminaSystem(new RPGStatBlock());
//        float before = stamina.CurrentStamina;
//        bool result  = stamina.UseStamina(0f);

//        Assert.IsTrue(result,  "UseStamina(0) should return true.");
//        Assert.AreEqual(before, stamina.CurrentStamina, "Stamina should be unchanged.");
//    }
//}
