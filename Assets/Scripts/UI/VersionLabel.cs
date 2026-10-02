using System.Text;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI
{
    /// <summary>Writes the app version from Player Settings into its label once at start-up, for example "V1.1.0.".</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class VersionLabel : MonoBehaviour
    {
        private const string Prefix = "V";
        private const string Suffix = ".";

        /// <summary>Builds and shows the version text.</summary>
        private void Awake()
        {
            var versionText = new StringBuilder(Prefix).Append(Application.version).Append(Suffix);
            GetComponent<TMP_Text>().SetText(versionText);
        }
    }
}
