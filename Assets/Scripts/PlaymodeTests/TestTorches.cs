using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TestTorches
{
    // A Test behaves as an ordinary method
    [Test]
    public void UnitTest()
    {
        GameObject go = new GameObject();
        var torch = go.AddComponent<Torch>();
        Assert.IsTrue(torch.IsLit);
    }

    [UnityTest]
    public IEnumerator SmokeTest()
    {
        SceneManager.LoadScene("scene");
        yield return new WaitForSeconds(1);
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
}
