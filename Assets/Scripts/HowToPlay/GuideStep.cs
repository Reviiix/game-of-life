using System;
using UnityEngine;

namespace GameOfLife.HowToPlay
{
    /// <summary>One card of the how-to-play guide: a heading, a few lines of explanation and an optional looping demo board.</summary>
    [Serializable]
    public sealed class GuideStep
    {
        [SerializeField] private string heading;
        [SerializeField, TextArea(3, 8)] private string body;
        [SerializeField] private GuideDemo demo = new();

        public string Heading => heading;
        public string Body => body;
        public GuideDemo Demo => demo;
    }
}
