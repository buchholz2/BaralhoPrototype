#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Pif.UI.Editor
{
    [InitializeOnLoad]
    public static class PifPlayModeStartScene
    {
        private const string PifScenePath = "Assets/Scenes/PifTable.unity";

        static PifPlayModeStartScene()
        {
            EditorApplication.delayCall += EnsurePlayModeStartScene;
        }

        private static void EnsurePlayModeStartScene()
        {
            SceneAsset pifScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PifScenePath);
            if (pifScene == null)
                return;

            if (EditorSceneManager.playModeStartScene != pifScene)
                EditorSceneManager.playModeStartScene = pifScene;
        }
    }
}
#endif
