using System;
using UnityEditor.EditorTools;
using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl
{

    /// <summary>the final wrapper of CommandConsoleSystem build in Unity</summary>
    public class GameConsole : MonoBehaviour
    {

        [Header("Initialize Parameters")]
        [SerializeField]
        private GameConsoleRenderer consoleRenderer;

        [SerializeField]
        private GameConsoleHeader headerBar;

        [SerializeField, Tooltip("the capacity of input history, at least 1")]
        private int inputHistoryCapacity = 20;

        [SerializeField, Tooltip("the capacity of command query cache, at least 1")]
        private int commandQueryCacheCapacity = 20;

        [SerializeField, Tooltip("alternative command options count, at least 1")]
        private int alternativeCommandCount = 8;

        [SerializeField, Tooltip("should output with time information of [HH:mm:ss]")]
        private bool shouldOutputWithTime = true;

        [SerializeField, Tooltip("should record failed command input")]
        private bool shouldRecordFailedCommand = true;

        [SerializeField, Tooltip("should receive unity message")]
        private bool shouldReceiveUnityMessage = true;

        [SerializeField, Tooltip("[debug] output virtual machine exception call stack")]
        private bool shouldOutputVMExceptionStack = false;

        [SerializeField, Tooltip("initialize on awake")]
        private bool initializeOnAwake = true;

        ConsoleController<LogType> console;

        void Awake()
        {
            if(initializeOnAwake)Init();
        }

        /// <summary>initialize console, call this function to initialize console </summary>
        public void Init(){

            if (consoleRenderer == null)
            {
                Debug.LogError("ConsoleRenderer is missing!!");
                gameObject.SetActive(false);
                return;
            }
            gameObject.SetActive(true);

            /* intialize console */
            console = new ConsoleController<LogType>(
                consoleRenderer,
                new UserInput(),

                inputHistoryCapacity: inputHistoryCapacity,
                commandQueryCacheCapacity: commandQueryCacheCapacity,
                alternativeCommandCount: alternativeCommandCount,
                shouldRecordFailedCommand: shouldRecordFailedCommand,
                outputWithTime: shouldOutputWithTime,
                outputStackTraceOfCommandExecution: shouldOutputVMExceptionStack
            );
            if (shouldReceiveUnityMessage) Application.logMessageReceived += UnityConsoleLog;


            var parentTransform = (RectTransform)transform;
            headerBar.Init(( pos ) => parentTransform.position += (Vector3)pos);
        }

        void Update() => console.Update();
        void OnDestroy()
        {
            if (shouldReceiveUnityMessage)
                Application.logMessageReceived -= UnityConsoleLog;
        }

        void UnityConsoleLog(string msg, string stack, LogType type)
        {

            console.Output(msg, GetHexColor(type));
        }
        string GetHexColor(LogType type)
        {
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