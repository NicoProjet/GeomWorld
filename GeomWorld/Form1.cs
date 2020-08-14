using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TalkingHeads;
using TalkingHeads.BodyParts;

namespace GeomWorld
{
    public enum Forms
    {
        Circle,
        Rectangle,
        Triangle,
    }
    
    public partial class Form1 : Form
    {
        TalkingHead th = null;
        TalkingHead inactive_th = null;
        string description = "";
        Bitmap image = null;
        List<TalkingHeads.DataStructures.DiscriminationTree.Guess> ProcessingMemoryGuesser = new List<TalkingHeads.DataStructures.DiscriminationTree.Guess>();
        List<TalkingHeads.DataStructures.DiscriminationTree.Guess> ProcessingMemorySpeaker = new List<TalkingHeads.DataStructures.DiscriminationTree.Guess>();
        int lastSelect = -10;
        bool lastGuessWasCorrect = false;
        public Form1()
        {
            InitializeComponent();
        }

        private float Distance(Point p1, Point p2)
        {
            return (float) Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private void FillTriangle(Random rand, Graphics g, SolidBrush brush, Rectangle r)
        {
            FillTriangle(rand, g, brush, r.X, r.Y, r.Width, r.Height);
        }

        private void FillTriangle(Random rand, Graphics g, SolidBrush brush, int x, int y, int width, int height)
        // TO-DO: add a verification to avoid triangleso thin they look like lines and may sometimes be barely visible
        {
            int MinEdgeSize = Math.Min(this.PictureBox1.Width / TalkingHeads.Configuration.MinFormSizeDivide, this.PictureBox1.Height / TalkingHeads.Configuration.MinFormSizeDivide);
            List<Point> points = new List<Point>();
            for (int i = 0; i < 3; i++)
            {
                int newPointX = rand.Next(x, (x + width));
                int newPointY = rand.Next(y, (y + height));
                Point p = new Point(newPointX, newPointY);
                int counter = 0;
                while ((points.Count() == 0 || points.Any(a => Distance(a, p) < MinEdgeSize)) && counter < TalkingHeads.Configuration.MaxLoopCounter)
                {
                    newPointX = rand.Next(x, (x + width));
                    newPointY = rand.Next(y, (y + height));
                    p = new Point(newPointX, newPointY);
                    counter++;
                    if (counter == TalkingHeads.Configuration.MaxLoopCounter)
                    {
                        points.RemoveAll(a => true);
                        newPointX = rand.Next(x, (x + width));
                        newPointY = rand.Next(y, (y + height));
                        p = new Point(newPointX, newPointY);
                        points.Add(p);
                    }
                }
                points.Add(p);
            }
            g.FillPolygon(brush, points.ToArray());
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            PictureBox1_Render();
        }

        private void PictureBox1_Render()
        {
            Bitmap drawing = new Bitmap(PictureBox1.Width, PictureBox1.Height);
            Graphics g = Graphics.FromImage(drawing);

            // Global Properties
            int MinFormWidth = this.PictureBox1.Width / TalkingHeads.Configuration.MinFormSizeDivide;
            int MaxFormWidth = this.PictureBox1.Width / TalkingHeads.Configuration.MaxFormSizeDivide;
            int MinFormHeight = this.PictureBox1.Height / TalkingHeads.Configuration.MinFormSizeDivide;
            int MaxFormHeight = this.PictureBox1.Height / TalkingHeads.Configuration.MaxFormSizeDivide;

            // Random global properties
            Random rand = new Random();
            int numberOfForms = rand.Next(TalkingHeads.Configuration.MinNumberOfForms, TalkingHeads.Configuration.MaxNumberOfForms);

            // Containers to avoid intersections
            List<Rectangle> containers = new List<Rectangle>();

            // Set a white background (will be transparent if supported in file formats)
            using(SolidBrush sb = new SolidBrush(Color.White))
            {
                g.FillRectangle(sb, 0, 0, PictureBox1.Width, PictureBox1.Height);
            }


            for (uint i = 0; i < numberOfForms; i++)
            {
                // Size (of the rectangle containing the form) 
                int width;
                int height;
                Rectangle container;

                // Coordinates (of the top-left corner of the container)
                int x;
                int y;

                // Color
                Color color;
                if (TalkingHeads.Configuration.GrayScale)
                {
                    color = Color.FromArgb(rand.Next(TalkingHeads.Configuration.GrayScaleMinAlpha, 255), 0, 0, 0);
                }
                else
                {
                    color = Color.FromArgb(255, rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));
                }
                SolidBrush brush = new SolidBrush(color);

                // Form
                Array forms = Enum.GetValues(typeof(Forms));
                Forms form = (Forms)forms.GetValue(rand.Next(forms.Length));

                // Create rectangle area containing the form (used to avoir intersections in an easy but non area optimized way)
                int counter = 0;
                do
                {
                    width = (int)(MinFormWidth + (rand.NextDouble() * (MaxFormWidth - MinFormWidth)));
                    height = (int)(MinFormHeight + (rand.NextDouble() * (MaxFormHeight - MinFormHeight)));

                    x = (int)(rand.NextDouble() * (this.PictureBox1.Width - width));
                    y = (int)(rand.NextDouble() * (this.PictureBox1.Height - height));

                    container = new Rectangle(x, y, width, height);
                } while ((containers.Count() == 0 || containers.Any(a => a.IntersectsWith(container))) && counter++ < TalkingHeads.Configuration.MaxLoopCounter);
                containers.Add(container);

                switch (form)
                {
                    case Forms.Circle:
                        g.FillEllipse(brush, container);
                        break;
                    case Forms.Rectangle:
                        g.FillRectangle(brush, container);
                        break;
                    case Forms.Triangle:
                        FillTriangle(rand, g, brush, container);
                        break;
                    default:
                        break;
                }
            }
            PictureBox1.Image = drawing;
            image = drawing.Clone(new Rectangle(0, 0, drawing.Width, drawing.Height), drawing.PixelFormat);

            if (Configuration.PrintForms)
            {
                Console.WriteLine("Numer of forms: " + containers.Count());
                foreach (Rectangle r in containers)
                {
                    Console.WriteLine("x = " + r.X + "  |  y = " + r.Y + "  |  width = " + r.Width + "  |  height = " + r.Height);
                }
            }
        }

