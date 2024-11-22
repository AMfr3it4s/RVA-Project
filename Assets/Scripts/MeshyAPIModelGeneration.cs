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


public class MeshyAPIModelGeneration : MonoBehaviour
{
    [Header ("API EndPoints")]
    [Section("Model Generation EndPoint")]
    [SerializeField] private string apiEndpoint = "https://api.meshy.ai/v2/text-to-3d";
    [Section("Texture Generation EndPoint")]
    [SerializeField] private string textureEndpoint = "https://api.meshy.ai/v1/text-to-texture";
    [Header("Prompt for the Meshy API")]
    [SerializeField] private string objectPrompt = "A small cloud with a kid flying on it";

    [Header("Reference to the Input Field")]
   // [SerializeField] private InputField inputField;

    [Header("Reference to the Interaction Prefab for every generated Model")]
    [SerializeField] private GameObject interactionPrefab;

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject infoMenu;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private string previousModelUrl;    
    private string apiPrompt;
    private string apiKey = "msy_7CmTCMbl1bWILcXGlW3bdw1MMIue5Bnoi3om"; //"msy_qw5ZYGNc0WBVq1tG5hZo9aJIHtCuNoJkHshz"
    private string taskID = string.Empty;
    private bool isFetchingResponse = false;  
    private List<GameObject> createdObjects = new List<GameObject>();

    void Start()
    {   
        GenerateModel();
        Debug.Log("Request");   
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
                
    }

