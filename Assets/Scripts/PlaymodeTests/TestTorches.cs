using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TestTorches : InputTestFixture
{
    // A Test behaves as an ordinary method
    [Test]
    public void UnitTest_TorchIsUnlitOnDefault()
    {
        GameObject go = new GameObject();
        var torch = go.AddComponent<Torch>();
        Assert.IsTrue(torch.IsLit);
    }

    [UnityTest]
    public IEnumerator SmokeTest_SceneAndTorchesLoadCorrectly()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);

        Assert.IsNotNull(SceneManager.GetActiveScene(), "Scene failed to load");
        Assert.AreEqual("scene", SceneManager.GetActiveScene().name);

        Torch[] torches = Object.FindObjectsByType<Torch>(FindObjectsSortMode.None);
        Debug.Log(torches.Length);
        bool islit = false;
        foreach (var t in torches)
        {
            islit |= t.IsLit;
        }
        Assert.IsTrue(islit);
    }

    [UnityTest]
    public IEnumerator IntegrationTest()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);
        PlayerControllerController player = Object.FindAnyObjectByType<PlayerControllerController>();
        player.TestTorchesInteraction();
        Torch[] torches = Object.FindObjectsByType<Torch>(FindObjectsSortMode.None);
        bool islit = false;
        foreach (var t in torches)
        {
            islit |= t.IsLit;
        }
        Assert.IsTrue(!islit);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator TestTorchesWithEnumeratorPasses()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);

        Torch[] torches = Object.FindObjectsByType<Torch>(FindObjectsSortMode.None);
        Debug.Log(torches.Length);
        foreach (var t in torches)
        {
            Debug.Log(t.IsLit);
            Assert.IsTrue(t.IsLit);
        }
    }

    [UnityTest]
    public IEnumerator RegressionTest() //had input bugs
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Press(keyboard.eKey);
        Release(keyboard.eKey); Torch[] torches = Object.FindObjectsByType<Torch>(FindObjectsSortMode.None);
        bool islit = false;
        foreach (var t in torches)
        {
            islit |= t.IsLit;
        }
        Assert.IsTrue(!islit);
    }
}
