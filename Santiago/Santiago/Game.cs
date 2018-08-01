using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Santiago
{
    enum CallResult
    {
        Hit,
        Miss,
        Unknown
    }

    class CardCall
    {
        public string TargetName;
        public string SenderName;
        public string CardRequested;
        public CallResult Result;

        public CardCall(string targetName, string senderName, string cardRequested, CallResult result)
        {
            TargetName = targetName;
            SenderName = senderName;
            CardRequested = cardRequested;
            Result = result;
        }

        public CardCall() {}
    }

    class SuitCall
    {
        public string HalfSuitName;
        public string Team;
        public string SenderName;
        public CallResult Result;

        public SuitCall(string halfSuitName, string team, string senderName, CallResult result)
        {
            HalfSuitName = halfSuitName;
            Team = team;
            SenderName = senderName;
            Result = result;
        }

        public SuitCall() {}
    }

    class Game
    {
        List<Object> moveList = new List<Object>();
        public int RedTeamScore, BlueTeamScore;
        public string PlayerTurn;
        public bool GameOver = false;

        public void ProcessMove(SuitCall sc)
        {
            moveList.Add(sc);
            if (sc.Result == CallResult.Hit)
            {
                if (sc.Team == "Red") RedTeamScore++;
                else BlueTeamScore++;
            }
            else
            {
                if (sc.Team == "Red") BlueTeamScore++;
                else RedTeamScore++;
            }

            if (RedTeamScore + BlueTeamScore >= 9)
                GameOver = false; // check if the sum of the scores is 9 (call halfsuits have been called)
        }

        public void ProcessMove(CardCall cc)
        {
            moveList.Add(cc);
            if (cc.Result == CallResult.Miss)
                PlayerTurn = cc.TargetName;
        }

        public void ExportAFN(string gameName)
        {
            string fileString = "";
            for (int i = 0; i < moveList.Count; i++)
            {
                string data = "";
                if (moveList[i].GetType() == typeof(SuitCall))
                {
                    SuitCall c = (SuitCall)moveList[i];
                    data += c.SenderName + ";" + c.Team + ";" + c.HalfSuitName + ";";
                    if (c.Result == CallResult.Hit) data += "hit";
                    else if (c.Result == CallResult.Miss) data += "miss";
                    else data += "unknown";

                    fileString += data + Environment.NewLine;
                }
                else if (moveList[i].GetType() == typeof(CardCall))
                {
                    CardCall c = (CardCall)moveList[i];
                    data += c.SenderName + ";" + c.TargetName + ";" + c.CardRequested + ";";
                    if (c.Result == CallResult.Hit) data += "hit";
                    else if (c.Result == CallResult.Miss) data += "miss";
                    else data += "unknown";

                    fileString += data + Environment.NewLine;
                }
            }

            string path = gameName + ".afn";
            if (!File.Exists(path)) File.Create(path);

            File.WriteAllText(path, fileString);
        }
    }
}
