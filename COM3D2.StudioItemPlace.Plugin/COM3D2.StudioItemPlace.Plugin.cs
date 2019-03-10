using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityInjector;
using UnityInjector.Attributes;

namespace COM3D2.StudioItemPlace.Plugin
{
	#region PluginMain
	[PluginFilter( "COM3D2x64" ), PluginName("COM3D2.StudioItemPlace.Plugin"), PluginVersion("1.3.0.0")]
	public class StudioItemPlace : UnityInjector.PluginBase
	{
		private const string TitleName = "StudioItemPlace 1.3.0.0";

		public const string logHead = "[StudioItemPlace]";
		public const string indexPath = @"\cache\";
		public const string indexFile = @"cache.dat";

		private int windowId = 2030124;

		private bool reqShift = false;
		private bool reqCtrl = true;
		private bool reqAlt = false;
		private string mainKey = "f13";

		private bool useGear = true;
		GameObject gear = null;

		private bool isGUI = false;

		private const int placeSize = 35;

		private int placeMenu = 0;

		private string[] mpnL;
		private string[] mpnN;

		public List<PlaceItemInstance> placed = new List<PlaceItemInstance>();
		private List<PlaceItem> items = new List<PlaceItem>();

		private PlaceItemInstance current = null;
		private int gizmoType = 0;

		private static string outputDir;
		private string saveName = "";
		private string saveExtension = @".itm";
		private string[] saveList = null;

		private bool createStart = false;
		private bool createdend = false;
		private bool created = false;
		private int currentCat = 0;

		private int winPosX = 0;
		private int winPosY = 0;
		private int loadOnce = 100;

		private int readMenuComplete = 0;

		private bool viewVanilla = true;
		private bool viewMod = true;

		private TMorph tempTMorph = null;

		private static Dictionary<string, MenuDataInfo> menuDataMap = new Dictionary<string, MenuDataInfo>();
		private static Dictionary<string, List<string>> categoryMenuMap = new Dictionary<string, List<string>>();
		private static Dictionary<string, Texture2D> textureMap = new Dictionary<string, Texture2D>();
		public static string readMenuState = "";
		private static List<string> removeList = new List<string>();
		private static List<string> indexCreateList = new List<string>();

		private bool attachMaid = false;
		public Maid curMaid = null;

		private Vector2 catViewVector = Vector2.zero;
		private Vector2 placeViewVector = Vector2.zero;
		private Vector2 placedViewVector = Vector2.zero;
		private Vector2 saveViewVector = Vector2.zero;

		private enum MenuLoadState
		{
			none, index, full, invalid
		}
		private class MenuDataInfo
		{
			public string menu;
			public string fullPath;
			public bool mod;
			public MenuLoadState loadState = MenuLoadState.none;
			public string category = "";
			private string name = "";
			public string getName()
			{
				ReadMenuData();
				return name;
			}
			private string icon = "";
			public string getIcon()
			{
				ReadMenuData();
				return icon;
			}
			private string innerCategory = "";
			public string getInnerCategory()
			{
				ReadMenuData();
				return innerCategory;
			}
			private bool innerCategoryMulti = false;
			public bool getInnerCategoryMult()
			{
				ReadMenuData();
				return innerCategoryMulti;
			}
			private string model = "";
			public string getModel()
			{
				ReadMenuData();
				return model;
			}
			private MaterialDataInfo[] materials = null;
			public MaterialDataInfo[] getMaterials()
			{
				ReadMenuData();
				return materials;
			}
			public MenuDataInfo(string menu, string fullPath, bool mod)
			{
				this.menu = menu;
				this.fullPath = fullPath;
				this.mod = mod;
			}
			public bool ReadMenuData()
			{
				if (loadState == MenuLoadState.invalid)
				{
					return false;
				}
				else if (loadState == MenuLoadState.full)
				{
					return true;
				}
				string tmpCategory = category;

				List<MaterialDataInfo> ml = new List<MaterialDataInfo>();
				byte[] b = readFileAll(menu, fullPath, mod);
				if (b == null || b.Length == 0)
				{
					loadState = MenuLoadState.invalid;
					return false;
				}
				MemoryStream ms = new MemoryStream(b);
				using (BinaryReader br = new BinaryReader(ms, System.Text.Encoding.UTF8))
				{
					try
					{
						br.ReadString();
						br.ReadInt32();
						br.ReadString();
						name = br.ReadString();
						category = br.ReadString();
						br.ReadString();
						long num2 = br.ReadInt32();
						string text7 = string.Empty;
						string text8 = string.Empty;
						while (true)
						{
							int num4 = br.ReadByte();
							text8 = text7;
							text7 = string.Empty;
							if (num4 == 0)
							{
								break;
							}
							for (int i = 0; i < num4; i++)
							{
								text7 = text7 + "\"" + br.ReadString() + "\" ";
							}
							if (!(text7 == string.Empty))
							{
								string stringCom = UTY.GetStringCom(text7);
								string[] stringList = UTY.GetStringList(text7);
								if (stringCom == "name")
								{
									string text9 = stringList[1];
									string text10 = string.Empty;
									string arg = string.Empty;
									int j;
									for (j = 0; j < text9.Length && text9[j] != '\u3000' && text9[j] != ' '; j++)
									{
										text10 += text9[j];
									}
									for (; j < text9.Length; j++)
									{
										arg += text9[j];
									}
									if (text10 != "")
									{
										name = text10;
									}
								}
								else if (stringCom == "category")
								{
									category = stringList[1].ToLower();
								}
								else if (stringCom == "icon" || stringCom == "icons")
								{
									icon = stringList[1];
								}
								else if (stringCom == "additem")
								{
									model = stringList[1];
									if (innerCategory != "")
									{
										innerCategoryMulti = true;
									}
									innerCategory = stringList[2];
								}
								else if (stringCom == "?テリアル変更")
								{
									MaterialDataInfo mat = new MaterialDataInfo();
									mat.slotName = stringList[1];
									mat.matNo = int.Parse(stringList[2]);
									mat.matName = stringList[3];
									ml.Add(mat);
								}
							}
						}
					}
					catch (Exception)
					{
						//
					}
				}
				materials = ml.ToArray();
				if (loadState == MenuLoadState.index)
				{
					if (tmpCategory != category)
					{
						Debug.Log(logHead + "内容が変わったため" + indexFile + "を更新:" + menu + "," + fullPath);
						string[] a = { menu };
						appendIndexFile(a);
					}
				}
				loadState = MenuLoadState.full;
				return true;
			}
		}
		private class MaterialDataInfo
		{
			public string slotName = "";
			public int matNo = 0;
			public string matName = "";
			public MaterialDataInfo()
			{
				//
			}
		}

