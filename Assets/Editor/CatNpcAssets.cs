using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using HoAndTheBells.NPC;

namespace HoAndTheBells.Editor
{
    public static class CatNpcAssets
    {
        public const string ModelPath = "Assets/Art/Cats/ProtaCat.fbx";
        public static readonly string[] Models = {"G.Calda Fina", "G.Calda Grossa", "G.Calda Grossa-Gordo", "G.Calda Reta"};
        public const float ModelScale = 0.25f;

        public static AnimatorController Prepare()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null) throw new InvalidOperationException("Export ProtaCat.fbx first.");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionY = true;
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            var animations = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
            foreach (string name in new[] {"Idle", "Walk", "Run"})
                if (!animations.ContainsKey(name)) throw new InvalidOperationException($"Missing clip {name}.");

            const string path = "Assets/Animations/Cats/CatLocomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idle = machine.AddState("Idle"); idle.motion = animations["Idle"];
            var walk = machine.AddState("Walk"); walk.motion = animations["Walk"];
            var run = machine.AddState("Run"); run.motion = animations["Run"];
            machine.defaultState = idle;
            Transition(idle, walk, AnimatorConditionMode.Greater, 0.1f);
            Transition(walk, idle, AnimatorConditionMode.Less, 0.08f);
            Transition(walk, run, AnimatorConditionMode.Greater, 1.7f);
            Transition(run, walk, AnimatorConditionMode.Less, 1.6f);
            return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.15f;
            transition.AddCondition(mode, threshold, "Speed");
        }

        public static GameObject CreatePrefab(int index, int agentType, AnimatorController controller)
        {
            var root = new GameObject("Cat NPC - " + Models[index]);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Visual";
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * ModelScale;
            model.transform.localPosition = new Vector3(0, -0.003f, 0);
            bool fat = index == 2;
            var colors = new[] {new Color(0.78f, 0.46f, 0.23f), new Color(0.72f, 0.75f, 0.8f),
                new Color(0.4f, 0.43f, 0.5f), new Color(0.9f, 0.81f, 0.62f)};
            var fur = Material("Cat " + index, colors[index]);
            var bag = Material("Backpack", new Color(0.22f, 0.38f, 0.3f));
            var straps = Material("Backpack Straps", new Color(0.22f, 0.13f, 0.07f));
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                bool body = renderer.name.StartsWith("G.Calda");
                bool active = body ? renderer.name == Models[index] : renderer.name.EndsWith(".001") == fat;
                renderer.gameObject.SetActive(active);
                renderer.sharedMaterial = body ? fur : renderer.name.StartsWith("Bolsa") ? bag : straps;
                renderer.updateWhenOffscreen = true;
            }
            var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = agentType;
            agent.radius = 0.28f;
            agent.height = 1.35f;
            agent.speed = 1.2f;
            agent.acceleration = 6f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.18f;
            agent.avoidancePriority = 40 + index * 10;
            root.AddComponent<CatNpcWander>().Configure(animator, model.transform, Array.Empty<Transform>(), Array.Empty<CatStairRoute>(), index);
            string path = "Assets/Prefabs/Cats/" + Models[index] + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material Material(string name, Color color)
        {
            string path = "Assets/Materials/Cats/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) {name = name};
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.22f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
