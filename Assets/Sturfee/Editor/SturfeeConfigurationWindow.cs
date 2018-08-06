using Sturfee.Unity.XR.Core.Config;
using Sturfee.Unity.XR.Core.Constants;
using Sturfee.Unity.XR.Core.Features;
using Sturfee.Unity.XR.Providers.Components.Imu;
using Sturfee.Unity.XR.Providers.Components.Gps;
using Sturfee.Unity.XR.Providers.Components.ArCamera;
using Sturfee.Unity.XR.Providers.Components.Video;
using Sturfee.Unity.XR.Providers.Components.Lighting;

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Sturfee.Unity.XR.Package.Utilities;
using System.Reflection;

//[InitializeOnLoad]
public class SturfeeConfigurationWindow : EditorWindow
{
    public GUISkin SturfeeSkin;
    public MonoScript source;
    public static int OpenToSection = 0;


    string _apiKey = "N/A";
    //string _spatialRefGps = "";
    private int MemCacheLimit
    {
        set
        {
            if (value != _memCacheLimit)
            {
                _memCacheLimit = value;
                SaveConfiguration();
            }
        }
    }
    private int FileCacheLimit
    {
        set
        {
            if (value != _fileCacheLimit)
            {
                _fileCacheLimit = value;
                SaveConfiguration();
            }
        }
    }

    private int _memCacheLimit = 9;
    private int _fileCacheLimit = 25;
    private int _accessLevel = 0;
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;

    static string _configurationFile;
    private SturfeeConfiguration _config = null;
    private Vector2 _scrollPosition;
    private Texture2D _logo;
    private static int _currentTab = 0;

    private Color _sturfeePrimaryColor = new Color(25f / 255.0f, 190f / 255.0f, 200f / 255.0f);
    private Color _sturfeeSecondaryColor = new Color(238f / 255.0f, 66f / 255.0f, 102f / 255.0f);
    private Color _sturfeeErrorColor = new Color(183f / 255.0f, 48f / 255.0f, 48f / 255.0f);
    private Color _sturfeeDarkBackgroundColor = new Color(35f / 255.0f, 35f / 255.0f, 35f / 255.0f);

    private bool _loadingSubscription = false;
    private bool _accessTokenValid = false;
    private bool _setupFinished = false;
    private static UnityWebRequest _www;

    //Provider Set related
    private bool _createProviderSet;
    private bool _editingProviderSet;
    private ProviderSet _providerSet;
    private float _providerButtonWidth = 100;
    private int _controlId;
    private Type _imuType;
    private Type _gpsType;
    private Type _cameraType;
    private Type _videoType;
    private Type _lightingType;
    private string _providerSetName;
    private bool _displayEmptyNameError;

    [MenuItem("Sturfee/Configure", false, 0)]
    public static void ShowWindow()
    {
        _configurationFile = Path.Combine(Paths.SturfeeResourcesAbsolute, Paths.ConfigFile);

        SturfeeConfigurationWindow window = EditorWindow.GetWindow<SturfeeConfigurationWindow>();
        Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Sturfee/Editor/Images/sturfee_official_icon-black.png");
        GUIContent customTitleContent = new GUIContent("Sturfee", icon);
        window.titleContent = customTitleContent;
        window.Show();

        _currentTab = OpenToSection;
        //GUI.UnfocusWindow();
    }

