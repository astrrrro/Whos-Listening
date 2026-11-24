using System;

namespace WhosListening
{
    public class GridLayout
    {
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int VisibleRows { get; set; }
        public bool Scroll { get; set; }
    }

    // Calcula la grilla de tarjetas: las cuentas mas la tarjeta "Agregar".
    public static class CardGrid
    {
        public const int MaxColumns = 4;
        public const int MaxVisibleRows = 3;

        public static GridLayout Compute(int accountCount)
        {
            return Compute(accountCount, MaxColumns);
        }

        // maxColumns limita las columnas cuando la pantalla es angosta (entre 1 y MaxColumns).
        public static GridLayout Compute(int accountCount, int maxColumns)
        {
            int limit = Math.Max(1, Math.Min(maxColumns, MaxColumns));
            int total = Math.Max(0, accountCount) + 1;
            int columns = Math.Min(total, limit);
            int rows = (total + columns - 1) / columns;

            GridLayout g = new GridLayout();
            g.Columns = columns;
            g.Rows = rows;
            g.VisibleRows = Math.Min(rows, MaxVisibleRows);
            g.Scroll = rows > MaxVisibleRows;
            return g;
        }
    }
}
