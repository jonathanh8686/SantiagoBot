using System;
using System.Collections.Generic;
using System.Linq;

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
        private double[] publicProbability = new double[54];
        private List<int> hand = new List<int>();
        private int[][] haveHalfSuit = new int[6][];


        #region Variable Parameters

        // TODO: Genetic Optimization?
        private double minProbabilityThreshold = 0.5; // the minimum probability that a player has a card to call it
        private double minHaveSuitThreshold = 0.0; // the minimum number of cards they must have the in halfsuit to randomly call it
        private double riskFactor = 1.0; // TODO: Maybe have seperate riskFactors for each moving module
        private double minProbabilityCountingThreshold = 0.9; // the minimum (public) probability to consider that card lost if incorrectly called

        #endregion


        private readonly Dictionary<string, int> CardIndex;
        private readonly Dictionary<int, string> NumberCard;
        private readonly string[] CardNames;
        private readonly Dictionary<string, string[]> HalfSuits;
        private readonly List<string> Players;
        private readonly Dictionary<string, string> PlayerTeams;


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
            PlayerTeams = Program.PlayerTeams;


            // TODO: Support not 6 player games
            Utility.Log("AI Instance Initalized.");

            // Initalize Probabilities
            cardProbability[0] = new double[54];
            for (var i = 1; i < cardProbability.Length; i++)
            {
                cardProbability[i] = new double[54]; // The probablity of each card will be saved
                for (int j = 0; j < cardProbability[i].Length; j++)
                    cardProbability[i][j] = 0.2;
            }

            for (var i = 0; i < publicProbability.Length; i++)
                publicProbability[i] = 0.2;

            // Initalize Halfsuit information
            for (var i = 0; i < 6; i++)
                haveHalfSuit[i] = new int[9];

            InitalizeHand(); // Get Santiago's hand
            foreach (var cardNum in hand)
            {
                cardProbability[0][cardNum] = 1.0f; // Santiago must have these cards
                for (int i = 0; i < HalfSuits.Values.Count; i++)
                {
                    if (HalfSuits.Values.ToArray()[i].Contains(NumberCard[cardNum]))
                        haveHalfSuit[0][i]++;
                }
            }
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
            var bestCardCall = new CardCall
            {
                Result = CallResult.Unknown,
                SenderName = "santiago"
            };

            var possibleCards = GetPossibleCards();

            var bmProbability = GetProbabilityMove(possibleCards, out var foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmProbability.CardName;
                bestCardCall.TargetName = bmProbability.TargetName;

                return bestCardCall;
            }

            var bmHaveSuit = GetMinSuitMove(possibleCards, out foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmHaveSuit.CardName;
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
                        bestHalfSuitIndex = j;
                        bestPlayerID = i;
                        minCardsFound = haveHalfSuit[i][j];
                    }
                }
            }

            List<CandidateMove> possibleCardCalls = new List<CandidateMove>();
            var bestCardProbability = 0.0;
            for (var j = 0; j < HalfSuits.Values.ToArray()[bestHalfSuitIndex].Length; j++)
            {
                var cardIdNum = CardIndex[HalfSuits.Values.ToArray()[bestHalfSuitIndex][j]];
                if (!(cardProbability[bestPlayerID][cardIdNum] >= bestCardProbability)) continue;
                if (cardProbability[bestPlayerID][cardIdNum] < minHaveSuitThreshold) continue;
                if (!possibleCards.Contains(cardIdNum)) continue;
                if (PlayerTeams[Players[bestPlayerID]] == PlayerTeams["santiago"]) continue;

                if (!MoveCostBenefit(new CandidateMove // make sure that the move meets cost-benefit
                {
                    TargetName = Players[bestPlayerID],
                    CardName = NumberCard[cardIdNum]
                })) continue;


                foundMove = true;

                bestCardProbability = cardProbability[bestPlayerID][cardIdNum];
                bmMinSuit.CardId = cardIdNum;
                bmMinSuit.CardName = NumberCard[cardIdNum];
                bmMinSuit.HitProbability = bestCardProbability;
                bmMinSuit.SenderId = 0;
                bmMinSuit.SenderName = "santiago";
                bmMinSuit.TargetId = bestPlayerID;
                bmMinSuit.TargetName = Players[bestPlayerID];


                possibleCardCalls.Add(new CandidateMove()
                {
                    CardId = bmMinSuit.CardId,
                    CardName = bmMinSuit.CardName,
                    HitProbability = bmMinSuit.HitProbability,
                    SenderId = bmMinSuit.SenderId,
                    SenderName = bmMinSuit.SenderName,
                    TargetId = bmMinSuit.TargetId,
                    TargetName = bmMinSuit.TargetName
                });
            }

            return foundMove ? possibleCardCalls[new Random().Next(possibleCardCalls.Count)] : null;
        }
        private CandidateMove GetProbabilityMove(List<int> possibleCards, out bool foundMove)
        {
            // TODO: if there are several cards with equal probability randomize the card to choose
            CandidateMove bmProbability = new CandidateMove();
            foundMove = false;
            for (var i = 0; i < possibleCards.Count; i++)
            {
                for (var j = 1; j < cardProbability.Length; j++)
                {
                    if (!(cardProbability[j][possibleCards[i]] > bmProbability.HitProbability)) continue;
                    if (cardProbability[j][possibleCards[i]] < minProbabilityThreshold) continue;
                    if (PlayerTeams[Players[j]] == PlayerTeams["santiago"]) continue;

                    if (!MoveCostBenefit(new CandidateMove // make sure that the move meets cost-benefit
                    {
                        TargetName = Players[j],
                        CardName = NumberCard[possibleCards[i]]
                    })) continue;

                    foundMove = true;
                    bmProbability.TargetId = j;
                    bmProbability.TargetName = Players[j];
                    bmProbability.SenderId = 0;
                    bmProbability.SenderName = "santiago";
                    bmProbability.CardId = possibleCards[i];
                    bmProbability.CardName = NumberCard[possibleCards[i]];
                    bmProbability.HitProbability = cardProbability[j][possibleCards[i]];
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
                if (cc.TargetName == "santiago")
                    hand.Remove(CardIndex[cc.CardRequested]);
                if (cc.SenderName == "santiago")
                    hand.Add(CardIndex[cc.CardRequested]);

                for (var i = 0; i < 6; i++)
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
                for (var i = 0; i < 6; i++)
                {
                    if (i == Players.IndexOf(cc.SenderName) || i == Players.IndexOf(cc.TargetName))
                    {
                        double spreadProb = cardProbability[i][CardIndex[cc.CardRequested]] / 5.0;
                        for (int j = 0; j < 6; j++)
                        {
                            if (j == i) continue;
                            cardProbability[i][CardIndex[cc.CardRequested]] += spreadProb;
                        }
                        cardProbability[i][CardIndex[cc.CardRequested]] = 0.0;
                    }
                }

                // which halfsuit is cardRequested in
                for (int i = 0; i < HalfSuits.Values.ToArray().Length; i++)
                {
                    if (HalfSuits.Values.ToArray()[i].Contains(cc.CardRequested))
                    {
                        haveHalfSuit[Players.IndexOf(cc.SenderName)][i] =
                            Math.Max(haveHalfSuit[Players.IndexOf(cc.SenderName)][i], 1);
                    }
                }

            }

            SuitCall teamSuitCall = CheckTeammateSuitCalls(); // check if you can certainly (kind of) get a suit
            if (teamSuitCall != null)
            {
                Console.WriteLine($"Santiago has called the {teamSuitCall.HalfSuitName}");
                ProcessMove(teamSuitCall);
            }


        }

        private SuitCall CheckTeammateSuitCalls()
        {
            SuitCall sc = new SuitCall();
            for (int i = 0; i < HalfSuits.Keys.Count; i++)
            {
                int mustHaveOnTeam = 0;
                for (int j = 0; j < HalfSuits[HalfSuits.Keys.ToArray()[i]].Length; j++)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        if (PlayerTeams[Players[k]] == PlayerTeams["santiago"])
                            if (Math.Abs(cardProbability[k][CardIndex[HalfSuits[HalfSuits.Keys.ToArray()[i]][j]]] - 1) < 0.01)
                                mustHaveOnTeam++;
                    }
                }

                if (mustHaveOnTeam == 6)
                {
                    // they have all of a halfsuit
                    sc.Team = PlayerTeams["santiago"];
                    sc.HalfSuitName = HalfSuits.Keys.ToArray()[i];
                    sc.SenderName = "santiago";

                    return sc;
                }
            }

            return null;
        }

        /// <summary>
        /// Process the suitcall by removing all information about those halfsuits
        /// </summary>
        /// <param name="sc">SuitCall made</param>
        public void ProcessMove(SuitCall sc)
        {
            var halfSuitCalled = HalfSuits[sc.HalfSuitName]; // get the names of the card called

            foreach (var card in halfSuitCalled)
                hand.Remove(CardIndex[card]);

            foreach (var card in halfSuitCalled)
            {
                for (int j = 0; j < 6; j++)
                {
                    haveHalfSuit[j][HalfSuits.Keys.ToList().IndexOf(sc.HalfSuitName)] = 0;
                    cardProbability[j][CardIndex[card]] = 0.0;
                }
            }
        }

        private bool MoveCostBenefit(CandidateMove cc)
        {
            int hsIndex = -1;
            for (var i = 0; i < HalfSuits.Keys.Count; i++)
            {
                if (!HalfSuits[HalfSuits.Keys.ToList()[i]].Contains(cc.CardName)) continue;

                hsIndex = i;
                break;
            }

            return (1.0 - cardProbability[Players.IndexOf(cc.TargetName)][CardIndex[cc.CardName]]) *
                   CountPossibleLoss(hsIndex) <
                   cardProbability[Players.IndexOf(cc.TargetName)][CardIndex[cc.CardName]] * riskFactor;
        }

        private int CountPossibleLoss(int halfSuitID)
        {
            var res = 0;
            string halfSuitName = HalfSuits.Keys.ToArray()[halfSuitID];
            for (var i = 0; i < HalfSuits[halfSuitName].Length; i++)
            {
                if (publicProbability[CardIndex[HalfSuits[halfSuitName][i]]] >= minProbabilityCountingThreshold)
                    res++;
            }

            return res;
        }

    }
}
