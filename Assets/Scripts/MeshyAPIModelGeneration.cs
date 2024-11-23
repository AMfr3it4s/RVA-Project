using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using TriLibCore;
using LibTessDotNet;
using CandyCoded.env;
using Unity.VisualScripting;





public class MeshyAPIModelGeneration : MonoBehaviour
{
    [Header ("API EndPoints")]
    [Section("Model Generation EndPoint")]
    [SerializeField] private string modelEndpoint = "https://api.meshy.ai/v2/text-to-3d";
    [Section("Texture Generation EndPoint")]
    [SerializeField] private string textureEndpoint = "https://api.meshy.ai/v1/text-to-texture";
    [Header("Prompt for the Meshy API")]
    [SerializeField] private string objectPrompt = "A small cloud with a kid flying on it";
    [Header("Model URL")]
    [SerializeField] private string modelUrl;
    [Header("Texture URL")]
    [SerializeField] private string textureUrl;
    [SerializeField] private string metallicUrl;
    [SerializeField] private string roughnessUrl;
    [SerializeField] private string normalUrl;
    [Header("Reference to the Input Field")]
    [SerializeField] private TMP_InputField inputField;
    [Header("Reference to the Interaction Prefab for every generated Model")]
    [SerializeField] private GameObject interactionPrefab;
    [Header("Reference to Error Handler Button")]
    [SerializeField] private GameObject buttonGameobject;
    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject infoMenu;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private string errorFlag;
    private string previousModelUrl;
    private string previousTextureUrl;
    private string apiPrompt;
    private string apiKey;
    private string taskID = string.Empty;
    private string taskIdTexture = string.Empty;
    private bool isFetchingResponse = false;
    private bool isFetchingResponseTexture = false;
    private List<GameObject> createdObjects = new List<GameObject>();

    void Start()
    {
        apiPrompt = inputField.text;
        //switch 1,2,3
        env.variables.TryGetValue("API_KEY3", out apiKey);
        GenerateModel();
    }
    void Update()
    {   
        //Verified if API returned an taskID and if the task is not being fetched
        if (!string.IsNullOrEmpty(taskID) && !isFetchingResponse)
    {
        StartCoroutine(GetResponse());
        audioSource.Play();
        Debug.Log("LOADING 3D MODEL");
        messageText.text = "LOADING 3D MODEL";
    }   
        if(!string.IsNullOrEmpty(taskIdTexture)  && !isFetchingResponseTexture)
        {
            StartCoroutine(GetResponseTexture());
        }
                
    }

    // Does the first request to the meshy API with the prompt given by the user, it will retain the task ID, this task will be updated with the GetResponse() function
    public IEnumerator RequestObject()
    {   
        
        Debug.Log("ENTERING REQUEST OBJECT TO THE API AREA");
        messageText.text = "ENTERING REQUEST OBJECT TO THE API AREA";

        var requestBody = new
        {
            mode = "preview",
            prompt = objectPrompt , //Dinamicaly Pass the Input from the user change to apiPrompt latter || Mannualy change to objectPrompt
            art_style = "realistic",
            negative_prompt = "medium quality",
            texture_richness = "medium"
        };

        string json = JsonConvert.SerializeObject(requestBody);
        byte[] byteData = Encoding.UTF8.GetBytes(json);


        using (UnityWebRequest request = new UnityWebRequest(modelEndpoint, "POST"))
        {
            //byte[] bodyRaw = byteData;
            request.uploadHandler = new UploadHandlerRaw(byteData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization",$"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending request" +json);
            messageText.text = "Sending request";

            // Send the request
            Debug.Log("Sending request");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                errorFlag = "Request Object";
                buttonGameobject.SetActive(true);
            }
            else
            {
                Debug.Log("Response: " + request.downloadHandler.text);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                if (response.ContainsKey("result"))
                {
                    taskID = response["result"].ToString();
                    Debug.Log("Task ID: " + taskID);
                    messageText.text = "Task ID: " + taskID;
                }
                else
                {
                    Debug.LogError("Error: 'result' field not found in the response.");
                    messageText.text = "Error: 'result' field not found in the response";
                }
            }
        }
    }