        private void PictureBox1_LoadImage()
        {
            OpenFileDialog OpenFileDialog1 = new OpenFileDialog();
            OpenFileDialog1.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Png Image|*.png";
            OpenFileDialog1.Title = "Load an Image File";
            OpenFileDialog1.ShowDialog();
            if (OpenFileDialog1.FileName != "")
            {
                PictureBox1.Image = Image.FromFile(OpenFileDialog1.FileName);
                image = ((Bitmap) PictureBox1.Image).Clone(new Rectangle(0, 0, PictureBox1.Image.Width, PictureBox1.Image.Height), PictureBox1.Image.PixelFormat);
            }
        }

        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            
        }

        private Bitmap DrawControlToBitmap(Control control)
        {
            Bitmap bmp = new Bitmap(control.Width, control.Height);
            Graphics g = Graphics.FromImage(bmp);
            Rectangle r = control.RectangleToScreen(control.ClientRectangle);
            g.CopyFromScreen(r.Location, Point.Empty, control.Size);
            return bmp;
        }

        private void PictureBox1_ScreenShot()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp";
            saveFileDialog1.Title = "Save an Image File";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                System.Threading.Thread.Sleep(100);
                Bitmap bmp;
                bmp = DrawControlToBitmap(this.PictureBox1);
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                        bmp.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
                        break;

