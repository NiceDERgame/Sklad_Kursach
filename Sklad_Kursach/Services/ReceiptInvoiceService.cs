using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Sklad_Kursach.Services
{
    public class ReceiptInvoiceItem
    {
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int ShelfLifeHours { get; set; }

        public decimal Sum
        {
            get { return Quantity * Price; }
        }
    }

    public class ReceiptInvoiceData
    {
        public string DocumentNumber { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string SupplierName { get; set; }
        public string EmployeeName { get; set; }
        public decimal TotalSum { get; set; }
        public List<ReceiptInvoiceItem> Items { get; set; } = new List<ReceiptInvoiceItem>();
    }

    public static class ReceiptInvoiceService
    {
        public static string CreateInvoiceDocx(ReceiptInvoiceData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrWhiteSpace(data.DocumentNumber))
                throw new InvalidOperationException("Номер документа не задан.");

            if (data.Items == null || data.Items.Count == 0)
                throw new InvalidOperationException("Список товаров пуст.");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string invoicesDir = Path.Combine(baseDir, "Invoices");

            if (!Directory.Exists(invoicesDir))
                Directory.CreateDirectory(invoicesDir);

            string safeNumber = MakeSafeFileName(data.DocumentNumber);
            string fileName = $"Накладная_{safeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            string fullPath = Path.Combine(invoicesDir, fileName);

            using (WordprocessingDocument doc = WordprocessingDocument.Create(fullPath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();

                body.Append(CreateParagraph("ПРИХОДНАЯ НАКЛАДНАЯ", true, JustificationValues.Center, "32"));
                body.Append(CreateParagraph("№ " + data.DocumentNumber, false, JustificationValues.Center, "28"));
                body.Append(CreateParagraph("Дата: " + data.ReceiptDate.ToString("dd.MM.yyyy HH:mm"), false, JustificationValues.Left, "24"));
                body.Append(CreateParagraph("Поставщик: " + data.SupplierName, false, JustificationValues.Left, "24"));
                body.Append(CreateParagraph("Принял: " + data.EmployeeName, false, JustificationValues.Left, "24"));
                body.Append(CreateParagraph(" ", false, JustificationValues.Left, "24"));

                Table table = new Table();

                TableProperties props = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 8 },
                        new BottomBorder { Val = BorderValues.Single, Size = 8 },
                        new LeftBorder { Val = BorderValues.Single, Size = 8 },
                        new RightBorder { Val = BorderValues.Single, Size = 8 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 8 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 8 }
                    )
                );
                table.AppendChild(props);

                table.Append(CreateRow("Наименование", "Категория", "Кол-во", "Цена", "Сумма", "Срок (ч)"));

                foreach (ReceiptInvoiceItem item in data.Items)
                {
                    table.Append(CreateRow(
                        item.ProductName,
                        item.CategoryName,
                        item.Quantity.ToString(),
                        item.Price.ToString("0.00"),
                        item.Sum.ToString("0.00"),
                        item.ShelfLifeHours.ToString()
                    ));
                }

                body.Append(table);
                body.Append(CreateParagraph(" ", false, JustificationValues.Left, "24"));
                body.Append(CreateParagraph("Итого: " + data.TotalSum.ToString("0.00") + " руб.", true, JustificationValues.Right, "26"));
                body.Append(CreateParagraph(" ", false, JustificationValues.Left, "24"));
                body.Append(CreateParagraph("Подпись ответственного лица: ____________________", false, JustificationValues.Left, "24"));

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return fullPath;
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value;
        }

        private static Paragraph CreateParagraph(string text, bool bold, JustificationValues align, string fontSize)
        {
            RunProperties runProps = new RunProperties();
            if (bold)
                runProps.Append(new Bold());
            runProps.Append(new FontSize { Val = fontSize });

            Run run = new Run();
            run.Append(runProps);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            ParagraphProperties pProps = new ParagraphProperties(
                new Justification { Val = align }
            );

            Paragraph p = new Paragraph();
            p.Append(pProps);
            p.Append(run);

            return p;
        }

        private static TableRow CreateRow(params string[] values)
        {
            TableRow row = new TableRow();

            foreach (string value in values)
            {
                TableCell cell = new TableCell(
                    new Paragraph(
                        new Run(
                            new Text(value ?? string.Empty)
                        )
                    )
                );

                row.Append(cell);
            }

            return row;
        }
    }
}