    public IEnumerator RequestTexture()
    {
        
        Debug.Log("ENTERING REQUEST OBJECT TO THE API AREA");
        messageText.text = "ENTERING REQUEST OBJECT TO THE API AREA";

        var requestBody = new
        {
           //request body 
           model_url = previousModelUrl,
           object_prompt = "realistic texture",
           style_prompt = "realistic",
           enable_original_uv = true,
           enable_pbr = true,
           resolution = "2048",
           negative_prompt = "medium quality",
        };

        string json = JsonConvert.SerializeObject(requestBody);
        byte[] byteData = Encoding.UTF8.GetBytes(json);


        using (UnityWebRequest request = new UnityWebRequest(textureEndpoint, "POST"))
        {
            //byte[] bodyRaw = byteData;
            request.uploadHandler = new UploadHandlerRaw(byteData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization",$"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending request" +json);
            messageText.text = "Sending request";

            // Send the request
            Debug.Log("Sending request");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                Debug.Log("Response: " + request.downloadHandler.text);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                if (response.ContainsKey("result"))
                {
                    taskIdTexture = response["result"].ToString();
                    Debug.Log("Task ID: " + taskID);
                    messageText.text = "Task ID: " + taskIdTexture;
                }
                else
                {
                    Debug.LogError("Error: 'result' field not found in the response.");
                    messageText.text = "Error: 'result' field not found in the response";
                }
            }
        }
    }

    public IEnumerator GetResponseTexture()
    {
     isFetchingResponseTexture = true;

    //Loop to check if the API returned de taskId, runs every 10 seconds
    while (!string.IsNullOrEmpty(taskIdTexture))
    {
        Debug.Log("Fetching response from Meshy...");
        messageText.text = "Fetching response from Meshy";

        using (UnityWebRequest request = UnityWebRequest.Get($"{textureEndpoint}/{taskIdTexture}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching response: " + request.error);
                messageText.text = "Error fetching response: " + request.error;
                buttonGameobject.SetActive(true);
                break; 
            }
            else
            {
                Debug.Log("Response received: " + request.downloadHandler.text);
                messageText.text = "Response received: " + request.downloadHandler.text;

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);

                        if (response.ContainsKey("status") && response["status"].ToString() == "SUCCEEDED" && response.ContainsKey("texture_urls"))
                        {   
                            Debug.Log("Texture generation succeeded!");
                            messageText.text = "Texture generation succeeded!";

                            var textureUrls = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(response["texture_urls"].ToString()); ;
                            if (textureUrls != null && textureUrls.Count > 0 && textureUrls[0].ContainsKey("base_color"))
                            {
                                string baseColorUrl = textureUrls[0]["base_color"];
                                string metallicUrls = textureUrls[0]["metallic"];
                                string roroughnessUrls = textureUrls[0]["roughness"];
                                string normalUrls = textureUrls[0]["normal"];
                            messageText.text = "Generating Texture";
                                textureUrl = baseColorUrl;
                                metallicUrl = metallicUrls;
                                roughnessUrl = roroughnessUrls;
                                normalUrl = normalUrls;
                                StartLoadingTextures();
                                taskID = string.Empty;
                                infoMenu.SetActive(false);
                                audioSource.Stop();
                                break;
                            }
                        }
                        else
                        {
                            Debug.Log("Texture not ready yet. Retrying...");
                            messageText.text = "Texture Not Ready Yet. Retrying...";
                        
                        }
                    
                }
                    
        }

