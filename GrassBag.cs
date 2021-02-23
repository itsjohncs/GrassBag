using System.Collections;
using System.Reflection;
using Modding;
using UnityEngine;
using ModCommon;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;

namespace GrassBag
{
    public class GrassStat
    {
        public readonly int total;
        public readonly int mowed;

        public GrassStat(int total, int mowed)
        {
            this.total = total;
            this.mowed = mowed;
        }
    }

    public class GrassRegistry : ModSettings
    {
        // All stats tuples are: (total, mowed)
        private GrassStat globalStats = new GrassStat(0, 0);
        private Dictionary<string, GrassStat> statsByScene = new Dictionary<string, GrassStat>();

        private bool IsMowableGrass(GameObject gameObject)
        {
            return
                // If the game calls it grass, we will too
                gameObject.name.ToLower().Contains("grass") &&
                // Lots of grass isn't hittable, so we need to check if there's
                // any colliders created for this.
                gameObject.GetComponentsInChildren<Collider2D>().Length > 0;
        }

        private string GetKeyForGrass(string sceneName, GameObject gameObject)
        {
            if (sceneName.Contains("/"))
            {
                throw new System.ArgumentException("sceneName cannot contain /");
            }

            return sceneName + "/" + gameObject.name +
                   "(" +
                        gameObject.transform.position.x + "," +
                        gameObject.transform.position.y +
                   ")";
        }

        public GrassStat GetGlobalGrassStats()
        {
            return globalStats;
        }

        public GrassStat GetSceneGrassStats(string sceneName)
        {
            if (statsByScene.TryGetValue(sceneName, out GrassStat stats))
            {
                return stats;
            } else
            {
                return new GrassStat(0, 0);
            }
        }

        // Called for all game objects. If it's grass it'll be registered.
        public bool MaybeRegisterGrass(string sceneName, GameObject gameObject)
        {
            string key = GetKeyForGrass(sceneName, gameObject);
            if (IsMowableGrass(gameObject) && !BoolValues.ContainsKey(key))
            {
                BoolValues.Add(key, false);

                globalStats = new GrassStat(globalStats.total + 1, globalStats.mowed);

                if (statsByScene.TryGetValue(sceneName, out GrassStat sceneStats))
                {
                    statsByScene[sceneName] = new GrassStat(sceneStats.total + 1, sceneStats.mowed);
                }
                else
                {
                    statsByScene.Add(sceneName, new GrassStat(1, 0));
                }

                Modding.Logger.Log("Discovered " + key);

                return true;
            }

            return false;
        }

        // Called when a game object is destroyed. If it was grass, we'll mark it as mowed.
        public bool MaybeRegisterMow(string sceneName, GameObject gameObject)
        {
            string key = GetKeyForGrass(sceneName, gameObject);
            if (BoolValues.ContainsKey(key) && !BoolValues[key])
            {
                BoolValues[key] = true;

                globalStats = new GrassStat(globalStats.total, globalStats.mowed + 1);

                GrassStat sceneStats = statsByScene[sceneName];
                statsByScene[sceneName] = new GrassStat(sceneStats.total, sceneStats.mowed + 1);

                Modding.Logger.Log("Mowed " + key);

                return true;
            }

            return false;
        }
        public string[] GetKeys()
        {
            string[] keys = new string[BoolValues.Count];
            BoolValues.Keys.CopyTo(keys, 0);
            return keys;
        }

        public void Prepoluate(string[] keys)
        {
            foreach (string key in keys)
            {
                if (!BoolValues.ContainsKey(key))
                {
                    BoolValues.Add(key, false);
                }
            }

            RecalculateStats();
        }

        public void RecalculateStats()
        {
            globalStats = new GrassStat(0, 0);
            statsByScene = new Dictionary<string, GrassStat>();
            foreach (KeyValuePair<string, bool> kv in BoolValues)
            {
                string[] parts = kv.Key.Split(new char[] { '/' }, 2);
                string sceneName = parts[0];

                bool isMowed = kv.Value;

                globalStats = new GrassStat(globalStats.total + 1, globalStats.mowed + (isMowed ? 1 : 0));

                if (statsByScene.TryGetValue(sceneName, out GrassStat sceneStats))
                {
                    statsByScene[sceneName] = new GrassStat(sceneStats.total + 1, sceneStats.mowed + (isMowed ? 1 : 0));
                }
                else
                {
                    statsByScene.Add(sceneName, new GrassStat(1, (isMowed ? 1 : 0)));
                }
            }
        }
    }

