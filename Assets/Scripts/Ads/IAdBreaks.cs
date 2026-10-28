using System;

namespace GameOfLife.Ads
{
    /// <summary>Lets a game mode start a game behind an ad break: the game starts straight away if no ad is due, or once the ad closes.</summary>
    public interface IAdBreaks
    {
        /// <summary>Shows the pre-game ad if one is due, then starts the game; if no ad shows, starts it at once.</summary>
        void RunBeforeGame(Action startGame);
    }
}
