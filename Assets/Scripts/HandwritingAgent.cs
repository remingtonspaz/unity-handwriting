using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.XR;

/// <summary>
/// Struct containing info relevant to capture camera placement and orientation for one written character
/// </summary>
public struct WritingAreaInfo
{
    /// <summary>
    /// size of area encompassing lines to predict
    /// </summary>
    public float size;
    /// <summary>
    /// center of collection of lines to predict
    /// </summary>
    public Vector3 center;
    /// <summary>
    /// the direction the writing is facing
    /// </summary>
    public Vector3 normal;
    /// <summary>
    /// up normal, usually as applicable to writing surface
    /// </summary>
    public Vector3 upNormal;
}

/// <summary>
/// Handwriting predictor: captures handwriting image from relevant surface and feeds it to the prediction model
/// as a RenderTexture. Extends ML-Agents Agent.
/// </summary>
public class HandwritingAgent : Agent
{
    /// <summary>
    /// array with all possible characters, ordered by index in NN model
    /// </summary>
    private readonly char[] allChars =
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd',
        'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r',
        's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F',
        'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
        'U', 'V', 'W', 'X', 'Y', 'Z'
    };

    /// <summary>
    /// Layer mask for prediction camera's culling mask
    /// </summary>
    [Tooltip("Layer mask for prediction camera's culling mask")]
    public LayerMask relevantLayers;
    /// <summary>
    /// Background color of prediction camera
    /// </summary>
    [Tooltip("Background color of prediction camera")]
    public Color cameraBackgroundColor;
    /// <summary>
    /// String of characters passed so far. Exposed for flexibility
    /// </summary>
    [Tooltip("String of characters passed so far. Exposed for flexibility")]
    public string parsedText;
    /// <summary>
    /// Text 3D component used to display parsed text in game
    /// </summary>
    [Tooltip("Text 3D component used to display parsed text in game")]
    public TextMesh textMesh;
    /// <summary>
    /// Should colors be inverted in the camera capture?
    /// </summary>
    [Tooltip("Should colors be inverted in the camera capture?")]
    public bool invert = true;
    
    /// <summary>
    /// A self-generated ortographic camera used for capturing to Agent's input RenderTexture
    /// </summary>
    private Camera _agentCam;
    /// <summary>
    /// Last predicted character
    /// </summary>
    private char _lastPredict = Char.MinValue;
    /// <summary>
    /// Last prediction's confidence
    /// </summary>
    private float _lastConfidence;
    /// <summary>
    /// The invert post-effect on the generated camera
    /// </summary>
    private Invert _cameraInvertComponent;

    /// <summary>
    /// Last predicted character (read only)
    /// </summary>
    public char lastPredict => _lastPredict;

    /// <summary>
    /// Last prediction's confidence (read only)
    /// </summary>
    public float LastConfidence => _lastConfidence;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _cameraInvertComponent.invert = invert;
        if (Input.GetKeyDown(KeyCode.X))
        {
            parsedText = "";
            if (textMesh) textMesh.text = parsedText;
        }
    }

    /// <summary>
    /// Executes prediction - sets cam to capture correct area and calls RequestDecision()
    /// </summary>
    /// <param name="writingAreaInfo">info of the current area to capture</param>
    public void Predict(WritingAreaInfo writingAreaInfo)
    {
        //moves and sets camera size according to passed info struct
        _agentCam.transform.position = writingAreaInfo.center - writingAreaInfo.normal;
        _agentCam.transform.rotation = Quaternion.LookRotation(writingAreaInfo.normal,
            writingAreaInfo.upNormal);
        _agentCam.orthographicSize = writingAreaInfo.size;
        
        _agentCam.Render(); //make sure the camera renders updated view
        
        RequestDecision();
    }

    /// <summary>
    /// Extends from base class, creates necessary components for Agent's input (Camera and RenderTexture)
    /// </summary>
    public override void InitializeAgent()
    {
        base.InitializeAgent();
        
        //create prediction camera and set up properties
        GameObject cameraGO = new GameObject("Agent Camera");
        _agentCam = cameraGO.AddComponent<Camera>();
        _cameraInvertComponent = cameraGO.AddComponent<Invert>(); //add image inverter post-effect b/c NN model accepts white-hot input
        _agentCam.orthographic = true;
        _agentCam.cullingMask = relevantLayers;
        _agentCam.backgroundColor = cameraBackgroundColor;
        _agentCam.clearFlags = CameraClearFlags.Color; //camera only renders text lines

        //create target render texture. Dimension is 28x28
        RenderTexture rt = new RenderTexture(28,28,3);
        _agentCam.targetTexture = rt;
        agentParameters.agentRenderTextures = new List<RenderTexture>(new RenderTexture[]{rt}); //assign to agent parameter
    }

    /// <summary>
    /// Handles prediction results. Stores highest confidence and character on variables
    /// </summary>
    /// <param name="vectorAction">Confidence values of all possible characters (62 in EMNIST dataset)</param>
    /// <param name="textAction"></param>
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        int indexAtMax = vectorAction.ToList().IndexOf(vectorAction.Max()); //get index of max value
        //vectorAction contains the confidence values of all possible characters - 62 in total.
        string arrStr = "";
        
        //debugs
        for (int i = 0; i < vectorAction.Length; i++)
        {
            arrStr += $"{allChars[i]} {vectorAction[i]:.0000}\n";
        }
        Debug.Log(gameObject.name+" confidence values:\n"+arrStr);
        Debug.Log($"{gameObject.name} prediction result: {allChars[indexAtMax]} {vectorAction[indexAtMax]}");
        
        _lastPredict = allChars[indexAtMax]; //get character with highest confidence
        _lastConfidence = vectorAction[indexAtMax];
        parsedText += _lastPredict;
        if (textMesh) textMesh.text = parsedText;
    }
}
