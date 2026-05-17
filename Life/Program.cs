using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;

namespace Life
{
    public class Cell
    {
        public bool IsAlive { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Cell(int x, int y, bool isAlive = false)
        {
            X = x;
            Y = y;
            IsAlive = isAlive;
        }

        public Cell Clone()
        {
            return new Cell(X, Y, IsAlive);
        }
    }

    public class Board
    {
        public Cell[,] Grid { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Generation { get; private set; }

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            Grid = new Cell[width, height];
            Generation = 0;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Grid[i, j] = new Cell(i, j);
                }
            }
        }

        public void SetCell(int x, int y, bool alive)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Grid[x, y].IsAlive = alive;
            }
        }

        public bool GetCell(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return Grid[x, y].IsAlive;
            }
            return false;
        }

        private int CountLiveNeighbors(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (GetCell(x + dx, y + dy)) count++;
                }
            }
            return count;
        }

        public void NextGeneration()
        {
            var newGrid = new Cell[Width, Height];
            
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    newGrid[i, j] = new Cell(i, j);
                    int liveNeighbors = CountLiveNeighbors(i, j);
                    
                    if (Grid[i, j].IsAlive)
                    {
                        newGrid[i, j].IsAlive = (liveNeighbors == 2 || liveNeighbors == 3);
                    }
                    else
                    {
                        newGrid[i, j].IsAlive = (liveNeighbors == 3);
                    }
                }
            }
            
            Grid = newGrid;
            Generation++;
        }

        public int CountLiveCells()
        {
            int count = 0;
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (Grid[i, j].IsAlive) count++;
                }
            }
            return count;
        }

        public List<List<Cell>> FindCombinations()
        {
            var combinations = new List<List<Cell>>();
            var visited = new bool[Width, Height];

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (Grid[i, j].IsAlive && !visited[i, j])
                    {
                        var combination = new List<Cell>();
                        var queue = new Queue<(int, int)>();
                        queue.Enqueue((i, j));
                        visited[i, j] = true;

                        while (queue.Count > 0)
                        {
                            var (x, y) = queue.Dequeue();
                            combination.Add(Grid[x, y]);

                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    int nx = x + dx, ny = y + dy;
                                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height &&
                                        Grid[nx, ny].IsAlive && !visited[nx, ny])
                                    {
                                        visited[nx, ny] = true;
                                        queue.Enqueue((nx, ny));
                                    }
                                }
                            }
                        }
                        combinations.Add(combination);
                    }
                }
            }
            return combinations;
        }

        public string ClassifyCombination(List<Cell> combination)
        {
            var pattern = new HashSet<(int, int)>();
            int minX = combination.Min(c => c.X);
            int minY = combination.Min(c => c.Y);
            
            foreach (var cell in combination)
            {
                pattern.Add((cell.X - minX, cell.Y - minY));
            }

            // Известные стабильные фигуры
            var block = new HashSet<(int, int)> { (0,0), (1,0), (0,1), (1,1) };
            var beeHive = new HashSet<(int, int)> { (1,0), (2,0), (0,1), (3,1), (1,2), (2,2) };
            var loaf = new HashSet<(int, int)> { (1,0), (2,0), (0,1), (3,1), (1,2), (3,2), (2,3) };
            var boat = new HashSet<(int, int)> { (0,0), (1,0), (0,1), (2,1), (1,2) };
            var tub = new HashSet<(int, int)> { (1,0), (0,1), (2,1), (1,2) };
            var pond = new HashSet<(int, int)> { (1,0), (2,0), (0,1), (3,1), (0,2), (3,2), (1,3), (2,3) };

            if (pattern.SetEquals(block)) return "Block (устойчивая)";
            if (pattern.SetEquals(beeHive)) return "Beehive (устойчивая)";
            if (pattern.SetEquals(loaf)) return "Loaf (устойчивая)";
            if (pattern.SetEquals(boat)) return "Boat (устойчивая)";
            if (pattern.SetEquals(tub)) return "Tub (устойчивая)";
            if (pattern.SetEquals(pond)) return "Pond (устойчивая)";
            
            return $"Неизвестная фигура (размер: {combination.Count})";
        }

        public void SaveToFile(string filename)
        {
            var data = new BoardData
            {
                Width = Width,
                Height = Height,
                Generation = Generation,
                Cells = new List<CellData>()
            };

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (Grid[i, j].IsAlive)
                    {
                        data.Cells.Add(new CellData { X = i, Y = j });
                    }
                }
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }

        public void LoadFromFile(string filename)
        {
            string json = File.ReadAllText(filename);
            var data = JsonSerializer.Deserialize<BoardData>(json);
            
            Width = data.Width;
            Height = data.Height;
            Generation = data.Generation;
            Grid = new Cell[Width, Height];
            
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    Grid[i, j] = new Cell(i, j);
                }
            }
            
            foreach (var cell in data.Cells)
            {
                SetCell(cell.X, cell.Y, true);
            }
        }

        public void RandomFill(double density)
        {
            var random = new Random();
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    SetCell(i, j, random.NextDouble() < density);
                }
            }
            Generation = 0;
        }

        public void Display()
        {
            Console.Clear();
            Console.WriteLine($"Поколение: {Generation}, Живых клеток: {CountLiveCells()}");
            Console.WriteLine(new string('-', Width + 2));
            
            for (int j = 0; j < Height; j++)
            {
                Console.Write("|");
                for (int i = 0; i < Width; i++)
                {
                    Console.Write(Grid[i, j].IsAlive ? "█" : " ");
                }
                Console.WriteLine("|");
            }
            Console.WriteLine(new string('-', Width + 2));
        }
    }

    public class BoardData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Generation { get; set; }
        public List<CellData> Cells { get; set; }
    }

    public class CellData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class Settings
    {
        public int Width { get; set; } = 40;
        public int Height { get; set; } = 20;
        public int DelayMs { get; set; } = 100;
        public bool AutoMode { get; set; } = false;
        public int MaxGenerations { get; set; } = 1000;

        public static Settings Load(string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<Settings>(json);
            }
            return new Settings();
        }

        public void Save(string filename)
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }
    }

    public class Program
    {
        private static Board board;
        private static Settings settings;
        private static List<(int generation, int liveCells)> history = new List<(int, int)>();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Загрузка настроек
            settings = Settings.Load("settings.json");
            board = new Board(settings.Width, settings.Height);
            
            Console.WriteLine("Игра Жизнь Конвея");
            Console.WriteLine("1. Случайное заполнение");
            Console.WriteLine("2. Загрузить фигуру");
            Console.WriteLine("3. Исследование стабильности");
            Console.WriteLine("4. Запуск с параметрами");
            Console.Write("Выберите действие: ");
            
            string choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    Console.Write("Введите плотность (0-1): ");
                    double density = double.Parse(Console.ReadLine());
                    board.RandomFill(density);
                    RunSimulation();
                    break;
                case "2":
                    LoadPreset();
                    RunSimulation();
                    break;
                case "3":
                    RunStabilityResearch();
                    break;
                case "4":
                    RunWithParameters();
                    break;
            }
        }

        static void LoadPreset()
        {
            Console.WriteLine("Доступные фигуры:");
            Console.WriteLine("1. Блок (устойчивая)");
            Console.WriteLine("2. Планер (движущаяся)");
            Console.WriteLine("3. Мигалка (периодическая)");
            Console.WriteLine("4. Ружье Госпера");
            Console.Write("Выберите фигуру: ");
            
            string preset = Console.ReadLine();
            
            switch (preset)
            {
                case "1":
                    LoadBlock();
                    break;
                case "2":
                    LoadGlider();
                    break;
                case "3":
                    LoadBlinker();
                    break;
                case "4":
                    LoadGosperGliderGun();
                    break;
            }
        }

        static void LoadBlock()
        {
            board.SetCell(10, 10, true);
            board.SetCell(11, 10, true);
            board.SetCell(10, 11, true);
            board.SetCell(11, 11, true);
        }

        static void LoadGlider()
        {
            board.SetCell(5, 5, true);
            board.SetCell(6, 6, true);
            board.SetCell(4, 7, true);
            board.SetCell(5, 7, true);
            board.SetCell(6, 7, true);
        }

        static void LoadBlinker()
        {
            board.SetCell(10, 10, true);
            board.SetCell(10, 11, true);
            board.SetCell(10, 12, true);
        }

        static void LoadGosperGliderGun()
        {
            // Упрощенная версия ружья Госпера
            int[,] gun = {
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,1,1},
                {0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,1,1},
                {1,1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
                {1,1,0,0,0,0,0,0,0,0,1,0,0,0,1,0,1,1,0,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}
            };
            
            for (int i = 0; i < gun.GetLength(0) && i < settings.Height; i++)
            {
                for (int j = 0; j < gun.GetLength(1) && j < settings.Width; j++)
                {
                    if (gun[i, j] == 1)
                        board.SetCell(j + 5, i + 5, true);
                }
            }
        }

        static void RunSimulation()
        {
            board.Display();
            int stableCount = 0;
            int previousLiveCount = board.CountLiveCells();
            
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape) break;
                    if (key == ConsoleKey.Space)
                    {
                        board.NextGeneration();
                        board.Display();
                    }
                }
                
                board.NextGeneration();
                board.Display();
                
                int currentLiveCount = board.CountLiveCells();
                if (currentLiveCount == previousLiveCount)
                {
                    stableCount++;
                    if (stableCount > 5)
                    {
                        Console.WriteLine("Достигнуто стабильное состояние!");
                        break;
                    }
                }
                else
                {
                    stableCount = 0;
                    previousLiveCount = currentLiveCount;
                }
                
                if (board.Generation >= settings.MaxGenerations) break;
                System.Threading.Thread.Sleep(settings.DelayMs);
            }
            
            Console.WriteLine("Симуляция завершена. Нажмите любую клавишу...");
            Console.ReadKey();
        }

        static void RunStabilityResearch()
        {
            var results = new List<(double density, int generationsToStable)>();
            
            Console.WriteLine("Исследование стабильности...");
            
            for (double density = 0.1; density <= 0.9; density += 0.1)
            {
                int totalGenerations = 0;
                int attempts = 10;
                
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    board = new Board(50, 50);
                    board.RandomFill(density);
                    
                    int stableCount = 0;
                    int previousLiveCount = board.CountLiveCells();
                    int generation = 0;
                    
                    while (generation < 500)
                    {
                        board.NextGeneration();
                        generation++;
                        
                        int currentLiveCount = board.CountLiveCells();
                        if (currentLiveCount == previousLiveCount)
                        {
                            stableCount++;
                            if (stableCount > 10)
                            {
                                break;
                            }
                        }
                        else
                        {
                            stableCount = 0;
                            previousLiveCount = currentLiveCount;
                        }
                    }
                    
                    totalGenerations += generation;
                }
                
                int avgGenerations = totalGenerations / attempts;
                results.Add((density, avgGenerations));
                Console.WriteLine($"Плотность: {density:F1}, Среднее время до стабилизации: {avgGenerations}");
            }
            
            SaveResults(results);
            GeneratePlot(results);
        }

        static void RunWithParameters()
        {
            Console.Write("Введите количество поколений для анализа: ");
            int maxGen = int.Parse(Console.ReadLine());
            
            board.RandomFill(0.3);
            history.Clear();
            
            for (int gen = 0; gen < maxGen; gen++)
            {
                history.Add((board.Generation, board.CountLiveCells()));
                board.NextGeneration();
                
                if (gen % 10 == 0)
                {
                    Console.WriteLine($"Поколение {gen}: {board.CountLiveCells()} живых клеток");
                }
            }
            
            AnalyzeCombinations();
        }

        static void AnalyzeCombinations()
        {
            var combinations = board.FindCombinations();
            Console.WriteLine($"\nНайдено комбинаций: {combinations.Count}");
            
            var classification = new Dictionary<string, int>();
            
            foreach (var combo in combinations)
            {
                string type = board.ClassifyCombination(combo);
                if (classification.ContainsKey(type))
                    classification[type]++;
                else
                    classification[type] = 1;
            }
            
            Console.WriteLine("\nКлассификация фигур:");
            foreach (var kvp in classification)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }

        static void SaveResults(List<(double density, int generationsToStable)> results)
        {
            Directory.CreateDirectory("Data");
            using (StreamWriter writer = new StreamWriter("Data/data.txt"))
            {
                writer.WriteLine("Плотность\tПоколения_до_стабилизации");
                foreach (var result in results)
                {
                    writer.WriteLine($"{result.density:F1}\t{result.generationsToStable}");
                }
            }
        }

        static void GeneratePlot(List<(double density, int generationsToStable)> results)
        {
            // Создаем простой график в консоли
            Console.WriteLine("\nГрафик зависимости стабильности от плотности:");
            Console.WriteLine("Плотность | Поколения");
            Console.WriteLine("----------+----------");
            
            foreach (var result in results)
            {
                int barLength = Math.Min(50, result.generationsToStable / 10);
                string bar = new string('#', barLength);
                Console.WriteLine($"{result.density:F1}        | {bar} ({result.generationsToStable})");
            }
        }
    }
}
