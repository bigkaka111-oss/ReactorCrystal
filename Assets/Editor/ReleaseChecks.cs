using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic gameplay contracts. Does not modify or save the open scene.</summary>
public static class ReleaseChecks
{
    private static void Require(bool condition,string message) { if(!condition)throw new Exception(message); }
    private sealed class Fixture : IDisposable
    {
        public readonly GameObject root;
        public readonly CrystalDrive drive;
        public readonly QuestSystem quest;
        public Fixture()
        {
            root=new GameObject("ReleaseChecks (temporary)") {hideFlags=HideFlags.HideAndDontSave};
            drive=root.AddComponent<CrystalDrive>();
            quest=root.AddComponent<QuestSystem>();
            typeof(QuestSystem).GetField("crystalDrive",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(quest,drive);
            quest.ResetQuest();
        }
        public void Tick(float dt) { drive.Simulate(dt);quest.Tick(dt); }
        public void Dispose()=>UnityEngine.Object.DestroyImmediate(root);
    }
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode before deterministic checks.");
        var results=new List<string>();
        void Test(string name,Action body)
        {
            try {body();results.Add("PASS | "+name);}
            catch(Exception e){results.Add("FAIL | "+name+" | "+e.Message);}
        }
        Test("Known current and stabilizer response",()=>{
            using var f=new Fixture();f.drive.ChangePistons(75);f.drive.ChangeMPSDepth(100);f.drive.ChangeRPM(300);
            for(int i=0;i<1000;i++)f.drive.Simulate(.02f);
            Require(Mathf.Abs(f.drive.currentAmperes-10)<.01f,"Expected 10 A");
            f.drive.ChangeMZSDepth(100);
            for(int i=0;i<1000;i++)f.drive.Simulate(.02f);
            Require(Mathf.Abs(f.drive.currentAmperes-2)<.01f,"Expected 2 A");
        });
        Test("Pressure boundaries and RPM clamp",()=>{
            using var f=new Fixture();
            foreach(float p in new[]{0f,69f,70f,85f,86f,100f})
            {f.drive.ChangePistons(p);f.drive.Simulate(.02f);Require(f.drive.crystalActive==(p>=70&&p<=85),"Pressure "+p);}
            f.drive.ChangeRPM(5000);Require(f.drive.mpsRPM==1200,"Upper clamp");
            f.drive.ChangeRPM(-100);Require(f.drive.mpsRPM==0,"Lower clamp");
            f.drive.ChangeRPM(340);Require(f.drive.mpsRPM==300,"Discrete RPM");
        });
        Test("Failure wins a simultaneous completion and is terminal",()=>{
            using var f=new Fixture();f.quest.steps=new[]{new DifficultyStep{holdSeconds=.1f,minAmps=8,maxAmps=12}};
            f.quest.ResetQuest();f.drive.ChangePistons(75);f.drive.Simulate(.02f);f.drive.currentAmperes=10;f.quest.StartQuest();f.drive.instability=100;
            int failed=0,won=0;f.quest.OnQuestFailed+=()=>failed++;f.quest.OnQuestCompleted+=()=>won++;
            f.quest.Tick(1f);f.quest.StartQuest();f.quest.Tick(5f);f.quest.FailQuest();
            Require(f.quest.IsFailed&&!f.quest.IsCompleted&&failed==1&&won==0,"Conflicting outcomes");
            f.drive.ChangeRPM(1200);Require(f.drive.mpsRPM==0,"Input after failure");
        });
        Test("Victory is terminal and ignores later failure",()=>{
            using var f=new Fixture();f.quest.steps=new[]{new DifficultyStep{holdSeconds=.1f,minAmps=8,maxAmps=12}};
            f.quest.ResetQuest();f.drive.ChangePistons(75);f.drive.Simulate(.02f);f.drive.currentAmperes=10;
            f.quest.Tick(1);Require(f.quest.IsCompleted,"Expected victory");
            Require(!f.quest.FailQuest(),"Late failure accepted");f.drive.ChangeRPM(900);
            Require(f.quest.IsCompleted&&f.drive.mpsRPM==0,"Terminal mutation");
        });
        Test("Stop does not auto-start",()=>{
            using var f=new Fixture();f.drive.ChangePistons(75);f.drive.Simulate(.02f);f.drive.currentAmperes=10;
            f.quest.StartQuest();f.quest.StopQuest();f.quest.Tick(2);
            Require(f.quest.State==QuestSystem.SessionState.Stopped,"Auto-restarted");
        });
        Test("Reset restores the entire initial contract",()=>{
            using var f=new Fixture();f.drive.ChangePistons(75);f.drive.Simulate(.02f);f.drive.currentAmperes=10;f.quest.Tick(11);
            Require(f.quest.CurrentStep==1,"Fixture did not advance");
            f.drive.instability=50;f.drive.LockPistons();f.quest.ResetQuest();
            Require(f.quest.CurrentStep==0&&!f.quest.IsRunning&&f.drive.TargetCurrentMin==8&&f.drive.TargetCurrentMax==12,"Stale target");
            Require(Mathf.Abs(f.drive.GetInstabilityGrowthRate()-.008f)<.00001f,"Stale difficulty");
            Require(f.drive.instability==0&&f.drive.currentAmperes==0&&!f.drive.pistonLocked&&f.drive.ControlsEnabled,"Stale drive state");
        });
        Test("Locked lever rejects hidden target; unlock has no jump",()=>{
            using var f=new Fixture();f.drive.ChangePistons(75);
            var lever=f.root.AddComponent<CrystalLever>();lever.drive=f.drive;lever.action=CrystalLever.LeverAction.Pistons;lever.SyncFromDrive();
            f.drive.LockPistons();lever.SetTargetValue(20);Require(lever.TargetValue==75,"Hidden lock target");
            f.drive.ChangePistons(20);Require(f.drive.pistonPressure==75,"Lock bypass");
            f.drive.LockPistons();Require(lever.TargetValue==75&&f.drive.pistonPressure==75,"Unlock jump");
        });
        Test("Emergency stop preserves stage and cools in seconds",()=>{
            using var f=new Fixture();f.drive.ChangePistons(75);f.drive.ChangeRPM(1000);f.drive.instability=60;
            f.drive.EmergencyStop();Require(f.drive.pistonPressure==0&&f.drive.mpsRPM==0&&!f.drive.pistonLocked,"Shutdown incomplete");
            for(int i=0;i<300;i++)f.Tick(.02f);
            Require(f.drive.instability<25&&f.quest.CurrentStep==0&&!f.quest.IsFailed,"No recovery");
        });
        Test("Progress burns outside range and inactive current cannot advance",()=>{
            using var f=new Fixture();f.drive.ChangePistons(75);f.drive.Simulate(.02f);f.drive.currentAmperes=10;f.quest.Tick(2);
            f.drive.currentAmperes=20;f.quest.Tick(.5f);Require(Mathf.Abs(f.quest.CurrentHoldTimer-1)<.01f,"Wrong decay");
            f.drive.crystalActive=false;f.drive.currentAmperes=10;f.quest.Tick(2);Require(f.quest.CurrentHoldTimer==0,"Inactive progress");
        });
        foreach(int fps in new[]{30,60,120})
            Test("Full legal five-stage simulation at "+fps+" Hz",()=>{
                using var f=new Fixture();float dt=1f/fps;f.drive.ChangePistons(75);f.drive.ChangeMPSDepth(100);
                int[] rpm={300,500,700,900,1000};float elapsed=0;
                while(!f.quest.IsTerminal&&elapsed<180)
                {f.drive.ChangeRPM(rpm[Mathf.Clamp(f.quest.CurrentStep,0,4)]);f.Tick(dt);elapsed+=dt;}
                Require(f.quest.IsCompleted&&!f.quest.IsFailed,"Could not finish, risk "+f.drive.instability);
                Require(f.drive.instability<95,"No safety margin: "+f.drive.instability);
                results.Add("METRIC | "+fps+" Hz | "+elapsed.ToString("F2")+" s | risk "+f.drive.instability.ToString("F2"));
            });
        return string.Join("\n",results);
    }
}

