using System;
using System.Linq;
using HoAndTheBells.NPC;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class ValidateCats
{
    public static object CheckSavedScene()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Expected edit mode.");
        var cats = UnityEngine.Object.FindObjectsByType<CatNpcWander>();
        if (cats.Length != 4) throw new InvalidOperationException("Expected four NPCs.");
        var surface = UnityEngine.Object.FindObjectsByType<NavMeshSurface>().Single();
        if (!surface.navMeshData || !AssetDatabase.Contains(surface.navMeshData))
            throw new InvalidOperationException("Navigation data was not saved.");
        var report = cats.Select(c => {
            var agent = c.GetComponent<NavMeshAgent>();
            var animator = c.GetComponentInChildren<Animator>();
            var serialized = new SerializedObject(c);
            var stairs = serialized.FindProperty("stairs");
            if (agent.agentTypeID != surface.agentTypeID || stairs.arraySize != 2
                || serialized.FindProperty("destinations").arraySize != 12
                || c.GetComponentsInChildren<SkinnedMeshRenderer>().Length != 3
                || !animator.avatar.isValid || animator.applyRootMotion
                || c.Ascents.Any(n => n != 0) || c.Descents.Any(n => n != 0))
                throw new InvalidOperationException("Invalid saved configuration: " + c.name);
            for (int i = 0; i < stairs.arraySize; i++)
            {
                var route = stairs.GetArrayElementAtIndex(i);
                var bottom = (Transform)route.FindPropertyRelative("bottom").objectReferenceValue;
                var top = (Transform)route.FindPropertyRelative("top").objectReferenceValue;
                var path = new NavMeshPath();
                var filter = new NavMeshQueryFilter {agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas};
                if (!NavMesh.CalculatePath(bottom.position,top.position,filter,path) || path.status != NavMeshPathStatus.PathComplete
                    || !NavMesh.CalculatePath(top.position,bottom.position,filter,path) || path.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Incomplete saved route: " + c.name);
            }
            return new {c.name, renderers = 3, destinations = 12, completeStairRoutes = 2, agent.stoppingDistance};
        }).ToArray();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Scene has unsaved changes.");
        return new {passed = true, cats = report, savedNavigation = AssetDatabase.GetAssetPath(surface.navMeshData)};
    }

    public static object Preview()
    {
        var temporary = new GameObject("Cat preview temporary");
        var cameraObject = new GameObject("Cat preview camera");
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            string[] models = HoAndTheBells.Editor.CatNpcAssets.Models;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cats/ProtaCat.fbx").OfType<AnimationClip>().First(c => c.name == "Idle");
            for (int i = 0; i < models.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cats/" + models[i] + ".prefab");
                var cat = UnityEngine.Object.Instantiate(prefab, temporary.transform);
                cat.GetComponent<CatNpcWander>().enabled = false;
                cat.GetComponent<NavMeshAgent>().enabled = false;
                cat.transform.position = new Vector3((i - 1.5f) * 1.8f, 0, -7);
                var animator = cat.GetComponentInChildren<Animator>();
                animator.enabled = false;
                idle.SampleAnimation(animator.gameObject, 0.2f);
            }
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(1, 2.8f, -0.5f);
            camera.transform.LookAt(new Vector3(0, 0.7f, -7));
            camera.fieldOfView = 52;
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            System.IO.Directory.CreateDirectory("Temp/CatValidation");
            System.IO.File.WriteAllBytes("Temp/CatValidation/cats-preview.png", texture.EncodeToPNG());
            return "Temp/CatValidation/cats-preview.png";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(temporary);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    public static object Inspect()
    {
        var cats = UnityEngine.Object.FindObjectsByType<CatNpcWander>();
        return new {
            playing = Application.isPlaying, time = Time.time, frame = Time.frameCount,
            cats = cats.Select(c => {
                var a = c.GetComponent<NavMeshAgent>();
                var animator = c.GetComponentInChildren<Animator>();
                var renderers = c.GetComponentsInChildren<SkinnedMeshRenderer>();
                return new {
                    c.name, position = c.transform.position.ToString(), state = c.CurrentState,
                    c.Ascents, c.Descents, c.SawIdle, c.SawWalk, c.SawRun, c.FailedPaths,
                    a.agentTypeID, a.isOnNavMesh, path = a.isOnNavMesh ? a.pathStatus.ToString() : "unbound",
                    distance = a.isOnNavMesh && a.hasPath ? a.remainingDistance : 0,
                    velocity = a.velocity.magnitude, animator.isInitialized,
                    avatarValid = animator.avatar != null && animator.avatar.isValid,
                    animationSpeed = Application.isPlaying ? animator.GetFloat("Speed") : 0,
                    renderers = renderers.Select(r => new {r.name, bounds = r.bounds.ToString(), material = r.sharedMaterial?.name, bones = r.bones.Length}).ToArray(),
                    allMeshes = c.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => new {r.name, active = r.gameObject.activeSelf}).ToArray()
                };
            }).ToArray(),
            surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>()
                .Select(s => new {s.name, s.agentTypeID, baked = s.navMeshData != null, s.voxelSize, s.buildHeightMesh}).ToArray()
        };
    }

    public static object SampleAnimations()
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cats/ProtaCat.fbx")
            .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cats/G.Calda Fina.prefab");
        var root = UnityEngine.Object.Instantiate(prefab);
        root.name = "Cat Animation Inspection";
        root.GetComponent<CatNpcWander>().enabled = false;
        root.GetComponent<NavMeshAgent>().enabled = false;
        var animator = root.GetComponentInChildren<Animator>();
        animator.enabled = false;
        var mesh = new Mesh();
        try
        {
            return clips.Select(clip => new {
                clip.name, clip.length, clip.isLooping,
                samples = new[] {0f, 0.25f, 0.5f, 0.75f}.Select(t => {
                    clip.SampleAnimation(animator.gameObject, clip.length * t);
                    var renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.StartsWith("G.Calda"));
                    renderer.BakeMesh(mesh, true);
                    var verts = mesh.vertices.Select(v => renderer.transform.TransformPoint(v)).ToArray();
                    return new {t, minY = verts.Min(v => v.y), maxY = verts.Max(v => v.y),
                        leftFoot = animator.GetComponentsInChildren<Transform>().First(b => b.name == "Fut.L").position.ToString(),
                        bagCount = root.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r => !r.name.StartsWith("G.Calda"))};
                }).ToArray()
            }).ToArray();
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(root); }
    }
}
