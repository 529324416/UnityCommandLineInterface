using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl{

    /// <summary>the final wrapper of CommandConsoleSystem build in Unity</summary>
    public class GameConsole : MonoBehaviour{

        [SerializeField] 
        private GameConsoleRenderer consoleRenderer;

        [SerializeField, Tooltip("static parameter, at least 100")] 
        private int outputCapacity = 400;

        [SerializeField, Tooltip("static parameter, at least 1")] 
        private int inputHistoryCapacity = 20;

        [SerializeField, Tooltip("static parameter, alternative command options count, at least 1")]
        private int alternativeCommandCount = 8;

        [SerializeField, Tooltip("static parameter, should output with time information of [HH:mm:ss]")] 
        private bool shouldOutputWithTime = true;

        [SerializeField, Tooltip("static parameter, should record failed command input")] 
        private bool shouldRecordFailedCommand = true;

        [SerializeField, Tooltip("static parameter, use default command?")]
        private bool useDefaultCommand = true;

        static Console Instance { get; set; }

        void Awake(){
            if(consoleRenderer == null){
                Debug.LogError("ConsoleRenderer has not found!");
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            /* intialize console, you can set parameter what you like */
            Instance = new Console(
                consoleRenderer, 
                new UserInput(),
                commandSystem:null,         // use default command system
                memoryCapacity:inputHistoryCapacity,
                alternativeCommandCount:alternativeCommandCount,
                shouldRecordFailedCommand:shouldRecordFailedCommand,
                outputPanelCapacity:outputCapacity,
                outputWithTime:shouldOutputWithTime,
                useDefaultCommand:useDefaultCommand
            );
            Instance.CurrentCommandSystem.ExecuteSlience("logo");
        }
        void Update() => Instance.Update();

        public static void Output(string msg) => Instance.Output(msg);
        public static void Output(string msg, string color) => Instance.Output(msg, color);
        public static void Output(object data){
            string _ = data == null ? "null" : data.ToString();
            Instance.Output(_);
        }
        public static void Output(object data, string color){
            string _ = data == null ? "null" : data.ToString();
            Instance.Output(_, color);
        }
        public static void Output(string[] msg) => Instance.Output(msg);
        public static void Output(string[] msg, string color) => Instance.Output(msg, color);
    }
}