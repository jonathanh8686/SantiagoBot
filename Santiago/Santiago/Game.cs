using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Santiago
{
    /// <summary>
    /// Used to keep track of Game events
    /// Calling cards/halfsuits, missing, hitting, etc...
    /// </summary>
    class Game
    {
        public Dictionary<string, int> CardIndex = new Dictionary<string, int>();
        public Dictionary<int, string> NumberCard = new Dictionary<int, string>();
        private float[][] cardProbability = new float[6][];
        private List<int> hand = new List<int>();

        /// <summary>
        /// Initalize the needed variables (cardProbability, hand, etc...)
        /// Set all the known information
        /// </summary>
        public Game()
        {
            // TODO: Support !6 player games
            Utility.Log("Game Instance Initalized.");

            // Initalize Probabilities
            for (var i = 0; i < cardProbability.Length; i++)
                cardProbability[i] = new float[54]; // The probablity of each card will be saved

            // Initalize CardIndex and NumberCard
            var cardLines = File.ReadAllLines("CardAssignments.txt");
            for (int i = 0; i < cardLines.Length; i++)
            {
                CardIndex.Add(cardLines[i], i); // CardIndex[cardName] = cardNumericalValue
                NumberCard.Add(i, cardLines[i]);
            }

            InitalizeHand(); // Get Santiago's hand
            foreach (var cardNum in hand)
                cardProbability[0][cardNum] = 1.0f; // Santiago must have these cards
        }

        /// <summary>
        /// Ask the user to type in Santiago's hand
        /// </summary>
        private void InitalizeHand()
        {
            // Each player should have 9 cards
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Please type in Santiago's hand...");
            while(hand.Count < 9)
            {
                string currentCard = "";
                while (true)
                {
                    currentCard = Console.ReadLine();
                    if (!CardIndex.ContainsKey(currentCard)) // Check if card is recognized
                        Utility.Alert($"{currentCard} is an INVALID card! Please enter a valid card!");
                    else
                        break;
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                if (!hand.Contains(CardIndex[currentCard])) // Check if all cards in hand are unique
                    hand.Add(CardIndex[currentCard]);
                else
                    Utility.Alert($"{currentCard} is ALREADY in Santiago's hand! Please enter another card!");
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            Utility.Log("Initalized Santiago's Hand.");
            PrintHand();
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Allows the user to see what Santiago is holding
        /// </summary>
        private void PrintHand()
        {
            string handString = "";
            for (int i = 0; i < hand.Count; i++)
                handString += NumberCard[hand[i]] + " ";
            Console.WriteLine(handString);
        }

        /// <summary>
        /// Print the probabilites for each player/card pair
        /// </summary>
        /// <param name="selectedPlayer">Player whose probabilities will be printed (Set to -1 for all players)</param>
        public void PrintProbabilities(int selectedPlayer)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            if (selectedPlayer == -1)
            {
                for(int player = 0; player < 6; player++)
                    for(int card = 0; card < cardProbability[player].Length; card++)
                        Console.WriteLine($"{player} has a {cardProbability[player][card]} chance of having {NumberCard[card]}");
            }
            else
            {
                for (int card = 0; card < cardProbability[selectedPlayer].Length; card++)
                    Console.WriteLine($"{selectedPlayer} has a {cardProbability[selectedPlayer][card]} chance of having {NumberCard[card]}");
            }
        }

        
    }
}
