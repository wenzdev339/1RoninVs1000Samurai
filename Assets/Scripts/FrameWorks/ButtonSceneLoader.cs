using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ระบบสำหรับทดสอบปุ่มเท่านั้น
public class ButtonSceneLoader : MonoBehaviour
{
    [SerializeField] private String TargetScene;

    public void SceneLoader()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(TargetScene);
    }
}
