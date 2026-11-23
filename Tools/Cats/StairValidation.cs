using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HoAndTheBells.NPC;
using UnityEngine;
using UnityEngine.AI;

public static class CatStairValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static async Task<object> RetestStraightTail()
    {
        var cats = UnityEngine.Object.FindObjectsByType<CatNpcWander>();
        var cat = cats.Single(c => c.name == "G.Calda Reta");
        var others = cats.Where(c => c != cat).ToArray();
        float scale = Time.timeScale;
        int up = cat.Ascents[0], down = cat.Descents[0];
        try
        {
            foreach (var other in others)
            {
                other.enabled = false;
                other.GetComponent<NavMeshAgent>().enabled = false;
            }
            typeof(CatNpcWander).GetMethod("Wait",Private).Invoke(cat,null);
            Set(cat,"activeStair",-1); Set(cat,"ordinaryTrips",3); Set(cat,"nextStair",0); Set(cat,"waitUntil",0f);
            Time.timeScale = 3f;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while ((cat.Ascents[0] == up || cat.Descents[0] == down) && watch.Elapsed.TotalSeconds < 35)
            {
                await Task.Delay(100);
                if (!Application.isPlaying) throw new InvalidOperationException("Play Mode ended.");
            }
            var agent = cat.GetComponent<NavMeshAgent>();
            var report = new {cat.name, climbed = cat.Ascents[0] > up, descended = cat.Descents[0] > down,
                cat.Ascents, cat.Descents, cat.FailedPaths, cat.CurrentState, position = cat.transform.position.ToString(),
                destination = agent.destination.ToString(), path = agent.pathStatus.ToString(),
                agent.remainingDistance, corners = agent.path.corners.Select(p => p.ToString()).ToArray()};
            System.IO.File.WriteAllText("Temp/CatValidation/straight-tail-retest.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally
        {
            Time.timeScale = scale;
            foreach (var other in others)
            {
                other.GetComponent<NavMeshAgent>().enabled = true;
                other.enabled = true;
            }
        }
    }

    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var cats = UnityEngine.Object.FindObjectsByType<CatNpcWander>();
        if (cats.Length != 4) throw new InvalidOperationException("Expected four cats.");
        float previousScale = Time.timeScale;
        float start = Time.time;
        int firstFrame = Time.frameCount;
        var results = new List<object>();
        var clips = cats.ToDictionary(c => c, c => new HashSet<string>());
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Time.timeScale = 3f;
            for (int stair = 0; stair < 2; stair++)
            {
                int route = stair;
                var initialUp = cats.Select(c => c.Ascents[route]).ToArray();
                var initialDown = cats.Select(c => c.Descents[route]).ToArray();
                var maxHeight = cats.Select(c => 0f).ToArray();
                // Select the next normal stair visit without warping or replacing the movement code.
                foreach (var cat in cats)
                {
                    typeof(CatNpcWander).GetMethod("Wait", Private).Invoke(cat, null);
                    Set(cat,"activeStair",-1);
                    Set(cat,"ordinaryTrips",3);
                    Set(cat,"nextStair",route);
                    Set(cat,"waitUntil",0f);
                }
                bool captured = false;
                float deadline = Time.time + 150;
                while (Time.time < deadline && watch.Elapsed.TotalSeconds < 95)
                {
                    await Task.Delay(100);
                    if (!Application.isPlaying) throw new InvalidOperationException("Play Mode ended during stair validation.");
                    for (int i = 0; i < cats.Length; i++)
                    {
                        var cat = cats[i];
                        var animator = cat.GetComponentInChildren<Animator>();
                        foreach (var info in animator.GetCurrentAnimatorClipInfo(0)) clips[cat].Add(info.clip.name);
                        if (cat.CurrentState == $"Stairs {route + 1}: 1")
                        {
                            maxHeight[i] = Mathf.Max(maxHeight[i], cat.transform.position.y);
                            if (!captured && cat.transform.position.y > 0.5f && cat.transform.position.y < 1.2f)
                            {
                                Capture(cat.transform.position, route);
                                captured = true;
                            }
                        }
                    }
                    if (cats.Select((c,i) => c.Ascents[route] > initialUp[i] && c.Descents[route] > initialDown[i]).All(done => done)) break;
                }
                results.Add(new {
                    staircase = route + 1,
                    cats = cats.Select((c,i) => new {c.name, climbed = c.Ascents[route] > initialUp[i],
                        descended = c.Descents[route] > initialDown[i], highest = maxHeight[i], c.FailedPaths,
                        c.CurrentState, position = c.transform.position.ToString()}).ToArray()
                });
            }
            var report = new {firstFrame, lastFrame = Time.frameCount, simulatedSeconds = Time.time - start,
                routes = results, animations = clips.Select(p => new {p.Key.name, clips = p.Value.ToArray()}).ToArray()};
            System.IO.Directory.CreateDirectory("Temp/CatValidation");
            System.IO.File.WriteAllText("Temp/CatValidation/stairs.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally { Time.timeScale = previousScale; }
    }

    private static void Set(CatNpcWander cat, string field, object value)
        => typeof(CatNpcWander).GetField(field, Private).SetValue(cat,value);

    private static void Capture(Vector3 position, int route)
    {
        var cameraObject = new GameObject("Stair validation camera");
        var target = new RenderTexture(1280,720,24);
        var texture = new Texture2D(1280,720,TextureFormat.RGB24,false);
        var previous = RenderTexture.active;
        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = position + new Vector3(3,2.2f,-3);
            camera.transform.LookAt(position + Vector3.up * 0.6f);
            camera.fieldOfView = 55;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0);
            texture.Apply();
            System.IO.Directory.CreateDirectory("Temp/CatValidation");
            System.IO.File.WriteAllBytes($"Temp/CatValidation/stairs-{route+1}.png",texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
