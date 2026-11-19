using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace YnaAce.Editor
{
    // シーンや公開用データを変更せず、試合の境界条件を実際のゲーム実装で検証します。
    public static class GameplayValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static double playDeadline;
        private static bool hitTested;
        private static int lastHalf;

        public static void RunPlaySession()
        {
            SessionState.SetBool("YnaAce.PlayValidation", true);
            ResumePlayValidation();
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ResumePlayValidation()
        {
            if (!SessionState.GetBool("YnaAce.PlayValidation", false)) return;
            playDeadline = EditorApplication.timeSinceStartup + 180;
            hitTested = false;
            lastHalf = 0;
            EditorApplication.update -= PlayStep;
            EditorApplication.update += PlayStep;
        }

        private static void PlayStep()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > playDeadline) throw new TimeoutException("Full-match play validation timed out");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                var game = UnityEngine.Object.FindAnyObjectByType<YnaAceGame>();
                if (game == null || Get<Transform>(game, "ball") == null) return;
                Time.timeScale = 20f;
                int half = Get<int>(game, "inning") * 2 + (Get<bool>(game, "playerBatting") ? 0 : 1);
                if (half != lastHalf)
                {
                    lastHalf = half;
                    Debug.Log("YNA ACE play validation: half=" + half + " score=" + Get<int>(game, "playerScore") + ":" + Get<int>(game, "cpuScore"));
                }
                if (Get<bool>(game, "gameOver"))
                {
                    Check(Get<int>(game, "inning") >= 9 && hitTested, "full match and hit");
                    Invoke(game, "RestartGame");
                    Check(Get<int>(game, "inning") == 1 && Get<int>(game, "playerScore") == 0 && Get<int>(game, "cpuScore") == 0 && !Get<bool>(game, "gameOver"), "play-mode rematch");
                    Debug.Log("YNA ACE FULL MATCH PLAY VALIDATION PASSED (runtime hit, pitching, all halves through game over, rematch).");
                    SessionState.SetBool("YnaAce.PlayValidation", false);
                    EditorApplication.update -= PlayStep;
                    EditorApplication.Exit(0);
                    return;
                }
                if (!hitTested && Get<bool>(game, "playerBatting") && Get<bool>(game, "pitchInFlight"))
                {
                    Set(game, "battingAim", Get<Vector3>(game, "target"));
                    Set(game, "pitchStart", Time.time - Get<float>(game, "pitchDuration") * 0.9f);
                    Get<Transform>(game, "ball").position = Vector3.Lerp(new Vector3(0f, 1.85f, 17.5f), Get<Vector3>(game, "target"), 0.9f);
                    Invoke(game, "Swing");
                    Check(Get<bool>(game, "ballInPlay"), "runtime swing produces a ball in play");
                    hitTested = true;
                }
                if (!Get<bool>(game, "playerBatting"))
                {
                    if (!Get<bool>(game, "pitching")) Invoke(game, "SelectPitchZone", 2, 2);
                    Invoke(game, "StartPitch");
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                SessionState.SetBool("YnaAce.PlayValidation", false);
                EditorApplication.update -= PlayStep;
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/YNA ACE/Validate Gameplay")]
        public static void Run()
        {
            var root = new GameObject("Gameplay validation");
            var game = root.AddComponent<YnaAceGame>();
            try
            {
                Invoke(game, "CreatePlayers");
                Invoke(game, "CreateBall");
                Invoke(game, "BeginHalfInning");
                for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                {
                    Set(game, "playerBatting", false);
                    Invoke(game, "SelectPitchZone", x, y);
                    Check((bool)Invoke(game, "IsStrikeZone") == (x >= 1 && x <= 3 && y >= 1 && y <= 3), "zone " + x + "," + y);
                }
                Set(game, "playerBatting", true);
                var target = Get<Vector3>(game, "target");
                Invoke(game, "SelectPitchZone", 0, 0);
                Check(Get<Vector3>(game, "target") == target, "batting aim must not move the pitch");

                Invoke(game, "RestartGame");
                var bases = Get<bool[]>(game, "bases");
                bases[0] = bases[1] = bases[2] = true;
                Invoke(game, "AwardWalk", true);
                Check(Get<int>(game, "playerScore") == 1 && bases[0] && bases[1] && bases[2], "bases-loaded walk");

                Set(game, "paused", true);
                Invoke(game, "StartPitch");
                Check(!Get<bool>(game, "pitching"), "paused pitch");
                Invoke(game, "MoveRunner", 1f);
                Check(Get<int>(game, "playerScore") == 1, "paused runner");
                Set(game, "paused", false);
                Set(game, "changingHalf", true);
                Invoke(game, "MoveRunner", 1f);
                Check(Get<int>(game, "playerScore") == 1, "runner during inning change");

                Half(game, 9, true, 0, 1);
                Check(Get<bool>(game, "gameOver"), "skip ninth bottom when home team leads");
                Half(game, 9, false, 2, 1);
                Check(Get<bool>(game, "gameOver") && Get<string>(game, "call").Contains("あなたの勝ち"), "away win");
                Half(game, 9, false, 1, 1);
                Check(!Get<bool>(game, "gameOver") && Get<int>(game, "inning") == 10 && Get<bool>(game, "playerBatting"), "extra innings");
                Half(game, 12, false, 1, 1);
                Check(Get<bool>(game, "gameOver") && Get<string>(game, "call").Contains("引き分け"), "twelfth inning draw");
                Invoke(game, "RestartGame");
                Set(game, "inning", 10);
                Set(game, "playerBatting", false);
                Invoke(game, "AddRuns", false, 1);
                Invoke(game, "CheckWalkOff");
                Check(Get<bool>(game, "gameOver"), "walk-off");
                Invoke(game, "RestartGame");
                Check(!Get<bool>(game, "gameOver") && Get<int>(game, "inning") == 1 && Get<int>(game, "cpuScore") == 0 && Get<int>(game, "playerScore") == 0 && !bases[0] && !bases[1] && !bases[2], "rematch reset");
                Debug.Log("YNA ACE gameplay validation PASSED: 25 zones, aim separation, walk, pause, transition, both win paths, extra innings, draw, walk-off, rematch.");
            }
            finally
            {
                foreach (var name in new[] { "pitcher", "batter", "runner", "catcher", "umpire", "ball" })
                {
                    var actor = Get<Transform>(game, name);
                    if (actor != null) UnityEngine.Object.DestroyImmediate(actor.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Half(YnaAceGame game, int inning, bool top, int away, int home)
        {
            Invoke(game, "RestartGame");
            Set(game, "inning", inning);
            Set(game, "playerBatting", top);
            Set(game, "playerScore", away);
            Set(game, "cpuScore", home);
            var routine = (IEnumerator)Invoke(game, "ChangeHalfInning");
            while (routine.MoveNext()) { }
        }

        private static object Invoke(YnaAceGame game, string method, params object[] args) => typeof(YnaAceGame).GetMethod(method, Private).Invoke(game, args);
        private static T Get<T>(YnaAceGame game, string name) => (T)typeof(YnaAceGame).GetField(name, Private).GetValue(game);
        private static void Set(YnaAceGame game, string name, object value) => typeof(YnaAceGame).GetField(name, Private).SetValue(game, value);
        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("YNA ACE validation failed: " + label);
        }
    }
}
