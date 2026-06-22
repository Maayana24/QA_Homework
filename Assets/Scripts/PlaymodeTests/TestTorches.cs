using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Diagnostics;


public class TestTorches
{
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
