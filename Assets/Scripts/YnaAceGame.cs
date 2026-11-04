using System.Collections;
using System.Threading.Tasks;

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
        private int playerScore;
        private int cpuScore;
        private int inning = 1;
        private bool playerBatting = true;
        private bool gameOver;
        private string role = "打者";
        private string call = "ストライクゾーンを選び、来た球を SPACE またはクリックで打ってください。";
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;
        private Unity.Cloud.Gltfast.GltfImport playerModel;

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
            LoadRealPlayers();
            CreateBall();
            SetupCamera();
            BeginHalfInning();
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

        private async void LoadRealPlayers()
        {
            var model = new Unity.Cloud.Gltfast.GltfImport();
            string modelUrl = Application.streamingAssetsPath + "/Players/FoodCourier.glb";
            if (!await model.LoadAsync(modelUrl))
            {
                call = "人型モデルを読み込めなかったため、仮モデルで続行します。";
                return;
            }

            playerModel = model;
            await ReplaceWithRealModel(pitcher);
            await ReplaceWithRealModel(batter);
            await ReplaceWithRealModel(runner);
        }

        private async Task ReplaceWithRealModel(Transform actor)
        {
            foreach (Transform child in actor) child.gameObject.SetActive(false);
            var visual = new GameObject(actor.name + " リアルモデル");
            visual.transform.SetParent(actor, false);
            visual.transform.localScale = Vector3.one * 1.15f;
            await playerModel.InstantiateMainSceneAsync(visual.transform);
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
            if (gameOver)
            {
                if (Input.GetKeyDown(KeyCode.R)) RestartGame();
                return;
            }
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
            if (!swung)
            {
                if (playerBatting) CountPitch(false);
                else ResolveCpuAtPlate();
            }
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
                ballBody.linearVelocity = new Vector3(Random.Range(-7f, 7f), Random.Range(7f, 11f), Random.Range(12f, 20f));
                call = timing < 0.07f ? "完璧なタイミング！ 強い打球が飛んだ！" : "打った！ フェアグラウンドへ！";
                StartCoroutine(FieldBall(true));
            }
            else
            {
                call = "空振り！";
                CountPitch(true);
                pitching = false;
            }
        }

        private void ResolveCpuAtPlate()
        {
            bool inZone = IsStrikeZone();
            float decision = Random.value;
            if (!inZone && decision < 0.42f)
            {
                balls++;
                call = "CPU打者は見送った。ボール。";
                if (balls >= 4) { runner.gameObject.SetActive(true); call = "CPU打者、フォアボール。"; ResetCount(); }
                ResetBall();
                return;
            }
            if (decision < 0.28f)
            {
                strikes++;
                call = "CPU打者、空振り！";
                if (strikes >= 3) RegisterOut("三振！ CPU打者を打ち取りました。");
                ResetBall();
                return;
            }
            if (decision < 0.68f)
            {
                ballBody.isKinematic = false;
                ballBody.useGravity = true;
                ballBody.linearVelocity = new Vector3(Random.Range(-6f, 6f), Random.Range(6f, 10f), Random.Range(11f, 19f));
                call = "CPU打者が打った！ 守備へ！";
                StartCoroutine(FieldBall(false));
                return;
            }
            strikes = Mathf.Min(2, strikes + 1);
            call = "ファウル。追い込まれました。";
            ResetBall();
        }

        private IEnumerator FieldBall(bool playerHit)
        {
            float until = Time.time + 1.4f;
            while (Time.time < until)
            {
                pitcher.position = Vector3.MoveTowards(pitcher.position, new Vector3(ball.position.x, 0f, ball.position.z), Time.deltaTime * 5f);
                yield return null;
            }
            bool fielded = ball.position.z < 25f && ball.position.y < 2.5f;
            if (fielded)
            {
                RegisterOut("ピッチャーが前へ！ 打球を処理しました。アウト！");
            }
            else
            {
                if (playerHit) playerScore++; else cpuScore++;
                runner.gameObject.SetActive(true);
                call = playerHit ? "打球は抜けた！ あなたに1点追加！" : "打球が外野を抜けた。CPUに1点。";
            }
            ResetBall();
        }

        private void CountPitch(bool swingMiss)
        {
            bool inZone = IsStrikeZone();
            if (swingMiss || inZone) strikes++; else balls++;
            if (strikes >= 3) RegisterOut("三振！");
            if (balls >= 4) { call = "フォアボール。走者が出ました。"; runner.gameObject.SetActive(true); ResetCount(); }
            ResetBall();
        }

        private void ResetBall()
        {
            pitching = false;
            ballBody.isKinematic = true;
            ballBody.useGravity = false;
            ball.position = ballStart;
        }

        private bool IsStrikeZone() => Mathf.Abs(target.x) < 0.72f && target.y > 0.48f && target.y < 1.28f;

        private void RegisterOut(string message)
        {
            outs++;
            call = message;
            ResetCount();
            if (outs >= 3) StartCoroutine(ChangeHalfInning());
        }

        private IEnumerator ChangeHalfInning()
        {
            yield return new WaitForSeconds(1.6f);
            if (playerBatting && inning >= 9 && playerScore < cpuScore)
            {
                FinishGame();
                yield break;
            }
            if (!playerBatting && inning >= 9 && playerScore != cpuScore)
            {
                FinishGame();
                yield break;
            }
            if (!playerBatting) inning++;
            playerBatting = !playerBatting;
            BeginHalfInning();
        }

        private void BeginHalfInning()
        {
            outs = 0;
            ResetCount();
            runner.gameObject.SetActive(false);
            pitcher.position = new Vector3(0f, 0f, 18.44f);
            role = playerBatting ? "打者" : "投手";
            call = inning + "回" + (playerBatting ? "表：あなたの攻撃です。ENTERで投球を待ちます。" : "裏：あなたの投球です。ゾーンを選んで投げてください。");
        }

        private void ResetCount()
        {
            strikes = 0;
            balls = 0;
        }

        private void FinishGame()
        {
            gameOver = true;
            role = "試合終了";
            call = playerScore > cpuScore ? "ゲームセット！ あなたの勝ちです。Rで再戦。" : "ゲームセット。CPUの勝ちです。Rで再戦。";
        }

        private void RestartGame()
        {
            StopAllCoroutines();
            playerScore = 0;
            cpuScore = 0;
            inning = 1;
            playerBatting = true;
            gameOver = false;
            ResetBall();
            BeginHalfInning();
        }

        private void MoveRunner(float direction)
        {
            if (!runner.gameObject.activeSelf) return;
            runner.position += new Vector3(direction * 0.55f, 0f, 0.55f);
            call = direction > 0 ? "右足で前へ。走者が進む！" : "左足で姿勢を整える。";
        }

        private void AnimatePlayers()
        {
            float sway = Mathf.Sin(Time.time * 3f) * 2f;
            batter.localRotation = Quaternion.Euler(0f, 180f + sway, 0f);
            pitcher.localRotation = Quaternion.Euler(0f, sway, 0f);
            if (pitching) pitcher.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 12f) * 10f, 0f, 0f);
            AnimateActor(pitcher, Time.time * 5f, pitching);
            AnimateActor(batter, Time.time * 4f + 1f, pitching && role == "打者");
            if (runner.gameObject.activeSelf) AnimateActor(runner, Time.time * 9f, true);
        }

        private static void AnimateActor(Transform actor, float clock, bool active)
        {
            var leftArm = actor.Find("腕L");
            var rightArm = actor.Find("腕R");
            var leftLeg = actor.Find("脚L");
            var rightLeg = actor.Find("脚R");
            if (leftArm == null || rightArm == null || leftLeg == null || rightLeg == null) return;
            float step = active ? Mathf.Sin(clock) * 24f : Mathf.Sin(clock * 0.5f) * 3f;
            leftArm.localRotation = Quaternion.Euler(-step, 0f, -8f);
            rightArm.localRotation = Quaternion.Euler(step, 0f, 8f);
            leftLeg.localRotation = Quaternion.Euler(step * 0.65f, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-step * 0.65f, 0f, 0f);
        }

        private void OnGUI()
        {
            if (titleStyle == null) ConfigureGui();
            GUI.Label(new Rect(24, 18, 500, 44), "YNA ACE  UNITY WEBGL", titleStyle);
            GUI.Box(new Rect(24, 72, 390, 150), "", panelStyle);
            GUI.Label(new Rect(40, 84, 360, 132), "あなたの役：" + role + "\n" + inning + "回" + (playerBatting ? "表" : "裏") + "\n\n" + call, textStyle);

            // 元のWeb版に合わせた右上のラインスコアとカウント。
            GUI.Box(new Rect(Screen.width - 390, 22, 366, 150), "", panelStyle);
            GUI.Label(new Rect(Screen.width - 374, 34, 340, 26), "イニング　1 2 3 4 5 6 7 8 9　計", smallStyle);
            GUI.Label(new Rect(Screen.width - 374, 65, 340, 28), "あなた　　" + ScoreLine(playerScore) + "　" + playerScore, textStyle);
            GUI.Label(new Rect(Screen.width - 374, 95, 340, 28), "CPU　　　" + ScoreLine(cpuScore) + "　" + cpuScore, textStyle);
            GUI.Label(new Rect(Screen.width - 374, 130, 340, 28), "B " + balls + "　S " + strikes + "　O " + outs + "　　" + (runner.gameObject.activeSelf ? "走者あり" : "走者なし"), smallStyle);

            int gridSize = Mathf.Clamp(Screen.width / 14, 42, 62);
            int gridWidth = gridSize * 5 + 4 * 4;
            int gridX = (Screen.width - gridWidth) / 2;
            int gridY = Screen.height - gridSize * 5 - 110;
            GUI.Label(new Rect(gridX, gridY - 30, gridWidth, 26), role == "投手" ? "投げるコース" : "打つコース（予想位置）", textStyle);
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
            {
                Rect cell = new Rect(gridX + x * (gridSize + 4), gridY + y * (gridSize + 4), gridSize, gridSize);
                bool strike = x is >= 1 and <= 3 && y is >= 1 and <= 3;
                Color old = GUI.color;
                GUI.color = strike ? new Color(0.24f, 0.58f, 0.82f, 0.95f) : new Color(0.05f, 0.1f, 0.16f, 0.9f);
                if (GUI.Button(cell, ""))
                {
                    target = new Vector3((x - 2) * 0.38f, 1.38f - y * 0.2f, 0.25f);
                    if (role == "投手" && !pitching && !gameOver) StartCoroutine(ThrowPitch());
                    else if (role == "打者" && !pitching) call = "狙いを定めました。ENTER で投球を待ちます。";
                }
                GUI.color = old;
            }
            GUI.Label(new Rect(gridX, gridY + gridSize * 5 + 16, gridWidth, 26), gameOver ? "R：再戦" : (paused ? "一時停止中（Pで再開）" : "SPACE/クリック：スイング　　P：一時停止　　A/D：走塁"), smallStyle);
        }

        private static string ScoreLine(int score)
        {
            return score == 0 ? "-  -  -  -  -  -  -  -  -" : "-  -  -  -  " + score + "  -  -  -  -";
        }

        private void ConfigureGui()
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(0.02f, 0.04f, 0.09f, 0.82f)) } };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white } };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture;
        }

        private static GameObject MakePrimitive(PrimitiveType type, string label, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(type); item.name = label; item.transform.position = position; item.transform.localScale = scale;
            var renderer = item.GetComponent<Renderer>();
            // Resourcesに含めたWebGL対応マテリアルを使い、シェーダー stripping による
            // マゼンタ表示を防ぎます。フォールバックも残してEditor実行時の復旧性を確保します。
            var baseMaterial = Resources.Load<Material>("YnaAceMaterial");
            if (baseMaterial != null)
            {
                renderer.material = new Material(baseMaterial);
            }
            else
            {
                var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                if (shader != null) renderer.material.shader = shader;
            }
            renderer.material.color = color;
            return item;
        }

        private static Transform MakePlayer(string label, Vector3 position, Color uniform)
        {
            var root = new GameObject(label); root.transform.position = position;
            MakePart(root.transform, PrimitiveType.Capsule, "胴体", new Vector3(0f, 1.05f, 0f), new Vector3(0.62f, 0.78f, 0.42f), uniform, Vector3.zero);
            MakePart(root.transform, PrimitiveType.Sphere, "頭", new Vector3(0f, 2.15f, 0f), Vector3.one * 0.42f, new Color(0.92f, 0.67f, 0.48f), Vector3.zero);
            MakePart(root.transform, PrimitiveType.Capsule, "腕L", new Vector3(-0.48f, 1.15f, 0f), new Vector3(0.18f, 0.55f, 0.18f), uniform, new Vector3(0f, 0f, -12f));
            MakePart(root.transform, PrimitiveType.Capsule, "腕R", new Vector3(0.48f, 1.15f, 0f), new Vector3(0.18f, 0.55f, 0.18f), uniform, new Vector3(0f, 0f, 12f));
            MakePart(root.transform, PrimitiveType.Capsule, "脚L", new Vector3(-0.22f, 0.05f, 0f), new Vector3(0.22f, 0.65f, 0.22f), uniform, Vector3.zero);
            MakePart(root.transform, PrimitiveType.Capsule, "脚R", new Vector3(0.22f, 0.05f, 0f), new Vector3(0.22f, 0.65f, 0.22f), uniform, Vector3.zero);
            MakePart(root.transform, PrimitiveType.Cylinder, "帽子", new Vector3(0f, 2.52f, 0f), new Vector3(0.46f, 0.12f, 0.46f), uniform * 0.8f, Vector3.zero);
            MakePart(root.transform, PrimitiveType.Sphere, "グローブ", new Vector3(0.66f, 1.03f, 0.14f), Vector3.one * 0.19f, new Color(0.2f, 0.09f, 0.03f), Vector3.zero);
            return root.transform;
        }

        private static GameObject MakePart(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 scale, Color color, Vector3 rotation)
        {
            var part = MakePrimitive(type, name, parent.position, scale, color);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(rotation);
            part.transform.localScale = scale;
            return part;
        }
    }
}
