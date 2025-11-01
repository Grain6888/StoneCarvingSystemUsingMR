using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadHalloweenScene()
    {
        SceneManager.LoadScene("HalloweenScene");
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }
}