    void OnGUI()
    {
        if (_config == null)
        {
            LoadConfig();
        }

        //GUI.skin = BuildSturfeeSkin();
        GUI.skin = SturfeeSkin;        

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        {
            var boxStyle = new GUIStyle(GUI.skin.label);
            boxStyle.normal.background = EditorGUIUtility.whiteTexture;

            EditorGUILayout.BeginVertical(boxStyle);// GUI.skin.box);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();//.FlexibleSpace();
                _logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Sturfee/Editor/Images/sturfee_official_logo-black_small.png");
                GUILayout.Label(_logo, GUILayout.MaxHeight(64), GUILayout.Width(200));
                EditorGUILayout.Space();//.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                var buttonStyle = new GUIStyle(GUI.skin.button);//.label);
                buttonStyle.normal.textColor = Color.white;// _sturfeePrimaryColor;
                buttonStyle.normal.background = MakeTex(2, 2, _sturfeePrimaryColor); //EditorGUIUtility.whiteTexture;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Sturfee.com", buttonStyle, GUILayout.Width(150), GUILayout.Height(30)))
                {
                    Application.OpenURL("https://sturfee.com");
                }
                if (GUILayout.Button("Developer Portal", buttonStyle, GUILayout.Width(150), GUILayout.Height(30)))
                {
                    Application.OpenURL("https://developer.sturfee.com");
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                
                if (_accessTokenValid)
                {
                    AddSpace(4);
                }
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var dllFile = new FileInfo(@"Assets/Sturfee/SDK/Sturfee.Unity.XR.Core.dll");
                string assemblyVersion = Assembly.LoadFile(dllFile.FullName).GetName().Version.ToString();
                GUILayout.Label("v" + assemblyVersion);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (!_accessTokenValid && !_loadingSubscription)
            {
                var errorStyle = new GUIStyle(GUI.skin.label);
                errorStyle.normal.background = MakeTex(2, 2, new Color(160 / 255.0f, 40 / 255.0f, 40 / 255.0f));
                errorStyle.normal.textColor = Color.white;
                errorStyle.alignment = TextAnchor.MiddleCenter;
                EditorGUILayout.BeginHorizontal(errorStyle);
                EditorGUILayout.PrefixLabel("Invalid Subscription", errorStyle);
                EditorGUILayout.EndHorizontal();

                _currentTab = GUILayout.Toolbar(_currentTab, new string[] { "Subscription" });
                _currentTab = 0;
            }
            else if (!_accessTokenValid && _loadingSubscription)
            {
                var loadingStyle = new GUIStyle(GUI.skin.label);
                loadingStyle.normal.background = MakeTex(2, 2, _sturfeeSecondaryColor);
                loadingStyle.normal.textColor = Color.white;
                loadingStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.BeginHorizontal(loadingStyle);
                GUI.contentColor = Color.white;
                EditorGUILayout.PrefixLabel("Loading Subscription...", loadingStyle);
                GUILayout.EndHorizontal();
            }
            else
            {
                //var successStyle = new GUIStyle(GUI.skin.label);
                //successStyle.normal.background = MakeTex(2, 2, _sturfeePrimaryColor);
                //successStyle.normal.textColor = Color.white;
                //successStyle.alignment = TextAnchor.MiddleCenter;
                //GUILayout.BeginHorizontal(successStyle);
                //GUI.contentColor = Color.white;
                //EditorGUILayout.PrefixLabel("Active Subscription", successStyle);
                //GUILayout.EndHorizontal();                
                _currentTab = GUILayout.Toolbar(_currentTab, new string[] { "Subscription", "Config", "Objects" });
            }

            switch (_currentTab)
            {
                case 0:
                    GUILayout.Label("Subscription Settings", EditorStyles.boldLabel);

                    EditorGUILayout.BeginVertical();
                    {
                        _apiKey = EditorGUILayout.TextField("API Key", _apiKey);
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Request Access", GUILayout.Width(100), GUILayout.Height(30)))
                            {
                                SaveConfiguration();

                                // get access from the API
                                _loadingSubscription = true;
                                CheckSubscription(_apiKey, HandleSubscriptionResult);
                            }
                        }                        
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Label("Available Features", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Access Level: Tier " + _accessLevel);

                        Texture2D tierImage;
                        var tierImageH = 375;
                        var tierImageW = 450;
                        switch (_accessLevel)
                        {
                            case 1:
                                EditorGUILayout.BeginHorizontal();
                                {
                                    tierImage = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                    "Assets/Sturfee/Editor/Images/Tier-1.png");
                                    GUILayout.Label(tierImage, GUILayout.MaxHeight(tierImageH), GUILayout.Width(tierImageW));
                                    EditorGUILayout.Space();
                                    EditorGUILayout.BeginVertical();
                                    {
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.LabelField(" - Localization");
                                        EditorGUILayout.LabelField(" - World Anchors");
                                        EditorGUILayout.LabelField(" - Basic Light System");
                                        EditorGUILayout.LabelField(" - Basic Surface Detection");
                                        GUILayout.FlexibleSpace();
                                    }
                                    EditorGUILayout.EndVertical();
                                }                                
                                EditorGUILayout.EndHorizontal();
                                break;
                            case 2:
                                EditorGUILayout.BeginHorizontal();
                                {
                                    tierImage = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                    "Assets/Sturfee/Editor/Images/Tier-2.png");
                                    GUILayout.Label(tierImage, GUILayout.MaxHeight(tierImageH), GUILayout.Width(tierImageW));
                                    EditorGUILayout.Space();
                                    EditorGUILayout.BeginVertical();
                                    {
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.LabelField(" - Localization");
                                        EditorGUILayout.LabelField(" - World Anchors");
                                        EditorGUILayout.LabelField(" - Basic Light System");
                                        EditorGUILayout.LabelField(" - Basic Surface Detection");
                                        EditorGUILayout.LabelField(" - Full Terrain Detection");
                                        GUILayout.FlexibleSpace();
                                    }
                                    EditorGUILayout.EndVertical();
                                }                                
                                EditorGUILayout.EndHorizontal();

                                break;
                            case 3:
                                EditorGUILayout.BeginHorizontal();
                                {
                                    tierImage = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                                                        "Assets/Sturfee/Editor/Images/Tier-3.png");
                                    GUILayout.Label(tierImage, GUILayout.MaxHeight(tierImageH), GUILayout.Width(tierImageW));
                                    EditorGUILayout.Space();
                                    EditorGUILayout.BeginVertical();
                                    {
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.LabelField(" - Localization");
                                        EditorGUILayout.LabelField(" - World Anchors");
                                        EditorGUILayout.LabelField(" - Basic Light System");
                                        EditorGUILayout.LabelField(" - Basic Surface Detection");
                                        EditorGUILayout.LabelField(" - Full Terrain Detection");
                                        EditorGUILayout.LabelField(" - Full Building Detection");
                                        GUILayout.FlexibleSpace();
                                    }
                                    EditorGUILayout.EndVertical();
                                }                                
                                EditorGUILayout.EndHorizontal();

                                break;
                            case 4:
                                EditorGUILayout.BeginHorizontal();
                                {
                                    tierImage = AssetDatabase.LoadAssetAtPath<Texture2D>(
                                    "Assets/Sturfee/Editor/Images/Tier-4.png");
                                    GUILayout.Label(tierImage, GUILayout.MaxHeight(tierImageH), GUILayout.Width(tierImageW));
                                    EditorGUILayout.Space();
                                    EditorGUILayout.BeginVertical();
                                    {
                                        GUILayout.FlexibleSpace();
                                        EditorGUILayout.LabelField(" - Localization");
                                        EditorGUILayout.LabelField(" - World Anchors");
                                        EditorGUILayout.LabelField(" - Basic Light System");
                                        EditorGUILayout.LabelField(" - Basic Surface Detection");
                                        EditorGUILayout.LabelField(" - Full Terrain Detection");
                                        EditorGUILayout.LabelField(" - Full Building Detection");
                                        EditorGUILayout.LabelField(" - Dynamic Objects");
                                        GUILayout.FlexibleSpace();
                                    }
                                    EditorGUILayout.EndVertical();
                                }
                                EditorGUILayout.EndHorizontal();
                                break;
                            default:
                                break;
                        }
                    }
                    EditorGUILayout.EndVertical();
                    break;
                case 1:
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                        GUILayout.Label("Caching", EditorStyles.boldLabel);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Memory Cache");
                        MemCacheLimit = EditorGUILayout.IntSlider(_config.MemoryCacheLimit, 0, 25);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("File Cache");
                        FileCacheLimit = EditorGUILayout.IntSlider(_config.FileCacheLimit, 0, 100);
                        EditorGUILayout.EndHorizontal();
                        AddSpace(2);
                        if (GUILayout.Button("Clear", GUILayout.Width(100), GUILayout.Height(30)))
                        {
                            Sturfee.Unity.XR.Core.Tile.CacheManager.Clear();
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                    #region Spatial Transform
                    //EditorGUILayout.BeginVertical();
                    //{
                    //    EditorGUILayout.BeginHorizontal();           
                    //    EditorGUILayout.PrefixLabel("Spatial Mode (Lat, Long)");
                    //    _spatialRefGps = EditorGUILayout.TextField(_spatialRefGps);
                    //    EditorGUILayout.EndHorizontal();
                    //    if(_config.SpatialRefGps != _spatialRefGps)
                    //    {
                    //        Debug.Log("GPS Saved");
                    //        SaveConfiguration();
                    //    }
                    //}
                    //EditorGUILayout.EndVertical();

                    //EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                    #endregion



                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.Label("Custom XR Session Providers", EditorStyles.boldLabel);
                        _createProviderSet = (GameObject.FindObjectsOfType<ProviderSet>().Length == 1);
                        if (!_createProviderSet && GUILayout.Button("Create New Provider Set", GUILayout.Width(200), GUILayout.Height(30)))
                        {
                            _providerSet = new GameObject("Provider set").AddComponent<ProviderSet>();
                            _providerSetName = null;

                        }

                        // You can always edit your Provider set by going to Resources/Provider Sets/Custom
                        if (!_editingProviderSet && !_createProviderSet && GUILayout.Button("Edit Existing Provider Set", GUILayout.Width(200), GUILayout.Height(30)))
                        {
                            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "Set", _controlId + 99);
                        }

                        ManageProviders();
                    }                    
                    EditorGUILayout.EndVertical();

