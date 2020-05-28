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
                    color = Color.FromArgb(rand.Next(0, 255), 0, 0, 0);
                }
                else
                {
                    color = Color.FromArgb(rand.Next(255, 255), rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));
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
            PictureBox1.Image = Image.FromFile(OpenFileDialog1.FileName);
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
                switch (saveFileDialog1.FilterIndex)
                {
                    case 1:
                        PictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Jpeg);
                        break;
                    case 2:
                        PictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
                        break;
                    case 3:
                        PictureBox1.Image.Save(saveFileDialog1.FileName, ImageFormat.Png);
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
            PictureBox1.Image.Save(fileName, format);
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
            Bitmap bmp = DrawControlToBitmap(PictureBox1);
            List<TalkingHeads.DataStructures.Form> forms = Eyes.FindForms(bmp, ImageFormat.Bmp);
            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            Pen pen = new Pen(Color.Black);
            g.DrawRectangles(pen, forms.Select(x => x.Rect).ToArray());
            foreach(TalkingHeads.DataStructures.Form form in forms)
            {
                Point pt = form.GetCenter();
                Rectangle rect = new Rectangle(pt.X - (Configuration.SizeOfIdRectangle/2), pt.Y - (Configuration.SizeOfIdRectangle / 2), Configuration.SizeOfIdRectangle, Configuration.SizeOfIdRectangle);
                g.FillRectangle(Brushes.White, rect);
                g.DrawString("" + form.ID, new Font("Tahoma", 10), Brushes.Black, rect);
            }

            PictureBox1.Image = bmp;
        }

        private void Test()
        {
            // Scaling
            TalkingHead th = new TalkingHead();
            Brain.GetDiscriminationTrees(th, Memory.LoadImageToBmp("C:\\Users\\Nicolas Feron\\Pictures\\TalkingHeads\\" + "1.bmp"), ImageFormat.Bmp);
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
                Test();
                e.SuppressKeyPress = true;
            }
        }

        
    }
}
