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

                // EXTENSION GUIDE: 
                // When you finish creating the RandomBot.cs file, uncomment the 2 lines below:
                // case "Random Bot": 
                //     return new RandomBot();

                default:
                    return new Human();
            }
        }

        // This list will automatically be populated into the UI (ComboBox)
        public static string[] GetAvailableModes()
        {
            return new string[] { "Human", "Nega Premium" };
            // When a new bot is added, change to: new string[] { "Human", "Nega Premium", "Random Bot" };
        }
    }
}