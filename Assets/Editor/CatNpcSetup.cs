using System;
using System.Linq;
using HoAndTheBells.NPC;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace HoAndTheBells.Editor
{
    public static class CatNpcSetup
    {
        [MenuItem("Tools/Ho and the Bells/Configure Cat NPCs")]
        public static void Configure()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
                throw new InvalidOperationException("Open SampleScene first.");
            foreach (string folder in new[] {"Assets/Animations/Cats", "Assets/Materials/Cats", "Assets/Prefabs/Cats", "Assets/Navigation/Cats"})
                EnsureFolder(folder);
            var controller = CatNpcAssets.Prepare();
            var previous = GameObject.Find("Cat NPC Demo");
            if (previous) UnityEngine.Object.DestroyImmediate(previous);
            var root = new GameObject("Cat NPC Demo");
            Undo.RegisterCreatedObjectUndo(root, "Configure cat NPCs");
            int agentType = CatAgentType();
            var navigation = new GameObject("Cat Navigation");
            navigation.transform.SetParent(root.transform, false);
            var surface = navigation.AddComponent<NavMeshSurface>();
            surface.agentTypeID = agentType;
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0, 2, 18);
            surface.size = new Vector3(42, 10, 64);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize = true;
            surface.voxelSize = 0.045f;
            surface.buildHeightMesh = true;
            var player = GameObject.Find("Adventure Player");
            bool playerActive = player && player.activeSelf;
            try
            {
                if (player) player.SetActive(false);
                surface.BuildNavMesh();
            }
            finally { if (player) player.SetActive(playerActive); }
            if (surface.navMeshData == null) throw new InvalidOperationException("Cat NavMesh bake failed.");
            surface.navMeshData.name = "CatNavigation";
            const string dataPath = "Assets/Navigation/Cats/CatNavigation.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath);
            if (existing)
            {
                var baked = surface.navMeshData;
                surface.RemoveData();
                EditorUtility.CopySerialized(baked, existing);
                surface.navMeshData = existing;
                UnityEngine.Object.DestroyImmediate(baked);
                surface.AddData();
                EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(surface.navMeshData, dataPath);

            var points = new GameObject("Destinations").transform;
            points.SetParent(root.transform, false);
            Vector3[] locations = {
                new Vector3(-3,0,-7), new Vector3(3,0,-6), new Vector3(-7,0,-1),
                new Vector3(6,0,0), new Vector3(-7,0,7), new Vector3(1,0,8),
                new Vector3(0,0,15), new Vector3(-7,0,20), new Vector3(5,0,20),
                new Vector3(-1,0,34), new Vector3(6,0,34), new Vector3(0,0,42)
            };
            var destinations = locations.Select((p, i) => Marker(points, "Walk " + (i + 1), p, agentType)).ToArray();
            var stairs = new[] {
                new CatStairRoute {
                    bottom = Marker(points,"Courtyard stairs bottom",new Vector3(-5,0,-0.2f),agentType),
                    top = Marker(points,"Courtyard stairs top",new Vector3(-5,1.25f,4.2f),agentType)},
                new CatStairRoute {
                    bottom = Marker(points,"District stairs bottom",new Vector3(4,0,21.8f),agentType),
                    top = Marker(points,"District stairs top",new Vector3(4,1.5f,27.8f),agentType)}
            };
            foreach (var route in stairs)
            {
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(route.bottom.position, route.top.position,
                    new NavMeshQueryFilter {agentTypeID = agentType, areaMask = NavMesh.AllAreas}, path)
                    || path.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Incomplete stair route: " + route.bottom.name);
            }
            Vector3[] spawns = {new Vector3(-3,0,-4),new Vector3(3,0,-4),new Vector3(-6,0,18),new Vector3(6,0,18)};
            for (int i = 0; i < CatNpcAssets.Models.Length; i++)
            {
                var prefab = CatNpcAssets.CreatePrefab(i, agentType, controller);
                var cat = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                cat.transform.SetParent(root.transform, false);
                cat.transform.position = Snap(spawns[i], agentType);
                var behaviour = cat.GetComponent<CatNpcWander>();
                var animator = cat.GetComponentInChildren<Animator>();
                float lane = (i - 1.5f) * 0.42f;
                var personalStairs = stairs.Select((route, stairIndex) => new CatStairRoute {
                    bottom = Marker(points, $"Cat {i + 1} stairs {stairIndex + 1} bottom", route.bottom.position + Vector3.right * lane, agentType),
                    top = Marker(points, $"Cat {i + 1} stairs {stairIndex + 1} top", route.top.position + Vector3.right * lane, agentType)
                }).ToArray();
                behaviour.Configure(animator, animator.transform, destinations, personalStairs, i);
                PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static int CatAgentType()
        {
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
            {
                var found = NavMesh.GetSettingsByIndex(i);
                if (NavMesh.GetSettingsNameFromID(found.agentTypeID) == "Cat NPC") return found.agentTypeID;
            }
            var settings = NavMesh.CreateSettings();
            // Agent definitions live in the project's serialized navigation settings;
            // the runtime NavMesh API only creates and reads those definitions.
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0];
            var serialized = new SerializedObject(asset);
            var definitions = serialized.FindProperty("m_Settings");
            var names = serialized.FindProperty("m_SettingNames");
            for (int i = 0; i < definitions.arraySize; i++)
            {
                var definition = definitions.GetArrayElementAtIndex(i);
                if (definition.FindPropertyRelative("agentTypeID").intValue != settings.agentTypeID) continue;
                definition.FindPropertyRelative("agentRadius").floatValue = 0.28f;
                definition.FindPropertyRelative("agentHeight").floatValue = 1.35f;
                definition.FindPropertyRelative("agentClimb").floatValue = 0.3f;
                definition.FindPropertyRelative("agentSlope").floatValue = 45f;
                names.GetArrayElementAtIndex(i).stringValue = "Cat NPC";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return settings.agentTypeID;
        }

        private static Transform Marker(Transform parent, string name, Vector3 position, int type)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.position = Snap(position, type);
            return marker;
        }

        private static Vector3 Snap(Vector3 position, int type)
        {
            if (NavMesh.SamplePosition(position, out var hit, 0.4f,
                new NavMeshQueryFilter {agentTypeID = type, areaMask = NavMesh.AllAreas})) return hit.position;
            throw new InvalidOperationException("No cat NavMesh at " + position);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
