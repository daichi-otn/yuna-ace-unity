using System.Collections;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem;

namespace YnaAce
{
    /// <summary>WebGL でも追加アセットなしで動く、YNA ACE の最初の3Dプレイアブル版です。</summary>
    public sealed class YnaAceGame : MonoBehaviour
    {
        private Camera gameCamera;
        private Transform pitcher;
        private Transform batter;
        private Transform runner;
        private Transform catcher;
        private Transform umpire;
        private Transform ball;
        private Rigidbody ballBody;
        private Vector3 ballStart;
        private Vector3 target = new Vector3(0f, 1f, 0.2f);
        private float pitchStart;
        private float pitchDuration;
        private bool pitching;
        private bool pitchInFlight;
        private bool ballInPlay;
        private bool paused;
        private bool swung;
        private int strikes;
        private int balls;
        private int outs;
        private int playerScore;
        private int cpuScore;
        private readonly int[] playerInningRuns = new int[30];
        private readonly int[] cpuInningRuns = new int[30];
        private readonly bool[] bases = new bool[3];
        private int inning = 1;
        private bool playerBatting = true;
        private bool gameOver;
        private bool changingHalf;
        private string role = "打者";
        private string call = "ストライクゾーンを選び、来た球を SPACE またはクリックで打ってください。";
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;
        private GUIStyle targetStyle;
        private Unity.Cloud.Gltfast.GltfImport playerModel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindAnyObjectByType<YnaAceGame>() == null)
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
            catcher = MakePlayer("キャッチャー", new Vector3(0f, 0f, -1.4f), new Color(0.12f, 0.12f, 0.15f));
            umpire = MakePlayer("審判", new Vector3(0.75f, 0f, -2.1f), new Color(0.08f, 0.08f, 0.1f));
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
            await ReplaceWithRealModel(pitcher, new Color(0.12f, 0.3f, 0.9f));
            await ReplaceWithRealModel(batter, new Color(0.85f, 0.12f, 0.08f), true);
            await ReplaceWithRealModel(runner, new Color(0.95f, 0.65f, 0.08f));
            await ReplaceWithRealModel(catcher, new Color(0.08f, 0.1f, 0.16f));
            await ReplaceWithRealModel(umpire, new Color(0.03f, 0.03f, 0.04f));
        }

        private async Task ReplaceWithRealModel(Transform actor, Color uniformColor, bool addBat = false)
        {
            var visual = new GameObject(actor.name + " リアルモデル");
            visual.transform.SetParent(actor, false);
            visual.transform.localScale = Vector3.one;
            bool instantiated = await playerModel.InstantiateMainSceneAsync(visual.transform);
            if (!instantiated)
            {
                Destroy(visual);
                call = "人型モデルの生成に失敗したため、仮モデルで続行します。";
                return;
            }

            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Destroy(visual);
                call = "人型モデルの表示に失敗したため、仮モデルで続行します。";
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y < 0.01f)
            {
                Destroy(visual);
                call = "人型モデルの寸法を取得できなかったため、仮モデルで続行します。";
                return;
            }

            // glTF素材ごとの単位差を吸収し、実寸約1.8mに統一して足元を地面へ合わせる。
            float modelScale = 1.8f / bounds.size.y;
            visual.transform.localScale = Vector3.one * modelScale;
            visual.transform.localPosition = new Vector3(0f, -bounds.min.y * modelScale, 0f);
            ApplyRoleMaterials(visual, uniformColor);
            PlayModelAnimation(visual);
            if (addBat) AddBat(visual.transform, uniformColor);

            // 正しい大きさの実モデルが生成できた後で、カプセル仮モデルだけを隠す。
            foreach (Transform child in actor)
                if (child != visual.transform) child.gameObject.SetActive(false);
        }

        private static void ApplyRoleMaterials(GameObject visual, Color uniformColor)
        {
            var webGlMaterial = Resources.Load<Material>("YnaAceMaterial");
            if (webGlMaterial == null) return;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var importedMaterials = renderer.materials;
                var safeMaterials = new Material[importedMaterials.Length];
                for (int i = 0; i < importedMaterials.Length; i++)
                {
                    var imported = importedMaterials[i];
                    var safe = new Material(webGlMaterial) { color = uniformColor };
                    if (imported != null)
                    {
                        Texture colorTexture = null;
                        if (imported.HasProperty("baseColorTexture"))
                            colorTexture = imported.GetTexture("baseColorTexture");
                        else if (imported.HasProperty("_BaseMap"))
                            colorTexture = imported.GetTexture("_BaseMap");
                        else if (imported.HasProperty("_MainTex"))
                            colorTexture = imported.GetTexture("_MainTex");

                        if (colorTexture != null)
                        {
                            if (safe.HasProperty("_BaseMap")) safe.SetTexture("_BaseMap", colorTexture);
                            else if (safe.HasProperty("_MainTex")) safe.SetTexture("_MainTex", colorTexture);
                        }
                    }
                    safeMaterials[i] = safe;
                }
                renderer.materials = safeMaterials;
            }
        }

        private static void PlayModelAnimation(GameObject visual)
        {
            var animation = visual.GetComponentInChildren<Animation>(true);
            if (animation != null && animation.clip != null) animation.Play(animation.clip.name);
        }

        private static void AddBat(Transform parent, Color uniformColor)
        {
            var bat = MakePrimitive(PrimitiveType.Cylinder, "バット", parent.position, new Vector3(0.06f, 0.65f, 0.06f), new Color(0.35f, 0.12f, 0.03f));
            bat.transform.SetParent(parent, false);
            bat.transform.localPosition = new Vector3(0.55f, 1.15f, 0.16f);
            bat.transform.localRotation = Quaternion.Euler(20f, 0f, 68f);
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
            if (KeyPressed(Key.P))
            {
                paused = !paused;
                Time.timeScale = paused ? 0f : 1f;
            }
            if (paused) return;
            if (gameOver)
            {
                if (KeyPressed(Key.R)) RestartGame();
                return;
            }
            if (KeyPressed(Key.Space) || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)) Swing();
            if (KeyPressed(Key.A)) MoveRunner(-1f);
            if (KeyPressed(Key.D)) MoveRunner(1f);
            if (!pitching && !ballInPlay && !changingHalf && role == "打者" && KeyPressed(Key.Enter)) StartCoroutine(ThrowPitch());
            AnimatePlayers();
        }

        private static bool KeyPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

        private IEnumerator ThrowPitch()
        {
            pitching = true;
            pitchInFlight = false;
            swung = false;
            ballBody.isKinematic = true;
            ballBody.useGravity = false;
            ballStart = new Vector3(0f, 1.85f, 17.5f);
            ball.position = ballStart;
            call = "ピッチャーがうなずいた。1.8秒後に投球します。";
            yield return new WaitForSeconds(1.8f);
            pitchStart = Time.time;
            pitchDuration = Random.Range(0.72f, 1.25f);
            pitchInFlight = true;
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
            pitchInFlight = false;
            swung = false;
        }

        private void Swing()
        {
            if (!pitching || !pitchInFlight || swung || role != "打者") return;
            swung = true;
            float timing = Mathf.Abs((Time.time - pitchStart) / pitchDuration - 0.86f);
            float distance = Vector3.Distance(ball.position, new Vector3(-0.15f, 1.05f, 0.35f));
            if (timing < 0.18f && distance < 2.1f)
            {
                StopAllCoroutines();
                pitching = false;
                pitchInFlight = false;
                ballInPlay = true;
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
                if (balls >= 4) AwardWalk(false);
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
                ballInPlay = true;
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
                bool[] oldBases = (bool[])bases.Clone();
                System.Array.Clear(bases, 0, bases.Length);
                int runs = 0;
                for (int i = 0; i < oldBases.Length; i++)
                {
                    if (!oldBases[i]) continue;
                    int destination = i + 1;
                    if (destination >= bases.Length) runs++;
                    else bases[destination] = true;
                }
                bases[0] = true;
                AddRuns(playerHit, runs);
                runner.gameObject.SetActive(true);
                SyncRunnerVisual();
                call = runs > 0 ? "走者が生還！ " + runs + "点入りました。打者は一塁へ。" : "打球が抜けた！ 打者は一塁、走者は進塁！";
            }
            ResetBall();
            ballInPlay = false;
            swung = false;
        }

        private void CountPitch(bool swingMiss)
        {
            bool inZone = IsStrikeZone();
            if (swingMiss || inZone) strikes++; else balls++;
            if (strikes >= 3) RegisterOut("三振！");
            else if (balls >= 4) AwardWalk(playerBatting);
            ResetBall();
        }

        private void AwardWalk(bool playerOnOffense)
        {
            if (bases[0] && bases[1] && bases[2])
            {
                AddRuns(playerOnOffense, 1);
            }
            if (bases[0] && bases[1]) bases[2] = true;
            if (bases[0]) bases[1] = true;
            bases[0] = true;
            runner.gameObject.SetActive(true);
            SyncRunnerVisual();
            call = "フォアボール。押し出しを含め走者が進塁しました。";
            ResetCount();
        }

        private void AddRuns(bool playerOnOffense, int runs)
        {
            if (runs <= 0) return;
            int index = Mathf.Clamp(inning - 1, 0, playerInningRuns.Length - 1);
            if (playerOnOffense)
            {
                playerScore += runs;
                playerInningRuns[index] += runs;
            }
            else
            {
                cpuScore += runs;
                cpuInningRuns[index] += runs;
            }
        }

        private void ResetBall()
        {
            pitching = false;
            pitchInFlight = false;
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
            if (outs >= 3 && !changingHalf) StartCoroutine(ChangeHalfInning());
        }

        private IEnumerator ChangeHalfInning()
        {
            changingHalf = true;
            yield return new WaitForSeconds(1.6f);
            // 9回表終了時、後攻が既にリードしていれば9回裏を行わず終了。
            if (playerBatting && inning >= 9 && cpuScore > playerScore)
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
            changingHalf = false;
        }

        private void BeginHalfInning()
        {
            outs = 0;
            ResetCount();
            ballInPlay = false;
            runner.gameObject.SetActive(false);
            System.Array.Clear(bases, 0, bases.Length);
            runner.position = new Vector3(-0.4f, 0f, 0.9f);
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
            changingHalf = false;
            ballInPlay = false;
            System.Array.Clear(bases, 0, bases.Length);
            System.Array.Clear(playerInningRuns, 0, playerInningRuns.Length);
            System.Array.Clear(cpuInningRuns, 0, cpuInningRuns.Length);
            paused = false;
            Time.timeScale = 1f;
            ResetBall();
            BeginHalfInning();
        }

        private void MoveRunner(float direction)
        {
            if (!runner.gameObject.activeSelf || role != "打者" || pitching || ballInPlay) return;
            int occupied = -1;
            for (int i = 2; i >= 0; i--) if (bases[i]) { occupied = i; break; }
            if (occupied < 0) { call = "走者はいません。"; return; }
            if (direction > 0)
            {
                if (occupied < 2 && bases[occupied + 1])
                {
                    call = "前の塁が埋まっています。打球を待ちましょう。";
                    return;
                }
                bases[occupied] = false;
                if (occupied == 2) AddRuns(true, 1);
                else bases[occupied + 1] = true;
                call = occupied == 2 ? "走者がホームイン！ 1点追加！" : "走者が次の塁へ進みました。";
                SyncRunnerVisual();
            }
            else
            {
                Vector3 nextBase = occupied == 2 ? new Vector3(0f, 0f, 0f) : BasePosition(occupied + 1);
                runner.position += (nextBase - runner.position).normalized * 0.55f;
                call = "左足でリードを取りました。Dで次の塁へ走れます。";
            }
        }

        private void SyncRunnerVisual()
        {
            int visibleBase = -1;
            for (int i = bases.Length - 1; i >= 0; i--)
                if (bases[i]) { visibleBase = i; break; }
            runner.gameObject.SetActive(visibleBase >= 0);
            if (visibleBase >= 0) runner.position = BasePosition(visibleBase);
        }

        private static Vector3 BasePosition(int baseIndex)
        {
            return baseIndex switch
            {
                0 => new Vector3(8f, 0f, 8f),
                1 => new Vector3(0f, 0f, 16f),
                _ => new Vector3(-8f, 0f, 8f)
            };
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
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
            Matrix4x4 previousMatrix = GUI.matrix;
            float uiScale = Mathf.Min(1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            uiScale = Mathf.Max(0.35f, uiScale);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            float uiWidth = Screen.width / uiScale;
            float uiHeight = Screen.height / uiScale;
            GUI.Label(new Rect(24, 18, 500, 44), "YNA ACE  UNITY WEBGL", titleStyle);
            GUI.Box(new Rect(24, 72, 390, 150), "", panelStyle);
            GUI.Label(new Rect(40, 84, 360, 132), "あなたの役：" + role + "\n" + inning + "回" + (playerBatting ? "表" : "裏") + "\n\n" + call, textStyle);

            // 元のWeb版に合わせた右上のラインスコアとカウント。
            GUI.Box(new Rect(uiWidth - 390, 22, 366, 150), "", panelStyle);
            GUI.Label(new Rect(uiWidth - 374, 34, 340, 26), "イニング　1 2 3 4 5 6 7 8 9　計", smallStyle);
            GUI.Label(new Rect(uiWidth - 374, 65, 340, 28), "あなた　　" + ScoreLine(playerInningRuns) + "計 " + playerScore, textStyle);
            GUI.Label(new Rect(uiWidth - 374, 95, 340, 28), "CPU　　　" + ScoreLine(cpuInningRuns) + "計 " + cpuScore, textStyle);
            GUI.Label(new Rect(uiWidth - 374, 130, 340, 28), "B " + balls + "　S " + strikes + "　O " + outs + "　塁 " + (bases[2] ? "●" : "○") + (bases[1] ? "●" : "○") + (bases[0] ? "●" : "○"), smallStyle);

            int gridSize = Mathf.Clamp((int)(uiWidth / 14f), 42, 62);
            int gridWidth = gridSize * 5 + 4 * 4;
            int gridX = (int)((uiWidth - gridWidth) / 2f);
            int gridY = (int)(uiHeight - gridSize * 5 - 110);
            GUI.Label(new Rect(gridX, gridY - 30, gridWidth, 26), role == "投手" ? "投げるコース" : "打つコース（予想位置）", textStyle);
            for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
            {
                Rect cell = new Rect(gridX + x * (gridSize + 4), gridY + y * (gridSize + 4), gridSize, gridSize);
                bool strike = x is >= 1 and <= 3 && y is >= 1 and <= 3;
                Color old = GUI.color;
                GUI.color = strike ? new Color(0.24f, 0.58f, 0.82f, 0.95f) : new Color(0.05f, 0.1f, 0.16f, 0.9f);
                if (GUI.Button(cell, ""))
                {
                    if (!pitching && !ballInPlay && !changingHalf && !gameOver)
                    {
                        target = new Vector3((x - 2) * 0.38f, 1.38f - y * 0.2f, 0.25f);
                        if (role == "投手") StartCoroutine(ThrowPitch());
                        else if (role == "打者") call = "狙いを定めました。ENTER または投球ボタンで開始。";
                    }
                }
                GUI.color = old;
                bool isSelected = Mathf.Abs(target.x - (x - 2) * 0.38f) < 0.01f && Mathf.Abs(target.y - (1.38f - y * 0.2f)) < 0.01f;
                if (isSelected) GUI.Label(cell, "●", targetStyle);
            }
            Rect actionRect = new Rect(gridX, gridY + gridSize * 5 + 16, gridWidth, 32);
            if (role == "打者" && !pitching && !ballInPlay && !changingHalf && !gameOver)
            {
                if (GUI.Button(actionRect, "投球を待つ（ENTER）")) StartCoroutine(ThrowPitch());
            }
            else
            {
                GUI.Label(actionRect, gameOver ? "R：再戦" : (paused ? "一時停止中（Pで再開）" : (ballInPlay ? "打球処理中" : "SPACE/クリック：スイング　P：停止　A/D：走塁")), smallStyle);
            }
            GUI.matrix = previousMatrix;
        }

        private string ScoreLine(int[] runs)
        {
            var innings = new System.Text.StringBuilder();
            int shown = Mathf.Min(Mathf.Max(9, inning), runs.Length);
            for (int i = 1; i <= shown; i++)
                innings.Append(runs[i - 1]).Append(' ');
            return innings.ToString();
        }

        private void ConfigureGui()
        {
            var japaneseFont = Resources.Load<Font>("NotoSansJP");
            titleStyle = new GUIStyle(GUI.skin.label) { font = japaneseFont, fontSize = 26, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTexture(new Color(0.02f, 0.04f, 0.09f, 0.82f)) } };
            textStyle = new GUIStyle(GUI.skin.label) { font = japaneseFont, fontSize = 15, wordWrap = true, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { font = japaneseFont, fontSize = 13, normal = { textColor = Color.white } };
            targetStyle = new GUIStyle(GUI.skin.label) { font = japaneseFont, fontSize = 24, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.78f, 0.18f) } };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture;
        }

        private static GameObject MakePrimitive(PrimitiveType type, string label, Vector3 position, Vector3 scale, Color color)
        {
            var item = GameObject.CreatePrimitive(type); item.name = label; item.transform.position = position; item.transform.localScale = scale;
            // WebGLのmanaged strippingでCreatePrimitiveが内部追加するCollider型が
            // 除外されないよう、球とカプセルの型をこのアセンブリから明示参照します。
            if (type == PrimitiveType.Sphere && item.GetComponent<SphereCollider>() == null)
                item.AddComponent<SphereCollider>();
            else if (type == PrimitiveType.Capsule && item.GetComponent<CapsuleCollider>() == null)
                item.AddComponent<CapsuleCollider>();
            else if (type == PrimitiveType.Plane && item.GetComponent<MeshCollider>() == null)
                item.AddComponent<MeshCollider>();
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