    // Does the first request to the meshy API with the prompt given by the user, it will retain the task ID, this task will be updated with the GetResponse() function
    public IEnumerator RequestObject()
    {   
       // apiPrompt = inputField.text;
        Debug.Log("ENTERING REQUEST OBJECT TO THE API AREA");
        messageText.text = "ENTERING REQUEST OBJECT TO THE API AREA";

        var requestBody = new
        {
            mode = "preview",
            prompt = objectPrompt , //Dinamicaly Pass the Input from the user
            art_style = "realistic",
            negative_prompt = "medium quality"
        };

        string json = JsonConvert.SerializeObject(requestBody);
        byte[] byteData = Encoding.UTF8.GetBytes(json);


        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
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
        // apiPrompt = inputField.text;
        Debug.Log("ENTERING REQUEST OBJECT TO THE API AREA");
        messageText.text = "ENTERING REQUEST OBJECT TO THE API AREA";

        var requestBody = new
        {
           //request body 
           model_url = previousModelUrl,
           style_prompt = "realistic",
           enable_original_uv = true,
           enable_pbr = true,
           resolution = "4096",
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

    public IEnumerator GetResponseTexture()
    {
     isFetchingResponse = true;

    //Loop to check if the API returned de taskId, runs every 10 seconds
    while (!string.IsNullOrEmpty(taskID))
    {
        Debug.Log("Fetching response from Meshy...");
        messageText.text = "Fetching response from Meshy";

        using (UnityWebRequest request = UnityWebRequest.Get($"{textureEndpoint}/{taskID}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching response: " + request.error);
                messageText.text = "Error fetching response: " + request.error;
                break; 
            }
            else
            {
                Debug.Log("Response received: " + request.downloadHandler.text);
                messageText.text = "Response received: " + request.downloadHandler.text;

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);

                if (response.ContainsKey("status") && response["status"].ToString() == "SUCCEEDED" && response.ContainsKey("texture_urls"))
                {
                    Debug.Log("Model generation succeeded!");
                    messageText.text = "Model generation succeeded!";

                    var textureUrls = JsonConvert.DeserializeObject<Dictionary<string, string>>(response["texture_urls"].ToString());
                    if (textureUrls.ContainsKey("base_color"))
                    {   
                        messageText.text = "Generating Texture";
                        Texture texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                        Renderer renderer = createdObjects[createdObjects.Count - 1].GetComponent<Renderer>();
                        renderer.material.SetTexture("_BaseMap", texture);
                        
                        taskID = string.Empty;
                        infoMenu.SetActive(false);
                        break; 
                    }
                }
                else
                {
                    Debug.Log("Model not ready yet. Retrying...");
                    messageText.text = "Model not ready yet. Retrying...";
                }
            }
        }

        yield return new WaitForSeconds(10); // Waiting Time
    }

    isFetchingResponse = false;
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

        using (UnityWebRequest request = UnityWebRequest.Get($"{apiEndpoint}/{taskID}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching response: " + request.error);
                messageText.text = "Error fetching response: " + request.error;
                break; 
            }
            else
            {
                Debug.Log("Response received: " + request.downloadHandler.text);
                messageText.text = "Response received: " + request.downloadHandler.text;

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);

                if (response.ContainsKey("status") && response["status"].ToString() == "SUCCEEDED" && response.ContainsKey("model_urls"))
                {
                    Debug.Log("Model generation succeeded!");
                    messageText.text = "Model generation succeeded!";

                    var modelUrls = JsonConvert.DeserializeObject<Dictionary<string, string>>(response["model_urls"].ToString());
                    if (modelUrls.ContainsKey("glb"))
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
                }
                else
                {
                    Debug.Log("Model not ready yet. Retrying...");
                    messageText.text = "Model not ready yet. Retrying...";
                }
            }
        }

        yield return new WaitForSeconds(10); // Waiting Time
    }

    isFetchingResponse = false;
}

    public void GenerateModel()
    {
        //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            //cube.transform.position = new Vector3(0, 0, 2);
            //cube.transform.rotation = Quaternion.Euler(new Vector3(45, 45, 0));


            //Loading a gltf

            //GameObject gameObject = new GameObject();

            //var gltf = gameObject.AddComponent<GLTFast.GltfAsset>();
            //gltf.Url = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Duck/glTF/Duck.gltf";

            //gameObject.transform.position = new Vector3(0, 0, 2);
            //gameObject.transform.rotation = Quaternion.Euler(new Vector3(45, 45, 0));


            //Starts function to do a request to the API
            StartCoroutine(RequestObject());
            

            //current.addComponent<BoxCollider>();
    }


     private void AddHandInteraction(GameObject generatedModel)
    {   
        if(generatedModel == null)
        {
            Debug.Log("Model not found");
            return;
        }


        // Add Rigidbody
        Rigidbody rb = generatedModel.GetComponent<Rigidbody>();
        if(rb == null)
        {
            rb = generatedModel.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 20f;
        
        // Add Collider
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
        collider.sharedMesh = mesh;




        // Add Grabbale Interaction for commands and hands~

        //Attach GameObject Interaction To parent Object
        GameObject interactionPrefabInstance = Instantiate(interactionPrefab, generatedModel.transform, worldPositionStays:false);
        interactionPrefabInstance.transform.SetParent(generatedModel.transform, worldPositionStays: false);
       

        /*
        //Interaction Components of the object
       Grabbable grabbable = interactionPrefab.GetComponent<Grabbable>();
       HandGrabInteractable handGrabInteractable = interactionPrefab.GetComponent<HandGrabInteractable>();
       GrabInteractable grabInteractable = interactionPrefab.GetComponent<GrabInteractable>();
       GrabFreeTransformer grabFreeTransformer = interactionPrefab.GetComponent<GrabFreeTransformer>();

       //Populate Necessary fields of the components
       if(grabbable != null)
        {
            grabbable.InjectOptionalRigidbody(generatedModel.GetComponent<Rigidbody>());
            grabbable.TransferOnSecondSelection = false;
            grabbable.InjectOptionalOneGrabTransformer(grabFreeTransformer);
            grabbable.InjectOptionalTwoGrabTransformer(grabFreeTransformer);
            grabbable.InjectOptionalTargetTransform(generatedModel.transform);
        }
       if(handGrabInteractable != null)
        {
            handGrabInteractable.InjectRigidbody(generatedModel.GetComponent<Rigidbody>());
            handGrabInteractable.InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.Pinch);
        }
        if(grabInteractable != null)
        {
            grabInteractable.InjectRigidbody(generatedModel.GetComponent<Rigidbody>());
        }
        */
                
    }






}
