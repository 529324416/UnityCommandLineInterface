using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.CommandLineInterface.UnityImpl
{

    /// <summary>default implementation of alternative options panel</summary>
    public class GameConsoleAlternativeOptionsPanel : MonoBehaviour
    {

        [SerializeField] private Text textPanel;

        List<string> options;

        public int SelectionIndex
        {
            set
            {
                textPanel.text = string.Empty;
                if (options == null || options.Count == 0) return;
                string output = string.Empty;
                for (int i = 0; i < options.Count; i++)
                {
                    if (i == value)
                    {
                        output += $"<color=\"#92e8c0\">{options[i]}</color>\n";
                        continue;
                    }
                    output += options[i] + "\n";
                }
                textPanel.text = output;
            }
        }

        /// <summary>render current alternative options</summary>
        public void SetOptions(List<string> values)
        {
            this.options = values;
        }
    }
}