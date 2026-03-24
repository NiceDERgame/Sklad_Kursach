using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Sklad_Kursach.Services
{
    public class ReceiptInvoiceData
    {
        public int ReceiptId { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public string SupplierName { get; set; }
        public string WarehouseName { get; set; }
        public string AcceptedBy { get; set; }
        public List<ReceiptInvoiceItem> Items { get; set; } = new List<ReceiptInvoiceItem>();
    }

    public class ReceiptInvoiceItem
    {
        public int RowNumber { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public int ShelfLifeHours { get; set; }

        public decimal Sum => Quantity * Price;
    }

    public static class ReceiptInvoiceService
    {
        public static string CreateInvoiceDocx(ReceiptInvoiceData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Items == null || data.Items.Count == 0)
                throw new InvalidOperationException("Нельзя создать накладную без товаров.");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string folder = Path.Combine(
                baseDir,
                "Documents",
                "Receipts",
                data.DocumentDate.ToString("yyyy"),
                data.DocumentDate.ToString("MM"));

            Directory.CreateDirectory(folder);

            string safeNumber = MakeSafeFileName(data.DocumentNumber);
            string fileName = $"Receipt_{safeNumber}_{data.DocumentDate:yyyy-MM-dd_HH-mm-ss}.docx";
            string fullPath = Path.Combine(folder, fileName);

            using (WordprocessingDocument wordDocument =
                WordprocessingDocument.Create(fullPath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();

                body.Append(CreateParagraph(
                    "ПРИЁМНАЯ НАКЛАДНАЯ",
                    true,
                    "28",
                    JustificationValues.Center));

                body.Append(CreateParagraph($"Номер документа: {data.DocumentNumber}"));
                body.Append(CreateParagraph($"Дата: {data.DocumentDate:dd.MM.yyyy HH:mm}"));
                body.Append(CreateParagraph($"Поставщик: {data.SupplierName}"));
                body.Append(CreateParagraph($"Склад: {data.WarehouseName}"));
                body.Append(CreateParagraph($"Ответственный за приёмку: {data.AcceptedBy}"));
                body.Append(CreateEmptyParagraph());

                Table table = new Table();

                TableProperties tableProperties = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 8 },
                        new BottomBorder { Val = BorderValues.Single, Size = 8 },
                        new LeftBorder { Val = BorderValues.Single, Size = 8 },
                        new RightBorder { Val = BorderValues.Single, Size = 8 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 8 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 8 }
                    )
                );

                table.AppendChild(tableProperties);

                table.Append(
                    CreateTableRow(
                        true,
                        "№",
                        "Наименование",
                        "Категория",
                        "Кол-во",
                        "Цена",
                        "Сумма",
                        "Срок хранения (ч)"
                    )
                );

                decimal total = 0m;

                foreach (var item in data.Items)
                {
                    table.Append(
                        CreateTableRow(
                            false,
                            item.RowNumber.ToString(),
                            item.ProductName ?? "",
                            item.CategoryName ?? "",
                            item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
                            item.Price.ToString("0.00", CultureInfo.InvariantCulture),
                            item.Sum.ToString("0.00", CultureInfo.InvariantCulture),
                            item.ShelfLifeHours.ToString()
                        )
                    );

                    total += item.Sum;
                }

                body.Append(table);
                body.Append(CreateEmptyParagraph());
                body.Append(CreateParagraph(
                    $"Итого: {total.ToString("0.00", CultureInfo.InvariantCulture)} руб.",
                    true,
                    "24",
                    JustificationValues.Right));
                body.Append(CreateEmptyParagraph());
                body.Append(CreateParagraph("Документ сформирован автоматически."));
                body.Append(CreateParagraph($"Ответственный: {data.AcceptedBy}"));
                body.Append(CreateParagraph("Подпись: ________________________"));

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return fullPath;
        }

        private static Paragraph CreateParagraph(
    string text,
    bool bold = false,
    string fontSize = "22",
    JustificationValues? justification = null)
        {
            JustificationValues actualJustification = justification ?? JustificationValues.Left;

            RunProperties runProperties = new RunProperties();
            if (bold)
                runProperties.Append(new Bold());

            runProperties.Append(new FontSize { Val = fontSize });

            Run run = new Run();
            run.Append(runProperties);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            ParagraphProperties paragraphProperties = new ParagraphProperties(
                new Justification { Val = actualJustification },
                new SpacingBetweenLines { After = "120" }
            );

            Paragraph paragraph = new Paragraph();
            paragraph.Append(paragraphProperties);
            paragraph.Append(run);

            return paragraph;
        }

        private static Paragraph CreateEmptyParagraph()
        {
            return new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { After = "120" }
                ),
                new Run(new Text(""))
            );
        }

        private static TableRow CreateTableRow(bool bold, params string[] values)
        {
            TableRow row = new TableRow();

            foreach (string value in values)
            {
                TableCell cell = new TableCell();

                Paragraph paragraph = CreateParagraph(value ?? "", bold, "22", JustificationValues.Center);

                cell.Append(paragraph);
                cell.Append(new TableCellProperties(
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
                ));

                row.Append(cell);
            }

            return row;
        }

        private static string MakeSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "NoNumber";

            foreach (char c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');

            return fileName.Replace(" ", "_");
        }
    }
}