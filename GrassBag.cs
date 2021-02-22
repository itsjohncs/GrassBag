using System.Collections;
using System.Reflection;
using Modding;
using UnityEngine;
using ModCommon;
using System.Collections.Generic;
using UnityEngine.UI;

namespace GrassBag
{
    public class GrassRegistry : ModSettings
    {
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
            return sceneName + "/" + gameObject.name;
        }

        public (int, int) GetGlobalGrassStats()
        {
            int total = BoolValues.Count;
            int numMowed = 0;
            foreach (bool mowed in BoolValues.Values)
            {
                if (mowed)
                {
                    numMowed += 1;
                }
            }

            return (total, numMowed);
        }

        public (int, int) GetSceneGrassStats(string sceneName)
        {
            int total = 0;
            int numMowed = 0;
            foreach (KeyValuePair<string, bool> kv in BoolValues)
            {
                if (kv.Key.StartsWith(sceneName + "/"))
                {
                    total += 1;

                    if (kv.Value)
                    {
                        numMowed += 1;
                    }
                }
            }

            return (total, numMowed);
        }

        // Called for all game objects. If it's grass it'll be registered.
        public bool MaybeRegisterGrass(string sceneName, GameObject gameObject)
        {
            string key = GetKeyForGrass(sceneName, gameObject);
            if (IsMowableGrass(gameObject) && !BoolValues.ContainsKey(key))
            {
                BoolValues.Add(key, false);
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

        public void Rehydrate(string[] keys)
        {
            foreach (string key in keys)
            {
                if (!BoolValues.ContainsKey(key))
                {
                    BoolValues.Add(key, false);
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

        /// <summary>
        /// Fetches the Mod Version From AssemblyInfo.AssemblyVersion
        /// </summary>
        /// <returns>Mod's Version</returns>
        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        /// <summary>
        /// Called after the class has been constructed.
        /// </summary>
        public override void Initialize()
        {
            Instance = this;
            
            ModHooks.Instance.SlashHitHook += OnSlashHit;
            ModHooks.Instance.SavegameSaveHook += OnSave;
            ModHooks.Instance.NewGameHook += OnNewGame;

            ContractorManager.Instance.StartCoroutine(FindGrassForever());
            ContractorManager.Instance.StartCoroutine(UpdateGrassCountForever());
        }

        private IEnumerator FindGrassForever()
        {
            while (true)
            {
                try
                {
                    string sceneName = GameManager.instance.sceneName;
                    foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
                    {
                        if (KnownGrass.MaybeRegisterGrass(sceneName, gameObject))
                        {
                            Log("Grass discovered " + sceneName + "/" + gameObject.name);
                        }
                    }
                } catch (System.Exception e)
                {
                    Log("Error occurred while finding grass");
                    Log(e.ToString());
                }

                yield return new WaitForSeconds(10);
            }
        }

        private IEnumerator UpdateGrassCountForever()
        {
            Text status;
            try
            {
                CanvasUtil.CreateFonts();

                GameObject canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080));
                canvas.GetComponent<Canvas>().sortingOrder = 1;

                Object.DontDestroyOnLoad(canvas);

                status = CanvasUtil.CreateTextPanel(
                    canvas,
                    "",
                    21,
                    TextAnchor.MiddleRight,
                    new CanvasUtil.RectData(new Vector2(600, 50), new Vector2(600, 1040), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)),
                    true).GetComponent<Text>();
            }
            catch (System.Exception e)
            {
                Log("Error occurred while preparing grass status overlay... stopping");
                Log(e.ToString());
                yield break;
            }

            while (true)
            {
                try
                {
                    string sceneName = GameManager.instance.sceneName;
                    (int globalTotal, int globalMowed) = KnownGrass.GetGlobalGrassStats();
                    (int sceneTotal, int sceneMowed) = KnownGrass.GetSceneGrassStats(sceneName);
                    status.text = string.Format("{0}/{1} globally -- {2}/{3} in room", globalMowed, globalTotal, sceneMowed, sceneTotal);
                }
                catch (System.Exception e)
                {
                    Log("Error occurred while updating grass status overlay.");
                    Log(e.ToString());
                }

                yield return new WaitForSeconds(10);
            }

            yield break;
        }


        public void OnSave(int id)
        {
            System.IO.File.WriteAllLines(Application.persistentDataPath + ModHooks.PathSeperator + "AllGrass.txt", KnownGrass.GetKeys());
        }

        public void OnNewGame()
        {
            KnownGrass.Rehydrate(System.IO.File.ReadAllLines(Application.persistentDataPath + ModHooks.PathSeperator + "AllGrass.txt"));
        }

        public void OnSlashHit(Collider2D otherCollider, GameObject gameObject)
        {
            if (KnownGrass.MaybeRegisterMow(GameManager.instance.sceneName, otherCollider.gameObject))
            {
                Log("Grass mowed " + GameManager.instance.sceneName + "/" + otherCollider.gameObject.name);
            }
        }
    }

}
