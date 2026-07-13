#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Datamaxxer.Editor
{
    public static class ClearPlayerPrefs
    {
        [MenuItem("Datamaxxer/Clear PlayerPrefs")]
        public static void ClearPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Datamaxxer] PlayerPrefs cleared successfully. Default values will be used on next play.");
        }
    }
}
#endif
