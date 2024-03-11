using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class WebRequestCaller
{
    public static string jiraApiUrl = "https://yoursite.atlassian.net/rest/api/2";
    public static string email = "Your Auth Mail From Jira";
    public static string authToken = "Your Auth Token From Jira";

    #region GET-Example

    //StartCoroutine(RequestWebService("GET", ""));
    #endregion
    #region POST-Comment
    //        string jsonBody = (@"{
    //    ""body"": ""This is a comment regarding the quality of the response.""
    //}");

    //        StartCoroutine(RequestWebService("POST","/comment",jsonBody));
    #endregion

    public static IEnumerator RequestWebServiceAttachment(string endpoint, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            yield break;
        }

        string getDataUrl = jiraApiUrl + endpoint;

        // Create a formData instance to attach the file
        byte[] fileBytes = File.ReadAllBytes(filePath);
        WWWForm formData = new();
        formData.AddBinaryData("file", fileBytes, Path.GetFileName(filePath));

        using (UnityWebRequest webData = UnityWebRequest.Post(getDataUrl, formData))
        {

            // Set the authentication headers
            string encodedCredentials = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(email + ":" + authToken));
            webData.SetRequestHeader("Authorization", "Basic " + encodedCredentials);
            webData.SetRequestHeader("X-Atlassian-Token", "no-check");

            Debug.Log($"Upload process started with {Path.GetFileName(filePath)} file.");
            yield return webData.SendWebRequest();

            if (webData.result is UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("-- ERROR --");
                Debug.Log(webData.error);
            }
            else
            {
                if (webData.result is UnityWebRequest.Result.Success)
                {
                    Debug.Log("Succesfully uploaded");
                }
                else
                {
                    if (webData.isDone)
                    {
                        Debug.Log("---------------- Response Raw ----------------");
                        Debug.Log(Encoding.UTF8.GetString(webData.downloadHandler.data));
                    }
                }

            }
        }
    }

    public static IEnumerator RequestWebService(string method, string endpoint, string body = null, Action<string> callback = null)
    {
        Debug.Log("---------------- URL ----------------");
        string getDataUrl = jiraApiUrl + endpoint;
        Debug.Log(getDataUrl);
        string encodedCredentials = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(email + ":" + authToken));

        var webData = new UnityWebRequest();
        webData.url = getDataUrl;
        webData.method = method;
        webData.downloadHandler = new DownloadHandlerBuffer();

        webData.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(body) ? null : Encoding.UTF8.GetBytes(body));
        webData.timeout = 60;

        webData.SetRequestHeader("Accept", "application/json");
        webData.SetRequestHeader("Content-Type", "application/json");
        webData.SetRequestHeader("Authorization", "Basic " + encodedCredentials);
        yield return webData.SendWebRequest();
        if (webData.isNetworkError)
        {
            Debug.Log("---------------- ERROR ----------------");
            Debug.Log(webData.error);
        }
        else
        {
            if (webData.isDone)
            {
                Debug.Log("---------------- Response Raw ----------------");
                Debug.Log(Encoding.UTF8.GetString(webData.downloadHandler.data));

                string response = webData.downloadHandler.text;
                if (callback != null)
                {
                    callback(response);
                }
            }
        }
    }


}
