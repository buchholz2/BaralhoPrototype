using UnityEngine;
using UnityEngine.SceneManagement;
using Pif.Gameplay.PIF;

namespace Pif.UI
{
    public static class PifRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallPifSceneSystems()
        {
            Scene active = SceneManager.GetActiveScene();
            string scenePath = active.path.Replace('\\', '/');
            bool isPifScene = scenePath.Contains("/_Pif/Scenes/") ||
                              active.name.IndexOf("Pif", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isPifScene)
                return;

            GameObject host = GameObject.Find("GameManager");
            if (host == null)
                host = new GameObject("GameManager");

            BoardLayoutPif layout = host.GetComponent<BoardLayoutPif>();
            if (layout == null)
                layout = host.AddComponent<BoardLayoutPif>();
            layout.ApplyLayoutNow();

            PifGameManager manager = host.GetComponent<PifGameManager>();
            if (manager == null)
                manager = host.AddComponent<PifGameManager>();

            manager.EnsureRuntimeDependencies();
        }
    }
}