		public class PlaceItemInstance
		{
			public GameObject go = null;
			public GizmoRender gr = null;
			public WorldTransformAxis wta = null;
			public PlaceItem pi = null;
			public PlaceItemInstance(PlaceItem pi, StudioItemPlace instance)
			{
				this.pi = new PlaceItem(pi.menu, pi.model, pi.name, pi.tex);
				try
				{
					go = instance.LoadSkinMesh_R(pi.model, pi.menu);
					if (go != null)
					{
						go.name = "myItem" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
						gr = go.AddComponent<GizmoRender>();
						gr.Visible = false;
						wta = PhotoWindowManager.CreateWorldTransformAxis(go, false);
						wta.Visible = false;
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e.ToString());
				}
			}
		}

		public class PlaceItem
		{
			public string menu;
			public string model;
			public string name;
			public string tex;
			public PlaceItem(string menu, string model, string name, string tex)
			{
				this.menu = menu;
				this.model = model;
				this.name = name;
				this.tex = tex;
			}
		}

		private string getIniValue(string p1, string p2, string def)
		{
			ExIni.IniKey k = Preferences[p1][p2];
			if (k != null && k.Value != null && k.Value != "")
			{
				return k.Value;
			}
			saveIniValue(p1, p2, def);
			return def;
		}
		private void saveIniValue(string p1, string p2, string val)
		{
			Preferences[p1][p2].Value = val;
			SaveConfig();
		}
		private void iniLoad(string p1, string p2, ref int val)
		{
			val = int.Parse(getIniValue(p1, p2, val.ToString()));
		}
		private void iniLoad(string p1, string p2, ref float val)
		{
			val = float.Parse(getIniValue(p1, p2, val.ToString()));
		}
		private void iniLoad(string p1, string p2, ref bool val)
		{
			val = bool.Parse(getIniValue(p1, p2, val.ToString()));
		}
		private void iniLoad(string p1, string p2, ref string val)
		{
			val = getIniValue(p1, p2, val);
		}

