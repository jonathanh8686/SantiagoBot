﻿using System;
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
            for (int i = 0; i < CardNames.Length; i++)
            {
                CardIndex.Add(CardNames[i], i); // CardIndex[cardName] = cardNumericalValue
                NumberCard.Add(i, CardNames[i]);
            }

            // Initalize Halfsuits
            string[] halfSuitNames = File.ReadAllLines("HalfSuitNames.txt");

            string[] tempHalfSuits = File.ReadAllLines("HalfSuits.txt");
            for (int i = 0; i < tempHalfSuits.Length; i++)
                HalfSuits.Add(halfSuitNames[i], tempHalfSuits[i].Split(','));

            Console.ForegroundColor = ConsoleColor.White;
            var ai = new AI();

            for (int i = 2; i <= 6; i++)
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
            Game game = new Game();

            Console.WriteLine("Who's turn it is?");
            game.PlayerTurn = Console.ReadLine()?.ToLower();

            while (!game.GameOver)
            {
                if (game.PlayerTurn != "santiago")
                {
                    // Take in move made
                    Console.WriteLine($"{game.PlayerTurn}'s turn! What move did they make?");
                    var moveData = Console.ReadLine()?.Split(" ");

                    if (moveData != null)
                        for (var i = 0; i < moveData.Length; i++)
                            moveData[i] = moveData[i].ToLower();

                    if (moveData?[0] == "call") // ["call", HalfSuit, Result]
                    {
                        // Halfsuit Called
                        var res = moveData[2] == "hit" ? CallResult.Hit : CallResult.Miss;
                        var sc = new SuitCall(moveData[1], PlayerTeams[game.PlayerTurn], game.PlayerTurn, res);
                        game.ProcessMove(sc);
                        ai.ProcessMove(sc);
                    }
                    else // [TargetName, CardName, Result]
                    {
                        // Card Called
                        var res = moveData?[2] == "hit" ? CallResult.Hit : CallResult.Miss;
                        var cc = new CardCall(moveData?[0], game.PlayerTurn, moveData?[1], res);
                        game.ProcessMove(cc);
                        ai.ProcessMove(cc);
                    }

                }
                else
                {
                    // Make a move
                }
            }

        }
    }
}
