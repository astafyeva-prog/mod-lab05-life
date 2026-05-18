using Xunit;
using Life;

namespace Life.Tests
{
    public class BoardTests
    {
        [Fact]
        public void Constructor_CreatesEmptyBoard()
        {
            var board = new Board(10, 10);
            Assert.Equal(0, board.CountLiveCells());
            Assert.Equal(0, board.Generation);
        }

        [Fact]
        public void SetCell_SetsCellCorrectly()
        {
            var board = new Board(10, 10);
            board.SetCell(5, 5, true);
            Assert.True(board.GetCell(5, 5));
        }

        [Fact]
        public void CountLiveCells_ReturnsCorrectCount()
        {
            var board = new Board(10, 10);
            board.SetCell(1, 1, true);
            board.SetCell(2, 2, true);
            board.SetCell(3, 3, true);
            Assert.Equal(3, board.CountLiveCells());
        }

        [Fact]
        public void NextGeneration_BlockRemainsStable()
        {
            var board = new Board(10, 10);
            board.SetCell(4, 4, true);
            board.SetCell(5, 4, true);
            board.SetCell(4, 5, true);
            board.SetCell(5, 5, true);
            
            int initialCount = board.CountLiveCells();
            board.NextGeneration();
            
            Assert.Equal(initialCount, board.CountLiveCells());
            Assert.True(board.GetCell(4, 4));
            Assert.True(board.GetCell(5, 4));
            Assert.True(board.GetCell(4, 5));
            Assert.True(board.GetCell(5, 5));
        }

        [Fact]
        public void NextGeneration_BlinkerPeriodic()
        {
            var board = new Board(10, 10);
            board.SetCell(5, 4, true);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            
            board.NextGeneration();
            Assert.True(board.GetCell(4, 5));
            Assert.True(board.GetCell(5, 5));
            Assert.True(board.GetCell(6, 5));
            
            board.NextGeneration();
            Assert.True(board.GetCell(5, 4));
            Assert.True(board.GetCell(5, 5));
            Assert.True(board.GetCell(5, 6));
        }

        [Fact]
        public void NextGeneration_LonelyCellDies()
        {
            var board = new Board(10, 10);
            board.SetCell(5, 5, true);
            board.NextGeneration();
            Assert.False(board.GetCell(5, 5));
        }

        [Fact]
        public void NextGeneration_CellWithTwoNeighborsSurvives()
        {
            var board = new Board(10, 10);
            board.SetCell(5, 4, true);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            
            board.NextGeneration();
            Assert.True(board.GetCell(5, 5));
        }

        [Fact]
        public void NextGeneration_CellWithFourNeighborsDies()
        {
            var board = new Board(10, 10);
            board.SetCell(5, 4, true);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            board.SetCell(4, 5, true);
            board.SetCell(6, 5, true);
            
            board.NextGeneration();
            Assert.False(board.GetCell(5, 5));
        }

        [Fact]
        public void NextGeneration_DeadCellWithThreeNeighborsBorn()
        {
            var board = new Board(10, 10);
            board.SetCell(4, 4, true);
            board.SetCell(5, 4, true);
            board.SetCell(4, 5, true);
            
            board.NextGeneration();
            Assert.True(board.GetCell(5, 5));
        }

        [Fact]
        public void RandomFill_FillsWithCorrectDensity()
        {
            var board = new Board(100, 100);
            board.RandomFill(0.3);
            int liveCount = board.CountLiveCells();
            double actualDensity = liveCount / 10000.0;
            
            Assert.InRange(actualDensity, 0.25, 0.35);
        }

        [Fact]
        public void FindCombinations_DetectsSeparateGroups()
        {
            var board = new Board(10, 10);
            board.SetCell(1, 1, true);
            board.SetCell(1, 2, true);
            board.SetCell(5, 5, true);
            
            var combinations = board.FindCombinations();
            Assert.Equal(2, combinations.Count);
        }

        [Fact]
        public void FindCombinations_BlockAsSingleCombination()
        {
            var board = new Board(10, 10);
            board.SetCell(1, 1, true);
            board.SetCell(2, 1, true);
            board.SetCell(1, 2, true);
            board.SetCell(2, 2, true);
            
            var combinations = board.FindCombinations();
            Assert.Single(combinations);
            Assert.Equal(4, combinations[0].Count);
        }

        [Fact]
        public void ClassifyCombination_IdentifiesBlock()
        {
            var board = new Board(10, 10);
            board.SetCell(1, 1, true);
            board.SetCell(2, 1, true);
            board.SetCell(1, 2, true);
            board.SetCell(2, 2, true);
            
            var combinations = board.FindCombinations();
            string classification = board.ClassifyCombination(combinations[0]);
            Assert.Contains("Block", classification);
        }

        [Fact]
        public void ClassifyCombination_IdentifiesBlinker()
        {
            var board = new Board(10, 10);
            board.SetCell(5, 4, true);
            board.SetCell(5, 5, true);
            board.SetCell(5, 6, true);
            
            var combinations = board.FindCombinations();
            string classification = board.ClassifyCombination(combinations[0]);
            Assert.Contains("Blinker", classification);
        }

        [Fact]
        public void SaveAndLoad_PreservesBoardState()
        {
            var board = new Board(10, 10);
            board.SetCell(1, 1, true);
            board.SetCell(2, 2, true);
            board.SetCell(3, 3, true);
            board.NextGeneration();
            
            string testFile = "test_save.json";
            board.SaveToFile(testFile);
            
            var newBoard = new Board(10, 10);
            newBoard.LoadFromFile(testFile);
            
            Assert.Equal(board.Width, newBoard.Width);
            Assert.Equal(board.Height, newBoard.Height);
            Assert.Equal(board.Generation, newBoard.Generation);
            Assert.Equal(board.CountLiveCells(), newBoard.CountLiveCells());
            
            if (File.Exists(testFile))
                File.Delete(testFile);
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            var board = new Board(10, 10);
            board.SetCell(1, 1, true);
            board.SetCell(2, 2, true);
            
            var clone = board.Clone();
            clone.SetCell(1, 1, false);
            
            Assert.NotEqual(board.GetCell(1, 1), clone.GetCell(1, 1));
        }

        [Fact]
        public void GetCell_OutOfBounds_ReturnsFalse()
        {
            var board = new Board(10, 10);
            Assert.False(board.GetCell(-1, -1));
            Assert.False(board.GetCell(10, 10));
        }
    }
}
