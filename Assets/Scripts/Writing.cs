using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for writing components - handles management of LineRenderers
/// </summary>
[RequireComponent(typeof(HandwritingAgent))]
public class Writing : MonoBehaviour
{
    /// <summary>
    /// Prefab with the line renderer to use
    /// </summary>
    [Tooltip("Prefab with the line renderer to use")]
    public GameObject lineRendererPrefab;
    /// <summary>
    /// Index of layer that gets captured by prediction camera
    /// </summary>
    [Tooltip("Index of layer that gets captured by prediction camera")]
    public int predictLayerIndex;
    /// <summary>
    /// Delay between new points in line. Smaller values make higher res lines
    /// </summary>
    [Tooltip("Delay between new points in line. Smaller values make higher res lines")]
    public float lineFidelity;
    /// <summary>
    /// Multiplier for the character bounds size that gets passed to predictor
    /// </summary>
    [Tooltip("Multiplier for the character bounds size that gets passed to predictor")]
    public float boundSizeFactor = 1;
    /// <summary>
    /// Amount of delay to auto-predict after a line has finished drawing. Insert negative values to
    /// turn off auto-prediction
    /// </summary>
    [Tooltip("Amount of delay to auto-predict after a line has finished drawing. Insert negative values to" +
             "turn off auto-prediction")]
    public float predictDelay = -1;
    
    /// <summary>
    /// The current LineRenderer being given new points to
    /// </summary>
    protected LineRenderer _currentLine;
    /// <summary>
    /// Last time a point has been placed
    /// </summary>
    protected float _lastLinePlace;
    /// <summary>
    /// Parent of created line renderers. Auto generated
    /// </summary>
    protected Transform _lineParent;
    /// <summary>
    /// The prediction agent attached to this component
    /// </summary>
    protected HandwritingAgent _agent;
    /// <summary>
    /// Last time an auto prediction was made
    /// </summary>
    protected float _lastPredict;
    /// <summary>
    /// Last child index of LineRenderer included in previous prediction
    /// </summary>
    protected int _lastPredictIdx;
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        _lineParent = new GameObject("Line Parent").transform;
        _lineParent.parent = transform;
        _lineParent.localPosition = Vector3.zero;
        _lineParent.localRotation = Quaternion.identity;
        _agent = GetComponent<HandwritingAgent>();
        _lastPredictIdx = 0;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //Clear all lines and prediction string
        if (Input.GetKeyDown(KeyCode.X))
        {
            foreach (Transform line in _lineParent)
            {
                Destroy(line.gameObject);
            }
            
            _lastPredict = Time.time;
            _lastPredictIdx = 0;
        }
        
        //Manual prediction
        if (Input.GetKeyDown(KeyCode.F))
        {
            
            _agent.Predict(GetCurrentLinesData(_lastPredictIdx));
            _lastPredictIdx = _lineParent.childCount;
        }
        
        //Auto prediction
        if (predictDelay > 0 && _lastPredictIdx < _lineParent.childCount)
        {
            if (_lastPredict + predictDelay <= Time.time)
            {
                _agent.Predict(GetCurrentLinesData(_lastPredictIdx));
                _lastPredictIdx = _lineParent.childCount; 

                _lastPredict = Time.time;
            }
        }
    }

    /// <summary>
    /// Adds a new point to current LineRenderer. Creates a new one if necessary
    /// </summary>
    /// <param name="point">point to add</param>
    /// <param name="normal">direction the line is rendered towards</param>
    /// <param name="up">up vector of current surface</param>
    protected void UpdateLine(Vector3 point, Vector3 normal, Vector3 up)
    {
        //create new object with LineRenderer if none is currently active
        if (_currentLine == null)
        {
            _currentLine = Instantiate(lineRendererPrefab, point, Quaternion.identity).
                GetComponent<LineRenderer>();
            _currentLine.transform.parent = _lineParent;
            //set the line to face correct direction
            _currentLine.transform.rotation = Quaternion.LookRotation(normal,
                up);
            _currentLine.SetPositions(new Vector3[0]);
            _lastLinePlace = 0;
        }

        //add the point
        if (_lastLinePlace + lineFidelity <= Time.time)
        {
            _currentLine.positionCount += 1;
            //point is slightly offset by line's normal
            _currentLine.SetPosition(_currentLine.positionCount - 1,
                _currentLine.transform.InverseTransformPoint(point - 
                                                             normal * .01f));
            _lastLinePlace = Time.time; //resets the point placement timer
        }
        
        _lastPredict = Time.time;
    }

    /// <summary>
    /// Ends the current line
    /// </summary>
    protected void EndLine()
    {
        _currentLine = null;
    }
    
    /// <summary>
    /// Gets the extent, center, normal, and up vector of current set of lines to be predicted in the form of
    /// WritingAreaInfo struct
    /// </summary>
    /// <param name="startIdx"></param>
    /// <returns></returns>
    protected WritingAreaInfo GetCurrentLinesData(int startIdx)
    {
        WritingAreaInfo areaInfo = new WritingAreaInfo();
        int lineParentChildCount = _lineParent.childCount;

        float xMin = System.Single.PositiveInfinity;
        float xMax = System.Single.NegativeInfinity;
        float yMin = System.Single.PositiveInfinity;
        float yMax = System.Single.NegativeInfinity;

        int pointCount = 0;
        Vector3 pointSum = Vector3.zero;

        Vector3 sumNormal = Vector3.zero;
        Vector3 sumUp = Vector3.zero;

        //gets the extents and the sums of normals and positions
        if (startIdx < lineParentChildCount)
        {
            for (int i = 0; i < lineParentChildCount; i++)
            {
                Transform line = _lineParent.GetChild(i);
                if (i >= startIdx)
                {
                    line.gameObject.layer = predictLayerIndex;
                    sumNormal += line.forward;
                    sumUp += line.up;
                    LineRenderer lineRenderer = line.GetComponent<LineRenderer>();

                    pointCount += lineRenderer.positionCount;
                    Vector3[] points = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(points);

                    foreach (var locPoint in points)
                    {
                        Vector3 point = line.transform.TransformPoint(locPoint); //transform point to world space first
                        var right = line.right;
                        float x = Vector3.Dot(Vector3.Project(point, right),right);
                        var up = line.up;
                        float y = Vector3.Dot(Vector3.Project(point, up), up);

                        xMin = x < xMin ? x : xMin;
                        xMax = x > xMax ? x : xMax;
                        yMin = y < yMin ? y : yMin;
                        yMax = y > yMax ? y : yMax;

                        pointSum += point;
                    }
                }
                else
                {
                    line.gameObject.layer = 0;
                }
            }
        }

        float sizeX = Mathf.Abs(xMax - xMin);
        float sizeY = Mathf.Abs(yMax - yMin);
        
        //average out normals and positions to get center and avg. normal
        Vector3 center = (pointCount > 0)?pointSum / pointCount:Vector3.zero;
        Vector3 normal = (lineParentChildCount > 0)?sumNormal / lineParentChildCount:Vector3.forward;
        Vector3 upNormal = (lineParentChildCount > 0)?sumUp / lineParentChildCount:Vector3.forward;

        areaInfo.size = Mathf.Max(sizeX, sizeY) * boundSizeFactor; //largest size between x and y, also multiply
                                                                        //by bound padding factor
        areaInfo.center = center;
        areaInfo.normal = normal.normalized;
        areaInfo.upNormal = upNormal.normalized;

        return areaInfo;
    }
}
