using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;

namespace LabyrinthsStack
{
    internal interface IStack<T>
    {
        void Push(T item);
        T Pop();
        T Peek();
        int Count {get;}
    }
    
    internal class MyStack<T>: IStack<T>
    {
        private readonly LinkedList<T> _list = new LinkedList<T>();
        
        public void Push(T item) => _list.AddLast(item);

        public T Pop()
        {
            var ret = _list.Last.Value;
            _list.RemoveLast();
            // Thread.Sleep(10);
            return ret;
        }

        public T Peek() => _list.Last.Value;

        public int Count => _list.Count;
    }
    
    internal class DefaultStack<T>: Stack<T>, IStack<T> {}

    internal struct Point
    {
        public readonly int X;
        public readonly int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point(Point pt)
        {
            X = pt.X;
            Y = pt.Y;
        }

        public Point Up => new Point(X, Y + 1);
        public Point Down => new Point(X, Y - 1);
        public Point Right => new Point(X + 1, Y);
        public Point Left => new Point(X - 1, Y);

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Point other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static bool operator ==(Point l, Point r)
        {
            return l.X == r.X && l.Y == r.Y;
        }

        public static bool operator !=(Point l, Point r)
        {
            return !(l == r);
        }
    }

    internal enum Direction
    {
        None,
        Up,
        Right,
        Down,
        Left
    }

    internal class Labyrinth
    {
        private bool[,] _matrix;
        private Point _position;
        private readonly Point _start;
        private DefaultStack<Direction> _stack = new DefaultStack<Direction>();
        private MyStack<Direction> _myStack  = new MyStack<Direction>();
        private bool _useMyStack;

        public Labyrinth(bool[,] matrix, Point start, bool useMyStack = true)
        {
            _matrix = matrix;
            _position = new Point(start);
            _start = start;
            _useMyStack = useMyStack;
        }

        private IStack<Direction> Stack => _useMyStack? _myStack :_stack as IStack<Direction>;

        public bool AtExit =>
            _position != _start && (_position.X == 0 || 
                                    _position.X == _matrix.GetLength(0) - 1 ||
                                    _position.Y == 0 ||
                                    _position.Y == _matrix.GetLength(1) - 1);

        public bool CanMoveUp => _position.Y < _matrix.GetLength(1) - 1 && _matrix[_position.X, _position.Y + 1];
        public bool CanMoveRight => _position.X < _matrix.GetLength(0) - 1 && _matrix[_position.X + 1, _position.Y];
        public bool CanMoveDown => _position.Y > 0 && _matrix[_position.X, _position.Y - 1];
        public bool CanMoveLeft => _position.X > 0 && _matrix[_position.X - 1, _position.Y];

        public bool CanMove(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return CanMoveUp;
                case Direction.Right:
                    return CanMoveRight;
                case Direction.Down:
                    return CanMoveDown;
                case Direction.Left:
                    return CanMoveLeft;
                default:
                    return false;
            }
        }

        public int PathCount =>
            (CanMoveUp ? 1 : 0) + (CanMoveRight ? 1 : 0) + (CanMoveDown ? 1 : 0) + (CanMoveLeft ? 1 : 0);

        public bool DeadEnd => PathCount == 1;
        public bool Corridor => PathCount == 2;

        public bool MoveUp(bool push = true)
        {
            if (!CanMoveUp) return false;
            Console.WriteLine("Move up");
            _position = _position.Up;
            if (push) Stack.Push(Direction.Up);
            return true;
        }

        public bool MoveRight(bool push = true)
        {
            if (!CanMoveRight) return false;
            Console.WriteLine("Move right");
            _position = _position.Right;
            if (push) Stack.Push(Direction.Right);
            return true;
        }

        public bool MoveDown(bool push = true)
        {
            if (!CanMoveDown) return false;
            Console.WriteLine("Move down");
            _position = _position.Down;
            if (push) Stack.Push(Direction.Down);
            return true;
        }

