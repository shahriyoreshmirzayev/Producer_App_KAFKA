using MVCandKAFKA3.Models;
using OfficeOpenXml;

namespace MVCandKAFKA3.Services
{
    public class ExcelService
    {
        public ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // Shablon yaratish
        public byte[] CreateTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products Template");

            // Header
            worksheet.Cells[1, 1].Value = "Name";
            worksheet.Cells[1, 2].Value = "Category";
            worksheet.Cells[1, 3].Value = "Price";
            worksheet.Cells[1, 4].Value = "Description";
            worksheet.Cells[1, 5].Value = "Quantity";
            worksheet.Cells[1, 6].Value = "Manufacturer";

            // Header stil
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 129, 189));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Namuna ma'lumot
            worksheet.Cells[2, 1].Value = "Laptop Dell XPS 15";
            worksheet.Cells[2, 2].Value = "Electronics";
            worksheet.Cells[2, 3].Value = 5000000;
            worksheet.Cells[2, 4].Value = "Professional laptop for developers";
            worksheet.Cells[2, 5].Value = 10;
            worksheet.Cells[2, 6].Value = "Dell";

            worksheet.Cells[3, 1].Value = "Logitech Mouse";
            worksheet.Cells[3, 2].Value = "Accessories";
            worksheet.Cells[3, 3].Value = 150000;
            worksheet.Cells[3, 4].Value = "Wireless mouse";
            worksheet.Cells[3, 5].Value = 50;
            worksheet.Cells[3, 6].Value = "Logitech";

            // Instructions
            var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
            instructionsSheet.Cells[1, 1].Value = "Excel Shablon Yo'riqnomasi";
            instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionsSheet.Cells[1, 1].Style.Font.Size = 16;

            instructionsSheet.Cells[3, 1].Value = "Ustunlar:";
            instructionsSheet.Cells[4, 1].Value = "1. Name - Mahsulot nomi (majburiy)";
            instructionsSheet.Cells[5, 1].Value = "2. Category - Kategoriya (majburiy)";
            instructionsSheet.Cells[6, 1].Value = "3. Price - Narx (majburiy, raqam)";
            instructionsSheet.Cells[7, 1].Value = "4. Description - Tavsif (ixtiyoriy)";
            instructionsSheet.Cells[8, 1].Value = "5. Quantity - Miqdor (majburiy, raqam)";
            instructionsSheet.Cells[9, 1].Value = "6. Manufacturer - Ishlab chiqaruvchi (ixtiyoriy)";

            instructionsSheet.Cells[11, 1].Value = "Muhim:";
            instructionsSheet.Cells[12, 1].Value = "- Birinchi qator (header) o'zgartirilmasin!";
            instructionsSheet.Cells[13, 1].Value = "- Namuna ma'lumotlarni o'chirib, o'z ma'lumotlaringizni kiriting";
            instructionsSheet.Cells[14, 1].Value = "- Price va Quantity faqat raqam bo'lishi kerak";

            worksheet.Cells.AutoFitColumns();
            instructionsSheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        // Export qilish
        public byte[] ExportToExcel(List<Product> products)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Header
            worksheet.Cells[1, 1].Value = "Id";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Category";
            worksheet.Cells[1, 4].Value = "Price";
            worksheet.Cells[1, 5].Value = "Description";
            worksheet.Cells[1, 6].Value = "Quantity";
            worksheet.Cells[1, 7].Value = "Manufacturer";
            worksheet.Cells[1, 8].Value = "Kafka Status";
            worksheet.Cells[1, 9].Value = "Created Date";

            // Header stil
            using (var range = worksheet.Cells[1, 1, 1, 9])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 129, 189));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Ma'lumotlarni yozish
            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var row = i + 2;

                worksheet.Cells[row, 1].Value = product.Id;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.Category;
                worksheet.Cells[row, 4].Value = product.Price;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[row, 5].Value = product.Description;
                worksheet.Cells[row, 6].Value = product.Quantity;
                worksheet.Cells[row, 7].Value = product.Manufacturer;
                worksheet.Cells[row, 8].Value = product.KafkaStatus ?? "Not Sent";
                worksheet.Cells[row, 9].Value = product.CreatedDate;
                worksheet.Cells[row, 9].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        // Import qilish
        public List<Product> ImportFromExcel(Stream stream)
        {
            var products = new List<Product>();

            using var package = new ExcelPackage(stream);

            if (package.Workbook.Worksheets.Count == 0)
                return products;

            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount < 2)
                return products;

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var name = worksheet.Cells[row, 1].Value?.ToString();
                    var category = worksheet.Cells[row, 2].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
                        continue;

                    var product = new Product
                    {
                        Name = name,
                        Category = category,
                        Price = decimal.Parse(worksheet.Cells[row, 3].Value?.ToString() ?? "0"),
                        Description = worksheet.Cells[row, 4].Value?.ToString() ?? "",
                        Quantity = int.Parse(worksheet.Cells[row, 5].Value?.ToString() ?? "0"),
                        Manufacturer = worksheet.Cells[row, 6].Value?.ToString(),
                        CreatedDate = DateTime.UtcNow
                    };

                    products.Add(product);
                }
                catch
                {
                    continue;
                }
            }

            return products;
        }
    }
}