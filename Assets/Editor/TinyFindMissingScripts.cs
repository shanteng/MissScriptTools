using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class TinyFindMissingScripts : EditorWindow
{
    private string[] mPrefabPath = new string[] { "Assets" };
    private string[] mScriptPath = new string[] {
    "Assets/Scripts"};

    private Dictionary<string, string> mScriptGuidDic;
    private Dictionary<string, string> mPrefabAssetTextDic;
    private string mExcelName = "";
    private List<string> mUseList = new List<string>();
    private List<string> mShowList = new List<string>();
    private Vector2 scrollTextPos;
    Vector2 mPosPanel = Vector2.zero;
    Vector2 mPosPanel2 = Vector2.zero;
    [MenuItem("Tools/GuidMatch And MissiScript Tool",false, priority = 999)]
    public static void ShowWindow()
    {
        EditorWindow wind =  EditorWindow.GetWindow(typeof(TinyFindMissingScripts));
        wind.maximized = true;
    }

    private void InitAllScriptGuid()
    {
        if (this.mScriptGuidDic == null)
        {
            this.mScriptGuidDic = new Dictionary<string, string>();
            string[] guids = AssetDatabase.FindAssets("", this.mScriptPath);
            int len = guids.Length;
            for (int i = 0; i < len; i++)
            {
                // string asset_path = AssetDatabase.GUIDToAssetPath(guids[i]);
                this.mScriptGuidDic[guids[i]] = "1";// asset_path;
                string progressStr = combine("Please wait...(", i, "/", len, ")");
                EditorUtility.DisplayProgressBar("Scan Script", progressStr, i / (float)len);
            }
        }
        EditorUtility.ClearProgressBar();
    }

    void FindAndDisplay()
    {
        this.InitAllScriptGuid();
        this.mShowList.Clear();
        EditorUtility.DisplayProgressBar("Scan Prefab", "Please wait...", 0);

        string[] PrefabPath = new string[] { "Assets" };
        string[] allPath = AssetDatabase.FindAssets("t:Prefab", PrefabPath);
        int len = allPath.Length;
        for (int i = 0; i < len; i++)
        {
            string asset_path = AssetDatabase.GUIDToAssetPath(allPath[i]);
            var obj = AssetDatabase.LoadAssetAtPath(asset_path, typeof(GameObject)) as GameObject;
            if (obj == null)
                continue;

            List<string> missObjNames = new List<string>();
            var gos = obj.GetComponentsInChildren<Transform>(true);
            int goIndex = 1;
            foreach (var item in gos)
            {
                var components = item.GetComponents<Component>();
                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j] == null)
                    {
                        string path = GetHierarchyPath(item);
                        missObjNames.Add("  " + goIndex + ":" + path);
                        goIndex++;
                    }
                }
            }//end foreach

            if (missObjNames.Count > 0)
            {
                this.mShowList.Add(asset_path);
                this.mShowList.AddRange(missObjNames);
                Dictionary < string, bool> dicNot = this.GetNoUsedList(asset_path);
                List<string> guids = new List<string>();
                foreach (string key in dicNot.Keys)
                {
                    guids.Add(key);
                }
                if (guids.Count > 0)
                {
                    this.mShowList.Add("MissScript Guid:");
                    string keysStr = string.Join("\n", guids);
                    this.mShowList.Add(keysStr);
                }
                //查找哪些没有被引用
                this.mShowList.Add("\n");
            }
            string progressStr = combine("Please wait...(", i, "/", len, ")");
            EditorUtility.DisplayProgressBar("Scan Prefab", progressStr, i / (float)len);
        }//end for
        EditorUtility.ClearProgressBar();
    }//end func

    private Dictionary<string, bool>  GetNoUsedList(string asset_path)
    {
        Dictionary<string, bool> notUseList = new Dictionary<string, bool>();
        if (!File.Exists(asset_path))
            return notUseList;

        FileStream fs = new FileStream(asset_path, FileMode.Open, FileAccess.Read);
        byte[] buff = new byte[fs.Length];
        fs.Read(buff, 0, (int)fs.Length);
        string strText = Encoding.Default.GetString(buff);
        int starIndex = 0;

        while (true)
        {
            int indexOfScript = strText.IndexOf("m_Script:", starIndex);
            if (indexOfScript < 0)
                break;

            int indexOfGuid = strText.IndexOf("guid:", indexOfScript);
            if (indexOfGuid < 0)
                break;
            
            int indexOfDouHao = strText.IndexOf(",", indexOfGuid);
            if (indexOfGuid < 0)
                break;
            string guid = strText.Substring(indexOfGuid, indexOfDouHao - indexOfGuid).Replace("guid:","").Trim().TrimStart().TrimEnd();
            if (this.mScriptGuidDic.ContainsKey(guid) == false)
            {
                string pathNow =  AssetDatabase.GUIDToAssetPath(guid);
                if(pathNow.Equals(""))
                     notUseList[guid] = true;
            }
            starIndex = indexOfDouHao;
        }

        return notUseList;
    }

    void FindAndDelete()
    {
        this.mShowList.Clear();
        EditorUtility.DisplayProgressBar("Scan Prefab", "Please wait...", 0);
        this.mPrefabAssetTextDic = new Dictionary<string, string>();
        string[] allPath = AssetDatabase.FindAssets("t:Prefab", this.mPrefabPath);
        int len = allPath.Length;
        for (int i = 0; i < len; i++)
        {
            string asset_path = AssetDatabase.GUIDToAssetPath(allPath[i]);
            var obj = AssetDatabase.LoadAssetAtPath(asset_path, typeof(GameObject)) as GameObject;
            if (obj == null)
                continue;

            List<string> missObjNames = new List<string>();
            var gos = obj.GetComponentsInChildren<Transform>(true);
            int goIndex = 1;
            foreach (var item in gos)
            {
                var components = item.GetComponents<Component>();
                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j] == null)
                    {
                        string path = GetHierarchyPath(item);
                        missObjNames.Add("  " + goIndex + ":" + path);
                        goIndex++;
                    }
                }
            }

            if (missObjNames.Count > 0)
            {
                this.RemoveMiss(obj);
                this.mShowList.Add(asset_path);
                this.mShowList.AddRange(missObjNames);
                this.mShowList.Add("\n");
            }
            string progressStr = combine("Please wait...(", i, "/", len, ")");
            EditorUtility.DisplayProgressBar("Scan Prefab", progressStr, i / (float)len);
        }//end for
        EditorUtility.ClearProgressBar();

        if (mShowList.Count == 0)
        {
            EditorUtility.DisplayDialog("Congratulations~", "No Prefab Has Missing Script!", "Ok");
        }
        else
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    void DoMissingWindow(int windowID)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("查找无效Script的Guid", GUILayout.Width(150)))
        {
            this.FindAndDisplay();
        }
        else if (GUILayout.Button("查找并删除无效Script", GUILayout.Width(150)))
        {
            this.FindAndDelete();
        }
        EditorGUILayout.EndHorizontal();

        if (this.mShowList.Count > 0)
        {
            mPosPanel2 = EditorGUILayout.BeginScrollView(mPosPanel2);
            {
                string str ="结果如下：\n" + string.Join("\n", mShowList);
                EditorGUILayout.TextArea(str);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void RemoveMiss(GameObject obj)
    {
        var gos = obj.GetComponentsInChildren<Transform>(true);
        foreach (var item in gos)
        {
           GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
        }
    }

    void DoPanelWindow(int windowID)
    {
        EditorGUILayout.BeginHorizontal();
        mExcelName =  EditorGUILayout.TextField(this.mExcelName, GUILayout.Width(300));
        if (GUILayout.Button("查找Guid所有引用", GUILayout.Width(150)))
        {
            if (mExcelName.Equals(""))
                return;
            this.InitAllPrefab();
            this.mUseList.Clear();
            EditorUtility.DisplayProgressBar("Generating", "Please wait...", 1);
            foreach (string asset_path in this.mPrefabAssetTextDic.Keys)
            {
                string strText = this.mPrefabAssetTextDic[asset_path];
                if (strText.IndexOf(mExcelName) >= 0)
                {
                    this.mUseList.Add(asset_path);
                }
            }
            EditorUtility.ClearProgressBar();
        }
        EditorGUILayout.EndHorizontal();


        if (this.mUseList.Count > 0)
        {
            mPosPanel = EditorGUILayout.BeginScrollView(mPosPanel);
            {
                string str = string.Join("\n", mUseList);
                EditorGUILayout.TextArea(str);
            }
            EditorGUILayout.EndScrollView();
        }


    }

    public void OnGUI()
    {
        float ui_width = position.width / 2;
        float uiHeight = position.height - 10;// * 0.66f;
        float startX = ui_width;

        BeginWindows();
        Rect windowRectPanel = new Rect(0, 0, ui_width, uiHeight);
        GUILayout.Window(0, windowRectPanel, DoPanelWindow, "GuidMatchWindow");

        Rect windowRectBatch = new Rect(startX, 0, ui_width, uiHeight);
        GUILayout.Window(1, windowRectBatch, DoMissingWindow, "MissScriptWindow");

        EndWindows();
    }

    private void InitAllPrefab()
    {
        EditorUtility.DisplayProgressBar("Scan Prefab", "Please wait...", 0);
        this.mPrefabAssetTextDic = new Dictionary<string, string>();
        string[] allPath = AssetDatabase.FindAssets("t:Prefab", this.mPrefabPath);
        int len = allPath.Length;
        for (int i = 0; i < len; i++)
        {
            string asset_path = AssetDatabase.GUIDToAssetPath(allPath[i]);
            if (File.Exists(asset_path))
            {
                FileStream fs = new FileStream(asset_path, FileMode.Open, FileAccess.Read);
                byte[] buff = new byte[fs.Length];
                fs.Read(buff, 0, (int)fs.Length);
                string strText = Encoding.Default.GetString(buff);
                if (asset_path != null && strText != null)
                    mPrefabAssetTextDic[asset_path] = strText;
            }
            string progressStr = combine("Please wait...(", i, "/", len, ")");
            EditorUtility.DisplayProgressBar("Scan Prefab", progressStr, i / (float)len);
        }//end for
        EditorUtility.ClearProgressBar();
    }

    public static string GetHierarchyPath(Transform trans)
    {
        //获取节点所在Hierarchy路径
        if (null == trans) return string.Empty;
        if (null == trans.parent) return trans.name;
        return GetHierarchyPath(trans.parent) + "/" + trans.name;
    }

    private static object m_builderLock = new object();
    private static StringBuilder m_builder = new StringBuilder();
    public static string combine(string main, params object[] texts)
    {
        //文本组合
        lock (m_builderLock)
        {
            m_builder.Remove(0, m_builder.Length);
            m_builder.Append(main);
            var len = texts.Length;
            for (int i = 0; i < len; i++)
            {
                m_builder.Append(texts[i]);
            }
            return m_builder.ToString(0, m_builder.Length);
        }
    }

}//end class