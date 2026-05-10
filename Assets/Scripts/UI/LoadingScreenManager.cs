using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Project.UI
{
    public class LoadingScreenManager : MonoBehaviour
    {
        // ─── CONFIGURATION ───────────────────────────────────────────────────
        [Header("[ НАЗВАНИЕ СЦЕНЫ ]")]
        [SerializeField] private string sceneToLoad = "GameScene";

        [Header("[ МИНИМАЛЬНОЕ ВРЕМЯ ЗАГРУЗКИ ]")]
        [SerializeField] private float minimumLoadTime = 2f;

        [Header("[ ПОЛОСКА ПРОГРЕССА ]")]
        [SerializeField] private Image progressBarFill;
        [SerializeField] private float barSpeed = 3f;

        [Header("[ ТЕКСТ ]")]
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private float dotsSpeed = 0.4f;

        [Header("[ АРТ ]")]
        [SerializeField] private Image artImage;
        [SerializeField] private Sprite[] artSprites;
        // ─────────────────────────────────────────────────────────────────────

        private float currentProgress;
        private float targetProgress;

       private void Start()
{
    currentProgress = 0f;
    targetProgress  = 0f;

    if (artSprites != null && artSprites.Length > 0 && artImage != null)
    {
        artImage.sprite = artSprites[Random.Range(0, artSprites.Length)];
        
        
        artImage.preserveAspect = true; 
    }

    StartCoroutine(LoadSceneRoutine());
    StartCoroutine(AnimateDotsRoutine());
}

        private void Update()
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * barSpeed);

            if (progressBarFill != null)
                progressBarFill.fillAmount = currentProgress;
        }

        private IEnumerator LoadSceneRoutine()
        {
            float startTime      = Time.time;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

                bool timeElapsed   = Time.time - startTime >= minimumLoadTime;
                bool loadCompleted = operation.progress >= 0.9f;

                if (timeElapsed && loadCompleted)
                {
                    targetProgress = 1f;

                    // Ждём пока полоска визуально дойдёт до конца
                    while (currentProgress < 0.99f) yield return null;

                    yield return new WaitForSeconds(0.3f);

                    // Фейд исчезновения через SceneTransitionManager
                    if (SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.LoadScene(sceneToLoad);
                    else
                        operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private IEnumerator AnimateDotsRoutine()
        {
            if (loadingText == null) yield break;

            string baseText  = "Загрузка";
            int    dotsCount = 0;

            while (true)
            {
                dotsCount        = (dotsCount + 1) % 4;
                loadingText.text = baseText + new string('.', dotsCount);
                yield return new WaitForSeconds(dotsSpeed);
            }
        }
    }
}