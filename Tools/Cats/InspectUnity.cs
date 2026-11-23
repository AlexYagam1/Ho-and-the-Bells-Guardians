using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectCatsUnity
{
    public static object Inspect()
    {
        AssetDatabase.Refresh();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Cats/ProtaCat.fbx");
        return new {
            scene = SceneManager.GetActiveScene().path,
            playing = Application.isPlaying,
            clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cats/ProtaCat.fbx").OfType<AnimationClip>()
                .Select(c => new {c.name, c.length, c.frameRate}).ToArray(),
            model = model == null ? null : model.GetComponentsInChildren<Transform>(true)
                .Select(t => new { t.name, position = t.localPosition.ToString(), scale = t.localScale.ToString() }).ToArray(),
            geometry = Object.FindObjectsByType<Collider>()
                .Where(c => !c.transform.root.name.Contains("Player"))
                .Select(c => new {c.name, parent = c.transform.parent?.name, center = c.bounds.center.ToString(), size = c.bounds.size.ToString()}).ToArray(),
            navigation = Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>()
                .Select(s => new {s.name, s.agentTypeID, baked = s.navMeshData != null}).ToArray()
        };
    }
}
