using System.Drawing;
using System.Drawing.Drawing2D;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using Image = System.Drawing.Image;

namespace NavalBattle
{
    public class Field
    {
        List<List<int>> field = [];
        Dictionary<char, int> letterToIndex = new Dictionary<char, int>
        {
            {'А', 0}, {'Б', 1}, {'В', 2}, {'Г', 3}, {'Д', 4}, {'Е', 5},
            {'Ж', 6}, {'З', 7}, {'И', 8}, {'К', 9}, {'Л', 10}, {'М', 11},
            {'Н', 12}, {'О', 13}, {'П', 14}, {'Р', 15}, {'С', 16}, {'Т', 17},
            {'У', 18}, {'Ф', 19}, {'Х', 20}, {'Ц', 21}, {'Ч', 22}, {'Ш', 23},
            {'Щ', 24}, {'Ъ', 25}, {'Ы', 26}, {'Ь', 27}, {'Э', 28}, {'Ю', 29},
            {'Я', 30}
        };

        List<int> Ships = [4, 3, 2, 1];
        //List<int> Ships = [0, 0, 1, 0];
        public int AliveShips = 20;

        public bool Ready { get { return Ships.Sum() == 0; } }

        public Field() 
        {
            for (int i = 0; i < 10; i++)
            {
                field.Add([]);
                for (int j = 0; j < 10; j++)
                {
                    field[i].Add(0);
                }
            }
        }

