using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class MeshyAPIModelGeneration : MonoBehaviour
{
    //public Camera userCamera;
    public string apiEndpoint = "https://api.meshy.ai/v2/text-to-3d";
    public string objectPrompt = "A small cloud with a kid flying on it";
    private string apiKey = "msy_qw5ZYGNc0WBVq1tG5hZo9aJIHtCuNoJkHshz";//"msy_7CmTCMbl1bWILcXGlW3bdw1MMIue5Bnoi3om"
    public string taskID = string.Empty;
    private bool isFetchingResponse = false;  //In the start, it will not have any object to render, so the flag will stay true, when a r
    // list that holds all created objects - deleate all instances if desired
    public List<GameObject> createdObjects = new List<GameObject>();

    void Start()
    {
        GenerateModel();
    }
    // Update is called once per frame
    void Update()
    {   

        if (!string.IsNullOrEmpty(taskID) && !isFetchingResponse)
    {
        StartCoroutine(GetResponse());
        Debug.Log("LOADING 3D MODEL");
    }
        /*if (Input.GetButtonDown("Fire1"))
        {

            GenerateModel();

        }
        */

        
    }

    // Does the first request to the meshy API with the prompt given by the user, it will retain the task ID, this task will be updated with the GetResponse() function
    public IEnumerator RequestObject()
    {
        Debug.Log("ENTERING REQUEST OBJECT TO THE API AREA");

        var requestBody = new
        {
            mode = "preview",
            prompt = objectPrompt,
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
                }
                else
                {
                    Debug.LogError("Error: 'result' field not found in the response.");
                }
            }
        }
    }

    // Checks, in intervals of 5 seconds, the state of the task given to Meshy API, if the Model is rendered, the URL to the glb file is retrieved and rendered in runtime
    public IEnumerator GetResponse()
{
    isFetchingResponse = true;

    while (!string.IsNullOrEmpty(taskID))
    {
        Debug.Log("Fetching response from Meshy...");

        using (UnityWebRequest request = UnityWebRequest.Get($"{apiEndpoint}/{taskID}"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching response: " + request.error);
                break; // Sai do loop se houver erro na conexão
            }
            else
            {
                Debug.Log("Response received: " + request.downloadHandler.text);

                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);

                if (response.ContainsKey("status") && response["status"].ToString() == "SUCCEEDED" && response.ContainsKey("model_urls"))
                {
                    Debug.Log("Model generation succeeded!");

                    var modelUrls = JsonConvert.DeserializeObject<Dictionary<string, string>>(response["model_urls"].ToString());
                    if (modelUrls.ContainsKey("glb"))
                    {
                        string glbUrl = modelUrls["glb"];
                        Debug.Log($"GLB URL: {glbUrl}");

                        GameObject gameObject = new GameObject("GeneratedModel");
                        var gltf = gameObject.AddComponent<GLTFast.GltfAsset>();
                        gltf.Url = glbUrl;

                        gameObject.transform.position = new Vector3(0, 0, 2);
                        createdObjects.Add(gameObject);
                        taskID = string.Empty;
                        break; // Sai do loop após carregar o modelo
                    }
                }
                else
                {
                    Debug.Log("Model not ready yet. Retrying...");
                }
            }
        }

        yield return new WaitForSeconds(5); // Espera antes de verificar novamente
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

}
