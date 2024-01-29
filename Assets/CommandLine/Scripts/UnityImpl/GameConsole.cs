using System;
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

        ConsoleController console;

        void Awake(){
            if(consoleRenderer == null){
                Debug.LogError("ConsoleRenderer is missing!!");
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            /* intialize console */
            console = new ConsoleController(
                consoleRenderer, 
                new UserInput(),
                commandQueryCacheCapacity:inputHistoryCapacity,
                alternativeCommandCount:alternativeCommandCount,
                shouldRecordFailedCommand:shouldRecordFailedCommand,
                outputWithTime:shouldOutputWithTime
            );
            Application.logMessageReceived += UnityConsoleLog;
        }
        void Update() => console.Update();
        void OnDestroy() => Application.logMessageReceived -= UnityConsoleLog;

        void UnityConsoleLog(string msg, string stack, LogType type){

            console.Output(msg, GetHexColor(type));
        }
        string GetHexColor(LogType type){
            return type switch
            {
                LogType.Error or LogType.Exception or LogType.Assert => "#b13c45",
                LogType.Warning => "yellow",
                _ => "#fffde3",
            };
        }

        /// <summary>clear output of current console</summary>
        public void ClearOutput() => console.ClearOutputPanel();
    }
}