namespace WhosListening
{
    public static class CardGridTests
    {
        private static void Check(GridLayout g, int columns, int rows, int visibleRows, bool scroll, string name)
        {
            Assert.Equal(columns, g.Columns, name + " columnas");
            Assert.Equal(rows, g.Rows, name + " filas");
            Assert.Equal(visibleRows, g.VisibleRows, name + " filas visibles");
            Assert.Equal(scroll, g.Scroll, name + " scroll");
        }

        public static void TestZeroAccountsIsOneByOne()
        {
            Check(CardGrid.Compute(0), 1, 1, 1, false, "0");
        }

        public static void TestThreeAccountsFillOneRow()
        {
            Check(CardGrid.Compute(3), 4, 1, 1, false, "3");
        }

        public static void TestFourAccountsWrapToTwoRows()
        {
            Check(CardGrid.Compute(4), 4, 2, 2, false, "4");
        }

        public static void TestTwelveAccountsScroll()
        {
            Check(CardGrid.Compute(12), 4, 4, 3, true, "12");
        }

        public static void TestTwoAccountsThreeColumns()
        {
            Check(CardGrid.Compute(2), 3, 1, 1, false, "2");
        }

        public static void TestElevenAccountsNoScroll()
        {
            Check(CardGrid.Compute(11), 4, 3, 3, false, "11");
        }

        public static void TestNarrowScreenLimitsColumns()
        {
            Check(CardGrid.Compute(10, 2), 2, 6, 3, true, "10 en 2 col");
        }

        public static void TestMaxColumnsIsClamped()
        {
            Check(CardGrid.Compute(5, 0), 1, 6, 3, true, "5 en 0 col");
            Check(CardGrid.Compute(5, 9), 4, 2, 2, false, "5 en 9 col");
        }

        public static void TestInitialsOneWordIsOneLetter()
        {
            Assert.Equal("L", AccountCard.Initials("luna"), "");
        }

        public static void TestInitialsTwoWords()
        {
            Assert.Equal("OD", AccountCard.Initials("olga  diaz perez"), "");
        }

        public static void TestInitialsEmpty()
        {
            Assert.Equal("?", AccountCard.Initials("   "), "");
            Assert.Equal("?", AccountCard.Initials(null), "");
        }

        public static void TestPaletteIndexIsStableSumOfChars()
        {
            // 'a' = 97, 97 mod 8 = 1
            Assert.Equal(1, AccountCard.PaletteIndex("a"), "");
            // 'a' + 'b' = 195, 195 mod 8 = 3
            Assert.Equal(3, AccountCard.PaletteIndex("ab"), "");
            Assert.Equal(0, AccountCard.PaletteIndex(null), "");
            Assert.Equal(8, AccountCard.Palette.Length, "");
        }
    }
}
