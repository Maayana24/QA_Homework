using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestTorches
{
    // A Test behaves as an ordinary method
    [Test]
    public void TestTorchesSimplePasses()
    {
        GameObject go = new GameObject();
        Torch torch = go.AddComponent<Torch>();

        torch.FlameOn();

        Assert.IsTrue(torch.IsLit);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator TestTorchesWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
