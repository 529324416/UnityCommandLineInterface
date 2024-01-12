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

        ConsoleController Instance;

        void Awake(){
            if(consoleRenderer == null){
                Debug.LogError("ConsoleRenderer is missing!!");
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            /* intialize console, you can set parameter what you like */
            Instance = new ConsoleController(
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
            Application.logMessageReceived += UnityConsoleLog;
        }
        void Update() => Instance.Update();
        void OnDestory() => Application.logMessageReceived -= UnityConsoleLog;

        void UnityConsoleLog(string msg, string stack, LogType type){

            string color = "#fffde3";
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    color = "#b13c45";
                    break;
                case LogType.Warning:
                    color = "yellow";
                    break;
                case LogType.Log:
                    break;
            }
            Instance.Output(msg, color);
        }

        /// <summary>clear output of current console</summary>
        public void ClearOutput() => Instance.ClearOutputPanel();
    }
}