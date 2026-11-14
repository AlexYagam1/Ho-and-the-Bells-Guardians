using System;
using HoAndTheBells.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoAndTheBells.Editor
{
    public static class AdventureTraversalSetup
    {
        [MenuItem("Tools/Ho and the Bells/Configure Climbing and Stamina")]
        public static void Configure()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            const string prefabPath = "Assets/Prefabs/Player/AdventurePlayer.prefab";
            const string noisePath = "Assets/Settings/StaminaCameraNoise.asset";
            var noiseProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>(noisePath);
            if (noiseProfile == null)
            {
                noiseProfile = ScriptableObject.CreateInstance<NoiseSettings>();
                noiseProfile.PositionNoise = Array.Empty<NoiseSettings.TransformNoiseParams>();
                noiseProfile.OrientationNoise = new[] { new NoiseSettings.TransformNoiseParams {
                    X = new NoiseSettings.NoiseParams {Amplitude = 0.4f, Frequency = 2.1f},
                    Y = new NoiseSettings.NoiseParams {Amplitude = 0.4f, Frequency = 2.7f},
                    Z = new NoiseSettings.NoiseParams {Amplitude = 0.2f, Frequency = 3.3f}
                }};
                AssetDatabase.CreateAsset(noiseProfile, noisePath);
            }
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (!root.TryGetComponent<PlayerStamina>(out _)) root.AddComponent<PlayerStamina>();
                if (!root.TryGetComponent<PlayerTraversalProbe>(out _)) root.AddComponent<PlayerTraversalProbe>();
                var feedback = root.GetComponent<PlayerStaminaFeedback>() ?? root.AddComponent<PlayerStaminaFeedback>();
                var camera = root.GetComponentInChildren<CinemachineCamera>();
                var noise = camera.GetComponent<CinemachineBasicMultiChannelPerlin>() ?? camera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
                noise.NoiseProfile = noiseProfile; noise.AmplitudeGain = 0f; noise.FrequencyGain = 1f;
                var aura = root.GetComponentInChildren<StaminaAuraGraphic>(true);
                if (aura == null)
                {
                    var canvasObject = new GameObject("Stamina Aura Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
                    canvasObject.transform.SetParent(root.transform, false);
                    var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
                    var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 0.5f;
                    var graphic = new GameObject("Stamina Aura", typeof(RectTransform), typeof(CanvasRenderer), typeof(StaminaAuraGraphic));
                    graphic.transform.SetParent(canvasObject.transform, false);
                    var rect = graphic.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    aura = graphic.GetComponent<StaminaAuraGraphic>(); aura.raycastTarget = false; aura.color = Color.clear;
                }
                var settings = new SerializedObject(feedback);
                settings.FindProperty("aura").objectReferenceValue = aura;
                settings.FindProperty("noise").objectReferenceValue = noise;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            if (GameObject.Find("Traversal Test Area") == null)
            {
                var area = new GameObject("Traversal Test Area");
                Undo.RegisterCreatedObjectUndo(area, "Create climbing tests");
                var blue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AdventureDemo/Blue Markers.mat");
                var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AdventureDemo/Warm Stone.mat");
                var gold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AdventureDemo/Golden Steps.mat");
                Box(area,"Climbable Wall",new Vector3(8,2.25f,-3),new Vector3(3,4.5f,1),blue).AddComponent<ClimbableSurface>();
                Box(area,"Mantle Block",new Vector3(3,0.5f,-3),new Vector3(1.5f,1,1.5f),gold);
                Box(area,"Unclimbable Wall",new Vector3(-9,2,-1),new Vector3(2,4,1),stone);
                Box(area,"Narrow Ledge",new Vector3(0,0.55f,4),new Vector3(0.2f,1.1f,2),gold);
                Box(area,"Blocked Ledge",new Vector3(-2,0.5f,6),new Vector3(2,1,2),gold);
                Box(area,"Mantle Ceiling",new Vector3(-2,1.8f,6),new Vector3(2,0.2f,2),stone);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        private static GameObject Box(GameObject parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube); box.name=name; box.transform.SetParent(parent.transform);
            box.transform.position=position; box.transform.localScale=scale; box.GetComponent<Renderer>().sharedMaterial=material;
            return box;
        }
    }
}