    public class GrassBag : Mod
    {
        internal static GrassBag Instance;

        public GrassRegistry KnownGrass = new GrassRegistry();
        public override ModSettings SaveSettings
        {
            get => KnownGrass;
            set => KnownGrass = (GrassRegistry)value;
        }

        public override string GetVersion() => "1.4.1";
        
        /// <summary>
        /// Called after the class has been constructed.
        /// </summary>
        public override void Initialize()
        {
            Instance = this;
            
            ModHooks.Instance.SavegameSaveHook += OnSave;
            ModHooks.Instance.NewGameHook += OnNewGame;
            ModHooks.Instance.AfterSavegameLoadHook += OnSaveGameLoaded;
            ModHooks.Instance.HeroUpdateHook += OnHeroUpdate;

            // Some grass can only sometimes be cut... it's quite confusing and
            // I can't figure it out. So not only do we tap into ShouldCut
            // below which'll get us all the cuttable grass, we also make sure
            // that if our nail hit intersects with one of these "uncuttables"
            // we'll still count it as a mow.
            ModHooks.Instance.SlashHitHook += OnSlashHit;

            // The actual GrassCut object isn't instantiated reliably, but
            // ShouldCut seems to always be called reliably and when it returns
            // true, some grass is getting cut.
            On.GrassCut.ShouldCut += OnShouldCut;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            InitializeStatusText();
            InitializeGrassCompass();

            ContractorManager.Instance.StartCoroutine(FindNearestGrassForever());
        }

        private void OnSlashHit(Collider2D otherCollider, GameObject gameObject)
        {
            if (KnownGrass.MaybeRegisterMow(GameManager.instance.sceneName, otherCollider.gameObject))
            {
                UpdateStatusText();
            }
        }

        private Collider2D[] otherColliders = new Collider2D[30];

        private bool OnShouldCut(On.GrassCut.orig_ShouldCut orig, Collider2D collision)
        {
            try
            {
                if (orig(collision))
                {
                    int numFound = collision.GetContacts(otherColliders);
                    for (int i = 0; i < numFound && i < otherColliders.Length; ++i)
                    {
                        Collider2D other = otherColliders[i];
                        if (other != null && KnownGrass.MaybeRegisterMow(GameManager.instance.sceneName, other.gameObject))
                        {
                            UpdateStatusText();
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Log("Error occurred in should cut grass");
                Log(e.ToString());

                return false;
            }
        }

        private void OnSaveGameLoaded(SaveGameData data)
        {
            KnownGrass.RecalculateStats();
            UpdateStatusText();
        }

        private Text statusText;
        void InitializeStatusText()
        {
            CanvasUtil.CreateFonts();

            GameObject canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
            canvas.GetComponent<Canvas>().sortingOrder = 1;

            Object.DontDestroyOnLoad(canvas);
            
            statusText = CanvasUtil.CreateTextPanel(
                canvas,
                "",
                21,
                TextAnchor.MiddleRight,
                new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(600, 1040), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)),
                true).GetComponent<Text>();
        }

        GameObject compass = null;
        CanvasGroup compassCanvasGroup = null;
        void InitializeGrassCompass()
        {
            GameObject canvas = CanvasUtil.CreateCanvas(RenderMode.WorldSpace, new Vector2(1, 1));
            compassCanvasGroup = canvas.GetComponent<CanvasGroup>();
            canvas.GetComponent<Canvas>().sortingOrder = 1;
            Object.DontDestroyOnLoad(canvas);

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.green);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);

            compass = CanvasUtil.CreateImagePanel(canvas, sprite, new CanvasUtil.RectData(new Vector2(0.1f, 0.1f), new Vector2(0, 0)));

            compassCanvasGroup.alpha = 0;
        }

        private IEnumerator FindNearestGrassForever()
        {
            while (true)
            {
                try
                {
                    FindNearestGrass();
                }
                catch (System.Exception e)
                {
                    Log("Error occurred while finding nearest grass");
                    Log(e.ToString());
                }

                yield return new WaitForSeconds(1);
            }
        }


