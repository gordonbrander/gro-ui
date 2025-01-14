﻿using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using SimpleJSON;


public class DataManager : MonoBehaviour {

	public static DataManager dataManager;
	//public static const string version = "v1.0";
	// Save File variables
	public string username, token, ipAddress;
	public bool initialLoad = false, stayLoggedIn = false;

	//Guest logged in variable
	public bool isGuest=false;

	// Session stored variables
	public RuntimePlatform platform = Application.platform;
	public Dictionary<string, JSONNode> databaseMirror = new Dictionary<string, JSONNode> ();
	public GUISkin GUISkin1;
	public GameObject errorMessagePrefab, errorMessageWithStackPrefab;

	private const string saveFileName = "/Settings.dat";

	void Awake()
	{
		if (dataManager == null) 
		{
			DontDestroyOnLoad (gameObject);
			dataManager = this;
		} 
		else if (dataManager != this) 
		{
			Destroy (gameObject);
		}
	}

	IEnumerator Start()
	{
		yield return StartCoroutine("Load");
		initialLoad = true;
		yield return null;
	}

	public IEnumerator SuccessfulLogin()
	{
		// Load SystemView scene
		yield return null;
	}

	public IEnumerator Save()
	{
		BinaryFormatter bf = new BinaryFormatter ();
		FileStream file = File.Create (Application.persistentDataPath + saveFileName);

		SaveData data = new SaveData ();
		data.ipAddress = ipAddress;
		data.token = token;
		data.stayLoggedIn = stayLoggedIn;

		bf.Serialize (file, data);
		file.Close ();

		yield return null;
	}

	public IEnumerator Load()
	{
		
		if (File.Exists (Application.persistentDataPath + saveFileName)) 
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open (Application.persistentDataPath + saveFileName, FileMode.Open);
			SaveData data = (SaveData) bf.Deserialize(file);
			file.Close ();

			ipAddress = data.ipAddress;
			username = data.username;
			token = data.token;
			stayLoggedIn = data.stayLoggedIn;
		}
		yield return null;
	}

	public IEnumerator SingleQuery(string url)
	{
		WWW www = new WWW (url);
		yield return www;
		if (!string.IsNullOrEmpty (www.error)) 
		{
			Debug.Log (www.error);
			Debug.Log (www.text);
			string genericQueryErrorMessage = "Something went wrong with your request. Please try again. If problem persists, restart application";
			DisplayError(genericQueryErrorMessage);
			yield break;
		}
		JSONNode node = JSON.Parse (www.text);
		databaseMirror.Add (url, node);
		yield return null;
	}

	public IEnumerator ListQuery(string url, string accessor)
	{
		WWW www = new WWW (url);
		yield return www;
		if (!string.IsNullOrEmpty (www.error)) 
		{
			Debug.Log (www.error);
			Debug.Log (www.text);
			yield break;
		}
		JSONNode node = JSON.Parse (www.text);
		JSONArray nodeList = node [accessor].AsArray;
		foreach (JSONNode item in nodeList) 
		{
			databaseMirror.Add (item["url"], item);
		}
		yield return null;
	}

	public IEnumerator PostRequest(object[] parms)
	{
		string URL = (string) parms[0];
		byte[] rawData = new byte[1]{1};
		Dictionary<string, string> headers = new Dictionary<string, string>();
		if(parms.Length == 2)
		{
			WWWForm form = (WWWForm) parms[1];
			rawData = form.data;
			headers = form.headers;
		}
		else if (parms.Length == 3)
		{
			rawData = (byte[]) parms[1];
			headers = (Dictionary<string, string>) parms[2];
		}
		headers.Add("Authorization", token);
		WWW www = new WWW(URL, rawData, headers);
		yield return www;
		if(!string.IsNullOrEmpty(www.error))
		{
			Debug.Log (www.error);
			Debug.Log (www.text);
			string genericPostRequestErrorMessage = "Could not complete request action. Check that your information is correct and try again.";
			DisplayError(genericPostRequestErrorMessage);
		}
		
		yield return null;
	}

	public IEnumerator DeleteRequest(string url)//object[] parms)
	{
		Dictionary<string, string> headers = new Dictionary<string, string> ();
		byte[] noData = new byte[] {1};
		
		headers.Add("Content-Type", "application/json");
		headers.Add("X-HTTP-Method-Override", "DELETE");

		WWW www = new WWW (url, noData, headers);
		yield return www;
		if (!string.IsNullOrEmpty (www.error)) 
		{
			Debug.Log (www.error);
			Debug.Log (www.text);
			string genericDeleteRequestErrorMessage = "Could not delete requested item. If problem persists, restart application and try again. Note: Some items cannot be deleted.";
			DisplayError(genericDeleteRequestErrorMessage);
		}

		yield return null;
	}
	
	public IEnumerator PutRequest(object[] parms)
	{
		string url = (string) parms[0];
		WWWForm form = (WWWForm) parms[1];
		byte[] rawData = form.data;
		Dictionary<string, string> headers = form.headers;
		headers.Add("X-HTTP-Method-Override", "PUT");
		headers.Add("Authorization", token);
		
		WWW www = new WWW(url, rawData, headers);
		yield return www;
		if (!string.IsNullOrEmpty (www.error)) 
		{
			Debug.Log (www.error);
			Debug.Log (www.text);
			string genericPutRequestErrorMessage = "Could not update item. Check that all required information is correct and try again. If problem persists, restrat application and try again.";
			DisplayError(genericPutRequestErrorMessage);
		}
		yield return null;
	}
	
	public void DataDownload(Dictionary<string, string> sensingPoints, int startTime, int endTime, string filePath)
	{
		List<List<string>> allData = new List<List<string>>();
		for (int i = 0; i < sensingPoints.Count; i++)
		{
		}
	}
	
	public static int DateTimeNow(){
		int now = (int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds - 1;
		return now;
	}

	public void DisplayError(string message) {
		// Create new error box
		GameObject errorBox = Instantiate(errorMessagePrefab) as GameObject;
		errorBox.transform.SetParent(GameObject.FindWithTag("GUICanvas").transform, false);
		ErrorMessageController script = errorBox.GetComponent<ErrorMessageController>();
		// Set message
		script.SetErrorMessage(message);
	}
	
	public void DisplayErrorWithStack(string message, string stack)
	{
		// Create new error box
		GameObject errorBox = Instantiate(errorMessageWithStackPrefab) as GameObject;
		errorBox.transform.SetParent(GameObject.FindWithTag("GUICanvas").transform, false);
		ErrorMessageController script = errorBox.GetComponent<ErrorMessageController>();
		// Set message
		script.SetErrorMessage(message);
		// Set stack trace message
		script.SetStackMessage(stack);
	}
}

[Serializable]
class SaveData
{
	public string username, token, ipAddress;
	public bool stayLoggedIn;
}




