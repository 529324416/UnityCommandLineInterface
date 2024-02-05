using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RedSaw.CommandLineInterface.UnityImpl
{

    /// <summary>default implementation of IConsoleRenderer with Unity legacy UI</summary>
    public class GameConsoleRenderer : MonoBehaviour, IConsoleRenderer
    {
        [SerializeField] private Text outputPanel;
        [SerializeField] private InputField inputField;
        [SerializeField] private GameConsoleAlternativeOptionsPanel optionsPanel;

        private int outputPanelCapacity = 400;
        private int lineCount = 0;

        public bool IsVisible
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public bool IsInputFieldFocus => inputField.isFocused;
        public int OutputPanelCapacity => outputPanelCapacity;

        public string InputText
        {

            get => inputField.text;
            set => inputField.text = value;
        }
        public string InputTextToCursor
        {

            get => inputField.text[..inputField.caretPosition];
            set => inputField.text = value + inputField.text[inputField.caretPosition..];
        }

        public bool IsAlternativeOptionsActive
        {
            get => optionsPanel.gameObject.activeSelf;
            set => optionsPanel.gameObject.SetActive(value);
        }
        public List<string> AlternativeOptions
        {
            set => optionsPanel.SetOptions(value);
        }
        public int AlternativeOptionsIndex
        {
            set => optionsPanel.SelectionIndex = value;
        }

        public void ActivateInput()
        {
            inputField.Select();
            inputField.ActivateInputField();
            StartCoroutine(DisableHighlight());
        }
        IEnumerator DisableHighlight()
        {

            //Get original selection color
            Color originalTextColor = inputField.selectionColor;
            //Remove alpha
            originalTextColor.a = 0f;

            //Apply new selection color without alpha
            inputField.selectionColor = originalTextColor;

            //Wait one Frame(MUST DO THIS!)
            yield return null;

            //Change the caret pos to the end of the text
            inputField.caretPosition = inputField.text.Length;

            //Return alpha
            originalTextColor.a = 1f;

            //Apply new selection color with alpha
            inputField.selectionColor = originalTextColor;
        }

        public void SetInputCursorPosition(int value)
        {
            inputField.caretPosition = Mathf.Clamp(value, 0, inputField.text.Length);
        }

        public void MoveScrollBarToEnd()
        {
            StartCoroutine(MoveToLast());
        }
        IEnumerator MoveToLast()
        {
            yield return null;
            GetComponentInChildren<ScrollRect>(true).verticalNormalizedPosition = 0;
        }

        public void BindOnSubmit(Action<string> callback)
        {
            /* 
                cause maybe the implementation of the game console in Non-Unity GameEngine
                so use Action<string> instead of UnityEvent<string>
            */

            inputField.onSubmit.AddListener((string input) =>
            {
                callback?.Invoke(input);
            });
        }
        public void BindOnTextChanged(Action<string> callback)
        {
            /* 
                cause maybe the implementation of the game console in Non-Unity GameEngine
                so use Action<string> instead of UnityEvent<string>
            */
            inputField.onValueChanged.AddListener((string input) =>
            {
                callback?.Invoke(input);
            });
        }

        public void Focus()
        {
            inputField.Select();
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
        public void QuitFocus()
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void Clear()
        {
            lineCount = 0;
            outputPanel.text = string.Empty;
        }
        public void Output(string msg)
        {
            try
            {
                outputPanel.text += msg + "\n";
                if (++lineCount > outputPanelCapacity)
                {
                    outputPanel.text = outputPanel.text[(outputPanel.text.IndexOf('\n') + 1)..];
                    lineCount--;
                }
                /* update outputPanel's size */
                var generator = new TextGenerator();
                var settings = outputPanel.GetGenerationSettings(outputPanel.rectTransform.sizeDelta);
                outputPanel.rectTransform.sizeDelta =
                new Vector2(outputPanel.rectTransform.sizeDelta.x, generator.GetPreferredHeight(outputPanel.text, settings));

                GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0;
            }
            catch (Exception)
            {
                Clear();
            }
        }
        public void Output(string[] msgs)
        {
            Output(string.Concat(msgs, '\n'));
        }
        public void Output(string msg, string color = "#ffffff")
        {
            Output($"<color={color}>{msg}</color>");
        }
        public void Output(string[] msgs, string color = "#ffffff")
        {

            string msg = string.Empty;
            foreach (string line in msgs)
            {
                msg += $"<color=\"{color}\">{line}</color>\n";
            }
            Output(msg);
        }
    }
}