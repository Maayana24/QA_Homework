using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;


public class TestTorches
{
    private const int Load1ObjectBudget = 50;
    private const int Light50TorchesBudget = 100;
    private const string PlayerPath = "Assets/Prefabs/Player.prefab";
    private const string TorchPath = "Assets/Prefabs/Torch.prefab";

    [Test]
    public void StressTest_MinFloatPlayerSpeed()
    {
        PlayerControllerController player = AssetDatabase.LoadAssetAtPath<PlayerControllerController>(PlayerPath);
        PlayerControllerController p = GameObject.Instantiate(player);
        p.SetSpeed(float.MinValue);
        Assert.Greater(p.Speed, 0);
    }

    [Test]
    public void PerformanceTest_SpawnPlayer()
    {
        PlayerControllerController player = AssetDatabase.LoadAssetAtPath<PlayerControllerController>(PlayerPath);
        var sw = Stopwatch.StartNew();
        PlayerControllerController p = GameObject.Instantiate(player);
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, Load1ObjectBudget,
            $"Player Spawn took {sw.ElapsedMilliseconds}ms — budget is {Load1ObjectBudget}ms.");
    }

    [UnityTest]
    public IEnumerator LoadTest_Light50Torches()
    {
        List<Torch> torches = new List<Torch>();
        Torch torch = AssetDatabase.LoadAssetAtPath<Torch>(TorchPath);


        for (int i = 0; i < 50; i++)
        {
            Torch t = GameObject.Instantiate(torch);
            torches.Add(t);
        }

        yield return null; // let all Awakes run

        var sw = Stopwatch.StartNew();
        foreach (var t in torches)
            t.FlameOn();
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, Light50TorchesBudget,
            $"50 torches lighting {sw.ElapsedMilliseconds}ms.");
    }


    // A Test behaves as an ordinary method
    [Test]
    public void UnitTest_TorchIsUnlitOnDefault()
    {
        GameObject go = new GameObject();
        var torch = go.AddComponent<Torch>();
        Assert.IsTrue(!torch.IsLit);
    }

    [UnityTest]
    public IEnumerator SmokeTest_SceneAndTorchesLoadCorrectly()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);

        Assert.IsNotNull(SceneManager.GetActiveScene(), "Scene failed to load");
        Assert.AreEqual("scene", SceneManager.GetActiveScene().name);

        if (CheckTorches() != 0)
            Assert.IsTrue(false);
        else Assert.IsTrue(true);
    }

    [UnityTest]
    public IEnumerator IntegrationTest_InteractionOfTorchAndPlayer()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);
        PlayerControllerController player = Object.FindAnyObjectByType<PlayerControllerController>();
        player.TestTorchesInteraction();
        if (CheckTorches() != 1)
            Assert.IsTrue(false);
        else Assert.IsTrue(true);
    }

    [UnityTest]
    public IEnumerator RegressionTest_InteractionInput() //Interaction didn't work because the logic used local scale on the global grid
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);
        PlayerControllerController player = Object.FindAnyObjectByType<PlayerControllerController>();
        player.TestTorchesInteraction();
        Torch[] torches = Object.FindObjectsByType<Torch>(FindObjectsSortMode.None);

        Torch litTorch = null;
        bool oneTorch = false;
        for (int i = 0; i < torches.Length; i++)
        {
            if(torches[i].IsLit == true)
            {
                litTorch = torches[i];
                Assert.IsFalse(oneTorch);
                oneTorch = true;
            }
        }

        Assert.IsTrue(litTorch);

        Collider[] overlaps = Physics.OverlapSphere(player.transform.position, player.coll.height);
        bool isInRange = false;
        foreach (Collider overlappingColl in overlaps)
        {
            if (overlappingColl.tag == "Torch")
            {
                if (litTorch == overlappingColl.GetComponent<Torch>()) isInRange = true;
            }
        }
        Assert.IsTrue(isInRange);
    }

    [UnityTest]
    public IEnumerator FunctionalTest_PlayerCanLightTorch()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);

        Assert.IsNotNull(SceneManager.GetActiveScene(), "Scene failed to load");
        Assert.AreEqual("scene", SceneManager.GetActiveScene().name);

        if (CheckTorches() != 0)
            Assert.IsTrue(false);

        PlayerControllerController player = Object.FindAnyObjectByType<PlayerControllerController>();
        player.TestTorchesInteraction();

        if (CheckTorches() != 1)
            Assert.IsTrue(false);

        player.TestMove(Vector2.up);
        yield return new WaitForSeconds(1);
        player.TestMove(Vector2.zero);
        player.TestTorchesInteraction();

        if (CheckTorches() != 2)
            Assert.IsTrue(false);
        else Assert.IsTrue(true);
    }


    private int CheckTorches()
    {
        Torch[] torches = Object.FindObjectsByType<Torch>(FindObjectsSortMode.None);
        bool islit = false;
        int i = 0;
        foreach (var t in torches)
        {
            if (t.IsLit) i++;
        }
        return i;
    }
}
