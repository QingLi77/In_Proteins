using BeardedManStudios.Forge.Networking.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPanel : MonoBehaviour
{
    public Button resumeBtn, menuBtn, quitBtn;

    // Start is called before the first frame update
    void Start()
    {
        resumeBtn.onClick.AddListener(()=> {
            if (Time.timeScale <= 0)
            {
                resumeBtn.GetComponentInChildren<Text>().text = "RESUME";
                Time.timeScale = 1;
            }
            else 
            {
                resumeBtn.GetComponentInChildren<Text>().text = "PAUSE";
                Time.timeScale = 0;
            }
        });
        menuBtn.onClick.AddListener(() => {
            NetworkManager.Instance.Disconnect();
            SceneManager.LoadScene(0);
        });
        quitBtn.onClick.AddListener(() => {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        });
    }
}
