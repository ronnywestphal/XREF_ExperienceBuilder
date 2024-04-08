using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using AssetBundleBrowser.AssetBundleDataSource;
using PlasticGui.WorkspaceWindow.CodeReview;

public class PluginEditor : EditorWindow
{
    JObject parsedResponse;

    private Vector2 scrollPosition;
    private HashSet<string> selectedExperienceIds = new HashSet<string>();

    
    private string userID;

    [MenuItem("Window/UI Toolkit/PluginEditor")]
    public static void ShowExample()
    {
        PluginEditor wnd = GetWindow<PluginEditor>();
        wnd.titleContent = new GUIContent("PluginEditor");
    }

    void OnEnable()
    {
        CreateSignInUI();
    }

    private void CreateSignInUI()
    {
        VisualElement root = rootVisualElement;
        root.Clear();

        TextField emailField = new TextField("EmailField")
        {
            label = "Email"
        };
        root.Add(emailField);

        TextField passwordField = new TextField("PasswordField")
        {
            label = "Password",
            isPasswordField = true
        };
        root.Add(passwordField);

        Button button = new Button(() =>
        {
            //CoroutineRunner.Instance.StartCoroutine(AuthenticateUser(emailField.value, passwordField.value));
            CoroutineRunner.Instance.StartCoroutine(AuthenticateUser("west.ronny89@gmail.com", "Ron.West95!"));
        });
        button.style.height = 30;
        button.text = "Sign In";
        root.Add(button);
    }

    private void CreateMetadataUI(JObject metadata)
    {
        VisualElement root = rootVisualElement;
        root.Clear();

        Label userLabel = new Label("User ID: " + userID);
        userLabel.style.paddingTop = 10; 
        userLabel.style.paddingBottom = 10; 
        root.Add(userLabel);
        Label expLabel = new Label("Experiences");
        root.Add(expLabel);
        
        foreach (var pair in metadata)
        {
            VisualElement box = new VisualElement();
            box.style.borderTopWidth = box.style.borderRightWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = 1;
            box.style.borderTopColor = box.style.borderRightColor = box.style.borderBottomColor = box.style.borderLeftColor = new Color(0.6f, 0.6f, 0.6f);

            JObject experience = pair.Value as JObject;
            string title = experience["titles"]["English"]?.ToString() ?? pair.Key; // Use the experience id if the title is not available

            Button button = new Button(() =>
            {
                if (selectedExperienceIds.Contains(pair.Key))
                {
                    selectedExperienceIds.Remove(pair.Key);
                }
                else
                {
                    selectedExperienceIds.Add(pair.Key);
                }
                CreateUploadUI(pair.Key);
            })
            {
                text = "Experience: " + title
            };

            box.Add(button);
            root.Add(box);
        }        
    }

    private void CreateUploadUI(string experienceId)
    {
        VisualElement root = rootVisualElement;
        root.Clear();

        Button backButton = new Button(() =>
        {
            CreateMetadataUI(parsedResponse);
        })
        {
            text = "Back"
        };

        root.Add(backButton);
        Label label = new Label("Upload bundles to experience: " + experienceId);
        root.Add(label);

        Toggle androidToggle = new Toggle("Android");
        root.Add(androidToggle);

        Toggle iosToggle = new Toggle("iOS");
        root.Add(iosToggle);

        Label selectedBundleLabel = new Label();
        string selectedFolderPath = "";
        bool bundleCreated = false;

        Button browseButton = new Button(() =>
        {
            // Open a folder dialog and get the selected folder path
            string path = EditorUtility.OpenFilePanel("Select a folder", "", "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                selectedFolderPath = path;
                selectedBundleLabel.text = "Selected folder: " + path;
            }
        })
        {
            text = "Browse"
        };

        browseButton.SetEnabled(false);
        root.Add(browseButton);
        root.Add(selectedBundleLabel);
        
        ABBuildInfo buildInfo = null;
        string bundlePath = "";

        Button createBundleButton = new Button(() =>
        {
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                Debug.Log("No folder selected");
                return;
            }

            string relativeFolderPath = "Assets" + selectedFolderPath.Substring(Application.dataPath.Length);
            string prefabName = Path.GetFileNameWithoutExtension(selectedFolderPath); // Get the prefab name

            Label statusLabel = new Label();
            root.Add(statusLabel);

            // Get the first data source. Adjust this if you have multiple data sources.
            ABDataSource dataSource = AssetDatabaseABDataSource.CreateDataSources()[0];
            
            if (iosToggle.value)
            {
                statusLabel.text = "Building iOS Bundle...";
                buildInfo = new ABBuildInfo
                {
                    outputDirectory = "Assets/AssetBundles/IOS",
                    options = BuildAssetBundleOptions.None,
                    buildTarget = BuildTarget.iOS
                };
                dataSource.BuildAssetBundles(buildInfo);
                bundlePath = Path.Combine(buildInfo.outputDirectory, prefabName.ToLower() + ".assetbundle");

            }

            if (androidToggle.value)
            {
                statusLabel.text = "Building Android Bundle...";
                buildInfo = new ABBuildInfo
                {
                    outputDirectory = "Assets/AssetBundles/Android",
                    options = BuildAssetBundleOptions.None,
                    buildTarget = BuildTarget.Android
                };
                dataSource.BuildAssetBundles(buildInfo);
                bundlePath = Path.Combine(buildInfo.outputDirectory, prefabName.ToLower());

            }

            bundleCreated = true;
            statusLabel.text = "Bundle created";
            
            Debug.Log("Uploading bundles to experience: " + experienceId);
            CoroutineRunner.Instance.StartCoroutine(UploadBundle(experienceId, "artwork", buildInfo.buildTarget.ToString(), bundlePath, ""));
        })
        {
            text = "Create & Upload Bundle"
        };

