using System;

namespace NegaPremium
{
    /// <summary>
    /// Factory pattern to initialize players or AI Bots.
    /// </summary>
    public static class PlayerFactory
    {

        public static IPlayer CreatePlayer(string modeName)
        {
            switch (modeName)
            {
                case "Human":
                    return new Human();
                case "Nega Premium":
                    return new Engine();
                case "Tactical Bot":
                    return new TacticalBot();
                case "Heuristic MCTS":
                    return new HeuristicMCTSBot();
                case "HillClimbing":
                    return new Engine { Mode = Engine.SearchMode.HillClimbing };
                case "HillClimbingv2":
                    return new Engine { Mode = Engine.SearchMode.HillClimbingv2 };
                default:
                    return new Human();
            }
        }

        // This list will automatically be populated into the UI (ComboBox)
        public static string[] GetAvailableModes()
        {
            return new string[] { "Human", "Nega Premium", "Tactical Bot", "Heuristic MCTS", "HillClimbing", "HillClimbingv2" };
        }
    }
}