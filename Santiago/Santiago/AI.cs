using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
        private int[] handNumbers = new int[6];
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
                for (var j = 0; j < cardProbability[i].Length; j++)
                    cardProbability[i][j] = 0.2;
            }

            for (var i = 0; i < publicProbability.Length; i++)
                publicProbability[i] = 0.2;

            // Initalize Halfsuit information
            for (var i = 0; i < 6; i++)
            {
                handNumbers[i] = 9;
                haveHalfSuit[i] = new int[9];
                for (var j = 0; j < 9; j++)
                    if (i != 0)
                        haveHalfSuit[i][j] = -1;
            }

            InitalizeHand(); // Get Santiago's hand
            foreach (int cardNum in hand)
            {
                cardProbability[0][cardNum] = 1.0f; // Santiago must have these cards
                for (var i = 0; i < HalfSuits.Values.Count; i++)
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
                var currentCard = "";
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

            var bmProbability = GetProbabilityMove(possibleCards, out bool foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmProbability.CardName;
                bestCardCall.TargetName = bmProbability.TargetName;

                return bestCardCall;
            }

            Utility.Debug("No Probability Move found... Using MinSuit");

            var bmHaveSuit = GetMinSuitMove(possibleCards, out foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmHaveSuit.CardName;
                bestCardCall.TargetName = bmHaveSuit.TargetName;

                return bestCardCall;
            }

            Utility.Debug("No MinSuit Move found... Using NumberHand");

            var bmHandNumber = GetHandNumberMove(possibleCards, out foundMove);
            if (foundMove)
            {
                bestCardCall.CardRequested = bmHandNumber.CardName;
                bestCardCall.TargetName = bmHandNumber.TargetName;

                return bestCardCall;
            }

            Utility.Error("No strategy procduced a valid move!!!!!");
            return null; // shouldn't happen if thresholds are low enough
        }

        #region Move Strategies

        /// <summary>
        /// Get the best move based on who has the card with highest probability
        /// </summary>
        /// <param name="possibleCards">A list of possible cards to call</param>
        /// <param name="foundMove">An out parameter depending on if a move was found</param>
        /// <returns>The best move based on probability</returns>
        private CandidateMove GetProbabilityMove(List<int> possibleCards, out bool foundMove)
        {
            // TODO: if there are several cards with equal probability randomize the card to choose
            var bmProbability = new CandidateMove();
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
        /// Get the best move based on who has a certain amount of a halfsuit
        /// </summary>
        /// <param name="possibleCards">A list of possible cards to call</param>
        /// <param name="foundMove">An out parameter depending on if a move was found</param>
        /// <returns>The best move based on the minimum number of cards in a suit</returns>
        private CandidateMove GetMinSuitMove(List<int> possibleCards, out bool foundMove)
        {
            var bmMinSuit = new CandidateMove();
            foundMove = false;

            int bestPlayerID = 0, bestHalfSuitIndex = 0, minCardsFound = -1;
            for (var i = 1; i < haveHalfSuit.Length; i++)
            {
                // go through each player (except Santiago)
                for (var j = 0; j < haveHalfSuit[i].Length; j++)
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

            var possibleCardCalls = new List<CandidateMove>();
            var bestCardProbability = 0.0;
            for (var j = 0; j < HalfSuits.Values.ToArray()[bestHalfSuitIndex].Length; j++)
            {
                int cardIdNum = CardIndex[HalfSuits.Values.ToArray()[bestHalfSuitIndex][j]];
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

        /// <summary>
        /// Get the best moved based on who has the most cards
        /// </summary>
        /// <param name="possibleCards">A list of possible cards to call</param>
        /// <param name="foundMove">An out parameter depending on if a move was found</param>
        /// <returns>The best move based on probability</returns>
        private CandidateMove GetHandNumberMove(List<int> possibleCards, out bool foundMove)
        {
            var bmHandNumber = new CandidateMove();
            foundMove = false;

            int minNum = -1, minIndex = -1;
            for (int i = 0; i < handNumbers.Length; i++)
            {
                if (handNumbers[i] > minIndex)
                {
                    minNum = handNumbers[i];
                    minIndex = i;
                }
            }

            List<CandidateMove> possibleCandidates = new List<CandidateMove>();

            for (int i = 0; i < possibleCards.Count; i++)
            {
                if (cardProbability[minIndex][possibleCards[i]] > 0)
                {
                    foundMove = true;

                    bmHandNumber.CardId = possibleCards[i];
                    bmHandNumber.CardName = NumberCard[possibleCards[i]];
                    bmHandNumber.HitProbability = cardProbability[minIndex][possibleCards[i]];
                    bmHandNumber.SenderId = 0;
                    bmHandNumber.SenderName = "santiago";
                    bmHandNumber.TargetId = minIndex;
                    bmHandNumber.TargetName = Players[minIndex];

                    possibleCandidates.Add(new CandidateMove() // can't add by reference
                    {
                        CardId = bmHandNumber.CardId,
                        CardName = bmHandNumber.CardName,
                        HitProbability = bmHandNumber.HitProbability,
                        SenderId = bmHandNumber.SenderId,
                        SenderName = bmHandNumber.SenderName,
                        TargetId = bmHandNumber.TargetId,
                        TargetName = bmHandNumber.TargetName
                    });
                }
            }

            return possibleCandidates[new Random().Next(possibleCandidates.Count)];
        }
        #endregion

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
                foreach (string card in hs)
                    if (hand.Contains(CardIndex[card])) doesContain = true;

                if (doesContain) continue;

                // Remove cards that are in the halfsuit that you don't have
                foreach (string card in hs) possibleCards.Remove(CardIndex[card]);

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
            int halfSuitNumber;
            for (halfSuitNumber = 0; halfSuitNumber < HalfSuits.Count; halfSuitNumber++)
            {
                if (HalfSuits.Values.ToArray()[halfSuitNumber].Contains(cc.CardRequested))
                    break; // get the halfsuit index that the called card was in
            }

            if (cc.Result == CallResult.Hit)
            {
                handNumbers[Players.IndexOf(cc.SenderName)]++;
                handNumbers[Players.IndexOf(cc.TargetName)]--;

                if (cc.TargetName == "santiago")
                {
                    hand.Remove(CardIndex[cc.CardRequested]);
                    publicProbability[CardIndex[cc.CardRequested]] = 0;
                }
                if (cc.SenderName == "santiago")
                {
                    hand.Add(CardIndex[cc.CardRequested]);
                    publicProbability[CardIndex[cc.CardRequested]] = 1;
                }

                for (var i = 0; i < 6; i++)
                {
                    if (i == Players.IndexOf(cc.SenderName))
                    {
                        cardProbability[i][CardIndex[cc.CardRequested]] = 1.0;

                        // He must also have another of the same suit


                        if (haveHalfSuit[i][halfSuitNumber] >= 2) haveHalfSuit[i][halfSuitNumber]++; // each card they call after that adds 1
                        if (haveHalfSuit[i][halfSuitNumber] == -1) haveHalfSuit[i][halfSuitNumber] = 2; // card they gained and at least one more

                        if (haveHalfSuit[Players.IndexOf(cc.TargetName)][halfSuitNumber] != -1) // 
                            haveHalfSuit[Players.IndexOf(cc.TargetName)][halfSuitNumber]--; // target loses one of that halfsuit
                    }
                    else
                        cardProbability[i][CardIndex[cc.CardRequested]] = 0.0;

                }
            }
            else if (cc.Result == CallResult.Miss)
            {
                if (cc.SenderName == "santiago")
                    publicProbability[CardIndex[cc.CardRequested]] = 0;

                for (var i = 0; i < 6; i++)
                {
                    if (i == Players.IndexOf(cc.SenderName) || i == Players.IndexOf(cc.TargetName))
                    {
                        double spreadProb = cardProbability[i][CardIndex[cc.CardRequested]] / 5.0;
                        for (var j = 0; j < 6; j++)
                        {
                            if (j == i) continue;
                            cardProbability[i][CardIndex[cc.CardRequested]] += spreadProb;
                        }
                        cardProbability[i][CardIndex[cc.CardRequested]] = 0.0;
                    }
                }

                // which halfsuit is cardRequested in
                for (var i = 0; i < HalfSuits.Values.ToArray().Length; i++)
                {
                    if (HalfSuits.Values.ToArray()[i].Contains(cc.CardRequested))
                    {
                        haveHalfSuit[Players.IndexOf(cc.SenderName)][i] =
                            Math.Max(haveHalfSuit[Players.IndexOf(cc.SenderName)][i], 1);
                    }
                }

            }


            double knownCards = 0;
            for (int k = 0; k < 6; k++)
                knownCards += Math.Max(0, haveHalfSuit[k][halfSuitNumber]);
            for (int j = 0; j < HalfSuits[HalfSuits.Keys.ToArray()[halfSuitNumber]].Length; j++)
            {
                int cardID = CardIndex[HalfSuits[HalfSuits.Keys.ToArray()[halfSuitNumber]][j]];
                if (Math.Abs(cardProbability[Players.IndexOf(cc.SenderName)][cardID]) < 0.01) // account for cards that you know they DON'T have
                    knownCards++;
            }
            knownCards = Math.Min(knownCards, 6);

            for (int j = 0; j < HalfSuits[HalfSuits.Keys.ToArray()[halfSuitNumber]].Length; j++)
            {
                int cardID = CardIndex[HalfSuits[HalfSuits.Keys.ToArray()[halfSuitNumber]][j]];
                bool changeCard = true;
                for (int k = 0; k < 6; k++)
                {
                    if (Math.Abs(cardProbability[k][cardID] - 1) < 0.01)
                        changeCard = false;
                }

                if (Math.Abs(cardProbability[Players.IndexOf(cc.SenderName)][cardID]) < 0.01) changeCard = false;

                if (changeCard)
                {
                    cardProbability[Players.IndexOf(cc.SenderName)][cardID] =
                        Math.Max(cardProbability[Players.IndexOf(cc.SenderName)][cardID], 1 / (7 - knownCards));

                    if (Math.Abs(1 / (7 - knownCards) - 1) < 0.01)
                        cardProbability[Players.IndexOf(cc.SenderName)][cardID] = 2;
                }
            }

            var teamSuitCall = CheckTeammateSuitCalls(); // check if you can certainly (kind of) get a suit
            if (teamSuitCall != null)
            {
                Console.WriteLine($"Santiago has called the {teamSuitCall.HalfSuitName}");
                ProcessMove(teamSuitCall);
            }


        }

        /// <summary>
        /// Process the suitcall by removing all information about those halfsuits
        /// </summary>
        /// <param name="sc">SuitCall made</param>
        public void ProcessMove(SuitCall sc)
        {
            var halfSuitCalled = HalfSuits[sc.HalfSuitName]; // get the names of the card called

            foreach (string card in halfSuitCalled)
                hand.Remove(CardIndex[card]);

            foreach (string card in halfSuitCalled)
            {
                for (var j = 0; j < 6; j++)
                {
                    haveHalfSuit[j][HalfSuits.Keys.ToList().IndexOf(sc.HalfSuitName)] = 0;
                    cardProbability[j][CardIndex[card]] = 0.0;
                }
            }

            int cardsAccounted = 0;
            while (cardsAccounted < 6)
            {
                // TODO: Maybe keep track of which cards?
                Console.WriteLine("Who had how many cards?");
                var userInput = Console.ReadLine()?.Split(" ");
                if (userInput != null && !Players.Contains(userInput[0]))
                {
                    Utility.Error("Player not found! Please enter a valid player!");
                    continue;
                }

                if (userInput != null)
                {
                    handNumbers[Players.IndexOf(userInput[0])] -= int.Parse(userInput[1]);
                    cardsAccounted += int.Parse(userInput[1]);
                }
            }
        }

        /// <summary>
        /// Check if it's possible to certainly (almost) call given information about team
        /// </summary>
        /// <returns>Returns SuitCall object</returns>
        private SuitCall CheckTeammateSuitCalls()
        {
            var sc = new SuitCall();
            for (var i = 0; i < HalfSuits.Keys.Count; i++)
            {
                var mustHaveOnTeam = 0;
                for (var j = 0; j < HalfSuits[HalfSuits.Keys.ToArray()[i]].Length; j++)
                {
                    for (var k = 0; k < 6; k++)
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
        /// Calculate the check if the given moves passes cost benefit analysis
        /// </summary>
        /// <param name="cc">CandidateMove to check</param>
        /// <returns>Returns whether or not the move passes cost benefit</returns>
        private bool MoveCostBenefit(CandidateMove cc)
        {

            return (1.0 - cardProbability[Players.IndexOf(cc.TargetName)][CardIndex[cc.CardName]]) *
                   CountPossibleLoss(Players.IndexOf(cc.TargetName)) <
                   cardProbability[Players.IndexOf(cc.TargetName)][CardIndex[cc.CardName]] * riskFactor;
        }

        /// <summary>
        /// Find out how many cards that a certain player can (probably) take from you
        /// </summary>
        /// <param name="playerID">The player to count from</param>
        /// <returns>The total number of possible lost cards</returns>
        private int CountPossibleLoss(int playerID)
        {
            var res = 0;
            for (var i = 0; i < haveHalfSuit[playerID].Length; i++)
            {
                if (haveHalfSuit[playerID][i] == 0) continue; // if you are sure they don't have a card in that halfsuit
                // TODO: have different minProbabilityCountingThreshold for if you're sure they have a suit vs if you're not sure?
                string halfSuitName = HalfSuits.Keys.ToArray()[i];

                for (var j = 0; j < HalfSuits[halfSuitName].Length; j++)
                {
                    if (publicProbability[CardIndex[HalfSuits[halfSuitName][j]]] >= minProbabilityCountingThreshold)
                        res++;
                }
            }

            return res;
        }

    }
}