        public bool MoveLeft(bool push = true)
        {
            if (!CanMoveLeft) return false;
            Console.WriteLine("Move left");
            _position = _position.Left;
            if (push) Stack.Push(Direction.Left);
            return true;
        }

        public bool Move(Direction direction, bool push = true)
        {
            switch (direction)
            {
                case Direction.Up:
                    return MoveUp(push);
                case Direction.Right:
                    return MoveRight(push);
                case Direction.Down:
                    return MoveDown(push);
                case Direction.Left:
                    return MoveLeft(push);
                default:
                    return false;
            }
        }

        public Direction Rotate(Direction direction, bool clockwise = false)
        {
            switch (direction)
            {
                case Direction.Up:
                    return clockwise? Direction.Right : Direction.Left;
                case Direction.Right:
                    return clockwise? Direction.Down : Direction.Up;
                case Direction.Down:
                    return clockwise? Direction.Left : Direction.Right;
                case Direction.Left:
                    return clockwise? Direction.Up : Direction.Down;
                default:
                    return Direction.Up;
            }
        }

        public Direction Invert(Direction direction) => Rotate(Rotate(direction));

        public Direction Return()
        {
            if (Stack.Count == 0) return Direction.None;
            Console.WriteLine("--Return--");
            var direction = Stack.Pop();
            Move(Invert(direction), false);

            while (PathCount < 3)
            {
                if (Stack.Count == 0) return Direction.None;
                Console.WriteLine("--Return--");
                direction = Stack.Pop();
                Move(Invert(direction), false);
            }

            return direction;
        }

        public bool LeftHandMove()
        {
            if (Stack.Count > 0)
            {
                var prevDirection = Stack.Peek();
                if (Move(Rotate(prevDirection)) ||
                    Move(prevDirection) ||
                    Move(Rotate(prevDirection, true))) return true;

                var lastDirection = Return();

                return lastDirection != Direction.None && Move(Rotate(lastDirection, true));
            }

            const Direction direction = Direction.Up;
            return Move(Rotate(direction)) ||
                   Move(direction) ||
                   Move(Rotate(direction, true)) ||
                   Move(Invert(direction));
        }

        public bool Walk()
        {
            while (!AtExit && LeftHandMove());

            return AtExit;
        }
    }
    
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var dlg = new OpenFileDialog();
            dlg.InitialDirectory = Directory.GetCurrentDirectory();
            if (dlg.ShowDialog() == DialogResult.Cancel) return;
            var path = dlg.FileName;
            
            Labyrinth myLabyrinth = null, netLabyrinth = null;
            var readError = false;

            int startRow = 0, startCol = 0;
            bool[,] matrix = null;

            using (var csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new[] {"#"};
                csvParser.SetDelimiters(",");
                csvParser.HasFieldsEnclosedInQuotes = false;
                csvParser.TrimWhiteSpace = true;

                var line = csvParser.ReadFields();
                try
                {
                    var rows = int.Parse(line[0]);
                    var cols = int.Parse(line[1]);
                    
                    line = csvParser.ReadFields();
                    startRow = rows - int.Parse(line[0]) - 1;
                    startCol = int.Parse(line[1]);

                    matrix = new bool[cols, rows];
                    for (var i = 0; i < rows && !csvParser.EndOfData; i++)
                    {
                        line = csvParser.ReadFields();
                        for (var j = 0; j < cols; j++) matrix[j, rows - i - 1] = int.Parse(line[j]) == 0;
                    }
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Error reading CSV.");
                    readError = true;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Error reading CSV.");
                    readError = true;
                }
            }
            
            if (readError) return;
            
            myLabyrinth = new Labyrinth(matrix, new Point(startCol, startRow));
            netLabyrinth = new Labyrinth(matrix, new Point(startCol, startRow), false);
            
            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine(myLabyrinth.Walk());
            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);
            var myTime = sw.ElapsedTicks;
            sw.Restart();
            Console.WriteLine(netLabyrinth.Walk());
            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);
            Console.Write("My algorithm is slower on ");
            Console.Write(myTime - sw.ElapsedTicks);
            Console.Write(" ticks.");
        }
    }
}