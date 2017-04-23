using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.WSA.WebCam;
using System.Linq;

public class VisionManager : MonoBehaviour {
	private string visionApiKey = "{{ YOUR KEY HERE }}";
	private string visionApiUrl = "https://westus.api.cognitive.microsoft.com/vision/v1.0/analyze?visualFeatures=Tags";
	private PhotoCapture photoCapture = null;

	public Text Status;
	public Text Output;
	public Text Tags;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void AnalyzeView(){
		Status.text = "STARTING SCAN...";
		Output.text = string.Empty;
		Tags.text = string.Empty;

		PhotoCapture.CreateAsync (false, OnPhotoCaptureCreated);
	}

	void GetTags(byte[] image)
	{
		var coroutine = RunComputerVision(image);
		StartCoroutine(coroutine);
	}

	IEnumerator RunComputerVision(byte[] image)
	{
		var headers = new Dictionary<string, string>() {
			{ "Ocp-Apim-Subscription-Key", visionApiKey },
			{ "Content-Type", "application/octet-stream" }
		};

		WWW www = new WWW(visionApiUrl, image, headers);
		yield return www;

		List<string> tags = new List<string>();
		var jsonResults = www.text;
		var myObject = JsonUtility.FromJson<AnalysisResult>(jsonResults);
		foreach (var tag in myObject.tags)
		{
			tags.Add(tag.name);
		}
		Tags.text = "RESULTS:\n\n" + string.Join("\n", tags.ToArray());
		Status.text = "SCAN COMPLETE";
	}

	void OnPhotoCaptureCreated(PhotoCapture captureObject)
	{
		photoCapture = captureObject;

		Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

		CameraParameters c = new CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = cameraResolution.width;
		c.cameraResolutionHeight = cameraResolution.height;
		c.pixelFormat = CapturePixelFormat.BGRA32;

        try
        {
            photoCapture.StartPhotoModeAsync(c, OnPhotoModeStarted);
        }
        catch (System.Exception ex)
        {
            throw ex;
        }
	}

	void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			string filename = string.Format(@"moment.jpg");
			string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
			photoCapture.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
		}
		else
		{
			Output.text = "ERROR:\n\nUnable to start photo mode.";
			Status.text = "SCAN FAILED";
		}
	}

	void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result){
		if (result.success) {
			string fileName = string.Format (@"moment.jpg");
			string filePath = System.IO.Path.Combine (Application.persistentDataPath, fileName);
			byte[] image = System.IO.File.ReadAllBytes (filePath);
			GetTags (image);
		} else {
			Output.text = "ERROR:\n\nFailed to capture photo to disk";
			Status.text = "SCAN FAILED";
		}

		photoCapture.StopPhotoModeAsync (OnStoppedPhotoMode);
	}

	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result){
		photoCapture.Dispose ();
		photoCapture = null;
	}
}