                    case 2:
                        bmp.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
                        break;
                }
            }
        }

        private void SaveImage()
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Png Image|*.png";
            saveFileDialog1.Title = "Save an Image File";
            saveFileDialog1.ShowDialog();

            if (saveFileDialog1.FileName != "")
            {
                Bitmap bmp;
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                        bmp = image;
                        if (bmp == null) PictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
                        else bmp.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
                        break;
                    case 2:
                        bmp = image;
                        if (bmp == null) PictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
                        else bmp.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
                        break;
                    case 3:
                        bmp = image;
                        if (bmp == null) PictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Png);
                        else bmp.Save(saveFileDialog1.FileName, ImageFormat.Png);
                        break;
                }
            }
        }

        private void SaveImages(int number, string formatStr)
        {
            string directory = "C:\\Users\\Nicolas Feron\\Pictures\\TalkingHeads\\";
            string fileName = directory + 0 + formatStr;
            ImageFormat format;
            switch (formatStr)
            {
                case ".jpg":
                    format = ImageFormat.Jpeg;
                    break;
                case ".bmp":
                    format = ImageFormat.Bmp;
                    break;
                case ".png":
                    format = ImageFormat.Png;
                    break;
                default:
                    format = ImageFormat.Png;
                    break;
            }
            Bitmap bmp = image;
            if (bmp == null) PictureBox1.Image.Save(fileName, format);
            else bmp.Save(fileName, format);
            for (int i = 1; i < number; i++)
            {
                fileName = directory + i + formatStr;
                PictureBox1_Render();
                PictureBox1.Image.Save(fileName, format);
            }
        }

        private void SaveImagesDialog()
        {
            Form dialog = new Form()
            {
                Width = 330,
                Height = 150,
                Text = "Save n images.",
            };
            Label inputLabel = new Label()
            {
                Left = 50,
                Top = 20,
                Text = "Number of Images",
            };
            NumericUpDown input = new NumericUpDown()
            {
                Left = 150,
                Top = 20,
                Width = 100,
                Value = 50,
            };
            Label selectLabel = new Label()
            {
                Left = 50,
                Top = 50,
                Text = "File encoding",
            };
            ComboBox select = new ComboBox()
            {
                Left = 150,
                Top = 50,
                Width = 100,
            };
            string[] choices = { ".jpg", ".bmp", ".png" };
            select.Items.AddRange(choices);
            select.SelectedText = ".jpg";
            Button confirmBtn = new Button()
            {
                Text = "Save",
                Left = 120,
                Width = 50,
                Top = 80,
            };
            confirmBtn.Click += (sender, e) => { 
                SaveImages((int)input.Value, select.Text);
                dialog.Close(); 
            };

            dialog.Controls.Add(inputLabel);
            dialog.Controls.Add(input);
            dialog.Controls.Add(selectLabel);
            dialog.Controls.Add(select);
            dialog.Controls.Add(confirmBtn);
            dialog.ShowDialog();
        }

        private void ShowSegmentation()
        {
            Bitmap bmp = image;
            if (bmp == null)
            {
                image = DrawControlToBitmap(PictureBox1);
            }

            List<TalkingHeads.DataStructures.Form> forms = Eyes.FindForms(bmp, ImageFormat.Bmp);
            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            Pen pen = new Pen(Color.Black);
            g.DrawRectangles(pen, forms.Select(x => x.Rect).ToArray());
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            foreach (TalkingHeads.DataStructures.Form form in forms)
            {
                Point pt = form.GetCenter();
                Rectangle rect = new Rectangle(pt.X - (Configuration.SizeOfIdRectangle/2), pt.Y - (Configuration.SizeOfIdRectangle / 2), Configuration.SizeOfIdRectangle, Configuration.SizeOfIdRectangle);
                g.FillRectangle(Brushes.White, rect);
                g.DrawString("" + form.ID, new Font("Tahoma", 16), Brushes.Black, rect, format);
            }

            PictureBox1.Image = bmp;
        }

        private void LoadTalkingHead()
        {
            if (th == null)
            {
                th = new TalkingHead("Zero", true);
            }
            Memory.LoadTalkingHead(th);
            Console.WriteLine("Loaded Talking Head '" + th.Name + "'");
        }

        private void SaveTalkingHead()
        {
            if (th != null)
            {
                Memory.SaveTalkingHead(th);
            }
            Console.WriteLine("Saved Talking Head '" + th.Name + "'");
        }

        private void DescribeForm(bool print = true)
        {
            Bitmap bmp = image;
            if (bmp == null) bmp = DrawControlToBitmap(PictureBox1);

            if (th == null) LoadTalkingHead();
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Bmp);
                ProcessingMemorySpeaker.Clear();
                string guess = Brain.DiscriminationGameDescription(th, ms, bmp.Width, bmp.Height, out lastSelect, ProcessingMemorySpeaker, print);
                description = guess;
            }
        }

        private void MakeGuess(bool print = true)
        {
            Bitmap bmp = image;
            if (bmp == null) bmp = DrawControlToBitmap(PictureBox1);

            if (th == null) LoadTalkingHead();
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Bmp);
                ProcessingMemoryGuesser.Clear();
                int IDForm = Brain.DiscriminationGameGuessID(th, ms, bmp.Width, bmp.Height, description, ProcessingMemoryGuesser, print);
                if (IDForm == -2)
                {
                    if (print) Console.Write(th.Name + " could not recognize all the words.");
                    Brain.EnterCorrectForm(th, ms, bmp.Width, bmp.Height, description, lastSelect);
                }
                else if (IDForm < 0)
                {
                    lastGuessWasCorrect = false;
                    if (print) Console.Write("I tried to find a form with ");
                    foreach(TalkingHeads.DataStructures.DiscriminationTree.Guess item in ProcessingMemoryGuesser)
                    {
                        if (print) Console.Write(item.Node.Print() + " ");
                    }
                    if (print) Console.Write("\n");
                    Brain.EnterCorrectForm(th, ms, bmp.Width, bmp.Height, description, lastSelect);
                }
                else if (IDForm == lastSelect)
                {
                    lastGuessWasCorrect = true;
                    CorrectGuess(print);
                }
                else
                {
                    lastGuessWasCorrect = false;
                    IncorrectGuess(print);
                    Brain.EnterCorrectForm(th, ms, bmp.Width, bmp.Height, description, lastSelect);
                }
            }
            //int IDForm = Brain.DiscriminationGameGuessID(th, bmp, ImageFormat.Bmp, description, true);
        }

        private void CorrectGuess(bool print = true)
        {
            th.UpdateScore(ProcessingMemoryGuesser, true);
            if (inactive_th != null) inactive_th.UpdateScore(ProcessingMemorySpeaker, true);
            if (print) Console.WriteLine("Last guess was correct, scores have been updated accordingly.");
        }

        private void IncorrectGuess(bool print = true)
        {
            th.UpdateScore(ProcessingMemoryGuesser, false);
            if (inactive_th != null) inactive_th.UpdateScore(ProcessingMemorySpeaker, false);
            if (print) Console.WriteLine("Last guess was incorrect, scores have been updated accordingly.");
        }

        private void DoMultipleTests(int numberOfTests, bool print, bool printDetails = true)
        {
            int CorrectCounter = 0;
            for (int i = 0; i<numberOfTests; i++)
            {
                if (printDetails) Console.WriteLine("Test n°" + i);
                PictureBox1_Render(); // new image
                DescribeForm(print); // description
                MakeGuess(print); // Guess
                if (lastGuessWasCorrect)
                {
                    CorrectCounter++;
                }
            }

            if (printDetails) Console.WriteLine("\n\n\n ---- RESULTS ---- \n");
            Console.WriteLine("" + CorrectCounter + " tests were successful, percentage of success = " +(100 * ((double) CorrectCounter / (double) numberOfTests)) + "%");
        }

        private void SingleTestAndSave(bool print = true)
        {
            PictureBox1_Render(); // new image
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

        private void MultipleTests()
        {
            Form dialog = new Form()
            {
                Width = 330,
                Height = 150,
                Text = "Do n Tests.",
            };
            Label inputLabel = new Label()
            {
                Left = 50,
                Top = 20,
                Text = "Number of tests",
            };
            NumericUpDown input = new NumericUpDown()
            {
                Left = 150,
                Top = 20,
                Width = 100,
                Value = 50,
                Maximum = 250,
            };
            Label printProcessLabel = new Label()
            {
                Left = 50,
                Top = 50,
                Text = "Print the process",
            };
            CheckBox printProcess = new CheckBox() 
            {
                Left = 150,
                Top = 50,
                AutoCheck = true,
                Checked = true,
            };

            Button confirmBtn = new Button()
            {
                Text = "Go",
                Left = 120,
                Width = 50,
                Top = 80,
            }; 
            confirmBtn.Click += (sender, e) => {
                DoMultipleTests((int)input.Value, printProcess.Checked);
                dialog.Close();
            };

            dialog.Controls.Add(inputLabel);
            dialog.Controls.Add(input);
            dialog.Controls.Add(printProcessLabel);
            dialog.Controls.Add(printProcess);
            dialog.Controls.Add(confirmBtn);
            dialog.ShowDialog();
        }

        private void GetDataForGraph()
        {
            for (int i=0; i<20; i++)
            {
                Console.Write("Test n°" + i + ": ");
                DoMultipleTests(50, false, false);
            }
        }

        private void DoMultipleGames(int numberOfTests, bool print, bool printDetails = true)
        {
            int CorrectCounter = 0;
            if (th == null) th = new TalkingHead("Zero", true);
            if (inactive_th == null) inactive_th = new TalkingHead("Borderlands", true);
            for (int i = 0; i < numberOfTests; i++)
            {
                if (printDetails) Console.WriteLine("Game n°" + i);
                PictureBox1_Render(); // new image
                if (printDetails) Console.WriteLine("Active th: " + th.Name);
                DescribeForm(print); // description

                TalkingHead tmp = th;
                th = inactive_th;
                inactive_th = tmp;

                if (printDetails) Console.WriteLine("Active th: " + th.Name);
                MakeGuess(print); // Guess
                if (lastGuessWasCorrect)
                {
                    CorrectCounter++;
                }
            }

            if (printDetails) Console.WriteLine("\n\n\n ---- RESULTS ---- \n");
            Console.WriteLine("" + CorrectCounter + " tests were successful, percentage of success = " + (100 * ((double)CorrectCounter / (double)numberOfTests)) + "%");
            Memory.SaveTalkingHead(th);
            Memory.SaveTalkingHead(inactive_th);
        }

        private void MultipleGames()
        {
            Form dialog = new Form()
            {
                Width = 330,
                Height = 150,
                Text = "Do n Discrimination Games.",
            };
            Label inputLabel = new Label()
            {
                Left = 50,
                Top = 20,
                Text = "Number of games",
            };
            NumericUpDown input = new NumericUpDown()
            {
                Left = 150,
                Top = 20,
                Width = 100,
                Value = 50,
                Maximum = 250,
            };
            Label printProcessLabel = new Label()
            {
                Left = 50,
                Top = 50,
                Text = "Print the process",
            };
            CheckBox printProcess = new CheckBox()
            {
                Left = 150,
                Top = 50,
                AutoCheck = true,
                Checked = true,
            };

            Button confirmBtn = new Button()
            {
                Text = "Go",
                Left = 120,
                Width = 50,
                Top = 80,
            };
            confirmBtn.Click += (sender, e) => {
                DoMultipleGames((int)input.Value, printProcess.Checked);
                dialog.Close();
            };

            dialog.Controls.Add(inputLabel);
            dialog.Controls.Add(input);
            dialog.Controls.Add(printProcessLabel);
            dialog.Controls.Add(printProcess);
            dialog.Controls.Add(confirmBtn);
            dialog.ShowDialog();
        }

        private void GetDataForGraphGames()
        {
            for (int i = 0; i < 20; i++)
            {
                Console.Write("Game n°" + i + ": ");
                DoMultipleGames(50, false, false);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S) // Ctrl+S
            {
                SaveImage();
                e.SuppressKeyPress = true;  // Stops other controls on the form receiving event.
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                SaveImagesDialog();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.X)
            {
                ShowSegmentation();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                PictureBox1_Render();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.L)
            {
                PictureBox1_LoadImage();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.T)
            {
                DescribeForm();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                SaveTalkingHead();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.U)
            {
                MakeGuess();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                LoadTalkingHead();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CorrectGuess();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                IncorrectGuess();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.G)
            {
                MultipleTests();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                SingleTestAndSave();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                GetDataForGraph();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                MultipleGames();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.P)
            {
                Console.WriteLine("Start batch of games:");
                GetDataForGraphGames();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.M)
            {
                DoMultipleGames(1, true, true);
                e.SuppressKeyPress = true;
            }
        }

        
    }
}
