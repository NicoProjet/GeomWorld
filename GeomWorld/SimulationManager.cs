using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TalkingHeads;
using TalkingHeads.BodyParts;
using dt = TalkingHeads.DataStructures;

namespace GeomWorld
{
    public class SimulationManager
    {
        public TalkingHead th = null;
        public TalkingHead inactive_th = null;
        public List<TalkingHead> guessersList = new List<TalkingHead>();
        public List<List<dt.DiscriminationTree.Guess>> ProcessingMemoryGuessers = new List<List<dt.DiscriminationTree.Guess>>();
        public string description = "";
        public Bitmap image = null;
        public List<dt.DiscriminationTree.Guess> ProcessingMemoryGuesser = new List<dt.DiscriminationTree.Guess>();
        public List<dt.DiscriminationTree.Guess> ProcessingMemorySpeaker = new List<dt.DiscriminationTree.Guess>();
        public int lastSelect = -10;
        public bool lastGuessWasCorrect = false;


        public void InitGuessersList()
        {
            guessersList.Clear();
            guessersList.Add(new TalkingHead("Zero", true));
            guessersList.Add(new TalkingHead("Borderlands", true));
            guessersList.Add(new TalkingHead("Albert", true));
            guessersList.Add(new TalkingHead("Robert", true));
            guessersList.Add(new TalkingHead("Siren", true));
            guessersList.Add(new TalkingHead("Tess", true));
        }
        public void LoadTalkingHead()
        {
            if (th == null)
            {
                th = new TalkingHead("Zero", true);
            }
            Memory.LoadTalkingHead(th);
            Console.WriteLine("Loaded Talking Head '" + th.Name + "'");
        }

        public void SaveTalkingHead()
        {
            if (th != null)
            {
                Memory.SaveTalkingHead(th);
            }
            Console.WriteLine("Saved Talking Head '" + th.Name + "'");
        }

