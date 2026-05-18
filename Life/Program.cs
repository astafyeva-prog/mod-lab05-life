using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
            var beehive = new HashSet<(int, int)> { (1,0), (2,0), (0,1), (3,1), (1,2), (2,2) };
            var loaf = new HashSet<(int, int)> { (1,0), (2,0), (0,1), (3,1), (1,2), (3,2), (2,3) };
            var boat = new HashSet<(int, int)> { (0,0), (1,0), (0,1), (2,1), (1,2) };
            var tub = new HashSet<(int, int)> { (1,0), (0,1), (2,1), (1,2) };
            var pond = new HashSet<(int, int)> { (1,0), (2,0), (0,1), (3,1), (0,2), (3,2), (1,3), (2,3) };
            var blinker = new HashSet<(int, int)> { (0,0), (1,0), (2,0) };
            var glider = new HashSet<(int, int)> { (0,1), (1,2), (2,0), (2,1), (2,2) };

            if (pattern.SetEquals(block)) return "Block (устойчивая)";
            if (pattern.SetEquals(beehive)) return "Beehive (устойчивая)";
            if (pattern.SetEquals(loaf)) return "Loaf (устойчивая)";
            if (pattern.SetEquals(boat)) return "Boat (устойчивая)";
            if (pattern.SetEquals(tub)) return "Tub (устойчивая)";
            if (pattern.SetEquals(pond)) return "Pond (устойчивая)";
            if (pattern.SetEquals(blinker)) return "Blinker (периодическая)";
            if (pattern.SetEquals(glider)) return "Glider (движущаяся)";
            
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

        public Board Clone()
        {
            var newBoard = new Board(Width, Height);
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    newBoard.SetCell(i, j, Grid[i, j].IsAlive);
                }
            }
            newBoard.Generation = Generation;
            return newBoard;
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
        public int Width { get; set; } = 80;
        public int Height { get; set; } = 40;
        public int DelayMs { get; set; } = 100;
        public bool AutoMode { get; set; } = false;
        public int MaxGenerations { get; set; } = 1000;
        public int StabilityThreshold { get; set; } = 10;
        public int ResearchFieldSize { get; set; } = 100;
        public int ResearchAttempts { get; set; } = 10;
        public int ResearchMaxGenerations { get; set; } = 500;

        public static Settings Load(string filename)
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
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

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Создание директорий
            Directory.CreateDirectory("Data");
            Directory.CreateDirectory("Presets");
            
            // Загрузка настроек
            settings = Settings.Load("settings.json");
            settings.Save("settings.json");
            
            board = new Board(settings.Width, settings.Height);
            
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Игра Жизнь Конвея ===");
                Console.WriteLine("1. Запуск симуляции");
                Console.WriteLine("2. Сохранить состояние");
                Console.WriteLine("3. Загрузить состояние");
                Console.WriteLine("4. Исследование стабильности");
                Console.WriteLine("5. Анализ фигур на поле");
                Console.WriteLine("6. Загрузить preset фигуру");
                Console.WriteLine("7. Настройки");
                Console.WriteLine("8. Выход");
                Console.Write("\nВыберите действие: ");
                
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        RunSimulation();
                        break;
                    case "2":
                        SaveGame();
                        break;
                    case "3":
                        LoadGame();
                        break;
                    case "4":
                        RunStabilityResearch();
                        break;
                    case "5":
                        AnalyzeBoard();
                        break;
                    case "6":
                        LoadPreset();
                        break;
                    case "7":
                        EditSettings();
                        break;
                    case "8":
                        return;
                }
            }
        }

        static void EditSettings()
        {
            Console.Clear();
            Console.WriteLine("=== Текущие настройки ===");
            Console.WriteLine($"1. Ширина поля: {settings.Width}");
            Console.WriteLine($"2. Высота поля: {settings.Height}");
            Console.WriteLine($"3. Задержка (мс): {settings.DelayMs}");
            Console.WriteLine($"4. Макс. поколений: {settings.MaxGenerations}");
            Console.WriteLine($"5. Порог стабильности: {settings.StabilityThreshold}");
            Console.WriteLine($"6. Размер поля для исследований: {settings.ResearchFieldSize}");
            Console.WriteLine($"7. Кол-во попыток: {settings.ResearchAttempts}");
            Console.WriteLine($"8. Макс. поколений в исследованиях: {settings.ResearchMaxGenerations}");
            Console.WriteLine("0. Назад");
            
            Console.Write("\nВыберите параметр для изменения: ");
            string choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    Console.Write("Новая ширина: ");
                    settings.Width = int.Parse(Console.ReadLine());
                    break;
                case "2":
                    Console.Write("Новая высота: ");
                    settings.Height = int.Parse(Console.ReadLine());
                    break;
                case "3":
                    Console.Write("Новая задержка (мс): ");
                    settings.DelayMs = int.Parse(Console.ReadLine());
                    break;
                case "4":
                    Console.Write("Новое макс. поколений: ");
                    settings.MaxGenerations = int.Parse(Console.ReadLine());
                    break;
                case "5":
                    Console.Write("Новый порог стабильности: ");
                    settings.StabilityThreshold = int.Parse(Console.ReadLine());
                    break;
                case "6":
                    Console.Write("Размер поля для исследований: ");
                    settings.ResearchFieldSize = int.Parse(Console.ReadLine());
                    break;
                case "7":
                    Console.Write("Кол-во попыток: ");
                    settings.ResearchAttempts = int.Parse(Console.ReadLine());
                    break;
                case "8":
                    Console.Write("Макс. поколений в исследованиях: ");
                    settings.ResearchMaxGenerations = int.Parse(Console.ReadLine());
                    break;
                case "0":
                    return;
            }
            
            settings.Save("settings.json");
            board = new Board(settings.Width, settings.Height);
            Console.WriteLine("\nНастройки сохранены. Нажмите любую клавишу...");
            Console.ReadKey();
        }

        static void SaveGame()
        {
            Console.Write("Введите имя файла для сохранения: ");
            string filename = Console.ReadLine();
            if (string.IsNullOrEmpty(filename)) filename = $"save_{DateTime.Now:yyyyMMdd_HHmmss}";
            
            board.SaveToFile($"Data/{filename}.json");
            Console.WriteLine($"Сохранено в Data/{filename}.json");
            Console.ReadKey();
        }

        static void LoadGame()
        {
            var files = Directory.GetFiles("Data", "*.json");
            if (files.Length == 0)
            {
                Console.WriteLine("Нет сохраненных файлов");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("Доступные сохранения:");
            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
            }
            
            Console.Write("Выберите файл: ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= files.Length)
            {
                board.LoadFromFile(files[choice - 1]);
                Console.WriteLine("Загружено успешно");
                Console.ReadKey();
                RunSimulation();
            }
        }

        static void LoadPreset()
        {
            Console.Clear();
            Console.WriteLine("=== Доступные фигуры ===");
            Console.WriteLine("1. Блок (устойчивая)");
            Console.WriteLine("2. Планер (движущаяся)");
            Console.WriteLine("3. Мигалка (периодическая)");
            Console.WriteLine("4. Ружье Госпера");
            Console.WriteLine("5. Пожиратель");
            Console.WriteLine("6. Паровоз");
            Console.WriteLine("7. Улей");
            Console.WriteLine("8. Лодка");
            Console.WriteLine("9. Пруд");
            Console.WriteLine("10. Корабль");
            Console.Write("\nВыберите фигуру: ");
            
            string preset = Console.ReadLine();
            
            board = new Board(settings.Width, settings.Height);
            
            switch (preset)
            {
                case "1": LoadBlock(); break;
                case "2": LoadGlider(); break;
                case "3": LoadBlinker(); break;
                case "4": LoadGosperGliderGun(); break;
                case "5": LoadEater(); break;
                case "6": LoadSpaceship(); break;
                case "7": LoadBeehive(); break;
                case "8": LoadBoat(); break;
                case "9": LoadPond(); break;
                case "10": LoadShip(); break;
                default: return;
            }
            
            Console.WriteLine("Фигура загружена. Нажмите любую клавишу для запуска...");
            Console.ReadKey();
            RunSimulation();
        }

        static void LoadBlock()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            board.SetCell(x, y, true);
            board.SetCell(x + 1, y, true);
            board.SetCell(x, y + 1, true);
            board.SetCell(x + 1, y + 1, true);
        }

        static void LoadGlider()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            board.SetCell(x, y, true);
            board.SetCell(x + 1, y + 1, true);
            board.SetCell(x - 1, y + 2, true);
            board.SetCell(x, y + 2, true);
            board.SetCell(x + 1, y + 2, true);
        }

        static void LoadBlinker()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            board.SetCell(x, y, true);
            board.SetCell(x, y + 1, true);
            board.SetCell(x, y + 2, true);
        }

        static void LoadGosperGliderGun()
        {
            // Ружье Госпера
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
            
            int startX = 10;
            int startY = 10;
            
            for (int i = 0; i < gun.GetLength(0) && startY + i < settings.Height; i++)
            {
                for (int j = 0; j < gun.GetLength(1) && startX + j < settings.Width; j++)
                {
                    if (gun[i, j] == 1)
                        board.SetCell(startX + j, startY + i, true);
                }
            }
        }

        static void LoadEater()
        {
            // Пожиратель
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            int[,] eater = {
                {1,0,0},
                {1,0,1},
                {1,1,1}
            };
            
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (eater[i, j] == 1)
                        board.SetCell(x + j, y + i, true);
        }

        static void LoadSpaceship()
        {
            // Паровоз (легкий корабль)
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            int[,] spaceship = {
                {0,1,0,0},
                {1,0,0,1},
                {1,0,0,1},
                {0,1,1,1}
            };
            
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    if (spaceship[i, j] == 1)
                        board.SetCell(x + j, y + i, true);
        }

        static void LoadBeehive()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            board.SetCell(x, y + 1, true);
            board.SetCell(x + 1, y, true);
            board.SetCell(x + 2, y, true);
            board.SetCell(x + 3, y + 1, true);
            board.SetCell(x + 1, y + 2, true);
            board.SetCell(x + 2, y + 2, true);
        }

        static void LoadBoat()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            board.SetCell(x, y, true);
            board.SetCell(x + 1, y, true);
            board.SetCell(x, y + 1, true);
            board.SetCell(x + 2, y + 1, true);
            board.SetCell(x + 1, y + 2, true);
        }

        static void LoadPond()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            board.SetCell(x + 1, y, true);
            board.SetCell(x + 2, y, true);
            board.SetCell(x, y + 1, true);
            board.SetCell(x + 3, y + 1, true);
            board.SetCell(x, y + 2, true);
            board.SetCell(x + 3, y + 2, true);
            board.SetCell(x + 1, y + 3, true);
            board.SetCell(x + 2, y + 3, true);
        }

        static void LoadShip()
        {
            int x = settings.Width / 2;
            int y = settings.Height / 2;
            int[,] ship = {
                {1,0,0},
                {1,0,1},
                {0,1,1}
            };
            
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (ship[i, j] == 1)
                        board.SetCell(x + j, y + i, true);
        }

        static void RunSimulation()
        {
            board.Display();
            int stableCount = 0;
            int previousLiveCount = board.CountLiveCells();
            bool paused = false;
            
            while (board.Generation < settings.MaxGenerations)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Escape) break;
                    if (key == ConsoleKey.Space) paused = !paused;
                    if (key == ConsoleKey.S) 
                    {
                        SaveGame();
                        Console.WriteLine("Сохранено!");
                    }
                }
                
                if (!paused)
                {
                    board.NextGeneration();
                    board.Display();
                    
                    int currentLiveCount = board.CountLiveCells();
                    if (currentLiveCount == previousLiveCount)
                    {
                        stableCount++;
                        if (stableCount > settings.StabilityThreshold)
                        {
                            Console.WriteLine("\n=== Достигнуто стабильное состояние! ===");
                            break;
                        }
                    }
                    else
                    {
                        stableCount = 0;
                        previousLiveCount = currentLiveCount;
                    }
                    
                    Thread.Sleep(settings.DelayMs);
                }
            }
            
            Console.WriteLine($"\nСимуляция завершена. Поколений: {board.Generation}");
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }

        static void RunStabilityResearch()
        {
            Console.Clear();
            Console.WriteLine("=== Исследование стабильности ===");
            
            var results = new List<(double density, int generationsToStable, double liveCellsPercent)>();
            
            for (double density = 0.05; density <= 0.95; density += 0.05)
            {
                int totalGenerations = 0;
                double totalLivePercent = 0;
                
                Console.WriteLine($"\nТестирование плотности {density:F2}...");
                
                for (int attempt = 0; attempt < settings.ResearchAttempts; attempt++)
                {
                    var researchBoard = new Board(settings.ResearchFieldSize, settings.ResearchFieldSize);
                    researchBoard.RandomFill(density);
                    
                    int stableCount = 0;
                    int previousLiveCount = researchBoard.CountLiveCells();
                    int generation = 0;
                    
                    while (generation < settings.ResearchMaxGenerations)
                    {
                        researchBoard.NextGeneration();
                        generation++;
                        
                        int currentLiveCount = researchBoard.CountLiveCells();
                        if (currentLiveCount == previousLiveCount)
                        {
                            stableCount++;
                            if (stableCount > settings.StabilityThreshold)
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
                    totalLivePercent += (double)researchBoard.CountLiveCells() / (settings.ResearchFieldSize * settings.ResearchFieldSize);
                    
                    Console.Write($".");
                }
                
                int avgGenerations = totalGenerations / settings.ResearchAttempts;
                double avgLivePercent = totalLivePercent / settings.ResearchAttempts;
                results.Add((density, avgGenerations, avgLivePercent));
                Console.WriteLine($" Готово! Среднее время: {avgGenerations}");
            }
            
            SaveResearchResults(results);
            GeneratePlot(results);
            
            Console.WriteLine("\nИсследование завершено. Результаты сохранены в Data/");
            Console.WriteLine("- data.txt (числовые данные)");
            Console.WriteLine("- plot.png (график)");
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        static void SaveResearchResults(List<(double density, int generations, double livePercent)> results)
        {
            using (StreamWriter writer = new StreamWriter("Data/data.txt"))
            {
                writer.WriteLine("# Результаты исследования стабильности игры Жизнь");
                writer.WriteLine("# Плотность\tПоколения_до_стабилизации\tПроцент_выживших_клеток");
                foreach (var result in results)
                {
                    writer.WriteLine($"{result.density:F3}\t{result.generations}\t{result.livePercent:F3}");
                }
            }
        }

        static void GeneratePlot(List<(double density, int generations, double livePercent)> results)
        {
            // Создание простого текстового графика в консоли
            Console.WriteLine("\n=== ГРАФИК ЗАВИСИМОСТИ ===");
            Console.WriteLine("Плотность | Поколения до стабилизации");
            Console.WriteLine("----------+--------------------------");
            
            int maxGenerations = results.Max(r => r.generations);
            int graphWidth = 60;
            
            foreach (var result in results)
            {
                int barLength = (int)((double)result.generations / maxGenerations * graphWidth);
                string bar = new string('█', barLength);
                Console.WriteLine($"{result.density:F2}     | {bar} {result.generations}");
            }
            
            // Создание CSV файла для построения графика в других программах
            using (StreamWriter writer = new StreamWriter("Data/plot_data.csv"))
            {
                writer.WriteLine("Density,GenerationsToStable,SurvivalRate");
                foreach (var result in results)
                {
                    writer.WriteLine($"{result.density},{result.generations},{result.livePercent}");
                }
            }
            
            Console.WriteLine($"\nМаксимальное время стабилизации: {maxGenerations} поколений");
            Console.WriteLine($"Минимальное время стабилизации: {results.Min(r => r.generations)} поколений");
        }

        static void AnalyzeBoard()
        {
            Console.Clear();
            Console.WriteLine("=== Анализ текущего поля ===");
            
            var combinations = board.FindCombinations();
            Console.WriteLine($"\nВсего живых клеток: {board.CountLiveCells()}");
            Console.WriteLine($"Найдено комбинаций (связанных групп): {combinations.Count}");
            
            var classification = new Dictionary<string, int>();
            int unknownCount = 0;
            
            foreach (var combo in combinations)
            {
                string type = board.ClassifyCombination(combo);
                if (type.Contains("Неизвестная"))
                    unknownCount++;
                else if (classification.ContainsKey(type))
                    classification[type]++;
                else
                    classification[type] = 1;
            }
            
            Console.WriteLine("\n=== Классификация фигур ===");
            if (classification.Count > 0)
            {
                foreach (var kvp in classification.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value} шт.");
                }
            }
            
            if (unknownCount > 0)
            {
                Console.WriteLine($"\nНеизвестные фигуры: {unknownCount} шт.");
                Console.WriteLine("\nСовет: Для определения неизвестных фигур изучите их форму");
            }
            
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}
