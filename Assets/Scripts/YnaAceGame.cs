using System.Collections;
using UnityEngine;

namespace YnaAce
{
    /// <summary>WebGL でも追加アセットなしで動く、YNA ACE の最初の3Dプレイアブル版です。</summary>
    public sealed class YnaAceGame : MonoBehaviour
    {
        private Camera gameCamera;
        private Transform pitcher;
        private Transform batter;
        private Transform runner;
        private Transform ball;
        private Rigidbody ballBody;
        private Vector3 ballStart;
        private Vector3 target = new Vector3(0f, 1f, 0.2f);
        private float pitchStart;
        private float pitchDuration;
        private bool pitching;
        private bool paused;
        private bool swung;
        private int strikes;
        private int balls;
        private int outs;
        private int score;
        private string role = "打者";
        private string call = "ストライクゾーンを選び、来た球を SPACE またはクリックで打ってください。";
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private GUIStyle textStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<YnaAceGame>() == null)
            {
                new GameObject("YNA ACE Game").AddComponent<YnaAceGame>();
            }
        }

        private void Start()
        {
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            BuildStadium();
            CreatePlayers();
            CreateBall();
            SetupCamera();
        }

        private void BuildStadium()
        {
            RenderSettings.ambientLight = new Color(0.68f, 0.75f, 0.82f);
            MakePrimitive(PrimitiveType.Plane, "芝生", new Vector3(0f, 0f, 18f), new Vector3(7f, 1f, 7f), new Color(0.08f, 0.38f, 0.13f));
            MakePrimitive(PrimitiveType.Cylinder, "マウンド", new Vector3(0f, 0.1f, 18.44f), new Vector3(1.4f, 0.1f, 1.4f), new Color(0.58f, 0.37f, 0.19f));
            MakePrimitive(PrimitiveType.Cube, "ホームベース", Vector3.zero, new Vector3(0.43f, 0.06f, 0.43f), Color.white);
            foreach (Vector3 basePos in new[] { new Vector3(8f, 0.05f, 8f), new Vector3(0f, 0.05f, 16f), new Vector3(-8f, 0.05f, 8f) })
                MakePrimitive(PrimitiveType.Cube, "塁", basePos, new Vector3(0.45f, 0.1f, 0.45f), Color.white);
            for (int i = -2; i <= 2; i++)
                MakePrimitive(PrimitiveType.Cube, "スタンド", new Vector3(i * 7f, 2.2f, 34f), new Vector3(6.5f, 3.5f, 1.8f), new Color(0.16f, 0.2f, 0.32f));
            MakePrimitive(PrimitiveType.Cube, "外野フェンス", new Vector3(0f, 1.6f, 43f), new Vector3(32f, 3.2f, 0.4f), new Color(0.04f, 0.2f, 0.11f));
            MakePrimitive(PrimitiveType.Cube, "スコアボード", new Vector3(0f, 7f, 42.5f), new Vector3(9f, 4f, 0.35f), new Color(0.03f, 0.05f, 0.08f));
            var sun = new GameObject("太陽").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private void CreatePlayers()
        {
            pitcher = MakePlayer("CPU ピッチャー", new Vector3(0f, 0f, 18.44f), new Color(0.2f, 0.42f, 0.95f));
            batter = MakePlayer("あなた（打者）", new Vector3(-0.9f, 0f, -0.25f), new Color(0.93f, 0.23f, 0.16f));
            MakePlayer("キャッチャー", new Vector3(0f, 0f, -1.4f), new Color(0.12f, 0.12f, 0.15f));
            MakePlayer("審判", new Vector3(0.75f, 0f, -2.1f), new Color(0.08f, 0.08f, 0.1f));
            runner = MakePlayer("走者", new Vector3(-0.4f, 0f, 0.9f), new Color(0.98f, 0.72f, 0.1f));
            runner.gameObject.SetActive(false);
        }

        private void CreateBall()
        {
            ball = MakePrimitive(PrimitiveType.Sphere, "ボール", new Vector3(0f, 1.8f, 17.7f), Vector3.one * 0.0732f, Color.white).transform;
            ballBody = ball.gameObject.AddComponent<Rigidbody>();
            ballBody.mass = 0.145f;
            ballBody.useGravity = false;
            ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ballStart = ball.position;
        }

        private void SetupCamera()
        {
            gameCamera = new GameObject("YNA ACE 三人称カメラ").AddComponent<Camera>();
            gameCamera.depth = 10;
            gameCamera.transform.position = new Vector3(0f, 3.4f, -8.5f);
            gameCamera.transform.LookAt(new Vector3(0f, 1.3f, 10f));
            gameCamera.fieldOfView = 58f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.P)) paused = !paused;
            if (paused) return;
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleRole();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) Swing();
            if (Input.GetKeyDown(KeyCode.A)) MoveRunner(-1f);
            if (Input.GetKeyDown(KeyCode.D)) MoveRunner(1f);
            if (!pitching && role == "打者" && Input.GetKeyDown(KeyCode.Return)) StartCoroutine(ThrowPitch());
            AnimatePlayers();
        }

        private IEnumerator ThrowPitch()
        {
            pitching = true;
            swung = false;
            ballBody.isKinematic = true;
            ballBody.useGravity = false;
            ballStart = new Vector3(0f, 1.85f, 17.5f);
            ball.position = ballStart;
            call = "ピッチャーがうなずいた……投球！";
            yield return new WaitForSeconds(0.75f);
            pitchStart = Time.time;
            pitchDuration = Random.Range(0.72f, 1.25f);
            while (Time.time - pitchStart < pitchDuration)
            {
                float t = (Time.time - pitchStart) / pitchDuration;
                Vector3 next = Vector3.Lerp(ballStart, target, t);
                next.y += Mathf.Sin(t * Mathf.PI) * 0.65f;
                ball.position = next;
                yield return null;
            }
            if (!swung) CountPitch(false);
            pitching = false;
        }

        private void Swing()
        {
            if (!pitching || swung || role != "打者") return;
            swung = true;
            float timing = Mathf.Abs((Time.time - pitchStart) / pitchDuration - 0.86f);
            float distance = Vector3.Distance(ball.position, new Vector3(-0.15f, 1.05f, 0.35f));
            if (timing < 0.18f && distance < 2.1f)
            {
                StopAllCoroutines();
                pitching = false;
                ballBody.isKinematic = false;
                ballBody.useGravity = true;
                ballBody.velocity = new Vector3(Random.Range(-7f, 7f), Random.Range(7f, 11f), Random.Range(12f, 20f));
                runner.gameObject.SetActive(true);
                call = timing < 0.07f ? "完璧なタイミング！ 強い打球が飛んだ！" : "打った！ フェアグラウンドへ！";
                StartCoroutine(FieldBall());
            }
            else
            {
                call = "空振り！";
                CountPitch(true);
                pitching = false;
                StopAllCoroutines();
            }
        }

        private IEnumerator FieldBall()
        {
            yield return new WaitForSeconds(0.65f);
            if (ball.position.z < 24f && ball.position.y < 3.5f)
            {
                call = "ピッチャーが前へ！ 打球を処理しました。アウト！";
                outs = Mathf.Min(3, outs + 1);
                ResetBall();
            }
            else
            {
                score++;
                call = "打球は抜けた！ 1点追加！";
                ResetBall();
            }
        }

        private void CountPitch(bool swingMiss)
        {
            bool inZone = Mathf.Abs(target.x) < 0.72f && target.y > 0.48f && target.y < 1.28f;
            if (swingMiss || inZone) strikes++; else balls++;
            if (strikes >= 3) { outs = Mathf.Min(3, outs + 1); call = "三振！"; strikes = 0; balls = 0; }
            if (balls >= 4) { call = "フォアボール。走者が出ました。"; balls = 0; strikes = 0; runner.gameObject.SetActive(true); }
            ResetBall();
        }

        private void ResetBall()
        {
            StopAllCoroutines();
            pitching = false;
            ballBody.isKinematic = true;
            ballBody.useGravity = false;
            ball.position = ballStart;
        }

        private void MoveRunner(float direction)
        {
            if (!runner.gameObject.activeSelf) return;
            runner.position += new Vector3(direction * 0.55f, 0f, 0.55f);
            call = direction > 0 ? "右足で前へ。走者が進む！" : "左足で姿勢を整える。";
        }

        private void ToggleRole()
        {
            role = role == "打者" ? "投手" : "打者";
            call = role == "投手" ? "あなたは投手です。ゾーンを選んで投げます。" : "あなたは打者です。ENTER で CPU が投球します。";
        }

        private void AnimatePlayers()
        {
            float sway = Mathf.Sin(Time.time * 3f) * 2f;
            batter.localRotation = Quaternion.Euler(0f, 180f + sway, 0f);
            pitcher.localRotation = Quaternion.Euler(0f, sway, 0f);
            if (pitching) pitcher.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 12f) * 10f, 0f, 0f);
        }

        private void OnGUI()
        {
            if (titleStyle == null) ConfigureGui();
            GUI.Label(new Rect(24, 18, 500, 44), "YNA ACE  UNITY WEBGL", titleStyle);
            GUI.Box(new Rect(24, 72, 360, 120), "", panelStyle);
            GUI.Label(new Rect(40, 84, 330, 96), "役割：" + role + "\nスコア：あなた " + score + " - CPU 0\nB " + balls + "   S " + strikes + "   O " + outs + "\n" + call, textStyle);
            GUI.Box(new Rect(Screen.width - 224, 22, 200, 250), "", panelStyle);
            GUI.Label(new Rect(Screen.width - 208, 34, 168, 26), "狙うストライクゾーン", textStyle);
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
            {
                Rect cell = new Rect(Screen.width - 204 + x * 34, 70 + y * 34, 30, 30);
                bool strike = x is >= 1 and <= 3 && y is >= 1 and <= 3;
                Color old = GUI.color;
                GUI.color = strike ? new Color(0.8f, 0.92f, 1f) : new Color(0.5f, 0.55f, 0.62f);
                if (GUI.Button(cell, ""))
                {
                    target = new Vector3((x - 2) * 0.38f, 1.38f - y * 0.2f, 0.25f);
                    if (role == "投手" && !pitching) StartCoroutine(ThrowPitch());
                    else if (role == "打者" && !pitching) call = "狙いを定めました。ENTER で投球を待ちます。";
                }
                GUI.color = old;
            }
            GUI.Label(new Rect(Screen.width - 208, 248, 180, 22), paused ? "一時停止中（Pで再開）" : "SPACE/クリック：スイング", textStyle);
        }

        private void ConfigureGui()
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(0.02f, 0.04f, 0.09f, 0.82f)) } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = Color.white } };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture;
        }

        private static GameObject MakePrimitive(PrimitiveType type, string label, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(type); item.name = label; item.transform.position = position; item.transform.localScale = scale;
            item.GetComponent<Renderer>().material.color = color; return item;
        }

        private static Transform MakePlayer(string label, Vector3 position, Color uniform)
        {
            var root = new GameObject(label); root.transform.position = position;
            var body = MakePrimitive(PrimitiveType.Capsule, label + " 本体", position + Vector3.up, new Vector3(0.62f, 1f, 0.62f), uniform); body.transform.SetParent(root.transform);
            var head = MakePrimitive(PrimitiveType.Sphere, label + " 頭", position + Vector3.up * 2.05f, Vector3.one * 0.42f, new Color(0.92f, 0.67f, 0.48f)); head.transform.SetParent(root.transform);
            return root.transform;
        }
    }
}