        Vector2? closestGrass = null;

        private void FindNearestGrass()
        {
            string currentScene = GameManager.instance.sceneName;
            if (currentScene == null)
            {
                return;
            }

            if (GameManager.instance == null || GameManager.instance.hero_ctrl == null || GameManager.instance.hero_ctrl.gameObject == null)
            {
                return;
            }
            Vector3 heroPosition3D = GameManager.instance.hero_ctrl.gameObject.transform.position;
            Vector2 heroPosition = new Vector2(heroPosition3D.x, heroPosition3D.y);

            // This was added quite late... and we are _not_ well equipped to
            // do it. So we'll do it in a probably-quite-slow manner but just
            // do it sparingly.
            Vector2 closestGrassPosition = Vector2.zero;
            float closestDistance = float.PositiveInfinity;
            foreach (KeyValuePair<string, bool> kv in KnownGrass.BoolValues)
            {
                if (kv.Value)
                {
                    continue;
                }

                string[] parts = kv.Key.Split(new char[] { '/' }, 2);

                string sceneName = parts[0];
                if (sceneName != currentScene)
                {
                    continue;
                }

                string[] xy = parts[1].Substring(parts[1].LastIndexOf('(') + 1).TrimEnd(new char[] { ')' }).Split(new char[] { ',' });
                Vector2 position = new Vector2(float.Parse(xy[0]), float.Parse(xy[1]));

                float distance = Vector2.Distance(heroPosition, position);
                if (distance < closestDistance)
                {
                    closestGrassPosition = position;
                    closestDistance = distance;
                }
            }

            if (closestDistance != float.PositiveInfinity)
            {
                closestGrass = closestGrassPosition;
            } else
            {
                closestGrass = null;
            }
        }

        private void OnHeroUpdate()
        {
            if (compass != null && GameManager.instance != null && GameManager.instance.hero_ctrl != null && GameManager.instance.hero_ctrl.gameObject != null)
            {
                if (closestGrass.HasValue)
                {
                    Vector3 heroPosition3D = GameManager.instance.hero_ctrl.gameObject.transform.position;
                    Vector2 heroPosition = new Vector2(heroPosition3D.x, heroPosition3D.y);

                    Vector2 normalizedDirection = closestGrass.Value - heroPosition;
                    normalizedDirection.Normalize();
                    Vector3 normalizedDirection3D = new Vector3(normalizedDirection.x, normalizedDirection.y, 0);

                    compass.transform.position = GameManager.instance.hero_ctrl.gameObject.transform.position + normalizedDirection3D * 1.5f;

                    compassCanvasGroup.alpha = 1;
                } else
                {
                    compassCanvasGroup.alpha = 0;
                }
            }
        }

        void UpdateStatusText()
        {
            string sceneName = GameManager.instance.sceneName;
            GrassStat globalStats = KnownGrass.GetGlobalGrassStats();
            GrassStat sceneStats = KnownGrass.GetSceneGrassStats(sceneName);
            statusText.text = string.Format("{0}/{1} globally -- {2}/{3} in room",
                                            globalStats.mowed, globalStats.total,
                                            sceneStats.mowed, sceneStats.total);
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            ContractorManager.Instance.StartCoroutine(FindGrass());
            UpdateStatusText();
        }

        private IEnumerator FindGrass()
        {
            // The docs suggest waiting a frame... I can afford a particularly
            // leisurely pace, so let's wait a whole second.
            yield return new WaitForSeconds(1);

            try
            {
                string sceneName = GameManager.instance.sceneName;
                foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                {
                    KnownGrass.MaybeRegisterGrass(sceneName, gameObject);
                }

                UpdateStatusText();
            } catch (System.Exception e)
            {
                Log("Error occurred while finding grass");
                Log(e.ToString());
            }
        }

        public void OnSave(int id)
        {
            System.IO.File.WriteAllLines(Application.persistentDataPath + ModHooks.PathSeperator + "AllGrass.txt", KnownGrass.GetKeys());
        }

        public void OnNewGame()
        {
            KnownGrass.Prepoluate(System.IO.File.ReadAllLines(Application.persistentDataPath + ModHooks.PathSeperator + "AllGrass.txt"));
            UpdateStatusText();
        }
    }

}