		public void Awake()
		{
			try
			{
				GameObject.DontDestroyOnLoad(this);
				SceneManager.sceneLoaded += OnSceneLoaded;

				outputDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Config\StudioItemPlace";
				if (!Directory.Exists(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}
				if (!Directory.Exists(outputDir + indexPath))
				{
					Directory.CreateDirectory(outputDir + indexPath);
				}

				iniLoad("default", "windowId", ref windowId);

				iniLoad("keyconfig", "mainkey", ref mainKey);
				iniLoad("keyconfig", "reqshift", ref reqShift);
				iniLoad("keyconfig", "reqctrl", ref reqCtrl);
				iniLoad("keyconfig", "reqalt", ref reqAlt);
				iniLoad("keyconfig", "useGear", ref useGear);

				iniLoad("position", "windowPosX", ref winPosX);
				iniLoad("position", "windowPosY", ref winPosY);

				iniLoad("system", "loadOnce", ref loadOnce);

				string[] mpnL = { "acchat", "headset", "wear", "skirt", "onepiece", "mizugi", "bra", "panz", "stkg", "shoes", "acckami", "megane",
					"acchead", "acchana", "accmimi", "glove", "acckubi", "acckubiwa", "acckamisub", "accnip", "accude", "accheso", "accashi", "accsenaka",
					"accshippo", "accxxx", "handitemr", "handiteml", "kousoku_upper", "kousoku_lower", "accvag", "accanl"};
				string[] mpnN = { "帽子", "ヘッドセット", "トップス", "スカ?ト", "ワンピ?ス", "水着", "ブラジャ?", "パンツ", "?ックス", "靴", "前髪", "メガネ",
					"アイ?スク", "?", "耳", "手袋", "ネックレス", "?ョ?カ?", "リ?ン", "乳首", "腕", "へそ", "足首", "背中",
					"しっぽ", "前穴", "右手アイテ?", "左手アイテ?", "拘束上半身", "拘束下半身", "前穴アイテ?", "後穴アイテ?"};
				this.mpnL = mpnL;
				this.mpnN = mpnN;
			}
			catch(Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		public void Update()
		{
			try
			{
				if (reqShift == (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
				{
					if (reqCtrl == (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
					{
						if (reqAlt == (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
						{
							if (Input.GetKeyDown(mainKey))
							{
								isGUI = !isGUI;
							}
						}
					}
				}
				if (createStart && !createdend)
				{
					readAllMenu(loadOnce);
					if (readMenuState == "complete")
					{
						createdend = true;
					}
				}
			}
			catch(Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		private static bool indexFileExist()
		{
			return File.Exists(outputDir + indexPath + indexFile);
		}
		private static void appendIndexFile(string[] menus)
		{
			using (StreamWriter sw = File.AppendText(outputDir + indexPath + indexFile))
			{
				int i = 0;
				foreach (string s in menus)
				{
					MenuDataInfo mdi = menuDataMap[s];
					if (mdi.category != null && mdi.category != "")
					{
						sw.WriteLine(s);
						sw.WriteLine(mdi.category);
						i++;
					}
				}
				Debug.Log(logHead + "キャッシュフ?イル書き込み数:" + i);
			}
		}
		private static void readIndexFile()
		{
			if (indexFileExist())
			{
				Debug.Log(logHead + "キャッシュフ?イルを読み込みます");
				using (StreamReader sr = File.OpenText(outputDir + indexPath + indexFile))
				{
					string menu = "";
					while ((menu = sr.ReadLine()) != null)
					{
						if (menu != "")
						{
							string category = sr.ReadLine();
							if (menuDataMap.ContainsKey(menu))
							{
								menuDataMap[menu].category = category;
								menuDataMap[menu].loadState = MenuLoadState.index;
							}
						}
					}
				}
			}
		}
		private void readAllMenu(int readAmount)
		{
			if (readMenuComplete < GameUty.MenuFiles.Length)
			{
				if (readMenuState == "")
				{
					readMenuState = "処理?備中(0/2)";
				}
				else if (readMenuState.Contains("処理?備中"))
				{
					if (readMenuState.Contains("(0/2)"))
					{
						foreach (string ful in GameUty.MenuFiles)
						{
							string mn = ful;
							bool isMod = false;
							if (mn.Contains(@"\"))
							{
								mn = mn.Substring(mn.LastIndexOf(@"\") + 1);
								isMod = true;
							}
							if (!menuDataMap.ContainsKey(mn))
							{
								if (mn.ToLower().Contains(".mod"))
								{
									isMod = true;
								}
								MenuDataInfo md = new MenuDataInfo(mn, ful, isMod);
								md.fullPath = ful;
								md.mod = isMod;
								menuDataMap.Add(mn, md);
							}
						}
						readMenuState = "処理?備中(1/2)";
					}
					else
					{
						foreach (string ful in GameUty.ModOnlysMenuFiles)
						{
							string mn = ful;
							if (mn.Contains(@"\"))
							{
								mn = mn.Substring(mn.LastIndexOf(@"\") + 1);
							}
							if (menuDataMap.ContainsKey(mn))
							{
								menuDataMap.Remove(mn);
							}
							MenuDataInfo md = new MenuDataInfo(mn, ful, true);
							md.fullPath = ful;
							md.mod = true;
							menuDataMap.Add(mn, md);
						}
						readMenuState = "キャッシュ読み込み開始";
					}
				}
				else if (readMenuState == "キャッシュ読み込み開始")
				{
					readIndexFile();
					readMenuState = "処理開始";
				}
				else if (readMenuState.Contains("処理開始"))
				{
					readMenuState = "処理開始(" + readMenuComplete + "/" + menuDataMap.Count + ")";
					for (int i = 0; i < readAmount && readMenuComplete < menuDataMap.Count; i++)
					{
						KeyValuePair<string, MenuDataInfo> kv = menuDataMap.ElementAt(readMenuComplete);
						if (kv.Value.loadState == MenuLoadState.index)
						{
							readAmount++;
						}
						else
						{
							bool rslt = kv.Value.ReadMenuData();
							if (!rslt)
							{
								removeList.Add(kv.Key);
							}
							else
							{
								indexCreateList.Add(kv.Key);
							}
						}
						readMenuComplete++;
					}
					if (readMenuComplete >= menuDataMap.Count)
					{
						readMenuState = "キャッシュ更新中";
					}
				}
				else if (readMenuState == "キャッシュ更新中")
				{
					if (indexCreateList.Count > 0)
					{
						appendIndexFile(indexCreateList.ToArray());
					}
					readMenuState = "最終処理中";
				}
				else if (readMenuState.Contains("最終処理中"))
				{
					foreach (string s in removeList)
					{
						menuDataMap.Remove(s);
					}
					foreach (KeyValuePair<string, MenuDataInfo> kv in menuDataMap)
					{
						if (kv.Value.category == null || kv.Value.category == "")
						{
							Debug.Log("[readAllMenu]カテゴリ?名を読み込めなかったためスキップ:" + kv.Key + "," + kv.Value.fullPath);
						}
						else
						{
							if (!categoryMenuMap.ContainsKey(kv.Value.category))
							{
								categoryMenuMap.Add(kv.Value.category, new List<string>());
							}
							if (!categoryMenuMap[kv.Value.category].Contains(kv.Key))
							{
								categoryMenuMap[kv.Value.category].Add(kv.Key);
							}
						}
					}
					readMenuState = "complete";
				}
			}
		}

		private static byte[] tryReadFileAll(string name, AFileSystemBase afsb)
		{
			byte[] b = null;
			using (AFileBase afb = afsb.FileOpen(name))
			{
				b = afb.ReadAll();
			}
			return b;
		}

		private static byte[] readFileAll(string name, string fullPath = null, bool isMod = true)
		{
			byte[] b = null;
			try
			{
				if (isMod)
				{
					b = tryReadFileAll(name, GameUty.FileSystemMod);
					if (b == null || b.Length == 0)
					{
						if (fullPath != null && fullPath != name)
						{
							b = tryReadFileAll(fullPath, GameUty.FileSystemMod);
						}
						if (b == null || b.Length == 0)
						{
							b = tryReadFileAll(name, GameUty.FileSystem);
							if (b == null || b.Length == 0)
							{
								b = tryReadFileAll(name, GameUty.FileSystemOld);
							}
						}
					}
				}
				else
				{
					b = tryReadFileAll(name, GameUty.FileSystem);
					if (b == null || b.Length == 0)
					{
						b = tryReadFileAll(name, GameUty.FileSystemOld);
						if (b == null || b.Length == 0)
						{
							b = tryReadFileAll(name, GameUty.FileSystemMod);
							if (fullPath != null && fullPath != name)
							{
								b = tryReadFileAll(fullPath, GameUty.FileSystemMod);
							}
						}
					}
				}
			}
			catch (Exception)
			{
				Debug.LogError("[readFileAll]?期しないフ?イル読み込みエラ?:" + name + "," + fullPath);
			}
			if (b == null || b.Length == 0)
			{
				Debug.LogWarning("[readFileAll]フ?イルが読み込めません:" + name + "," + fullPath);
			}
			return b;
		}

		private GameObject LoadSkinMesh_R(string modelName, string menuName)
		{
			if (tempTMorph == null)
			{
				TBody tb = new TBody();
				tb.boMAN = true;
				GameObject temp = new GameObject();
				temp.SetActive(false);
				tb.maid = temp.AddComponent<Maid>();
				tb.maid.enabled = false;
				tb.maid.m_goOffset = temp;
				TBodySkin tbs = new TBodySkin(tb, "", 0, false);
				tempTMorph = new TMorph(tbs);
			}
			GameObject gor = ImportCM.LoadSkinMesh_R(modelName, tempTMorph, "", tempTMorph.bodyskin, 0);

			SkinnedMeshRenderer smr = gor.GetComponentInChildren<SkinnedMeshRenderer>();
			if (smr != null)
			{
				if (menuName != null && menuName != "" && menuDataMap.ContainsKey(menuName))
				{
					MenuDataInfo md = menuDataMap[menuName];
					if (md.getMaterials() != null)
					{
						Material[] mate = smr.materials;
						foreach (MaterialDataInfo mat in md.getMaterials())
						{
							Material m = LoadMaterial(mat.matName);
							if (m != null)
							{
								if (mat.matNo >= mate.Length)
								{
									Material[] newArr = new Material[mat.matNo + 1];
									for (int i = 0; i < mate.Length; i++)
									{
										newArr[i] = mate[i];
									}
									mate = newArr;
								}
								mate[mat.matNo] = m;
							}
						}
						smr.materials = mate;
					}
				}
			}
			return gor;
		}

		private static Material LoadMaterial(string f_strFileName)
		{
			return ImportCM.LoadMaterial(f_strFileName, null, null);
		}

		private static Material ReadMaterial(BinaryReader r)
		{
			return ImportCM.ReadMaterial(r);
		}

		private void updateSaveList()
		{
			saveList = Directory.GetFiles(outputDir, @"*" + saveExtension);
			for (int i = 0; i < saveList.Length; i++)
			{
				saveList[i] = saveList[i].Substring(saveList[i].LastIndexOf(@"\") + 1);
			}
		}

		private void removePlaced(PlaceItemInstance pi)
		{
			removePlaced(placed.IndexOf(pi));
		}

		private void removePlaced(int i)
		{
			if (current == placed[i])
			{
				current = null;
			}
			try
			{
				if (placed[i] != null)
				{
					if (placed[i].wta != null)
					{
						placed[i].wta.Visible = false;
						UnityEngine.Object.Destroy(placed[i].wta);
					}
					if (placed[i].gr != null)
					{
						placed[i].gr.Visible = false;
						UnityEngine.Object.Destroy(placed[i].gr);
					}
					UnityEngine.Object.Destroy(placed[i].go);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
			placed.RemoveAt(i);
		}
		private void removePlacedAll()
		{
			while (placed.Count > 0)
			{
				removePlaced(placed.Count - 1);
			}
			current = null;
		}

		private void changeCurrent(PlaceItemInstance newCurrent)
		{
			if (current != null)
			{
				current.wta.Visible = false;
				current.gr.Visible = false;
			}
			current = newCurrent;
			if (gizmoType == 0)
			{
				current.wta.Visible = true;
			}
			else if (gizmoType == 1)
			{
				current.gr.Visible = true;
			}
		}

		private bool isExistItem(string menu)
		{
			foreach (PlaceItem pi in items)
			{
				if (pi.menu == menu)
				{
					return true;
				}
			}
			return false;
		}

		public AFileSystemBase preGetAFileSystemBase(string f_strFileName)
		{
			AFileSystemBase afsb = GameUty.FileSystemMod;
			if (!afsb.IsExistentFile(f_strFileName))
			{
				if (GameUty.FileSystem.IsExistentFile(f_strFileName))
				{
					afsb = GameUty.FileSystem;
				}
				else if (GameUty.FileSystemOld.IsExistentFile(f_strFileName))
				{
					afsb = GameUty.FileSystemOld;
				}
				else
				{
					return null;
				}
			}
			return afsb;
		}

		private Texture2D CreateTexture(string f_strFileName, bool storeCache)
		{
			if (textureMap.ContainsKey(f_strFileName))
			{
				return textureMap[f_strFileName];
			}
			AFileSystemBase afsb = preGetAFileSystemBase(f_strFileName);
			if (afsb == null)
			{
				return null;
			}

			Texture2D tex = ImportCM.LoadTexture(afsb, f_strFileName, false).CreateTexture2D();
			if (storeCache)
			{
				textureMap.Add(f_strFileName, tex);
			}
			return tex;
		}

		private CharacterMgr.Preset makeSinglePartsPreset(string menu, MPN mpn, int type)
		{
			MaidProp p2 = Maid.CreateProp(menu, mpn, type);
			CharacterMgr.Preset pr = new CharacterMgr.Preset();
			pr.ePreType = CharacterMgr.PresetType.All;
			List<MaidProp> lm = new List<MaidProp>();
			lm.Add(p2);
			pr.listMprop = lm;

			return pr;
		}
		private void placeOrAttach(PlaceItem fs)
		{
			if (!attachMaid)
			{
				if (fs.model != null && fs.model != "")
				{
					PlaceItemInstance p = new PlaceItemInstance(fs, this);
					if (p.go != null)
					{
						placed.Add(p);
						changeCurrent(placed.Last());
					}
					else
					{
						Debug.Log("配置処理エラ?:" + fs.menu);
					}
				}
			}
			else
			{
				if (curMaid != null && curMaid.Visible && !curMaid.IsBusy)
				{
					string curCat = mpnL[currentCat];
					if (curCat.Contains("handitem") || curCat == "kousoku_upper" || curCat == "kousoku_lower")
					{
						curCat = "handitem";
					}
					MaidProp p = curMaid.GetProp(curCat);
					CharacterMgr.Preset pr = makeSinglePartsPreset(fs.menu, (MPN)p.idx, p.type);
					GameMain.Instance.CharacterMgr.PresetSet(curMaid, pr);
				}
			}
		}

		private void setNextMaid(bool reverse)
		{
			List<Maid> maids = GameMain.Instance.CharacterMgr.GetStockMaidList();
			if (maids != null && maids.Count > 0)
			{
				int idx = 0;
				if (curMaid == null)
				{
					curMaid = maids[0];
				}
				else
				{
					idx = maids.IndexOf(curMaid);
				}
				do
				{
					idx += (reverse ? -1 : 1);
					if (idx < 0)
					{
						idx = maids.Count - 1;
					}
					else if (idx >= maids.Count)
					{
						idx = 0;
					}
				}
				while (!maids[idx].Visible && curMaid != maids[idx]);
				curMaid = (maids[idx].Visible ? maids[idx] : null);
			}
		}

		private void onGUIFunc(int winId)
		{
			try
			{
				GUIStyle gsButton = new GUIStyle(GUI.skin.button);
				gsButton.fontSize = 14;
				gsButton.alignment = TextAnchor.MiddleLeft;
				gsButton.wordWrap = false;
				GUIStyle gsButton3 = new GUIStyle(GUI.skin.button);
				gsButton3.fontSize = 14;
				gsButton3.normal.textColor = Color.cyan;
				gsButton3.hover.textColor = Color.cyan;
				gsButton3.active.textColor = Color.cyan;
				gsButton3.alignment = TextAnchor.MiddleLeft;
				gsButton3.wordWrap = false;
				GUIStyle gsLabel = new GUIStyle(GUI.skin.label);
				gsLabel.fontSize = 14;
				gsLabel.wordWrap = false;
				GUIStyle gsButtonEx = GUI.skin.button;
				gsButtonEx.fontSize = 12;
				gsButtonEx.alignment = TextAnchor.UpperLeft;
				gsButtonEx.wordWrap = true;
				GUIStyle gsBox = GUI.skin.box;
				gsBox.fontSize = 12;
				gsBox.alignment = TextAnchor.UpperLeft;
				gsBox.wordWrap = true;
				GUIStyle gsToggle = GUI.skin.toggle;
				gsToggle.fontSize = 14;
				gsToggle.alignment = TextAnchor.MiddleLeft;
				gsToggle.wordWrap = false;

				if (GUI.Button(new Rect(580 - 26 * 6, 0, 26, 26), "↑", gsButton))
				{
					winPosY -= 10;
					saveIniValue("position", "windowPosY", winPosY.ToString());
				}
				if (GUI.Button(new Rect(580 - 26 * 5, 0, 26, 26), "↓", gsButton))
				{
					winPosY += 10;
					saveIniValue("position", "windowPosY", winPosY.ToString());
				}
				if (GUI.Button(new Rect(580 - 26 * 4, 0, 26, 26), "←", gsButton))
				{
					winPosX -= 10;
					saveIniValue("position", "windowPosX", winPosX.ToString());
				}
				if (GUI.Button(new Rect(580 - 26 * 3, 0, 26, 26), "→", gsButton))
				{
					winPosX += 10;
					saveIniValue("position", "windowPosX", winPosX.ToString());
				}

				if (GUI.Button(new Rect(580 - 26, 0, 26, 26), "?", gsButton))
				{
					isGUI = false;
				}

				createStart = true;

				string menuTitle = "新規設置";
				if (placeMenu == 1)
				{
					menuTitle = "設置済み";
				}
				else if (placeMenu == 2)
				{
					menuTitle = "プリセット";
				}
				GUI.Label(new Rect(430, 55, 120, placeSize), menuTitle, gsLabel);
				if (created)
				{
					if (GUI.Button(new Rect(430, 55 + placeSize, 60, placeSize), "BACK", gsButton))
					{
						placeMenu--;
						if (placeMenu < 0)
						{
							placeMenu = 2;
						}
					}
					if (GUI.Button(new Rect(430 + 60, 55 + placeSize, 60, placeSize), "NEXT", gsButton))
					{
						placeMenu++;
						if (placeMenu > 2)
						{
							placeMenu = 0;
						}
					}
				}
				if (placeMenu == 0)
				{
					GUI.Label(new Rect(430 + 60, 55 + placeSize * 3, 60, placeSize), (viewVanilla ? "?示" : "非?示"), gsLabel);
					if (GUI.Button(new Rect(430, 55 + placeSize * 3, 60, placeSize), "?体", gsButton))
					{
						viewVanilla = !viewVanilla;
						created = false;
						placeViewVector = Vector2.zero;
					}
					GUI.Label(new Rect(430 + 60, 55 + placeSize * 4, 60, placeSize), (viewMod ? "?示" : "非?示"), gsLabel);
					if (GUI.Button(new Rect(430, 55 + placeSize * 4, 60, placeSize), "Mod", gsButton))
					{
						viewMod = !viewMod;
						created = false;
						placeViewVector = Vector2.zero;
					}
					attachMaid = !GUI.Toggle(new Rect(430, 55 + placeSize * 6, 120, placeSize), !attachMaid, "配置モ?ド", gsToggle);
					attachMaid = GUI.Toggle(new Rect(430, 55 + placeSize * 7, 120, placeSize), attachMaid, "メイドモ?ド", gsToggle);

					if (curMaid == null || !curMaid.Visible)
					{
						setNextMaid(false);
					}

					GUI.Label(new Rect(430, 55 + placeSize * 9, 150, placeSize), "選択中のメイド:", gsLabel);
					GUI.Label(new Rect(430, 55 + placeSize * 10, 150, placeSize), (curMaid != null ? curMaid.status.lastName + " " + curMaid.status.firstName : "なし"), gsLabel);
					if (GUI.Button(new Rect(430, 55 + placeSize * 11, 60, placeSize), "前へ", gsButton))
					{
						setNextMaid(true);
					}
					if (GUI.Button(new Rect(430 + 60, 55 + placeSize * 11, 60, placeSize), "次へ", gsButton))
					{
						setNextMaid(false);
					}
				}
				if (placeMenu == 1)
				{
					if (GUI.Button(new Rect(430, 55 + placeSize * 3, 60, placeSize), "移動", (gizmoType == 0 ? gsButton3 : gsButton)))
					{
						gizmoType = 0;
						if (current != null)
						{
							current.wta.Visible = true;
							current.gr.Visible = false;
						}
					}
					if (GUI.Button(new Rect(430, 55 + placeSize * 4, 60, placeSize), "回?", (gizmoType == 1 ? gsButton3 : gsButton)))
					{
						gizmoType = 1;
						if (current != null)
						{
							current.wta.Visible = false;
							current.gr.Visible = true;
						}
					}
					if (GUI.Button(new Rect(430, 55 + placeSize * 5, 60, placeSize), "なし", (gizmoType == 2 ? gsButton3 : gsButton)))
					{
						gizmoType = 2;
						if (current != null)
						{
							current.wta.Visible = false;
							current.gr.Visible = false;
						}
					}
					if (current != null)
					{
						GUI.Label(new Rect(430, 55 + placeSize * 6, 120, placeSize), "scale:" + current.go.transform.localScale.x.ToString("F2"), gsLabel);
						if (GUI.Button(new Rect(430, 55 + placeSize * 7, 60, placeSize), "-0.01", gsButton))
						{
							Vector3 v = current.go.transform.localScale;
							v.x -= 0.01f;
							v.y -= 0.01f;
							v.z -= 0.01f;
							current.go.transform.localScale = v;
						}
						if (GUI.Button(new Rect(430 + 60, 55 + placeSize * 7, 60, placeSize), "+0.01", gsButton))
						{
							Vector3 v = current.go.transform.localScale;
							v.x += 0.01f;
							v.y += 0.01f;
							v.z += 0.01f;
							current.go.transform.localScale = v;
						}
						if (GUI.Button(new Rect(430, 55 + placeSize * 8, 60, placeSize), "-0.1", gsButton))
						{
							Vector3 v = current.go.transform.localScale;
							v.x -= 0.1f;
							v.y -= 0.1f;
							v.z -= 0.1f;
							current.go.transform.localScale = v;
						}
						if (GUI.Button(new Rect(430 + 60, 55 + placeSize * 8, 60, placeSize), "+0.1", gsButton))
						{
							Vector3 v = current.go.transform.localScale;
							v.x += 0.1f;
							v.y += 0.1f;
							v.z += 0.1f;
							current.go.transform.localScale = v;
						}
						if (GUI.Button(new Rect(430, 55 + placeSize * 9, 60, placeSize), "-1", gsButton))
						{
							Vector3 v = current.go.transform.localScale;
							v.x -= 1f;
							v.y -= 1f;
							v.z -= 1f;
							current.go.transform.localScale = v;
						}
						if (GUI.Button(new Rect(430 + 60, 55 + placeSize * 9, 60, placeSize), "+1", gsButton))
						{
							Vector3 v = current.go.transform.localScale;
							v.x += 1f;
							v.y += 1f;
							v.z += 1f;
							current.go.transform.localScale = v;
						}
						if (GUI.Button(new Rect(430, 55 + placeSize * 11, 90, placeSize), "削除", gsButton))
						{
							removePlaced(current);
						}
					}
				}
				if (placeMenu == 2)
				{
					GUIStyle gsTextFiled = new GUIStyle(GUI.skin.textField);
					gsTextFiled.fontSize = 14;
					saveName = GUI.TextField(new Rect(430, 55 + placeSize * 3, 130, placeSize), saveName, gsTextFiled);

					if (GUI.Button(new Rect(430, 55 + placeSize * 4, 60, placeSize), "save", gsButton))
					{
						if (saveName != "" && placed.Count > 0)
						{
							using (StreamWriter sw = new StreamWriter(outputDir + @"\" + saveName + saveExtension, false))
							{
								foreach (PlaceItemInstance pii in placed)
								{
									string s = pii.go.name + ",";
									s += pii.go.transform.position.x.ToString() + ",";
									s += pii.go.transform.position.y.ToString() + ",";
									s += pii.go.transform.position.z.ToString() + ",";
									s += pii.go.transform.eulerAngles.x.ToString() + ",";
									s += pii.go.transform.eulerAngles.y.ToString() + ",";
									s += pii.go.transform.eulerAngles.z.ToString() + ",";
									s += pii.pi.menu + ",";
									s += pii.go.transform.localScale.x.ToString();
									sw.WriteLine(s);
								}
								sw.WriteLine("end");
							}
							updateSaveList();
						}
					}
				}

				if (placeMenu == 0)
				{
					catViewVector = GUI.BeginScrollView(new Rect(20, 55, 400, 50), catViewVector, new Rect(0, 0, 80 * mpnN.Length, 30));
					try
					{
						int i = 0;
						foreach (string s in mpnN)
						{
							if (GUI.Button(new Rect(80 * i, 0, 80, 30), mpnN[i], (currentCat == i ? gsButton3 : gsButton)))
							{
								created = false;
								currentCat = i;
								placeViewVector = Vector2.zero;
							}
							i++;
						}
					}
					catch (Exception e)
					{
						Debug.LogError(e.ToString());
					}
					GUI.EndScrollView();

					if (!created)
					{
						if (readMenuState == "complete")
						{
							created = true;
							items = new List<PlaceItem>();
							string curCat = mpnL[currentCat];
							if (curCat.Contains("handitem"))
							{
								curCat = "handitem";
							}
							if (categoryMenuMap.ContainsKey(curCat))
							{
								List<string> menus = categoryMenuMap[curCat];
								MenuDataInfo delItemName = null;
								foreach (string m in menus)
								{
									MenuDataInfo md = menuDataMap[m];
									if (curCat == "handitem")
									{
										if (m.ToLower().Contains("_del.menu") && !md.mod && m.ToLower().Contains(mpnL[currentCat]))
										{
											delItemName = md;
										}
										else if (!isExistItem(m) && !md.getInnerCategoryMult() && md.getInnerCategory().ToLower().Contains(mpnL[currentCat]) && !(!viewVanilla && !md.mod) && !(!viewMod && md.mod))
										{
											items.Add(new PlaceItem(m, md.getModel(), md.getName(), md.getIcon()));
										}
									}
									else if (curCat == "kousoku_upper" || curCat == "kousoku_lower" || curCat == "accvag" || curCat == "accanl")
									{
										if (m.ToLower().Contains("_del.menu") && !md.mod)
										{
											delItemName = md;
										}
										else if (!isExistItem(m) && !(!viewVanilla && !md.mod) && !(!viewMod && md.mod))
										{
											items.Add(new PlaceItem(m, md.getModel(), md.getName(), md.getIcon()));
										}
									}
									else
									{
										if (m.ToLower().Contains("_del.menu") && !md.mod)
										{
											delItemName = md;
										}
										else if (md.getIcon() != "" && !isExistItem(m) && !(!viewVanilla && !md.mod) && !(!viewMod && md.mod))
										{
											if (GameUty.IsExistFile(md.getIcon(), GameUty.FileSystem) || GameUty.IsExistFile(md.getIcon(), GameUty.FileSystemOld) || GameUty.IsExistFile(md.getIcon(), GameUty.FileSystemMod))
											{
												items.Add(new PlaceItem(m, md.getModel(), md.getName(), md.getIcon()));
											}
										}
									}
								}
								if (delItemName != null)
								{
									items.Insert(0, new PlaceItem(delItemName.menu, null, delItemName.getName(), delItemName.getIcon()));
								}
							}
						}
						else
						{
							GUI.Label(new Rect(40, 105, 800, 20), readMenuState, gsLabel);
						}
					}

					int startItemIdx = (int)(placeViewVector.y / 60) * 6;
					int endItemIdx = startItemIdx + ((600 - 135 - 10) / 60 + 2) * 6;

					placeViewVector = GUI.BeginScrollView(new Rect(20, 135, 400, 600 - 135 - 10), placeViewVector, new Rect(0, 0, 380, 60 * ((items.Count + 5) / 6)));
					try
					{
						for (int i = startItemIdx; i < items.Count && i < endItemIdx; i++)
						{
							PlaceItem fs = items[i];

							if (fs.tex != null && fs.tex != "")
							{
								if (GUI.Button(new Rect(60 * (i % 6), 60 * (i / 6), 60, 60), CreateTexture(fs.tex, true), gsButton))
								{
									placeOrAttach(fs);
								}
							}
							else
							{
								if (GUI.Button(new Rect(60 * (i % 6), 60 * (i / 6), 60, 60), fs.name, gsButtonEx))
								{
									placeOrAttach(fs);
								}
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogError(e.ToString());
					}
					GUI.EndScrollView();
				}

				if (placeMenu == 1)
				{
					placedViewVector = GUI.BeginScrollView(new Rect(20, 55, 400, 600 - 55 - 10), placedViewVector, new Rect(0, 0, 380, 60 * ((placed.Count + 5) / 6)));
					try
					{
						for (int i = 0; i < placed.Count; i++)
						{
							PlaceItemInstance fs = placed[i];

							if (current == fs)
							{
								if (fs.pi.tex != null && fs.pi.tex != "")
								{
									GUI.Box(new Rect(60 * (i % 6) - 5, 60 * (i / 6) - 5, 70, 70), CreateTexture(fs.pi.tex, true));
								}
								else
								{
									GUI.Box(new Rect(60 * (i % 6) - 5, 60 * (i / 6) - 5, 70, 70), fs.pi.name, gsBox);
								}
							}
							else
							{
								if (fs.pi.tex != null && fs.pi.tex != "")
								{
									if (GUI.Button(new Rect(60 * (i % 6) + 5, 60 * (i / 6) + 5, 50, 50), CreateTexture(fs.pi.tex, true), gsButton))
									{
										changeCurrent(placed[i]);
									}
								}
								else
								{
									if (GUI.Button(new Rect(60 * (i % 6) + 5, 60 * (i / 6) + 5, 50, 50), fs.pi.name, gsButtonEx))
									{
										changeCurrent(placed[i]);
									}
								}
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogError(e.ToString());
					}
					GUI.EndScrollView();
				}

				if (placeMenu == 2)
				{
					if (saveList == null)
					{
						updateSaveList();
					}
					saveViewVector = GUI.BeginScrollView(new Rect(20, 55, 400, 600 - 55 - 10), saveViewVector, new Rect(0, 0, 380, placeSize * saveList.Length));
					try
					{
						for (int i = 0; i < saveList.Length; i++)
						{
							string sn = saveList[i].Replace(saveExtension, "");

							if (GUI.Button(new Rect(0, placeSize * i, 380, placeSize), sn, gsButton))
							{
								removePlacedAll();

								using (StreamReader sr = new StreamReader(outputDir + @"\" + saveList[i]))
								{
									while (true)
									{
										string s = sr.ReadLine();
										if (s == "end")
										{
											break;
										}
										string[] sl = s.Split(',');
										MenuDataInfo md = menuDataMap[sl[7]];
										PlaceItem pi = new PlaceItem(sl[7], md.getModel(), md.getName(), md.getIcon());
										PlaceItemInstance pii = new PlaceItemInstance(pi, this);
										if (pii.go != null)
										{
											pii.go.name = sl[0];
											pii.go.transform.position = new Vector3(float.Parse(sl[1]), float.Parse(sl[2]), float.Parse(sl[3]));
											pii.go.transform.eulerAngles = new Vector3(float.Parse(sl[4]), float.Parse(sl[5]), float.Parse(sl[6]));
											pii.go.transform.localScale = new Vector3(float.Parse(sl[8]), float.Parse(sl[8]), float.Parse(sl[8]));
											placed.Add(pii);
										}
										else
										{
											Debug.Log("配置プリセット読み込み中エラ?:" + sl[7]);
										}
									}
								}
							}
						}
					}
					catch (Exception e)
					{
						Debug.LogError(e.ToString());
					}
					GUI.EndScrollView();
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		public void OnGUI()
		{
			try
			{
				if (isGUI)
				{
					GUIStyle gsWin = new GUIStyle(GUI.skin.box);
					gsWin.fontSize = 18;
					gsWin.alignment = TextAnchor.UpperLeft;
					Rect r = GUI.Window(windowId, new Rect(Screen.width - 800 + winPosX, 70 + winPosY, 580, 600), onGUIFunc, TitleName, gsWin);

					Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
					if (r.Contains(mousePos))
					{
						if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.GetAxis("Mouse ScrollWheel") != 0)
						{
							Input.ResetInputAxes();
						}
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		private void onGearButton(GameObject goButton)
		{
			isGUI = !isGUI;
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
		{
			try
			{
				if (useGear && gear == null && scene.name.Contains("SceneTitle"))
				{
					try
					{
						Assembly assembly = Assembly.GetExecutingAssembly();
						Stream stream = assembly.GetManifestResourceStream("COM3D2.StudioItemPlace.Plugin.icn.png");
						byte[] bt = new byte[stream.Length];
						stream.Read(bt, 0, (int)stream.Length);
						gear = GearMenu.Buttons.Add("StudioItemPlace", bt, onGearButton);
					}
					catch (Exception)
					{
						Debug.LogError("StudioItemPlace:ギアメニュ?の登?に失敗");
					}
				}
				removePlacedAll();
				tempTMorph = null;
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

		private void Initialize()
		{
			try
			{
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
		}

	}
	#endregion
}
 