        public void DescribeForm(bool print = true, TalkingHead speaker = null)
        {
            if (speaker == null && th == null) LoadTalkingHead();
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Bmp);
                ProcessingMemorySpeaker.Clear();
                string guess;
                if (speaker == null) guess = Brain.DiscriminationGameDescription(th, ms, image.Width, image.Height, out lastSelect, ProcessingMemorySpeaker, print);
                else guess = Brain.DiscriminationGameDescription(speaker, ms, image.Width, image.Height, out lastSelect, ProcessingMemorySpeaker, print);
                description = guess;
            }
        }

        public void MakeGuess(bool print = true, TalkingHead guesser = null)
        {
            if (guesser == null && th == null) LoadTalkingHead();
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Bmp);
                ProcessingMemoryGuesser.Clear();
                int IDForm;
                if (guesser == null) IDForm = Brain.DiscriminationGameGuessID(th, ms, image.Width, image.Height, description, ProcessingMemoryGuesser, print);
                else IDForm = Brain.DiscriminationGameGuessID(guesser, ms, image.Width, image.Height, description, ProcessingMemoryGuesser, print);
                if (IDForm == -2)
                {
                    if (guesser == null)
                    {
                        if (print) Console.Write(th.Name + " could not recognize all the words.");
                        Brain.EnterCorrectForm(th, ms, image.Width, image.Height, description, lastSelect);
                        //th.Erode();
                        //inactive_th.Erode();
                    }
                    else
                    {
                        if (print) Console.Write(guesser.Name + " could not recognize all the words.");
                        Brain.EnterCorrectForm(guesser, ms, image.Width, image.Height, description, lastSelect);
                        //guesser.Erode();
                    }
                    lastGuessWasCorrect = false;
                }
                else if (IDForm < 0)
                {
                    lastGuessWasCorrect = false;
                    if (print) Console.Write("I tried to find a form with ");
                    foreach (dt.DiscriminationTree.Guess item in ProcessingMemoryGuesser)
                    {
                        if (print) Console.Write(item.Node.Print() + " ");
                    }
                    if (print) Console.Write("\n");
                    if (guesser == null)
                    {
                        Brain.EnterCorrectForm(th, ms, image.Width, image.Height, description, lastSelect);
                        IncorrectGuess(print);
                    }
                    else
                    {
                        Brain.EnterCorrectForm(guesser, ms, image.Width, image.Height, description, lastSelect);
                        IncorrectGuess(guesser, ProcessingMemoryGuesser, print);
                    }
                }
                else if (IDForm == lastSelect)
                {
                    lastGuessWasCorrect = true;
                    if (guesser == null) CorrectGuess(print);
                    else CorrectGuess(guesser, ProcessingMemoryGuesser, print);
                }
                else
                {
                    lastGuessWasCorrect = false;
                    if (guesser == null)
                    {
                        IncorrectGuess(print);
                        Brain.EnterCorrectForm(th, ms, image.Width, image.Height, description, lastSelect);
                    }
                    else
                    {
                        IncorrectGuess(guesser, ProcessingMemoryGuesser, print);
                        Brain.EnterCorrectForm(guesser, ms, image.Width, image.Height, description, lastSelect);
                    }
                }
            }
        }

        public void CorrectGuess(bool print = true)
        {
            th.UpdateScore(ProcessingMemoryGuesser, true, true);
            if (inactive_th != null) inactive_th.UpdateScore(ProcessingMemorySpeaker, true, false);
            if (print) Console.WriteLine("Last guess was correct, scores have been updated accordingly.");
        }

        private void CorrectGuess(TalkingHead _th, List<dt.DiscriminationTree.Guess> processingMemory, bool print = true)
        {
            _th.UpdateScore(processingMemory, true, true);
            if (print) Console.WriteLine("Last guess was correct, scores have been updated accordingly.");
        }

        public void IncorrectGuess(bool print = true)
        {
            th.UpdateScore(ProcessingMemoryGuesser, false, true);
            if (inactive_th != null) inactive_th.UpdateScore(ProcessingMemorySpeaker, false, false);
            if (print) Console.WriteLine("Last guess was incorrect, scores have been updated accordingly.");
        }

        private void IncorrectGuess(TalkingHead _th, List<dt.DiscriminationTree.Guess> processingMemory, bool print = true)
        {
            _th.UpdateScore(processingMemory, false, true);
            if (print) Console.WriteLine("Last guess was incorrect, scores have been updated accordingly.");
        }

        public void SaveGuessersList()
        {
            foreach (TalkingHead _th in guessersList)
            {
                Memory.SaveTalkingHead(_th);
            }
        }

        public int DiscriminationGame(bool print, bool printDetails)
        {
            if (printDetails) Console.WriteLine("Active th: " + th.Name);
            DescribeForm(print); // description

            TalkingHead tmp = th;
            th = inactive_th;
            inactive_th = tmp;

            if (printDetails) Console.WriteLine("Active th: " + th.Name);
            MakeGuess(print); // Guess
            if (lastGuessWasCorrect)
            {
                return 1;
            }
            return 0;
        }
        
        public int GameWithMultipleGuessers(int i, bool print, bool printDetails = true)
        {
            int CurrentGameCorrectCounter = 0;
            int index = TalkingHeads.Configuration.seed.Next(guessersList.Count());
            TalkingHead speaker = guessersList.ElementAt(index);
            guessersList.Remove(speaker);

            if (printDetails) Console.WriteLine("Game n°" + i);
            if (printDetails) Console.WriteLine("Speaker: " + speaker.Name);
            DescribeForm(print, speaker); // description

            ProcessingMemoryGuessers.Clear();
            foreach (TalkingHead _th in guessersList)
            {
                if (printDetails) Console.WriteLine("guesser: " + _th.Name);
                MakeGuess(print, _th); // Guess
                ProcessingMemoryGuessers.Add(ProcessingMemoryGuesser.ConvertAll(x => new dt.DiscriminationTree.Guess() { Node = x.Node, Word = x.Word }));
                if (lastGuessWasCorrect) CurrentGameCorrectCounter++;
            }

            if (CurrentGameCorrectCounter > ((double)guessersList.Count() / 2))
            {
                CorrectGuess(speaker, ProcessingMemorySpeaker, false);
                guessersList.Add(speaker);
                return 1;
            }
            else
            {
                IncorrectGuess(speaker, ProcessingMemorySpeaker, false);
                guessersList.Add(speaker);
                return 0;
            }
        }
        

        public void SingleTestAndSave(bool print = true)
        {
            DescribeForm(print); // description
            MakeGuess(print); // Guess
            if (lastGuessWasCorrect)
            {
                CorrectGuess(print);
            }
            else
            {
                IncorrectGuess(print);
            }
            SaveTalkingHead();
        }
    }
}
