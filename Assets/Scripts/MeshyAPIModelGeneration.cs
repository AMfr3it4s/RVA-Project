using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using TriLibCore;
using CandyCoded.env;
using Meta.XR.MRUtilityKit.SceneDecorator;



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
    [SerializeField] private InputField inputField;
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
    [SerializeField] private List<GameObject> createdObjects = new List<GameObject>();

    void Start()
    {
        apiPrompt = inputField.text;
        //switch for the api keys 1,2,3
        env.variables.TryGetValue("API_KEY3", out apiKey);
       //GenerateModel(); //<---- Just For Debug Porpouse Later will be removed
       
    }
    void Update()
    {   
        //Verified if API returned an taskID and if the task is not being fetched
        if (!string.IsNullOrEmpty(taskID) && !isFetchingResponse)
    {
        StartCoroutine(GetResponse());
        audioSource.Play();
        messageText.text = "LOADING 3D MODEL";
    }  
        //Verified if API already ended the task of Generating the model afther that will generate the texture
        if(!string.IsNullOrEmpty(taskIdTexture)  && !isFetchingResponseTexture)
        {
            StartCoroutine(GetResponseTexture());
        }
                
    }

    // Does the first request to the meshy API with the prompt given by the user, it will retain the task ID, this task will be updated with the GetResponse() function
    public IEnumerator RequestObject()
    {   
        
        messageText.text = "ENTERING REQUEST OBJECT TO THE API AREA";

        var requestBody = new
        {
            mode = "preview",
            prompt = apiPrompt , //Dinamicaly Pass the Input from the user change to apiPrompt latter || Mannualy change to objectPrompt
            art_style = "realistic",
            negative_prompt = "low quality, simple",
            texture_richness = "medium",
            ai_model = "meshy-4",
            target_polycount = 30000,
            topology = "triangle",
        };

        string json = JsonConvert.SerializeObject(requestBody);
        byte[] byteData = Encoding.UTF8.GetBytes(json);


        using (UnityWebRequest request = new UnityWebRequest(modelEndpoint, "POST"))
        {

            request.uploadHandler = new UploadHandlerRaw(byteData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization",$"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            //Debug.Log("Sending request" +json);
            messageText.text = "Sending request";

            // Send the request
            Debug.Log("Sending request");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                errorFlag = "Request Object";
                messageText.text = "Error: " + request.error;
                
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
                    buttonGameobject.SetActive(true);
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
           object_prompt = $"The Model is an {apiPrompt} make me a texture for it realistic and multicolor", // To be Implemented to make this dynamic has the model is
           style_prompt = "realistic",
           enable_original_uv = false,
           enable_pbr = false,
           resolution = "4096",
           negative_prompt = "low quality, low poly, single color",
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

    //Loop to check if the API returned de taskId, runs every 10 seconds -> Needes to be tested the delay time meay not the ideal one
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
                                messageText.text = "Generating Texture";
                                textureUrl = baseColorUrl;
                                StartLoadingTextures();
                                taskIdTexture = string.Empty;
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
                        if(response.ContainsKey("status") && response["status"].ToString() == "FAILED") 
                    {
                        messageText.text = "Error Loading Texture Try Again";
                        errorFlag = "TextureFailed";
                        buttonGameobject.SetActive(true);
                        break;
                    }
                    
                }
                    
        }

        yield return new WaitForSeconds(10); // Waiting Time
    }

    isFetchingResponseTexture = false;
}   
    


    // Checks, in intervals of 10 seconds, the state of the task given to Meshy API, if the Model is rendered, the URL to the glb/fbx file is retrieved and rendered in runtime
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

                        // Obt�m a URL da textura (a primeira textura da lista)


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
        rb.isKinematic = false;
        rb.mass = 20f;

        // Add Collider
        messageText.text = "Applying Collider";
        MeshFilter meshFilter = generatedModel.GetComponentInChildren<MeshFilter>();
        if(meshFilter == null)
        {
            Debug.Log("MeshFilter not Found");
            return;
        }
        BoxCollider boxCollider = generatedModel.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = generatedModel.AddComponent<BoxCollider>();
            }
            boxCollider.size = new Vector3(1.5f,2f,1.5f);
            boxCollider.isTrigger = false;

        /*
        Mesh mesh = meshFilter.mesh;
        MeshCollider collider = generatedModel.GetComponent<MeshCollider>();
        if(collider == null)
        {
            collider = generatedModel.AddComponent<MeshCollider>();
        }
        collider.sharedMesh = mesh;
        collider.convex = false;
        collider.isTrigger = false;*/


        // Add Grabbale Interaction for commands and hands

        //Attach GameObject Interaction To parent Object


        var grabFreeTransform = generatedModel.GetComponent<GrabFreeTransformer>();        
        // Change Constraints In Grab Free Transformer
        if(grabFreeTransform ==null)
        {   
            grabFreeTransform = generatedModel.AddComponent<GrabFreeTransformer>();
            var newScaleConstraints = new TransformerUtils.ScaleConstraints
        {
            ConstraintsAreRelative = true, 
            XAxis = new TransformerUtils.ConstrainedAxis
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange { Min = 0.5f, Max = 3.0f } 
            },
            YAxis = new TransformerUtils.ConstrainedAxis
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange { Min = 0.5f, Max = 3.0f } 
            },
            ZAxis = new TransformerUtils.ConstrainedAxis
            {
                ConstrainAxis = true,
                AxisRange = new TransformerUtils.FloatRange { Min = 0.5f, Max = 3.0f } 
            }
        }; 
        grabFreeTransform.InjectOptionalScaleConstraints(newScaleConstraints);
        }


        var grabbable = generatedModel.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = generatedModel.AddComponent<Grabbable>();
        }
        grabbable.InjectOptionalRigidbody(rb);
        grabbable.enabled = true;
        grabbable.InjectOptionalTargetTransform(generatedModel.transform);
        grabbable.InjectOptionalOneGrabTransformer(grabFreeTransform);
        grabbable.InjectOptionalTwoGrabTransformer(grabFreeTransform);
        

        var handGrabInteractable = generatedModel.GetComponent<HandGrabInteractable>();
        if (handGrabInteractable == null)
        {
            handGrabInteractable = generatedModel.AddComponent<HandGrabInteractable>();
            handGrabInteractable.InjectRigidbody(rb);
            handGrabInteractable.InjectOptionalPointableElement(grabbable);
            handGrabInteractable.InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.Pinch);
            //handGrabInteractable.InjectPinchGrabRules()

        }
        handGrabInteractable.enabled = true;

        var grabInteractable = generatedModel.GetComponent<GrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = generatedModel.AddComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rb);
            grabInteractable.InjectOptionalPointableElement(grabbable);
        }
        grabInteractable.enabled = true;


        
        
        
        StartCoroutine(RequestTexture());

    }


    private Constraint CreateConstraint()
    {
        Constraint constraint = new Constraint();
        constraint.min = 0.5f;
        constraint.max = 3.0f;
        return constraint;
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


    //Load The textures provided by AI, and assign them to the material of the model
    private IEnumerator LoadTextures()
    {
       Texture2D baseColorTexture = null;

        // Download apenas a textura Base Map
        yield return StartCoroutine(DownloadTexture(textureUrl, texture => baseColorTexture = texture));

        if (baseColorTexture != null)
        {
            Material newMaterial = new Material(Shader.Find("Unlit/Texture"));

            // Define o Base Map no material
            newMaterial.SetTexture("_BaseMap", baseColorTexture);

            if (createdObjects.Count > 0)
            {
                GameObject loadedModel = createdObjects[createdObjects.Count - 1];
                var renderer = loadedModel.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material = newMaterial;
                }
            }

            Debug.Log("Textura Base Map foi carregada e aplicada.");
            messageText.text = "Textura Base Map foi carregada e aplicada.";
        }
        else
        {
            Debug.LogError("Erro ao carregar a textura Base Map.");
            messageText.text = "Erro ao carregar a textura Base Map.";
            errorFlag = "Erro ao carregar textura Base Map";
            buttonGameobject.SetActive(true);
        }

    }

    //Download Function For Textures Based on URL
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
        loadedModel.transform.localScale = new Vector3 (0.5f, 0.5f, 0.5f);
        loadedModel.transform.position = new Vector3(0, 1f, 1.5f);
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
            case "TextureFailed":
                messageText.text = "Trying Generate Texture again!";
                RequestTexture();
                break;
            //More Case To Do If needed Implement Here
            default:
                messageText.text = "Trying Generate Model again!";
                GenerateModel();
                break;
        }   
        
    }

    //When Something Happens Send a Message to the UI with the Flag Error
    public void OnButtonClickTry()
    {
        ErrorHandling(errorFlag);
        buttonGameobject.SetActive(false);
        errorFlag = "";
    }

    // On Click Button Submit Prompt To AI
    public void OnSubtmitButtonClick(InputField inputFromUser)
    {
       string userInput = inputFromUser.text;

    if (string.IsNullOrWhiteSpace(userInput))
    {
        Debug.Log("No Empty Fields");
        return;
    }

    apiPrompt = userInput;
    
    inputFromUser.text = string.Empty;
    }
    
}