        public bool AddShip(string x, int y, int type, string side)
        {
            if (string.IsNullOrEmpty(x) || x.Length != 1 || !letterToIndex.ContainsKey(x[0]))
                return false;

            if (Ships[type - 1] <= 0)
                return false;

            if (y < 1 || y > 10)
                return false;

            if (type < 1 || type > 4)
                return false;

            if (side != "h" && side != "w")
                return false;

            int xIndex = letterToIndex[x[0]];
            int yIndex = y - 1;

            if (side == "h")
            {
                if (yIndex + type > 10)
                    return false;
            }
            else 
            {
                if (xIndex + type > 10)
                    return false;
            }

            List<(int x, int y)> shipCoordinates = new List<(int, int)>();

            for (int i = 0; i < type; i++)
            {
                int currentX = xIndex;
                int currentY = yIndex;

                if (side == "h")
                    currentY = yIndex + i;
                else
                    currentX = xIndex + i;

                shipCoordinates.Add((currentX, currentY));
            }

            foreach (var coord in shipCoordinates)
            {
                if (field[coord.y][coord.x] != 0)
                    return false;
            }

            foreach (var coord in shipCoordinates)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int checkX = coord.x + dx;
                        int checkY = coord.y + dy;

                        if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                        {
                            int cellValue = field[checkY][checkX];
                            if (cellValue == 2 || cellValue == 3)
                                return false;
                        }
                    }
                }
            }

            foreach (var coord in shipCoordinates)
            {
                field[coord.y][coord.x] = 2;
            }

            foreach (var coord in shipCoordinates)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int checkX = coord.x + dx;
                        int checkY = coord.y + dy;

                        if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                        {
                            if (field[checkY][checkX] == 0)
                                field[checkY][checkX] = 4;
                        }
                    }
                }
            }

            Ships[type - 1]--;

            return true;
        }

        public Image DrawField()
        {
            int cellSize = 50;
            int offset = 50;
            int fieldSize = 10 * cellSize;
            int imageSize = fieldSize + offset;

            Bitmap bitmap = new Bitmap(imageSize, imageSize);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                g.Clear(System.Drawing.Color.White);

                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        int cellX = offset + x * cellSize;
                        int cellY = offset + y * cellSize;
                        Rectangle cellRect = new Rectangle(cellX, cellY, cellSize, cellSize);

                        System.Drawing.Color cellColor;
                        switch (field[y][x])
                        {
                            case 0:
                                cellColor = System.Drawing.Color.White;
                                break;
                            case 1:
                                cellColor = System.Drawing.Color.White;
                                break;
                            case 2: 
                                cellColor = System.Drawing.Color.Blue;
                                break;
                            case 3: 
                                cellColor = System.Drawing.Color.Black;
                                break;
                            case 4: 
                                cellColor = System.Drawing.Color.FromArgb(173, 216, 230);
                                break;
                            default:
                                cellColor = System.Drawing.Color.White;
                                break;
                        }

                        using (SolidBrush brush = new SolidBrush(cellColor))
                        {
                            g.FillRectangle(brush, cellRect);
                        }

                        
                        if (field[y][x] == 1)
                        {
                            int centerX = cellX + cellSize / 2;
                            int centerY = cellY + cellSize / 2;
                            int radius = cellSize / 4;

                            using (SolidBrush brush = new SolidBrush(System.Drawing.Color.Black))
                            {
                                g.FillEllipse(brush, centerX - radius, centerY - radius, radius * 2, radius * 2);
                            }
                        }
                    }
                }

                using (Pen gridPen = new Pen(System.Drawing.Color.Blue, 2))
                {
                    for (int i = 0; i <= 10; i++)
                    {
                        int x = offset + i * cellSize;
                        g.DrawLine(gridPen, x, offset, x, offset + fieldSize);
                    }

                    for (int i = 0; i <= 10; i++)
                    {
                        int y = offset + i * cellSize;
                        g.DrawLine(gridPen, offset, y, offset + fieldSize, y);
                    }
                }

                using (System.Drawing.Font font = new System.Drawing.Font("Arial", 16, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(System.Drawing.Color.Blue))
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;

                    string[] letters = { "А", "Б", "В", "Г", "Д", "Е", "Ж", "З", "И", "К" };
                    for (int i = 0; i < 10; i++)
                    {
                        int x = offset + i * cellSize + cellSize / 2;
                        int y = offset / 2;
                        Rectangle textRect = new Rectangle(x - cellSize / 2, y - cellSize / 4, cellSize, cellSize / 2);
                        g.DrawString(letters[i], font, textBrush, textRect, format);
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        int x = offset / 2;
                        int y = offset + i * cellSize + cellSize / 2;
                        Rectangle textRect = new Rectangle(x - cellSize / 2, y - cellSize / 4, cellSize, cellSize / 2);
                        g.DrawString((i + 1).ToString(), font, textBrush, textRect, format);
                    }
                }
            }

            return bitmap;
        }

        public bool Shoot(string x, int y, out bool hit)
        {
            hit = false;
            if (string.IsNullOrEmpty(x) || x.Length != 1 || !letterToIndex.ContainsKey(x[0]))
                return false;

            if (y < 1 || y > 10)
                return false;

            int xIndex = letterToIndex[x[0]];
            int yIndex = y - 1;

            if (xIndex < 0 || xIndex >= 10 || yIndex < 0 || yIndex >= 10)
                return false;

            int cellValue = field[yIndex][xIndex];

            if (cellValue == 1 || cellValue == 3)
                return false;

            if (cellValue == 2)
            {
                field[yIndex][xIndex] = 3;
                AliveShips--;

                CheckIfShipSunk(xIndex, yIndex);
                hit = true;
            }
            else if (cellValue == 0 || cellValue == 4)
            {
                field[yIndex][xIndex] = 1;
            }

            return true;
        }

        private void CheckIfShipSunk(int x, int y)
        {
            var shipCells = new List<(int x, int y)>();

            void SearchShip(int startX, int startY)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    int checkX = startX + dx;
                    if (checkX >= 0 && checkX < 10)
                    {
                        int val = field[startY][checkX];
                        if (val == 2 || val == 3)
                        {
                            if (!shipCells.Contains((checkX, startY)))
                                shipCells.Add((checkX, startY));
                        }
                        else if (val != 2 && val != 3 && dx > 0)
                            break;
                    }
                }

                for (int dy = -3; dy <= 3; dy++)
                {
                    int checkY = startY + dy;
                    if (checkY >= 0 && checkY < 10)
                    {
                        int val = field[checkY][startX];
                        if (val == 2 || val == 3)
                        {
                            if (!shipCells.Contains((startX, checkY)))
                                shipCells.Add((startX, checkY));
                        }
                        else if (val != 2 && val != 3 && dy > 0)
                            break;
                    }
                }
            }

            SearchShip(x, y);

            bool allSunk = true;
            foreach (var cell in shipCells)
            {
                if (field[cell.y][cell.x] != 3)
                {
                    allSunk = false;
                    break;
                }
            }

            if (allSunk && shipCells.Count > 0)
            {
                foreach (var cell in shipCells)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int checkX = cell.x + dx;
                            int checkY = cell.y + dy;

                            if (checkX >= 0 && checkX < 10 && checkY >= 0 && checkY < 10)
                            {
                                if (field[checkY][checkX] == 0 || field[checkY][checkX] == 4)
                                {
                                    field[checkY][checkX] = 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static Image DrawTwoFields(Field field1, Field field2)
        {
            int cellSize = 25;
            int offset = 40;
            int fieldSize = 10 * cellSize;
            int spacing = 30;
            int imageWidth = offset + fieldSize + spacing + fieldSize + offset;
            int imageHeight = offset + fieldSize + offset;

            Bitmap bitmap = new Bitmap(imageWidth, imageHeight);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                g.Clear(Color.White);

                DrawSingleField(g, field1.field, 0, cellSize, offset);

                DrawSingleField(g, field2.field, offset + fieldSize + spacing, cellSize, offset, true);
            }

            return bitmap;
        }

        private static void DrawSingleField(Graphics g, List<List<int>> fieldData, int startX, int cellSize, int offset, bool hidden = false)
        {
            int fieldSize = 10 * cellSize;

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int cellX = startX + offset + x * cellSize;
                    int cellY = offset + y * cellSize;
                    Rectangle cellRect = new Rectangle(cellX, cellY, cellSize, cellSize);

                    Color cellColor;
                    int cellValue = fieldData[y][x];

                    if (hidden)
                    {
                        // Для скрытого поля (противника)
                        switch (cellValue)
                        {
                            case 0: // Пустая клетка
                            case 2: // Корабль (не показываем)
                            case 4: // Запретная зона (не показываем)
                                cellColor = Color.White;
                                break;
                            case 1: // Пустая клетка, в которую стреляли (промах)
                                cellColor = Color.White;
                                break;
                            case 3: // Потопленный корабль - показываем как чёрный крестик или клетку с крестом
                                cellColor = Color.White; // Делаем фон белым, а крест нарисуем позже
                                break;
                            default:
                                cellColor = Color.White;
                                break;
                        }
                    }
                    else
                    {
                        // Для видимого поля (своего)
                        switch (cellValue)
                        {
                            case 0:
                                cellColor = Color.White;
                                break;
                            case 1:
                                cellColor = Color.White;
                                break;
                            case 2:
                                cellColor = Color.Blue;
                                break;
                            case 3:
                                cellColor = Color.Black;
                                break;
                            case 4:
                                cellColor = Color.FromArgb(173, 216, 230);
                                break;
                            default:
                                cellColor = Color.White;
                                break;
                        }
                    }

                    using (SolidBrush brush = new SolidBrush(cellColor))
                    {
                        g.FillRectangle(brush, cellRect);
                    }

                    // Рисуем результаты выстрелов
                    if (cellValue == 1) // Промах
                    {
                        int centerX = cellX + cellSize / 2;
                        int centerY = cellY + cellSize / 2;
                        int radius = cellSize / 4;

                        using (SolidBrush brush = new SolidBrush(Color.Black))
                        {
                            g.FillEllipse(brush, centerX - radius, centerY - radius, radius * 2, radius * 2);
                        }
                    }
                    else if (hidden && cellValue == 3) // Потопленный корабль на поле противника
                    {
                        // Рисуем чёрный крест (X) в клетке
                        using (Pen pen = new Pen(Color.Black, 2))
                        {
                            // Линия от верхнего левого до нижнего правого угла
                            g.DrawLine(pen, cellX + 2, cellY + 2, cellX + cellSize - 2, cellY + cellSize - 2);
                            // Линия от верхнего правого до нижнего левого угла
                            g.DrawLine(pen, cellX + cellSize - 2, cellY + 2, cellX + 2, cellY + cellSize - 2);
                        }
                    }
                }
            }

            using (Pen gridPen = new Pen(Color.Blue, 1))
            {
                for (int i = 0; i <= 10; i++)
                {
                    int x = startX + offset + i * cellSize;
                    g.DrawLine(gridPen, x, offset, x, offset + fieldSize);
                }

                for (int i = 0; i <= 10; i++)
                {
                    int y = offset + i * cellSize;
                    g.DrawLine(gridPen, startX + offset, y, startX + offset + fieldSize, y);
                }
            }

            using (Font font = new Font("Arial", 10, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.Blue))
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;

                string[] letters = { "А", "Б", "В", "Г", "Д", "Е", "Ж", "З", "И", "К" };
                for (int i = 0; i < 10; i++)
                {
                    int x = startX + offset + i * cellSize + cellSize / 2;
                    int y = offset / 2;
                    Rectangle textRect = new Rectangle(x - cellSize / 2, y - cellSize / 4, cellSize, cellSize / 2);
                    g.DrawString(letters[i], font, textBrush, textRect, format);
                }

                for (int i = 0; i < 10; i++)
                {
                    int x = startX + offset / 2;
                    int y = offset + i * cellSize + cellSize / 2;
                    Rectangle textRect = new Rectangle(x - cellSize / 2, y - cellSize / 4, cellSize, cellSize / 2);
                    g.DrawString((i + 1).ToString(), font, textBrush, textRect, format);
                }
            }
        }
    }
}
