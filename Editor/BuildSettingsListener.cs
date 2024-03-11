using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BuildSettingsListener : EditorWindow
{

    static string filePath;
    static BuildPlayerOptions buildOptions;
    public static List<string> issueKeyList = new List<string>();
    public static List<string> issueKeyListInfo = new List<string>();

    static int selectedIssuKeyIndex = 0;
    string searchText;

    static BuildSettingsListener()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildHandler);
    }

    static void BuildHandler(BuildPlayerOptions options)
    {
        buildOptions = options;
        GetWindow<BuildSettingsListener>("Jira Uploader");
    }

    private void OnGUI()
    {
        searchText = GUILayout.TextField(searchText);
        if (GUILayout.Button("Search"))
        {
            issueKeyList.Clear();
            issueKeyListInfo.Clear();

            if (!string.IsNullOrEmpty(searchText))
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(WebRequestCaller.RequestWebService("GET", $"/search/?jql=text%20~%20'{searchText}*'&fields=key,summary", callback: HandleIssueList));
            }
        }

        selectedIssuKeyIndex = EditorGUILayout.Popup("Choose Issue", selectedIssuKeyIndex, issueKeyListInfo.ToArray());

        if (GUILayout.Button("Start Build"))
        {
            BuildPipeline.BuildPlayer(buildOptions);
            filePath = buildOptions.locationPathName;
            // Start a coroutine to monitor the build process
            EditorApplication.update += WaitForBuildCompletion;
        }

        GUI.Label(new Rect(10, 100, position.width - 20, 20), "Possible Issues");

        foreach (string issueKey in issueKeyListInfo)
        {
            GUI.Label(new Rect(10, 120 + (20 * issueKeyListInfo.IndexOf(issueKey)), position.width - 20, 20), issueKey);
        }
    }

    static void WaitForBuildCompletion()
    {
        if (!BuildPipeline.isBuildingPlayer)
        {
            // Build process completed
            Debug.Log("Build process completed!");
            string selectedIssueString = issueKeyList[selectedIssuKeyIndex];

            EditorCoroutineUtility.StartCoroutineOwnerless(WebRequestCaller.RequestWebServiceAttachment($"/issue/{selectedIssueString}/attachments", filePath));
            EditorApplication.update -= WaitForBuildCompletion;
        }
    }

    static void HandleIssueList(string json)
    {
        // Parse the JSON to extract the list of issues
        JObject jsonObject = JObject.Parse(json);
        JArray issuesArray = (JArray)jsonObject["issues"];

        if (issuesArray != null)
        {
            foreach (JToken issueToken in issuesArray)
            {
                // Access the "fields" object and then retrieve the "summary" field
                string summary = issueToken["fields"]["summary"].ToString();

                // Create an Issue object with key and summary
                Issue issue = new Issue();
                issue.key = issueToken["key"].ToString();
                issue.summary = summary;

                Debug.Log("Issue Key: " + issue.key + "/ " + issue.summary);
                issueKeyList.Add(issue.key);
                issueKeyListInfo.Add(issue.key + " -- " + issue.summary);

            }
        }
    }


}



[System.Serializable]
public class IssueResponse
{
    public List<Issue> issues;
}

[System.Serializable]
public class Issue
{
    public string key;
    public string summary;
}
