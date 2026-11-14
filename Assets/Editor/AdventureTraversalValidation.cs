using System;
using System.Collections.Generic;
using HoAndTheBells.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace HoAndTheBells.Editor
{
    public static class AdventureTraversalValidation
    {
        // Runtime-only visual fixture. Leaving Play Mode restores the real player.
        public static string PrepareAuraPreview(float maximum, float reserve)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required.");
            var player=GameObject.Find("Adventure Player");
            var motor=player.GetComponent<AdventurePlayerMotor>();
            var input=player.GetComponent<AdventurePlayerInput>();
            var look=player.GetComponent<AdventurePlayerLook>();
            var feedback=player.GetComponent<PlayerStaminaFeedback>();
            var stamina=player.GetComponent<PlayerStamina>();
            var cc=player.GetComponent<CharacterController>();
            motor.enabled=input.enabled=look.enabled=feedback.enabled=false;
            motor.SendMessage("OnDisable");
            cc.enabled=false; player.transform.SetPositionAndRotation(new Vector3(1,0.05f,-8),Quaternion.identity); cc.enabled=true;
            Physics.SyncTransforms(); cc.Move(Vector3.down*0.001f);
            for(int i=0;i<40;i++) motor.Simulate(Vector2.zero,false,false,1f/60);
            stamina.SetMaximumStamina(maximum); stamina.Restore(maximum);
            cc.enabled=false; player.transform.SetPositionAndRotation(new Vector3(8,1.8f,-4),Quaternion.identity); cc.enabled=true;
            Physics.SyncTransforms(); cc.Move(Vector3.down*0.001f);
            motor.Simulate(Vector2.up,false,false,1f/60);
            if(motor.State!=AdventurePlayerMotor.MovementState.Climbing) throw new Exception("Preview failed to engage wall");
            stamina.Restore(maximum); stamina.Consume(maximum-reserve);
            look.ApplyLook(new Vector2(-600,0));
            feedback.AdvanceVisuals(0.3f);
            return "Preview: maximum="+maximum+", current="+stamina.Current+", extent="+player.GetComponentInChildren<StaminaAuraGraphic>().Coverage;
        }

        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required.");
            var report = new List<string>();
            var environment = new GameObject("Temporary Traversal Validation");
            var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/AdventurePlayer.prefab"));
            var motor = player.GetComponent<AdventurePlayerMotor>();
            var probe = player.GetComponent<PlayerTraversalProbe>();
            var stamina = player.GetComponent<PlayerStamina>();
            var input = player.GetComponent<AdventurePlayerInput>();
            var feedback = player.GetComponent<PlayerStaminaFeedback>();
            var cc = player.GetComponent<CharacterController>();
            var aura = player.GetComponentInChildren<StaminaAuraGraphic>();
            motor.enabled = input.enabled = player.GetComponent<AdventurePlayerLook>().enabled = feedback.enabled = false;
            foreach (var camera in player.GetComponentsInChildren<CinemachineCamera>()) camera.enabled = false;
            var basePosition = new Vector3(50,0,0);
            GameObject Box(string name, Vector3 center, Vector3 size, bool climbable = false)
            {
                var box=GameObject.CreatePrimitive(PrimitiveType.Cube); box.name=name; box.transform.SetParent(environment.transform);
                box.transform.position=basePosition+center; box.transform.localScale=size;
                if(climbable) box.AddComponent<ClimbableSurface>();
                Physics.SyncTransforms(); return box;
            }
            void Check(bool condition,string label)
            {
                if(!condition) throw new Exception(label);
            }
            void Test(string label,Action action)
            {
                try { action(); report.Add("PASS: "+label); }
                catch(Exception e) { report.Add("FAIL: "+label+" — "+e.Message); }
            }
            void Place(Vector3 feet)
            {
                cc.enabled=false;
                player.transform.SetPositionAndRotation(basePosition+feet-Vector3.up*(cc.center.y-cc.height/2f),Quaternion.identity);
                cc.enabled=true; Physics.SyncTransforms(); cc.Move(Vector3.down * 0.001f);
            }
            void Reset(Vector3 feet)
            {
                motor.SendMessage("OnDisable");
                Place(new Vector3(-10,0.05f,-10));
                for(int i=0;i<40;i++) motor.Simulate(Vector2.zero,false,false,1f/60f);
                stamina.SetMaximumStamina(100); stamina.Restore(100);
                Place(feet);
                if(feet.y<0.1f) for(int i=0;i<10;i++) motor.Simulate(Vector2.zero,false,false,1f/60f);
            }
            try
            {
                Box("Floor",new Vector3(0,-0.25f,0),new Vector3(40,0.5f,40));
                foreach(int fps in new[] {30,60,120})
                {
                    float dt=1f/fps;
                    Test("movement and double jump at "+fps+" Hz",()=>
                    {
                        Reset(new Vector3(-5,0.05f,-10)); var start=player.transform.position;
                        for(int i=0;i<fps;i++) motor.Simulate(Vector2.one,false,false,dt);
                        var displacement=player.transform.position-start; displacement.y=0;
                        Check(Mathf.Abs(displacement.magnitude-4)<0.04f,"diagonal speed");
                        Reset(new Vector3(-5,0.05f,-10)); start=player.transform.position;
                        for(int i=0;i<fps;i++) motor.Simulate(Vector2.up,true,false,dt);
                        Check(Mathf.Abs(player.transform.position.z-start.z-6.5f)<0.04f,"sprint speed");
                        float JumpTrace(bool third)
                        {
                            Reset(new Vector3(-5,0.05f,-10)); float peak=probe.Feet.y;
                            for(int i=0;i<fps*2;i++)
                            {
                                motor.Simulate(Vector2.zero,false,i==0 || i==fps/4 || (third && i==fps/2),dt);
                                peak=Mathf.Max(peak,probe.Feet.y);
                                if(i==fps/4) Check(!motor.AirJumpAvailable,"air jump consumed");
                            }
                            Check(motor.IsGrounded && motor.AirJumpAvailable,"landing restores jump"); return peak;
                        }
                        float second=JumpTrace(false), thirdPeak=JumpTrace(true);
                        Check(second>1.8f && Mathf.Abs(second-thirdPeak)<0.02f,"third jump rejected");
                        Reset(new Vector3(-5,2,-10));
                        motor.Simulate(Vector2.zero,false,true,dt);
                        Check(!motor.AirJumpAvailable,"walking off ledge permits exactly one air jump");
                    });
                    Test("climb drain / hold / recover at "+fps+" Hz",()=>
                    {
                        var wall=Box("Climb wall",new Vector3(0,5,0),new Vector3(6,10,0.5f),true);
                        try
                        {
                            Reset(new Vector3(0,0.05f,-0.8f));
                            for(int i=0;i<fps;i++) motor.Simulate(Vector2.up,false,false,dt);
                            Check(motor.State==AdventurePlayerMotor.MovementState.Climbing,"engage wall");
                            Check(Mathf.Abs(stamina.Current-75)<0.05f,"25/s climbing, got "+stamina.Current);
                            float y=probe.Feet.y;
                            for(int i=0;i<fps;i++) motor.Simulate(Vector2.zero,false,false,dt);
                            Check(Mathf.Abs(stamina.Current-65)<0.05f && Mathf.Abs(probe.Feet.y-y)<0.03f,"10/s while holding");
                            player.transform.rotation=Quaternion.Euler(0,180,0);
                            var position=player.transform.position;
                            for(int i=0;i<fps/2;i++) motor.Simulate(Vector2.right,false,false,dt);
                            Check(player.transform.position.x>position.x+0.4f,"lateral movement ignores view yaw");
                            motor.Simulate(Vector2.zero,false,false,dt,true);
                            Check(motor.State==AdventurePlayerMotor.MovementState.Locomotion,"Ctrl drops");
                            float remaining=stamina.Current;
                            motor.Simulate(Vector2.up,false,false,dt);
                            Check(motor.State==AdventurePlayerMotor.MovementState.Locomotion && stamina.Current==remaining,"no instant regrab or air recovery");
                            stamina.Consume(stamina.Current);
                            for(int i=0;i<fps;i++) stamina.TickRecovery(true,dt);
                            Check(Mathf.Abs(stamina.Current-28)<0.06f,"ground recovery delay and rate");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(wall); Physics.SyncTransforms(); }
                    });
                }
                Test("wall jump and exhaustion lock",()=>
                {
                    var wall=Box("Climb wall",new Vector3(0,5,0),new Vector3(6,10,0.5f),true);
                    try
                    {
                        Reset(new Vector3(0,2,-0.8f)); motor.Simulate(Vector2.up,false,false,1f/60);
                        bool available=motor.AirJumpAvailable; float before=stamina.Current; var position=player.transform.position;
                        motor.Simulate(Vector2.zero,false,true,1f/60);
                        Check(motor.State==AdventurePlayerMotor.MovementState.Locomotion && motor.AirJumpAvailable==available,"wall jump preserves reserve");
                        Check(Mathf.Abs(stamina.Current-before+15)<0.01f && player.transform.position.z<position.z && player.transform.position.y>position.y,"wall jump cost and impulse");
                        Reset(new Vector3(0,2,-0.8f)); motor.Simulate(Vector2.zero,false,true,1f/60);
                        motor.Simulate(Vector2.up,false,false,1f/60);
                        Check(!motor.AirJumpAvailable,"wall never restores consumed jump");
                        stamina.Consume(stamina.Current-0.01f);
                        motor.Simulate(Vector2.zero,false,false,1f/60);
                        Check(motor.ExhaustionLocked && motor.State==AdventurePlayerMotor.MovementState.Locomotion,"zero detaches in same tick");
                        stamina.Restore(100);
                        for(int i=0;i<20;i++) motor.Simulate(Vector2.zero,false,false,1f/60);
                        motor.Simulate(Vector2.up,false,false,1f/60);
                        Check(motor.State==AdventurePlayerMotor.MovementState.Locomotion,"cannot regrab after exhaustion before landing");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(wall); Physics.SyncTransforms(); }
                });
                Test("mantle valid, high, narrow and blocked",()=>
                {
                    var ledge=Box("Ledge",new Vector3(0,0.5f,0),new Vector3(3,1,2));
                    try
                    {
                        Reset(new Vector3(0,0.05f,-1.4f));
                        Check(probe.Mantle(Vector3.forward,out _,out _),"valid ledge detected");
                        for(int i=0;i<22;i++) motor.Simulate(Vector2.up,false,false,1f/60);
                        Check(probe.Feet.y>0.95f && motor.AirJumpAvailable,"mantle completes on top");
                        ledge.transform.position=basePosition+new Vector3(0,1,0); ledge.transform.localScale=new Vector3(3,2,2); Physics.SyncTransforms();
                        Reset(new Vector3(0,0.05f,-1.4f)); Check(!probe.Mantle(Vector3.forward,out _,out _),"too high rejected");
                        ledge.transform.position=basePosition+new Vector3(0,0.5f,0); ledge.transform.localScale=new Vector3(0.2f,1,2); Physics.SyncTransforms();
                        Check(!probe.Mantle(Vector3.forward,out _,out _),"narrow support rejected");
                        ledge.transform.localScale=new Vector3(3,1,2); Physics.SyncTransforms();
                        var ceiling=Box("Ceiling",new Vector3(0,1.8f,0),new Vector3(3,0.2f,2));
                        Check(!probe.Mantle(Vector3.forward,out _,out _),"ceiling rejected");
                        UnityEngine.Object.DestroyImmediate(ceiling); Physics.SyncTransforms();
                        motor.Simulate(Vector2.up,false,false,1f/60);
                        Check(motor.State==AdventurePlayerMotor.MovementState.Mantling,"started mantle");
                        ceiling=Box("New blocker",new Vector3(0,1.8f,-0.5f),new Vector3(3,0.2f,3));
                        for(int i=0;i<20;i++) motor.Simulate(Vector2.up,false,false,1f/60);
                        Check(motor.State!=AdventurePlayerMotor.MovementState.Mantling && probe.Feet.y<1,"blocked route cancels");
                        UnityEngine.Object.DestroyImmediate(ceiling);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(ledge); Physics.SyncTransforms(); }
                });
                Test("unmarked wall rejected",()=>
                {
                    var wall=Box("Unmarked",new Vector3(0,5,0),new Vector3(3,10,1));
                    try { Reset(new Vector3(0,1,-0.85f)); motor.Simulate(Vector2.up,false,false,1f/60); Check(motor.State==AdventurePlayerMotor.MovementState.Locomotion,"not climbing"); }
                    finally { UnityEngine.Object.DestroyImmediate(wall); }
                });
                Test("feedback pulse, exhaustion fade and control cancellation",()=>
                {
                    var wall=Box("Feedback wall",new Vector3(0,5,0),new Vector3(6,10,0.5f),true);
                    try
                    {
                        Reset(new Vector3(0,2,-0.8f)); feedback.enabled=true;
                        motor.Simulate(Vector2.up,false,false,1f/60);
                        feedback.AdvanceVisuals(0.15f);
                        Check(aura.color.a>0.3f,"green appears on engage");
                        stamina.Consume(stamina.Current-10);
                        float minimum=1,maximum=0;
                        for(int i=0;i<40;i++)
                        {
                            feedback.AdvanceVisuals(0.02f);
                            minimum=Mathf.Min(minimum,aura.color.a); maximum=Mathf.Max(maximum,aura.color.a);
                        }
                        Check(maximum-minimum>0.1f,"red opacity pulses");
                        var noise=player.GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
                        Check(noise.AmplitudeGain>0 && noise.NoiseProfile.PositionNoise.Length==0,"rotation-only fatigue noise");
                        stamina.Consume(stamina.Current-0.01f);
                        var position=player.transform.position;
                        motor.Simulate(Vector2.zero,false,false,1f/60);
                        feedback.AdvanceVisuals(0.01f);
                        Check(stamina.Current==0 && motor.State==AdventurePlayerMotor.MovementState.Locomotion && player.transform.position.y<position.y && aura.color.a>0,"red persists briefly on fall");
                        feedback.AdvanceVisuals(0.35f);
                        Check(aura.color.a==0 && noise.AmplitudeGain==0,"exhaustion clears after fade");
                        Reset(new Vector3(0,2,-0.8f)); motor.Simulate(Vector2.up,false,false,1f/60);
                        motor.Simulate(Vector2.up,false,false,1f/60,false,false);
                        feedback.ClearFeedback();
                        Check(motor.State==AdventurePlayerMotor.MovementState.Locomotion && aura.color.a==0,"loss of control detaches and clears visual");
                    }
                    finally { feedback.enabled=false; UnityEngine.Object.DestroyImmediate(wall); Physics.SyncTransforms(); }
                });
                Test("capacity, colors, coverage, pause and no bar",()=>
                {
                    Reset(new Vector3(-5,0.05f,-10));
                    foreach(float capacity in new[]{100f,150f,200f,250f})
                    {
                        stamina.SetMaximumStamina(capacity); Check(stamina.Current==100,"maximum increase does not refill");
                        feedback.AdvanceVisuals(0.25f);
                        Check(Mathf.Abs(aura.Coverage-Mathf.Clamp01(capacity/200f))<0.001f,"capacity coverage "+capacity);
                    }
                    stamina.Restore(150); Check(feedback.EvaluateColor(stamina.Current)==Color.white,"250 white");
                    Check(feedback.EvaluateColor(200)==Color.white && feedback.EvaluateColor(199)!=Color.white,"white threshold absolute");
                    Color at100=feedback.EvaluateColor(100); stamina.SetMaximumStamina(150);
                    Check(stamina.Current==150 && feedback.EvaluateColor(100)==at100,"decrease clamps, colors ignore maximum");
                    Check(feedback.EvaluateColor(15).r>0.9f && feedback.EvaluateColor(15).g<0.1f,"critical red");
                    Check(feedback.EvaluateColor(50).r>0.9f && feedback.EvaluateColor(50).g>0.2f,"orange");
                    feedback.AdvanceVisuals(0.25f); float coverage=aura.Coverage;
                    stamina.Consume(100); feedback.AdvanceVisuals(0.1f);
                    Check(aura.Coverage==coverage,"consumption does not shrink coverage");
                    Check(StaminaAuraGraphic.EdgeAlpha(new Vector2(0.5f,0.5f),1)==0,"center transparent");
                    Check(StaminaAuraGraphic.EdgeAlpha(new Vector2(0,0.75f),0.5f)==0,"upper half absent at 100");
                    Check(StaminaAuraGraphic.EdgeAlpha(new Vector2(0.5f,1),1)>0.9f,"top border at 200");
                    float reserve=stamina.Current; stamina.TickRecovery(true,0); feedback.AdvanceVisuals(0);
                    Check(stamina.Current==reserve,"pause freezes progress");
                    Check(player.GetComponentsInChildren<UnityEngine.UI.Slider>(true).Length==0,"no bar");
                    bool invalid=false; try{stamina.SetMaximumStamina(float.NaN);}catch(ArgumentOutOfRangeException){invalid=true;}
                    Check(invalid,"invalid maximum rejected");
                });
                return string.Join("\n",report);
            }
            finally { UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(environment); }
        }
    }
}
