using System;
using System.Collections.Generic;
using System.Reflection;
using HoAndTheBells.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HoAndTheBells.Editor
{
    public static class AdventurePlayerValidation
    {
        public static string Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
            var report = new List<string>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/AdventurePlayer.prefab");
            var go = UnityEngine.Object.Instantiate(prefab);
            go.name = "Temporary Controller Validation";
            var motor = go.GetComponent<AdventurePlayerMotor>();
            var input = go.GetComponent<AdventurePlayerInput>();
            var look = go.GetComponent<AdventurePlayerLook>();
            var cc = go.GetComponent<CharacterController>();
            motor.enabled = input.enabled = look.enabled = false;
            foreach (var camera in go.GetComponentsInChildren<Unity.Cinemachine.CinemachineCamera>()) camera.enabled = false;
            void Check(bool condition, string label)
            {
                if (!condition) throw new Exception("FAIL: " + label);
                report.Add("PASS: " + label);
            }
            void Reset(Vector3 position)
            {
                motor.SendMessage("OnDisable");
                cc.enabled = false; go.transform.SetPositionAndRotation(position, Quaternion.identity); cc.enabled = true;
                Physics.SyncTransforms();
                for (int i=0;i<10;i++) motor.Simulate(Vector2.zero,false,false,1f/60f);
            }
            try
            {
                foreach (int fps in new[] {30,60,120})
                {
                    Reset(new Vector3(2,0.05f,-8));
                    var start = go.transform.position;
                    for (int i=0;i<fps;i++) motor.Simulate(Vector2.up,false,false,1f/fps);
                    Check(Mathf.Abs(go.transform.position.z-start.z-4f)<0.03f, "walk 4 m at " + fps + " Hz");
                    Reset(new Vector3(2,0.05f,-8)); start = go.transform.position;
                    for (int i=0;i<fps;i++) motor.Simulate(Vector2.up,true,false,1f/fps);
                    Check(Mathf.Abs(go.transform.position.z-start.z-6.5f)<0.03f, "sprint 6.5 m at " + fps + " Hz");
                    Reset(new Vector3(2,0.05f,-8)); start = go.transform.position;
                    for (int i=0;i<fps;i++) motor.Simulate(Vector2.one,false,false,1f/fps);
                    var delta = go.transform.position-start; delta.y=0;
                    Check(Mathf.Abs(delta.magnitude-4f)<0.03f,"normalized diagonal at " + fps + " Hz");
                    Reset(new Vector3(2,0.05f,-8)); start=go.transform.position;
                    float highest=start.y;
                    for (int i=0;i<fps*2;i++)
                    {
                        motor.Simulate(Vector2.zero,false,i==0 || i==fps/4,1f/fps);
                        highest=Mathf.Max(highest,go.transform.position.y);
                    }
                    Check(Mathf.Abs(highest-start.y-1.2f)<0.04f && motor.IsGrounded,"jump height, no midair jump, landing at " + fps + " Hz");
                    go.transform.rotation=Quaternion.identity;
                    for(int i=0;i<fps;i++) look.ApplyLook(new Vector2(600f/fps,0));
                    Check(Mathf.Abs(Mathf.DeltaAngle(go.transform.eulerAngles.y,72f))<0.05f,"mouse displacement independent of " + fps + " Hz");
                }
                Reset(new Vector3(10,0.05f,-8));
                for(int i=0;i<120;i++) motor.Simulate(Vector2.right,true,false,1f/60);
                Check(go.transform.position.x<11.5f,"wall blocks controller");
                Reset(new Vector3(-5,0.05f,-4)); float ceilingMax=go.transform.position.y;
                for(int i=0;i<120;i++) {motor.Simulate(Vector2.zero,false,i==0,1f/60);ceilingMax=Mathf.Max(ceilingMax,go.transform.position.y);}
                Check(ceilingMax<0.5f && motor.IsGrounded,"ceiling cancels upward velocity and player lands");
                Reset(new Vector3(-5,0.05f,-0.5f));
                for(int i=0;i<75;i++) motor.Simulate(Vector2.up,false,false,1f/60);
                Check(go.transform.position.y>1.1f,"climbs 0.25 m steps");
                Reset(new Vector3(5,0.05f,-1));
                for(int i=0;i<100;i++) motor.Simulate(Vector2.up,false,false,1f/60);
                Check(go.transform.position.y>1.2f,"climbs 15 degree ramp");
                for(int i=0;i<75;i++) motor.Simulate(Vector2.right,false,false,1f/60);
                Check(go.transform.position.y<0.1f && motor.IsGrounded,"falls from landing and regains ground");
                look.ApplyLook(new Vector2(0,100000));
                Check(Mathf.Abs(Mathf.DeltaAngle(look.EyeTarget.localEulerAngles.x,-80f))<0.01f,"pitch clamped at -80");
                look.ApplyLook(new Vector2(0,-200000));
                Check(Mathf.Abs(Mathf.DeltaAngle(look.EyeTarget.localEulerAngles.x,80f))<0.01f,"pitch clamped at +80");
                motor.SetControlEnabled(false);
                Check(!input.HasControl && input.Move==Vector2.zero && Cursor.lockState==CursorLockMode.None,"public control lock releases cursor and clears intent");
                return string.Join("\n",report);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        public static string RunInput()
        {
            if (!Application.isPlaying || !Application.isFocused) throw new InvalidOperationException("Focus the playing Editor first.");
            var originalKeyboard=Keyboard.current; var originalMouse=Mouse.current;
            var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>();
            var input=UnityEngine.Object.FindAnyObjectByType<AdventurePlayerInput>();
            var update=typeof(AdventurePlayerInput).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
            void Tick(KeyboardState keys, MouseState pointer)
            {
                InputSystem.QueueStateEvent(keyboard,keys); InputSystem.QueueStateEvent(mouse,pointer);
                InputSystem.Update(); update.Invoke(input,null);
            }
            void Check(bool ok,string label) {if(!ok) throw new Exception("FAIL: "+label);}
            try
            {
                input.SetControlEnabled(true);
                Tick(new KeyboardState(Key.W,Key.D,Key.LeftShift,Key.Space),new MouseState {delta=new Vector2(20,-10)});
                Check(Mathf.Abs(input.Move.magnitude-1)<0.001f && input.Sprint && input.JumpPressed && input.Look.x==20,"direct device polling");
                Tick(new KeyboardState(Key.W,Key.Space),new MouseState());
                Check(!input.JumpPressed,"holding jump does not repeat the press edge");
                Tick(new KeyboardState(Key.Escape),new MouseState());
                Check(!input.HasControl && input.Move==Vector2.zero && Cursor.visible,"Escape releases controls");
                Tick(new KeyboardState(),new MouseState());
                Tick(new KeyboardState(),new MouseState().WithButton(MouseButton.Left));
                Check(input.HasControl && input.Look==Vector2.zero,"click recaptures without mouse jump");
                input.SendMessage("OnApplicationFocus",false);
                Check(!input.HasControl && input.Move==Vector2.zero,"focus loss clears intent");
                input.SetControlEnabled(false);
                Tick(new KeyboardState(Key.W),new MouseState().WithButton(MouseButton.Left));
                Check(!input.HasControl && input.Move==Vector2.zero,"menu control lock cannot be bypassed by click");
                var removed = new List<InputDevice>();
                foreach (var device in InputSystem.devices)
                    if (device is Keyboard || device is Mouse) removed.Add(device);
                try
                {
                    foreach(var device in removed) InputSystem.RemoveDevice(device);
                    input.SetControlEnabled(true);
                    update.Invoke(input,null);
                    Check(Keyboard.current==null && Mouse.current==null && !input.HasControl && input.Move==Vector2.zero,"missing devices clear intent without exceptions");
                }
                finally { foreach(var device in removed) InputSystem.AddDevice(device); }
                return "PASS: Keyboard.current / Mouse.current, sprint, jump edge and hold, diagonal, Escape, click recapture, focus loss, external control lock, missing devices";
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                originalKeyboard?.MakeCurrent(); originalMouse?.MakeCurrent(); input.SetControlEnabled(true);
            }
        }
    }
}