        yield return new WaitForSeconds(10); // Waiting Time
    }

    isFetchingResponseTexture = false;
}   
    


    // Checks, in intervals of 5 seconds, the state of the task given to Meshy API, if the Model is rendered, the URL to the glb file is retrieved and rendered in runtime
    public IEnumerator GetResponse()
{
    isFetchingResponse = true;

    //Loop to check if the API returned de taskId, runs every 10 seconds
    while (!string.IsNullOrEmpty(taskID))
    {
        Debug.Log("Fetching response from Meshy...");
        messageText.text = "Fetching response from Meshy";

        using (UnityWebRequest request = UnityWebRequest.Get($"{modelEndpoint}/{taskID}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching response: " + request.error);
                messageText.text = "Error fetching response: " + request.error;
                buttonGameobject.SetActive(true);
                errorFlag = "GetReponseObject";
                break; 
            }
            else
            {
                Debug.Log("Response received: " + request.downloadHandler.text);
                messageText.text = "Response received: " + request.downloadHandler.text;

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);

                if (response.ContainsKey("status") && response["status"].ToString() == "SUCCEEDED" && response.ContainsKey("model_urls") && response.ContainsKey("texture_urls"))
                {
                    Debug.Log("Model generation succeeded!");
                    messageText.text = "Model generation succeeded!";

                    var modelUrls = JsonConvert.DeserializeObject<Dictionary<string, string>>(response["model_urls"].ToString());

                    var textureUrls = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(response["texture_urls"].ToString());

                        // Obtém a URL da textura (a primeira textura da lista)


                        //GLB MODEL URL DOWNLOAD
                        /*if (modelUrls.ContainsKey("glb"))
                        {
                            string glbUrl = modelUrls["glb"];
                            Debug.Log($"GLB URL: {glbUrl}");
                            previousModelUrl = glbUrl;
                            messageText.text = "SpawningModel";

                            GameObject gameObject = new GameObject("GeneratedModel");
                            var gltf = gameObject.AddComponent<GLTFast.GltfAsset>();
                            gltf.Url = glbUrl;
                            gameObject.transform.position = new Vector3(0, 2, 1);
                            AddHandInteraction(gameObject);
                            createdObjects.Add(gameObject);
                            taskID = string.Empty;
                            infoMenu.SetActive(false);
                            audioSource.Stop();
                            break; 
                        }
                        */
                        //FBX MODEL URL DOWNLOAD

                        //Verified if the generated model has textures or not; it may have textures in the future.
                        if (textureUrls != null && textureUrls.Count > 0 && textureUrls[0].ContainsKey("base_color"))
                        {
                            if (modelUrls.ContainsKey("fbx") && modelUrls.ContainsKey("glb"))
                            {
                                string fbxUrl = modelUrls["fbx"];
                                string baseColorUrl = textureUrls[0]["base_color"];
                                Debug.Log($"FBX URL: {fbxUrl}");
                                previousModelUrl = fbxUrl;
                                previousTextureUrl = baseColorUrl;
                                messageText.text = "SpawningModel";
                                modelUrl = fbxUrl;
                                textureUrl = baseColorUrl;
                                LoadModel();
                                StartLoadingTextures();
                                taskID = string.Empty;
                                break;
                            }
                        }else
                        {
                            if (modelUrls.ContainsKey("fbx") && modelUrls.ContainsKey("glb"))
                            {
                                string fbxUrl = modelUrls["fbx"];
                                string glbUrl = modelUrls["glb"];
                                Debug.Log($"FBX URL: {fbxUrl}");
                                previousModelUrl = glbUrl;
                                messageText.text = "SpawningModel";
                                modelUrl = fbxUrl;
                                LoadModel();
                                taskID = string.Empty;
                                break;
                            }
                        }
                            
                }
                else
                {
                    Debug.Log("Model not ready yet. Retrying...");
                    messageText.text = "Model not ready yet. Retrying...";
                }

            buttonGameobject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(10); // Waiting Time
    }

    isFetchingResponse = false;
}   



    public void GenerateModel()
    {
        //Loading a gltf

        //GameObject gameObject = new GameObject();

        //var gltf = gameObject.AddComponent<GLTFast.GltfAsset>();
        //gltf.Url = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Duck/glTF/Duck.gltf";

        //Starts function to do a request to the API
        StartCoroutine(RequestObject());

    }


    private void AddHandInteraction(GameObject generatedModel)
    {
        messageText.text = "Finishing Model";
        if(generatedModel == null)
        {
            Debug.Log("Model not found");
            return;
        }


        // Add Rigidbody
        messageText.text = "Applying Rigidbody";
        Rigidbody rb = generatedModel.GetComponent<Rigidbody>();
        if(rb == null)
        {
            rb = generatedModel.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = true;
        rb.mass = 20f;

        

        // Add Collider
        messageText.text = "Applying Collider";
        MeshFilter meshFilter = generatedModel.GetComponentInChildren<MeshFilter>();
        if(meshFilter == null)
        {
            Debug.Log("MeshFilter not Found");
            return;
        }
        Mesh mesh = meshFilter.sharedMesh;
        MeshCollider collider = generatedModel.GetComponent<MeshCollider>();
        if(collider == null)
        {
            collider = generatedModel.AddComponent<MeshCollider>();
        }
        collider.sharedMesh = mesh;
        collider.convex = true;
        collider.isTrigger = false;


        // Add Grabbale Interaction for commands and hands

        //Attach GameObject Interaction To parent Object
        messageText.text = "Applying Interaction";
        GameObject interactionPrefabInstance = Instantiate(interactionPrefab, generatedModel.transform, worldPositionStays:false);
        interactionPrefabInstance.transform.SetParent(generatedModel.transform, worldPositionStays: false);

        var handGrabInteractable  = interactionPrefabInstance.GetComponent<HandGrabInteractable>();
        if(handGrabInteractable != null)
        {
            handGrabInteractable.enabled = true;
        }
        var grabInteractable = interactionPrefabInstance.GetComponent<GrabInteractable>();
        if(grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }
        
        interactionPrefabInstance.GetComponentInChildren<Grabbable>().InjectOptionalRigidbody(rb);
        interactionPrefabInstance.GetComponentInChildren<HandGrabInteractable>().InjectRigidbody(rb);
        interactionPrefabInstance.GetComponentInChildren<GrabInteractable>().InjectRigidbody(rb);


        StartCoroutine(RequestTexture());
        


    }

    private void LoadModel() 
    {
        var assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
        var webRequest = UnityWebRequest.Get(modelUrl);
        webRequest.SetRequestHeader("User-Agent", "Unity Web Request");
        webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        AssetDownloader.LoadModelFromUri(webRequest, OnLoad, OnMaterialsLoad, OnProgress, OnError, null, assetLoaderOptions, isZipFile: false, fileExtension: "fbx");
    }

    private void StartLoadingTextures()
    {
        StartCoroutine(LoadTextures());
    }

    private IEnumerator LoadTextures()
    {
        Texture2D baseColorTexture = null;
        Texture2D metallicTexture = null;
        Texture2D roughnessTexture = null;
        Texture2D normalTexture = null;

        
        yield return StartCoroutine(DownloadTexture(textureUrl, texture => baseColorTexture = texture));
        yield return StartCoroutine(DownloadTexture(metallicUrl, texture => metallicTexture = texture));
        yield return StartCoroutine(DownloadTexture(roughnessUrl, texture => roughnessTexture = texture));
        yield return StartCoroutine(DownloadTexture(normalUrl, texture => normalTexture = texture));

        
        if (baseColorTexture && metallicTexture && roughnessTexture && normalTexture)
        {
            
            Material newMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            newMaterial.SetTexture("_BaseMap", baseColorTexture);      
            newMaterial.SetTexture("_MetallicGlossMap", metallicTexture); 
            newMaterial.SetTexture("_SpecGlossMap", roughnessTexture); 
            newMaterial.SetTexture("_BumpMap", normalTexture);         

            
            if (createdObjects.Count > 0)
            {
                GameObject loadedModel = createdObjects[createdObjects.Count - 1];
                var renderer = loadedModel.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material = newMaterial;
                }
            }

            Debug.Log("All Textures Loaded and Applied");
            messageText.text = "All Textures Loaded and Applied";
        }
        else
        {
            Debug.LogError("Error While Loading One or More Textures");
            messageText.text = "Error While Loading One or More Textures";
            errorFlag = "Loading Textures";
            buttonGameobject.SetActive(true);
        }
    }

    private IEnumerator DownloadTexture(string url, System.Action<Texture2D> onSuccess)
    {
        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            webRequest.SetRequestHeader("User-Agent", "Unity Web Request");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                onSuccess?.Invoke(texture);
                
            }
            else
            {
                Debug.LogError($"Error loading texture from {url}: {webRequest.error}");
                messageText.text = $"Error loading texture from {url}: {webRequest.error}";
                errorFlag = "Loading Texture";
                buttonGameobject.SetActive(true);
            }
        }
    }


    private void OnError(IContextualizedError obj)
    {
        Debug.LogError($"Error While Loading The Model: {obj.GetInnerException()}");
        messageText.text = $"Error While Loading The Model: {obj.GetInnerException()}";
        errorFlag = "Loading Model";
        buttonGameobject.SetActive(true);
    }
    private void OnProgress(AssetLoaderContext assetLoaderContext, float progress)
    {
        Debug.Log($"Loading Model. Progress: {progress:P}");
        messageText.text = $"Loading Model. Progress: {progress:P}";
    }

    private void OnMaterialsLoad(AssetLoaderContext assetLoaderContext)
    {
        Debug.Log("Loading Materials");
        messageText.text = "Loading Materials";
        
    }

    private void OnLoad(AssetLoaderContext assetLoaderContext)
    {
        Debug.Log("Model Loaded");
        messageText.text = "Model Loaded";
        GameObject loadedModel = assetLoaderContext.RootGameObject;
        loadedModel.transform.position = new Vector3(0, 0.5f, 1.5f);
        createdObjects.Add(loadedModel);
        AddHandInteraction(loadedModel);

    }

    //Error Handling what to do if its get an error 
    private void ErrorHandling(string errorFlag) 
    {
        switch (errorFlag)
        {
            case "GetReponseObject":
                messageText.text = "Trying API Response again!";
                StartCoroutine(GetResponse());
                break;
            case "Request Object":
                messageText.text = "Trying API Request again!";
                StartCoroutine(RequestObject());
                break;
            case "Loading Model":
                messageText.text = "Trying Load Model again!";
                LoadModel();      
                break;
            default:
                messageText.text = "Trying Generate Model again!";
                GenerateModel();
                break;
        }   
        
    }

    public void OnButtonClickTry()
    {
        ErrorHandling(errorFlag);
        buttonGameobject.SetActive(false);
        errorFlag = "";
    }

}
