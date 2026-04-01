using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TetrisWinForms
{
    public partial class Form1 : Form
    {
        Timer gameTimer = new Timer();
        Timer clockTimer = new Timer();
        const int rows = 15;
        const int cols = 12;
        const int blockSize = 30;
        Color[,] grid = new Color[cols, rows];
        Tetromino current;
        Image backgroundImage;

        int offsetX = 169;
        int offsetY = 145;
        int score = 0;
        DateTime startTime;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Width = 700;
            this.Height = 700;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Tetris Dino Edition";

            backgroundImage = Image.FromFile("background.png");
            InitGame();

            gameTimer.Interval = 500;
            gameTimer.Tick += GameTick;
            gameTimer.Start();

            clockTimer.Interval = 1000;
            clockTimer.Tick += (s, e) => this.Invalidate();
            clockTimer.Start();

            this.Paint += Draw;
            this.KeyDown += HandleKeys;
        }

        void InitGame()
        {
            InitGrid();
            current = Tetromino.GetRandom();
            score = 0;
            startTime = DateTime.Now;
        }

        void InitGrid()
        {
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    grid[x, y] = Color.Black;
        }

        void GameTick(object sender, EventArgs e)
        {
            if (!Move(0, 1))
            {
                Merge();
                ClearLines();
                current = Tetromino.GetRandom();
                if (!IsValidPosition(current))
                {
                    gameTimer.Stop();
                    clockTimer.Stop();
                    var result = MessageBox.Show("Game Over!\nScore: " + score + "\n\nPlay again?", "Game Over", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        InitGame();
                        gameTimer.Start();
                        clockTimer.Start();
                    }
                    else
                    {
                        Application.Exit();
                    }
                    return;
                }
            }
            this.Invalidate();
        }

        void Merge()
        {
            foreach (Point p in current.Blocks)
            {
                int x = p.X + current.Pos.X;
                int y = p.Y + current.Pos.Y;
                if (y >= 0)
                    grid[x, y] = current.Color;
            }
        }

        void ClearLines()
        {
            for (int y = rows - 1; y >= 0; y--)
            {
                if (Enumerable.Range(0, cols).All(x => grid[x, y] != Color.Black))
                {
                    for (int yy = y; yy > 0; yy--)
                        for (int x = 0; x < cols; x++)
                            grid[x, yy] = grid[x, yy - 1];

                    for (int x = 0; x < cols; x++)
                        grid[x, 0] = Color.Black;

                    y++;
                    score += 100;
                }
            }
        }

        bool Move(int dx, int dy)
        {
            var test = current.Copy();
            test.Pos.Offset(dx, dy);
            if (IsValidPosition(test))
            {
                current.Pos = test.Pos;
                return true;
            }
            return false;
        }

        void Rotate()
        {
            var test = current.Copy();
            test.Rotate();
            if (IsValidPosition(test))
                current.Rotate();
        }

        bool IsValidPosition(Tetromino t)
        {
            foreach (Point p in t.Blocks)
            {
                int x = p.X + t.Pos.X;
                int y = p.Y + t.Pos.Y;
                if (x < 0 || x >= cols || y >= rows)
                    return false;
                if (y >= 0 && grid[x, y] != Color.Black)
                    return false;
            }
            return true;
        }

        void Draw(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(backgroundImage, 0, 0, this.Width, this.Height);

            // Draw existing blocks
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    Color color = grid[x, y];
                    int px = offsetX + x * blockSize;
                    int py = offsetY + y * blockSize;

                    if (color != Color.Black)
                    {
                        g.FillRectangle(new SolidBrush(color), px, py, blockSize - 1, blockSize - 1);
                    }

                    g.DrawRectangle(Pens.DarkSlateGray, px, py, blockSize - 1, blockSize - 1);
                }
            }

            // Draw current tetromino
            foreach (Point p in current.Blocks)
            {
                int x = p.X + current.Pos.X;
                int y = p.Y + current.Pos.Y;
                if (y >= 0)
                {
                    int px = offsetX + x * blockSize;
                    int py = offsetY + y * blockSize;
                    g.FillRectangle(new SolidBrush(current.Color), px, py, blockSize - 1, blockSize - 1);
                    g.DrawRectangle(Pens.Gray, px, py, blockSize - 1, blockSize - 1); // Use gray not black
                }
            }

            // Draw Score & Timer
            TimeSpan elapsed = DateTime.Now - startTime;
            string timeStr = elapsed.ToString(@"mm\:ss");

            using (Font font = new Font("Consolas", 14, FontStyle.Bold))
            {
                g.DrawString($"Score: {score}", font, Brushes.Black, 30, 20);
                g.DrawString($"Time: {timeStr}", font, Brushes.Black, 30, 50);
            }

        }

        void HandleKeys(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left: Move(-1, 0); break;
                case Keys.Right: Move(1, 0); break;
                case Keys.Down: Move(0, 1); break;
                case Keys.Up: Rotate(); break;
                case Keys.Space:
                    while (Move(0, 1)) { }
                    GameTick(null, null);
                    break;
            }
            this.Invalidate();
        }
    }

    public class Tetromino
    {
        public List<Point> Blocks;
        public Point Pos;
        public Color Color;

        static readonly List<List<Point>> shapes = new List<List<Point>>()
        {
            new List<Point> { new Point(0,0), new Point(1,0), new Point(0,1), new Point(1,1) }, // O
            new List<Point> { new Point(0,0), new Point(1,0), new Point(2,0), new Point(3,0) }, // I
            new List<Point> { new Point(1,0), new Point(0,1), new Point(1,1), new Point(2,1) }, // T
            new List<Point> { new Point(0,0), new Point(1,0), new Point(1,1), new Point(2,1) }, // S
            new List<Point> { new Point(1,0), new Point(2,0), new Point(0,1), new Point(1,1) }, // Z
            new List<Point> { new Point(0,0), new Point(0,1), new Point(1,1), new Point(2,1) }, // L
            new List<Point> { new Point(2,0), new Point(0,1), new Point(1,1), new Point(2,1) }, // J
        };

        static readonly Color[] colors = {
            Color.Yellow, Color.Cyan, Color.Purple, Color.Green,
            Color.Red, Color.Orange, Color.Blue
        };

        public Tetromino(List<Point> blocks, Color color)
        {
            Blocks = blocks;
            Color = color;
            Pos = new Point(3, -1);
        }

        public static Tetromino GetRandom()
        {
            var rand = new Random();
            int index = rand.Next(shapes.Count);
            return new Tetromino(shapes[index].Select(p => new Point(p.X, p.Y)).ToList(), colors[index]);
        }

        public Tetromino Copy()
        {
            return new Tetromino(Blocks.Select(p => new Point(p.X, p.Y)).ToList(), Color)
            {
                Pos = new Point(Pos.X, Pos.Y)
            };
        }

        public void Rotate()
        {
            for (int i = 0; i < Blocks.Count; i++)
            {
                int x = Blocks[i].X;
                int y = Blocks[i].Y;
                Blocks[i] = new Point(-y, x);
            }
        }
    }
}
