using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Vaultbreakers.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests share one physics world and one scene manager. A test that loads a generated
    /// scene leaves it loaded, and its colliders then answer the next test's queries: an arena dummy
    /// standing between a rig and its own target will quietly absorb the shot. Any test that builds
    /// its own rig starts here so it is measuring only what it created.
    /// </summary>
    internal static class IsolatedTestBed
    {
        public const string SceneName = "Empty_TestBed";

        public static IEnumerator Load()
        {
            var operation = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsNotNull(operation, "Empty_TestBed is not in the build settings; run the setup tool.");

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }
    }
}
