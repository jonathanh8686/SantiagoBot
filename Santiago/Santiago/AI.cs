using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace Santiago
{
    internal class CandidateMove
    {
        public string TargetName;
        public string SenderName;
        public int TargetId;
        public int SenderId;
        public string CardName;
        public int CardId;
        public double HitProbability;
    }

    /// <summary>
    /// Used to keep track of AI events
    /// Calling cards/halfsuits, missing, hitting, etc...
    /// </summary>
    class AI
    {
        private double[][] cardProbability = new double[6][];
        private List<int> hand = new List<int>();
        private int[][] haveHalfSuit = new int[6][];

        #region Variable Parameters

        // TODO: Genetic training to optimization these variables
        private double minProbabilityThreshold = 0.5;
        private double minHaveSuitThreshold = 0.0;

        #endregion


        private readonly Dictionary<string, int> CardIndex;
        private readonly Dictionary<int, string> NumberCard;
        private readonly string[] CardNames;
        private readonly Dictionary<string, string[]> HalfSuits;
        private readonly List<string> Players;


        /// <summary>
        /// Initalize the needed variables (cardProbability, hand, etc...)
        /// Set all the known information
        /// </summary>
        public AI()
        {
            // Initalize reference values
            CardIndex = Program.CardIndex;
            NumberCard = Program.NumberCard;
            CardNames = Program.CardNames;
            HalfSuits = Program.HalfSuits;
            Players = Program.Players;


            // TODO: Support not 6 player games
            Utility.Log("AI Instance Initalized.");

            // Initalize Probabilities
            for (var i = 0; i < cardProbability.Length; i++)
                cardProbability[i] = new double[54]; // The probablity of each card will be saved

            // Initalize Halfsuit information
            for (var i = 0; i < 6; i++)
                haveHalfSuit[i] = new int[7];

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
            while (hand.Count < 9)
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
            Console.WriteLine(hand.Aggregate("", (current, t) => current + (NumberCard[t] + " ")));
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
                for (var player = 0; player < 6; player++)
                    for (var card = 0; card < cardProbability[player].Length; card++)
                        Console.WriteLine($"{player} has a {cardProbability[player][card]} chance of having {NumberCard[card]}");
            }
            else
            {
                for (var card = 0; card < cardProbability[selectedPlayer].Length; card++)
                    Console.WriteLine($"{selectedPlayer} has a {cardProbability[selectedPlayer][card]} chance of having {NumberCard[card]}");
            }
        }

        /// <summary>
        /// Given the state of the game will return the move that Santiago will make
        /// </summary>
        /// <param name="gameState">An instance of Game that ends on Santiago's turn</param>
        /// <returns></returns>
        public CardCall MakeMove(Game gameState)
        {
            var bestCardCall = new CardCall();
            var possibleCards = GetPossibleCards();

            var bmProbability = GetProbabilityMove(possibleCards, out var foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmProbability.CardName;
                bestCardCall.SenderName = "santiago";
                bestCardCall.TargetName = bmProbability.TargetName;

                return bestCardCall;
            }

            var bmHaveSuit = GetMinSuitMove(possibleCards, out foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmHaveSuit.CardName;
                bestCardCall.SenderName = "santiago";
                bestCardCall.TargetName = bmHaveSuit.TargetName;

                return bestCardCall;
            }

            return null; // shouldn't happen if thresholds are low enough
        }

        private CandidateMove GetMinSuitMove(List<int> possibleCards, out bool foundMove)
        {
            CandidateMove bmMinSuit = new CandidateMove();
            foundMove = false;

            int bestPlayerID = 0, bestHalfSuitIndex = 0, minCardsFound = -1;
            for (int i = 1; i < haveHalfSuit.Length; i++)
            {
                // go through each player (except Santiago)
                for (int j = 0; j < haveHalfSuit[i].Length; j++)
                {
                    // go through each halfsuit
                    if (haveHalfSuit[i][j] > minCardsFound)
                    {
                        bestHalfSuitIndex = i;
                        bestPlayerID = j;
                        minCardsFound = haveHalfSuit[i][j];
                    }
                }
            }

            var bestCardProbability = 0.0;
            for (var j = 0; j < HalfSuits.Values.ToArray()[bestHalfSuitIndex].Length; j++)
            {
                var cardIdNum = CardIndex[HalfSuits.Values.ToArray()[bestHalfSuitIndex][j]];

                if (!(cardProbability[bestPlayerID][cardIdNum] > bestCardProbability)) continue;
                if (cardProbability[bestPlayerID][cardIdNum] < minHaveSuitThreshold) continue;

                foundMove = true;

                bestCardProbability = cardProbability[bestPlayerID][cardIdNum];
                bmMinSuit.CardId = cardIdNum;
                bmMinSuit.CardName = NumberCard[cardIdNum];
                bmMinSuit.HitProbability = bestCardProbability;
                bmMinSuit.SenderId = 0;
                bmMinSuit.SenderName = "santiago";
                bmMinSuit.TargetId = bestPlayerID;
                bmMinSuit.TargetName = Players[bestPlayerID];
            }

            return bmMinSuit;
        }
        private CandidateMove GetProbabilityMove(List<int> possibleCards, out bool foundMove)
        {
            CandidateMove bmProbability = new CandidateMove();
            foundMove = false;
            for (var i = 0; i < possibleCards.Count; i++)
            {
                for (var j = 0; j < cardProbability.Length; j++)
                {
                    if (!(cardProbability[j][i] > bmProbability.HitProbability)) continue;
                    if (cardProbability[j][i] < minProbabilityThreshold) continue;

                    foundMove = true;
                    bmProbability.TargetId = j;
                    bmProbability.TargetName = Players[j];
                    bmProbability.SenderId = 0;
                    bmProbability.SenderName = "santiago";
                    bmProbability.CardId = possibleCards[i];
                    bmProbability.CardName = NumberCard[possibleCards[i]];
                    bmProbability.HitProbability = cardProbability[j][i];
                }
            } // loop through all possible cards and find where the highest probability is

            return bmProbability;
        }

        /// <summary>
        /// Get the possible cards to analyze in the MakeMove function
        /// </summary>
        /// <returns>A List of integers containing the ID of each card that is a valid call</returns>
        private List<int> GetPossibleCards()
        {
            // Remove cards that are already in your hand
            var possibleCards = (from card in CardNames where !hand.Contains(CardIndex[card]) select CardIndex[card]).ToList();

            foreach (var hs in HalfSuits.Values)
            {
                var doesContain = false;
                foreach (var card in hs)
                    if (hand.Contains(CardIndex[card])) doesContain = true;

                if (doesContain) continue;

                // Remove cards that are in the halfsuit that you don't have
                foreach (var card in hs) possibleCards.Remove(CardIndex[card]);

            }

            return possibleCards;
        }

        /// <summary>
        /// Processes each move in the game
        /// Changes probabilities etc...
        /// </summary>
        /// <param name="cc">The move that was made</param>
        public void ProcessMove(CardCall cc)
        {
            if (cc.Result == CallResult.Hit)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i == Players.IndexOf(cc.SenderName))
                    {
                        cardProbability[i][CardIndex[cc.CardRequested]] = 1.0; // He must also have another of the same suit

                        int halfSuitNumber;
                        for (halfSuitNumber = 0; halfSuitNumber < HalfSuits.Count; halfSuitNumber++)
                        {
                            if (HalfSuits.Values.ToArray()[halfSuitNumber].Contains(cc.CardRequested))
                                break;
                        }

                        if (haveHalfSuit[i][halfSuitNumber] >= 2) haveHalfSuit[i][halfSuitNumber]++;
                        else haveHalfSuit[i][halfSuitNumber] = 2;
                        haveHalfSuit[Players.IndexOf(cc.TargetName)][halfSuitNumber]--;
                    }
                    else
                        cardProbability[i][CardIndex[cc.CardRequested]] = 0.0;

                }
            }
            else if (cc.Result == CallResult.Miss)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i == Players.IndexOf(cc.SenderName) || i == Players.IndexOf(cc.TargetName))
                    {
                        double spreadProb = cardProbability[i][CardIndex[cc.CardRequested]] / 5.0;
                        for (int j = 0; j < 6; j++)
                        {
                            if (j == i) continue;
                            cardProbability[i][CardIndex[cc.CardRequested]] += spreadProb;
                        }
                        cardProbability[i][CardIndex[cc.CardRequested]] = 0.0; // He must also have another of the same suit
                    }
                }
            }
        }

        public void ProcessMove(SuitCall sc)
        {

        }


    }
}
