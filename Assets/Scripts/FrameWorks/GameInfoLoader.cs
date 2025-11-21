using System.IO;
using TMPro;
using UnityEngine;

public class GameInfoLoader : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI gameInfoText;

    private string jsonFilePath = "Assets/Resources/Data/StreamingAssets/GameInfo/GameInfo.json";

    private void Start()
    {
        LoadGameInfo();
    }

    private void LoadGameInfo()
    {
        try
        {
            if (File.Exists(jsonFilePath))
            {
                string jsonData = File.ReadAllText(jsonFilePath);
                GameInfo gameInfo = JsonUtility.FromJson<GameInfo>(jsonData);

                // Update UI Elements
                gameInfoText.text = gameInfo.GameVersion + " " + gameInfo.Company;
            }
            else
            {
                Debug.LogError($"JSON file not found at path: {jsonFilePath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load game info: {ex.Message}");
        }
    }
}

[System.Serializable]
public class GameInfo
{
    public string Company;
    public string GameVersion;
}
