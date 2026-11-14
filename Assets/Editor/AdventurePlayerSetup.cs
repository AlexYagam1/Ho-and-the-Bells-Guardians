using System;
using HoAndTheBells.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoAndTheBells.Editor
{
    public static class AdventurePlayerSetup
    {
        private const string PrefabPath = "Assets/Prefabs/Player/AdventurePlayer.prefab";

        [MenuItem("Tools/Ho and the Bells/Create Adventure Player Demo")]
        public static void Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (GameObject.Find("Adventure Player") != null || GameObject.Find("Adventure Test Area") != null)
                throw new InvalidOperationException("Demo already exists. Edit the existing objects instead.");
            if (UniversalRenderPipeline.asset == null) throw new InvalidOperationException("URP required.");
            Folder("Assets", "Prefabs"); Folder("Assets/Prefabs", "Player");
            Folder("Assets", "Materials"); Folder("Assets/Materials", "AdventureDemo");
            var player = new GameObject("Adventure Player");
            Undo.RegisterCreatedObjectUndo(player, "Create adventure player");
            player.tag = "Player";
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.42f; cc.radius = 0.3f; cc.center = Vector3.up * 0.9f;
            cc.stepOffset = 0.3f; cc.slopeLimit = 45f; cc.skinWidth = 0.03f; cc.minMoveDistance = 0f;
            player.AddComponent<AdventurePlayerInput>();
            var look = player.AddComponent<AdventurePlayerLook>();
            player.AddComponent<AdventurePlayerMotor>();
            var eye = Child("Eye Target", player.transform, Vector3.up * 1.32f);
            var body = Child("Body Target", player.transform, Vector3.up * 0.9f);
            var serializedLook = new SerializedObject(look);
            serializedLook.FindProperty("eyeTarget").objectReferenceValue = eye;
            serializedLook.FindProperty("bodyTarget").objectReferenceValue = body;
            serializedLook.ApplyModifiedPropertiesWithoutUndo();
            var rig = Child("First Person Cinemachine", player.transform, Vector3.zero).gameObject;
            var cm = rig.AddComponent<CinemachineCamera>();
            cm.Follow = eye;
            cm.Lens.FieldOfView = 70f; cm.Lens.NearClipPlane = 0.05f; cm.Lens.FarClipPlane = 300f;
            rig.AddComponent<CinemachineHardLockToTarget>().Damping = 0f;
            rig.AddComponent<CinemachineRotateWithFollowTarget>().Damping = 0f;
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);
            player.transform.position = new Vector3(0f, 0.05f, -7f);

            var camera = Camera.main;
            if (camera == null) camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            Undo.RecordObject(camera, "Configure camera");
            camera.tag = "MainCamera"; camera.allowHDR = true; camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 300f;
            camera.transform.SetPositionAndRotation(player.transform.position + Vector3.up * 1.32f, Quaternion.identity);
            var brain = camera.GetComponent<CinemachineBrain>() ?? Undo.AddComponent<CinemachineBrain>(camera.gameObject);
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true; data.volumeLayerMask = 1;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            const string profilePath = "Assets/Settings/AdventureVibrantProfile.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
                throw new InvalidOperationException("Adventure volume profile already exists.");
            AssetDatabase.CreateAsset(profile, profilePath);
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
            var colors = profile.Add<ColorAdjustments>(true);
            colors.saturation.value = 15f; colors.contrast.value = 10f;
            profile.Add<Bloom>(true).intensity.value = 0f;
            profile.Add<MotionBlur>(true).intensity.value = 0f;
            profile.Add<DepthOfField>(true).mode.value = DepthOfFieldMode.Off;
            profile.Add<ChromaticAberration>(true).intensity.value = 0f;
            profile.Add<FilmGrain>(true).intensity.value = 0f;
            profile.Add<Vignette>(true).intensity.value = 0f;
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            var volume = GameObject.Find("Global Volume")?.GetComponent<Volume>();
            if (volume == null) volume = new GameObject("Global Volume").AddComponent<Volume>();
            Undo.RecordObject(volume, "Configure adventure volume");
            volume.gameObject.layer = 0; volume.isGlobal = true; volume.weight = 1f;
            volume.sharedProfile = profile;
            Undo.RecordObject(UniversalRenderPipeline.asset, "Enable HDR");
            UniversalRenderPipeline.asset.supportsHDR = true;
            EditorUtility.SetDirty(UniversalRenderPipeline.asset);

            var area = new GameObject("Adventure Test Area");
            Undo.RegisterCreatedObjectUndo(area, "Create exploration test area");
            var grass = Material("Meadow", new Color(0.35f, 0.61f, 0.23f));
            var stone = Material("Warm Stone", new Color(0.71f, 0.63f, 0.44f));
            var blue = Material("Blue Markers", new Color(0.15f, 0.48f, 0.65f));
            var gold = Material("Golden Steps", new Color(0.84f, 0.59f, 0.22f));
            Box(area, "Ground", new Vector3(0,-0.25f,0), new Vector3(24,0.5f,24), grass);
            Box(area, "North Wall", new Vector3(0,1.5f,12), new Vector3(24,3,0.5f), stone);
            Box(area, "South Wall", new Vector3(0,1.5f,-12), new Vector3(24,3,0.5f), stone);
            Box(area, "East Wall", new Vector3(12,1.5f,0), new Vector3(0.5f,3,24), stone);
            Box(area, "West Wall", new Vector3(-12,1.5f,0), new Vector3(0.5f,3,24), stone);
            for (int i=0;i<5;i++)
                Box(area, "Step " + (i+1), new Vector3(-5,(i+1)*0.125f,1+i*0.8f), new Vector3(2.5f,(i+1)*0.25f,0.8f), gold);
            var ramp = Box(area, "Ramp", new Vector3(5,0.7f,3), new Vector3(3,0.25f,5), blue);
            ramp.transform.rotation = Quaternion.Euler(-15,0,0);
            Box(area, "Ramp Landing", new Vector3(5,0.75f,6.5f), new Vector3(3,1.5f,2), blue);
            Box(area, "Low Ceiling", new Vector3(-5,2.35f,-4), new Vector3(3,0.3f,3), stone);
            Box(area, "Ceiling Support Left", new Vector3(-6.4f,1,-4), new Vector3(0.2f,2,3), stone);
            Box(area, "Ceiling Support Right", new Vector3(-3.6f,1,-4), new Vector3(0.2f,2,3), stone);
            for (int i=0;i<3;i++) Box(area, "View Marker " + i, new Vector3(-3+i*3,1.5f,9), new Vector3(0.6f,3,0.6f), blue);
            var light = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (light != null)
            {
                Undo.RecordObject(light, "Adventure lighting");
                light.intensity = 1.6f; light.color = new Color(1f,0.96f,0.86f); light.shadows = LightShadows.Soft;
                Undo.RecordObject(light.transform, "Adventure sunlight angle");
                light.transform.rotation = Quaternion.Euler(45,-35,0);
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f,0.7f,0.83f);
            RenderSettings.ambientEquatorColor = new Color(0.46f,0.51f,0.47f);
            RenderSettings.ambientGroundColor = new Color(0.28f,0.32f,0.22f);
            EditorUtility.SetDirty(profile);
            EditorSceneManager.MarkSceneDirty(player.scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(player.scene);
            Selection.activeGameObject = player;
        }

        private static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
        private static Transform Child(string name, Transform parent, Vector3 local)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = local; return t;
        }
        private static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name; material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness",0.15f);
            AssetDatabase.CreateAsset(material, "Assets/Materials/AdventureDemo/" + name + ".mat"); return material;
        }
        private static GameObject Box(GameObject root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name;
            box.transform.SetParent(root.transform); box.transform.position = position; box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material; return box;
        }
    }
}