                    break;
                case 2:
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    if (!_setupFinished && !SturfeeLayersExist())
                    {
                        GUILayout.Label("Unity Setup", EditorStyles.boldLabel);
                        if (GUILayout.Button("First Time Setup", GUILayout.Width(200), GUILayout.Height(30)))
                        {
                            CreateSturfeeLayers();
                            _setupFinished = true;
                        }
                    }
                    else
                    {
                        GUILayout.Label("XR Objects", EditorStyles.boldLabel);
                        if (GUILayout.Button("Create XRSession", GUILayout.Width(200), GUILayout.Height(30)))
                        {
                            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Sturfee/SDK/XRSession/SturfeeXRSession.prefab", typeof(GameObject));
                            GameObject clone = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                            clone.name = prefab.name;
                        }
                        if (GUILayout.Button("Create XRCamera", GUILayout.Width(200), GUILayout.Height(30)))
                        {
                            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Sturfee/SDK/XRSession/SturfeeXRCamera.prefab", typeof(GameObject));
                            GameObject clone = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                            clone.name = prefab.name;
                        }
                        if (GUILayout.Button("Create WorldAnchor", GUILayout.Width(200), GUILayout.Height(30)))
                        {
                            var anchor = new GameObject().AddComponent<WorldAnchor>();
                            anchor.gameObject.name = "WorldAnchor";
                        }
                    }
                    break;
            }

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            //myBool = EditorGUILayout.Toggle("Toggle", myBool);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();

        }
        EditorGUILayout.EndScrollView();
    }

    private void OnDestroy()
    {
        if (_providerSet != null)
        {
            DestroyImmediate(_providerSet.gameObject);
        }
    }

    private void HandleSubscriptionResult(SturfeeSubscriptionInfo info)
    {
        _loadingSubscription = false;

        _accessLevel = info.Tier;

        if (info.Tier > 0)
        {
            _accessTokenValid = true;
        }
        else
        {
            _accessTokenValid = false;
        }

        EditorApplication.update -= SturfeeSubscriptionManager.OnResponse;

        EditorWindow.GetWindow<SturfeeConfigurationWindow>().Focus();
    }

    private void SaveConfiguration()
    {
        var configuration = new SturfeeConfiguration
        {
            AccessToken = _apiKey,
            FileCacheLimit = _fileCacheLimit,
            MemoryCacheLimit = _memCacheLimit,
            //SpatialRefGps = _spatialRefGps                
        };

        var json = JsonUtility.ToJson(configuration);
        File.WriteAllText(_configurationFile, json);
        AssetDatabase.Refresh();
        Repaint();

        // set the config
        _config = configuration;
    }

    private void LoadConfig()
    {
        _configurationFile = Path.Combine(Paths.SturfeeResourcesAbsolute, Paths.ConfigFile);

        // create the directory, if needed
        if (!Directory.Exists(Paths.SturfeeResourcesAbsolute))
        {
            Directory.CreateDirectory(Paths.SturfeeResourcesAbsolute);
        }

        // create a new config file
        if (!File.Exists(_configurationFile))
        {
            var json = JsonUtility.ToJson(new SturfeeConfiguration
            {
                AccessToken = "N/A"
            });
            File.WriteAllText(_configurationFile, json);
        }

        TextAsset configurationTextAsset = Resources.Load<TextAsset>(Paths.SturfeeResourcesRelative);// _configurationFile);// Paths.SturfeeResourcesAbsolute);

        _config = configurationTextAsset == null ? null : JsonUtility.FromJson<SturfeeConfiguration>(configurationTextAsset.text);

        if (_config != null)
        {
            _apiKey = _config.AccessToken;
            //_spatialRefGps = _config.SpatialRefGps;
            _loadingSubscription = true;
            //CheckSubscription(_apiKey, HandleSubscriptionResult); // validates against the server
            var subscriptionInfo = SturfeeSubscriptionManager.GetSubscriptionInfo(_apiKey); // local
            HandleSubscriptionResult(subscriptionInfo);
        }
    }


    private GUISkin BuildSturfeeSkin()
    {
        var skin = new GUISkin();
        //skin.box.normal.background = 

        return skin;
    }

    private static bool SturfeeLayersExist()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        Type type = typeof(Sturfee.Unity.XR.Core.Constants.SturfeeLayers);
        foreach (var layer in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
        {
            var layerValue = layer.GetValue(null).ToString();
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSp = layers.GetArrayElementAtIndex(i);
                if (layerSp.stringValue == layerValue)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void CreateSturfeeLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        Type type = typeof(Sturfee.Unity.XR.Core.Constants.SturfeeLayers);
        foreach (var layer in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
        {
            var layerValue = layer.GetValue(null).ToString();
            bool existLayer = false;
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerSp = layers.GetArrayElementAtIndex(i);
                if (layerSp.stringValue == layerValue)
                {
                    existLayer = true;
                    break;
                }
            }
            for (int j = 8; j < layers.arraySize; j++)
            {
                SerializedProperty layerSP = layers.GetArrayElementAtIndex(j);
                if (layerSP.stringValue == "" && !existLayer)
                {
                    layerSP.stringValue = layerValue;
                    tagManager.ApplyModifiedProperties();

                    break;
                }
            }
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void ManageProviders()
    {
        _controlId = EditorGUIUtility.GetControlID(FocusType.Passive);

        if (Event.current.commandName == "ObjectSelectorClosed")
        {
            // existing provider set
            if (EditorGUIUtility.GetObjectPickerControlID() == (_controlId + 99) && !_editingProviderSet)
            {
                _editingProviderSet = true;
                if((GameObject)EditorGUIUtility.GetObjectPickerObject() != null)
                {
                    _providerSet = GameObject.Instantiate(((GameObject)EditorGUIUtility.GetObjectPickerObject())).GetComponent<ProviderSet>();
                    _providerSet.gameObject.name = ((GameObject)EditorGUIUtility.GetObjectPickerObject()).name;
                    _providerSetName = _providerSet.gameObject.name;

                    _providerSet.ImuProvider = _providerSet.gameObject.GetComponent<ImuProviderBase>();
                    _providerSet.GpsProvider = _providerSet.gameObject.GetComponent<GpsProviderBase>();
                    _providerSet.CameraProvider = _providerSet.gameObject.GetComponent<CameraProviderBase>();
                    _providerSet.VideoProvider = _providerSet.gameObject.GetComponent<VideoProviderBase>();
                    _providerSet.LightingProvider = _providerSet.gameObject.GetComponent<LightingProviderBase>();

                    if(_providerSet.ImuProvider != null)
                    {
                        _imuType = _providerSet.ImuProvider.GetType();
                    }
                    if (_providerSet.GpsProvider != null)
                    {
                        _gpsType = _providerSet.GpsProvider.GetType();
                    }
                    if (_providerSet.CameraProvider != null)
                    {
                        _cameraType = _providerSet.CameraProvider.GetType();
                    }
                    if (_providerSet.VideoProvider != null)
                    {
                        _videoType = _providerSet.VideoProvider.GetType();
                    }
                    if (_providerSet.LightingProvider != null)
                    {
                        _lightingType = _providerSet.LightingProvider.GetType();
                    }

                    EditorWindow.GetWindow<SturfeeConfigurationWindow>().Focus();
                }
                else
                {
                    _editingProviderSet = false;
                }
            }
        }

        if (!_createProviderSet && !_editingProviderSet)
        {
            _imuType = null;
            _gpsType = null;
            _cameraType = null;
            _videoType = null;
            _lightingType = null;
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                //EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                var editTitle = _editingProviderSet == true ? "EDIT" : "NEW";
                GUILayout.Label(editTitle + " PROVIDER SET", EditorStyles.boldLabel);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                _providerSetName = EditorGUILayout.TextField("Provider Set Name : ", _providerSetName);
                if (string.IsNullOrEmpty(_providerSetName))
                {
                    var redTextStyle = new GUIStyle();
                    redTextStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField("*required", redTextStyle);
                }

                var providerSetBtns = new GUIStyle();
                //providerSetBtns.normal.background = MakeTex(2, 2, _sturfeePrimaryColor);
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = _sturfeePrimaryColor;
                GUI.enabled = !string.IsNullOrEmpty(_providerSetName);
                if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    if (String.IsNullOrEmpty(_providerSetName))
                    {
                        _displayEmptyNameError = true;
                    }
                    else
                    {
                        if(_providerSetName.Contains("Provider Set"))
                        {
                            PrefabUtility.CreatePrefab("Assets/Sturfee/Resources/Provider Sets/Custom/" + _providerSetName + ".prefab", _providerSet.gameObject);
                        }
                        else
                        {
                            PrefabUtility.CreatePrefab("Assets/Sturfee/Resources/Provider Sets/Custom/" + _providerSetName + " Provider Set.prefab", _providerSet.gameObject);
                        }

                        // cleanup
                        _editingProviderSet = false;
                        _createProviderSet = false;
                        _imuType = null;
                        _gpsType = null;
                        _cameraType = null;
                        _videoType = null;
                        _lightingType = null;
                        DestroyImmediate(_providerSet.gameObject);
                    }
                }
                GUI.enabled = true;
                //providerSetBtns.normal.background = MakeTex(2, 2, Color.red);
                GUI.backgroundColor = _sturfeeErrorColor;
                if (GUILayout.Button("Cancel", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    _editingProviderSet = false;
                    _createProviderSet = false;

                    _imuType = null;
                    _gpsType = null;
                    _cameraType = null;
                    _videoType = null;
                    _lightingType = null;

                    GameObject.DestroyImmediate(_providerSet.gameObject);
                }
                GUI.backgroundColor = oldColor;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                //Imu
                if (EditorGUIUtility.GetObjectPickerControlID() == (_controlId + 1))
                {
                    _imuType = ((MonoScript)EditorGUIUtility.GetObjectPickerObject()).GetClass();
                    if (_providerSet.gameObject.GetComponent<ImuProviderBase>() != null)
                    {
                        DestroyImmediate(_providerSet.gameObject.GetComponent<ImuProviderBase>());
                    }
                    _providerSet.gameObject.AddComponent(_imuType);
                    _providerSet.ImuProvider = _providerSet.gameObject.GetComponent<ImuProviderBase>();
                }
                //Gps
                else if (EditorGUIUtility.GetObjectPickerControlID() == (_controlId + 2))
                {
                    _gpsType = ((MonoScript)EditorGUIUtility.GetObjectPickerObject()).GetClass();
                    if (_providerSet.gameObject.GetComponent<GpsProviderBase>() != null)
                    {
                        DestroyImmediate(_providerSet.gameObject.GetComponent<GpsProviderBase>());
                    }
                    _providerSet.gameObject.AddComponent(_gpsType);
                    _providerSet.GpsProvider = _providerSet.gameObject.GetComponent<GpsProviderBase>();
                }
                //Camera
                else if (EditorGUIUtility.GetObjectPickerControlID() == (_controlId + 3))
                {
                    _cameraType = ((MonoScript)EditorGUIUtility.GetObjectPickerObject()).GetClass();
                    if (_providerSet.gameObject.GetComponent<CameraProviderBase>() != null)
                    {
                        DestroyImmediate(_providerSet.gameObject.GetComponent<CameraProviderBase>());
                    }
                    _providerSet.gameObject.AddComponent(_cameraType);
                    _providerSet.CameraProvider = _providerSet.gameObject.GetComponent<CameraProviderBase>();
                }
                //Video
                else if (EditorGUIUtility.GetObjectPickerControlID() == (_controlId + 4))
                {
                    _videoType = ((MonoScript)EditorGUIUtility.GetObjectPickerObject()).GetClass();
                    if (_providerSet.gameObject.GetComponent<VideoProviderBase>() != null)
                    {
                        DestroyImmediate(_providerSet.gameObject.GetComponent<VideoProviderBase>());
                    }
                    _providerSet.gameObject.AddComponent(_videoType);
                    _providerSet.VideoProvider = _providerSet.gameObject.GetComponent<VideoProviderBase>();
                }
                //LIghting
                else if (EditorGUIUtility.GetObjectPickerControlID() == (_controlId + 5))
                {
                    _lightingType = ((MonoScript)EditorGUIUtility.GetObjectPickerObject()).GetClass();
                    if (_providerSet.gameObject.GetComponent<LightingProviderBase>() != null)
                    {
                        DestroyImmediate(_providerSet.gameObject.GetComponent<LightingProviderBase>());
                    }
                    _providerSet.gameObject.AddComponent(_lightingType);
                    _providerSet.LightingProvider = _providerSet.gameObject.GetComponent<LightingProviderBase>();
                }
            }

            //EditorGUILayout.BeginHorizontal();
            GUIStyle providerGui = new GUIStyle();
            var providerButtonWidth = 200f;
            var normalButtonWidth = 100f;
            providerGui.normal.background = MakeTex(2, 2, _sturfeeDarkBackgroundColor);
            EditorGUILayout.BeginVertical();// providerGui, GUILayout.MinWidth(2*_providerButtonWidth));
            {
                if (_imuType != null)
                {
                    //EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(providerGui);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("IMU Provider", EditorStyles.boldLabel);
                    if (GUILayout.Button("Change", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Imuprovider", _controlId + 1);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        _imuType = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_imuType != null)
                        Editor.CreateEditor(_providerSet.gameObject.GetComponent(_imuType)).OnInspectorGUI();
                    AddSpace(1);
                    EditorGUILayout.EndVertical();
                    //EditorGUILayout.EndHorizontal();

                    AddSpace(1);
                }
                else
                {
                    var imuBtnTxt = _imuType == null ? "Add IMU" : "Change IMU";
                    
                    if (GUILayout.Button(imuBtnTxt, GUILayout.Width(providerButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Imuprovider", _controlId + 1);
                    }
                }

                if (_gpsType != null)
                {
                    //EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(providerGui);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("GPS Provider", EditorStyles.boldLabel);
                    if (GUILayout.Button("Change", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Gpsprovider", _controlId + 2);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        _gpsType = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_gpsType != null)
                        Editor.CreateEditor(_providerSet.gameObject.GetComponent(_gpsType)).OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                    //EditorGUILayout.EndHorizontal();

                    AddSpace(1);
                }
                else
                {
                    var gpsBtnTxt = _gpsType == null ? "Add GPS" : "Change GPS";
                    if (GUILayout.Button(gpsBtnTxt, GUILayout.Width(providerButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Gpsprovider", _controlId + 2);
                    }
                }

                if (_cameraType != null)
                {

                    //EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(providerGui);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Camera Provider", EditorStyles.boldLabel);
                    if (GUILayout.Button("Change", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Cameraprovider", _controlId + 3);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        _cameraType = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_cameraType != null)
                        Editor.CreateEditor(_providerSet.gameObject.GetComponent(_cameraType)).OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                    //EditorGUILayout.EndHorizontal();

                    AddSpace(1);
                }
                else
                {
                    var cameraBtnTxt = _cameraType == null ? "Add Camera" : "Change Camera";
                    if (GUILayout.Button(cameraBtnTxt, GUILayout.Width(providerButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Cameraprovider", _controlId + 3);
                    }
                }

                if (_videoType != null)
                {
                    //EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(providerGui);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Video Provider", EditorStyles.boldLabel);
                    if (GUILayout.Button("Change", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Videoprovider", _controlId + 4);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        _videoType = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_videoType != null)
                        Editor.CreateEditor(_providerSet.gameObject.GetComponent(_videoType)).OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                    //EditorGUILayout.EndHorizontal();

                    AddSpace(1);
                }
                else
                {
                    var videoBtnTxt = _videoType == null ? "Add Video" : "Change Video";
                    if (GUILayout.Button(videoBtnTxt, GUILayout.Width(providerButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Videoprovider", _controlId + 4);
                    }
                }

                if (_lightingType != null)
                {
                    //EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(providerGui);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Lighting Provider", EditorStyles.boldLabel);
                    if (GUILayout.Button("Change", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Lightingprovider", _controlId + 5);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(normalButtonWidth), GUILayout.Height(30)))
                    {
                        _lightingType = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_lightingType != null)
                        Editor.CreateEditor(_providerSet.gameObject.GetComponent(_lightingType)).OnInspectorGUI();
                    EditorGUILayout.EndVertical();
                    //EditorGUILayout.EndHorizontal();

                    AddSpace(1);
                }
                else
                {
                    var lightingBtnTxt = _lightingType == null ? "Add Lighting" : "Change Lighting";
                    if (GUILayout.Button(lightingBtnTxt, GUILayout.Width(providerButtonWidth), GUILayout.Height(30)))
                    {
                        EditorGUIUtility.ShowObjectPicker<MonoScript>(null, false, "Lightingprovider", _controlId + 5);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            //EditorGUILayout.BeginHorizontal();
            //AddSpace(2);
            //if (GUILayout.Button("Save", GUILayout.Width(_providerButtonWidth), GUILayout.Height(30)))
            //{
            //    if (String.IsNullOrEmpty(_providerSetName))
            //    {
            //        _displayEmptyNameError = true;
            //    }
            //    else
            //    {
            //        PrefabUtility.CreatePrefab("Assets/Sturfee/Resources/Provider Sets/Custom/" + _providerSetName + ".prefab", _providerSet.gameObject);
            //        DestroyImmediate(_providerSet.gameObject);
            //    }
            //}
            //AddSpace(2);
            //EditorGUILayout.EndHorizontal();


            if (_displayEmptyNameError)
            {

                if (!String.IsNullOrEmpty(_providerSetName))
                {
                    _displayEmptyNameError = false;
                }
                else
                {
                    var errorStyle = new GUIStyle(GUI.skin.label);
                    errorStyle.normal.background = MakeTex(2, 2, new Color(160 / 255.0f, 40 / 255.0f, 40 / 255.0f));
                    errorStyle.normal.textColor = Color.white;
                    errorStyle.alignment = TextAnchor.MiddleCenter;

                    AddSpace(5);
                    GUILayout.Label("Provider Set Name cannot be empty", errorStyle);
                }
            }
            //            EditorGUILayout.EndHorizontal();

        }

    }

    private void AddSpace(int numberOfSpaces = 1)
    {
        for (int i = 0; i < numberOfSpaces; i++)
        {
            EditorGUILayout.Space();
        }
    }

    public static void CheckSubscription(string accessToken, Action<SturfeeSubscriptionInfo> callback)
    {
        _www = SturfeeSubscriptionManager.GetSubscription(accessToken, callback);

        EditorApplication.update += SturfeeSubscriptionManager.OnResponse;

        //_www.SendWebRequest();
    }
}







