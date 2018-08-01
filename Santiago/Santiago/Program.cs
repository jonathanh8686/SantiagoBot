using System;
using System.Collections.Generic;
using System.IO;

// Santiago is a fish AI with no usage of machine learning (yet)
namespace Santiago
{
    class Program
    {
        public static List<string> Players = new List<string>();
        public static Dictionary<string, string> PlayerTeams = new Dictionary<string, string>();
        public static Dictionary<string, int> CardIndex = new Dictionary<string, int>();
        public static Dictionary<int, string> NumberCard = new Dictionary<int, string>();
        public static string[] CardNames;
        public static Dictionary<string, string[]> HalfSuits = new Dictionary<string, string[]>();

        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            #region Initalize
            // Initalize CardIndex and NumberCard
            CardNames = File.ReadAllLines("CardAssignments.txt");
            for (var i = 0; i < CardNames.Length; i++)
            {
                CardIndex.Add(CardNames[i], i); // CardIndex[cardName] = cardNumericalValue
                NumberCard.Add(i, CardNames[i]);
            }

            // Initalize Halfsuits
            var halfSuitNames = File.ReadAllLines("HalfSuitNames.txt");

            var tempHalfSuits = File.ReadAllLines("HalfSuits.txt");
            for (var i = 0; i < tempHalfSuits.Length; i++)
                HalfSuits.Add(halfSuitNames[i], tempHalfSuits[i].Split(','));

            Console.ForegroundColor = ConsoleColor.White;
            var ai = new AI();

            Players.Add("santiago");
            PlayerTeams.Add("santiago", "Blue");

            for (var i = 2; i <= 6; i++)
            {
                Console.WriteLine($"What is player {i}'s name?");
                string nameInput = Console.ReadLine()?.ToLower();
                while (Players.Contains(nameInput))
                {
                    Console.WriteLine("That name is already taken!");
                    nameInput = Console.ReadLine()?.ToLower();
                }
                Players.Add(nameInput);
                if(Players.Count % 2 == 0)
                    PlayerTeams.Add(nameInput, "Red");
                else 
                    PlayerTeams.Add(nameInput, "Blue");

            } // initalize the Player (names) list
            #endregion

            // Start game
            var game = new Game();

            Console.WriteLine("Who's turn it is?");

            string inpPlayerTurn = Console.ReadLine()?.ToLower();
            while (!Players.Contains(inpPlayerTurn))
            {
                Utility.Alert($"{inpPlayerTurn} is not a player! Please enter a valid player name.");
                inpPlayerTurn = Console.ReadLine();
            }

            game.PlayerTurn = inpPlayerTurn;

            while (!game.GameOver)
            {
                if (game.PlayerTurn != "santiago")
                {
                    // Take in move made
                    Console.WriteLine($"{game.PlayerTurn}'s turn! What move did they make?");
                    var moveData = Console.ReadLine()?.Split(" ");

                    if (moveData?[0] == "call") // ["call", HalfSuit, Result]
                    {
                        // Halfsuit Called
                        if (!HalfSuits.ContainsKey(moveData?[1]))
                        {
                            Utility.Error("Halfsuit not recognized!");
                            continue;
                        }
                        if (moveData?[2] != "hit" && moveData?[2] != "miss")
                        {
                            Utility.Error("Result not recognized!");
                            continue;
                        }

                        var res = moveData[2] == "hit" ? CallResult.Hit : CallResult.Miss;
                        var sc = new SuitCall(moveData[1].ToUpper(), PlayerTeams[game.PlayerTurn], game.PlayerTurn, res);
                        game.ProcessMove(sc);
                        ai.ProcessMove(sc);
                    }
                    else if(Players.Contains(moveData?[0])) // [TargetName, CardName, Result]
                    {
                        // Card Called
                        if (!CardIndex.ContainsKey(moveData?[1]))
                        {
                            Utility.Error("Card not recognized!");
                            continue;
                        }
                        if (moveData?[2] != "hit" && moveData?[2] != "miss")
                        {
                            Utility.Error("Result not recognized!");
                            continue;
                        }

                        var res = moveData?[2] == "hit" ? CallResult.Hit : CallResult.Miss;
                        var cc = new CardCall(moveData?[0].ToLower(), game.PlayerTurn, moveData?[1], res);
                        game.ProcessMove(cc);
                        ai.ProcessMove(cc);
                    }
                    else
                    {
                        Utility.Error("Invalid command! Calltype not recognized!");
                        game.ExportAFN("testGame");
                    }

                }
                else
                {
                    // Make a move
                    // ReSharper disable once InconsistentNaming
                    var AIMove = ai.MakeMove(game);
                    Utility.PrintCardCall(AIMove);
                    Console.WriteLine("Result of Santiago's move? Hit/Miss");
                    string resString = Console.ReadLine()?.ToLower();

                    AIMove.Result = resString == "hit" ? CallResult.Hit : CallResult.Miss;

                    ai.ProcessMove(AIMove);
                    game.ProcessMove(AIMove);

                }
            }

        }
    }
}
