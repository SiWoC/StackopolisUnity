using UnityEngine;
using UnityEngine.SceneManagement;

namespace SiWoC.Stackopolis.Globals
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] string mainMenuSceneName = "MainMenu";

        readonly string[] sceneStack = new string[10];
        int stackIndex;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // Boot is not kept on the stack; MainMenu is the navigation root.
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void Forward(string sceneName)
        {
            sceneStack[stackIndex] = SceneManager.GetActiveScene().name;
            stackIndex++;
            SceneManager.LoadScene(sceneName);
        }

        public void Back()
        {
            if (stackIndex == 0)
                return;

            stackIndex--;
            SceneManager.LoadScene(sceneStack[stackIndex]);
        }
    }
}