        createBundleButton.SetEnabled(false);
        root.Add(createBundleButton);

        Button uploadButton = new Button(() =>
        {
            if (!bundleCreated)
            {
                Debug.Log("No bundle created");
                return;
            }

            Debug.Log("Uploading bundles to experience: " + experienceId);
            CoroutineRunner.Instance.StartCoroutine(UploadBundle(experienceId, "artwork", buildInfo.buildTarget.ToString(), bundlePath, ""));
        })
        {
            text = "Upload",
        };

        uploadButton.SetEnabled(false);
        root.Add(uploadButton);

        // Update the enabled state of the buttons whenever the selected folder path or the bundle created flag changes
        androidToggle.RegisterValueChangedCallback((evt) => { browseButton.SetEnabled(androidToggle.value || iosToggle.value); });
        iosToggle.RegisterValueChangedCallback((evt) => { browseButton.SetEnabled(iosToggle.value || androidToggle.value); });
        browseButton.clicked += () => { createBundleButton.SetEnabled(!string.IsNullOrEmpty(selectedFolderPath)); };
        createBundleButton.clicked += () => { uploadButton.SetEnabled(bundleCreated); };
    }

    private IEnumerator UploadBundle(string experienceId, string buildType, string platform, string bundlePath, string bundleType)
    {
        BundleInfo info = new BundleInfo
        {
            uid = userID,
            experienceId = experienceId,
            buildType = buildType,
            platform = platform,
            bundleType = bundleType,
            bundleFile = Convert.ToBase64String(File.ReadAllBytes(bundlePath))
        };

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(info));
        UnityWebRequest request = new UnityWebRequest("https://exp-uploadassetbundle-u7lfzepmiq-ew.a.run.app", "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
    }

    private IEnumerator AuthenticateUser(string aEmail, string aPassword)
    {
        var request = new UnityWebRequest("https://auth-authenticateuser-u7lfzepmiq-ew.a.run.app", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new LoginData { email = aEmail, password = aPassword }));
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            string json = request.downloadHandler.text;
            JObject parsedResponse = JObject.Parse(json);
            if (parsedResponse != null)
            {
                userID = parsedResponse["uid"].ToString(); 
                Debug.Log("User ID: " + userID);
                CoroutineRunner.Instance.StartCoroutine(GetUserExperienceMetadatas(userID));
            }
            else
            {
                Debug.Log("Response did not contain expected data");
            }
        }
    }
  
    private IEnumerator GetUserExperienceMetadatas(string uid)
    {
        var request = new UnityWebRequest("https://users-getuserexperiencemetadatashttps-u7lfzepmiq-ew.a.run.app", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new MetadataRequestParams{ uid = uid, includeTests = true, includeNotLive = true }));
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(request.error);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            string json = request.downloadHandler.text;
            parsedResponse = JObject.Parse(json);
            CreateMetadataUI(parsedResponse);   
        }
    }

    void OnDestroy()
    {
        CoroutineRunner.Instance.StopAllCoroutines();
    }
    [System.Serializable]
    private class LoginData
    {
        public string email;
        public string password;
    }
    private class MetadataRequestParams
    {
        public string uid;
        public bool includeTests;
        public bool includeNotLive;
    }

    [System.Serializable]
    public class BundleInfo
    {
        public string uid;
        public string experienceId;
        public string buildType;
        public string platform;
        public string bundleType;
        public string bundleFile;
    }

}
