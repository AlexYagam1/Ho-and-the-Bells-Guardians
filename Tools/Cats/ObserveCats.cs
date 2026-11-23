using System;
using System.Linq;
using System.Threading.Tasks;
using HoAndTheBells.NPC;
using UnityEngine;
using UnityEngine.AI;

public static class ObserveCats
{
    public static async Task<object> Observe()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode.");
        var cats = UnityEngine.Object.FindObjectsByType<CatNpcWander>();
        float originalScale = Time.timeScale;
        int firstFrame = Time.frameCount;
        float start = Time.time;
        var observed = cats.Select(c => new {cat = c, states = new System.Collections.Generic.HashSet<string>(),
            initial = c.transform.position}).ToArray();
        try
        {
            Time.timeScale = 3f;
            for (int sample = 0; sample < 400; sample++)
            {
                await Task.Delay(100);
                if (!Application.isPlaying) throw new InvalidOperationException("Play Mode ended during observation.");
                foreach (var entry in observed)
                {
                    var a = entry.cat.GetComponentInChildren<Animator>();
                    foreach (var clip in a.GetCurrentAnimatorClipInfo(0)) entry.states.Add(clip.clip.name);
                }
                if (cats.All(c => c.Ascents.All(n => n > 0) && c.Descents.All(n => n > 0)
                    && c.SawRun && c.SawWalk && c.SawIdle)) break;
            }
            var report = new {
                firstFrame, lastFrame = Time.frameCount, simulatedSeconds = Time.time - start,
                cats = observed.Select(e => new {
                    e.cat.name, position = e.cat.transform.position.ToString(), e.cat.CurrentState,
                    e.cat.Ascents, e.cat.Descents, e.cat.SawIdle, e.cat.SawWalk, e.cat.SawRun, e.cat.FailedPaths,
                    clips = e.states.ToArray(), moved = Vector3.Distance(e.initial,e.cat.transform.position),
                    bound = e.cat.GetComponent<NavMeshAgent>().isOnNavMesh
                }).ToArray()
            };
            System.IO.Directory.CreateDirectory("Temp/CatValidation");
            System.IO.File.WriteAllText("Temp/CatValidation/observation.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally { Time.timeScale = originalScale; }
    }
}
