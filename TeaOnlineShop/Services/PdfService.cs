using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TeaOnlineShop.Models.Dbase;
using TeaOnlineShop.Models.ViewModels;
using System.Linq;
using System.Text;

namespace TeaOnlineShop.Services
{
    public class PdfService
    {
        private readonly ILogger<PdfService> _logger;
        private static readonly BaseColor TABLE_HEADER_BG = new BaseColor(240, 240, 240);
        private static readonly BaseColor BORDER_COLOR = new BaseColor(220, 220, 220);
        private static readonly BaseColor SUCCESS_COLOR = new BaseColor(40, 167, 69);
        private static readonly BaseColor WARNING_COLOR = new BaseColor(255, 193, 7);
        private static readonly BaseColor DANGER_COLOR = new BaseColor(220, 53, 69);
        private static readonly BaseColor INFO_COLOR = new BaseColor(23, 162, 184);

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
        }

        public Task<byte[]> GenerateDemandForecastReport(AiDemandForecastHistoryRecord record)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var document = new Document(PageSize.A4, 36, 36, 54, 36);
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);

                    writer.PageEvent = new PdfHeaderFooter("Demand Forecast Report");
                    document.AddTitle("AI Demand Forecast Report");
                    document.AddAuthor("Tea Online Shop");
                    document.AddCreator("Tea Online Shop System");

                    document.Open();

                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 16, Font.BOLD);
                    Paragraph title = new Paragraph("AI Demand Forecast Report", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 15f
                    };
                    document.Add(title);

                    Font italicFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC);
                    Paragraph dateInfo = new Paragraph(
                        $"Generated on: {record.TimestampUtc.ToLocalTime():MMMM dd, yyyy HH:mm:ss}",
                        italicFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    document.Add(dateInfo);

                    Font labelFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    Font valueFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    string modelName = string.IsNullOrWhiteSpace(record.Model)
                        ? "N/A"
                        : record.Model;
                    string modelVersion = string.IsNullOrWhiteSpace(record.ModelVersion)
                        ? string.Empty
                        : $" v{record.ModelVersion}";

                    PdfPTable summaryTable = new PdfPTable(2)
                    {
                        WidthPercentage = 70,
                        HorizontalAlignment = Element.ALIGN_LEFT,
                        SpacingAfter = 20f
                    };
                    summaryTable.SetWidths(new float[] { 1.4f, 3f });
                    summaryTable.AddCell(GetCell("Tea Grade", labelFont));
                    summaryTable.AddCell(GetCell(record.Grade ?? "N/A", valueFont));
                    summaryTable.AddCell(GetCell("Forecast Horizon", labelFont));
                    summaryTable.AddCell(GetCell($"{record.HorizonDays} days", valueFont));
                    summaryTable.AddCell(GetCell("Model", labelFont));
                    summaryTable.AddCell(GetCell($"{modelName}{modelVersion}", valueFont));
                    summaryTable.AddCell(GetCell("Expected MAPE", labelFont));
                    summaryTable.AddCell(GetCell($"{record.ExpectedMape:0.###}%", valueFont));
                    summaryTable.AddCell(GetCell("Input Source", labelFont));
                    summaryTable.AddCell(GetCell(
                        string.IsNullOrWhiteSpace(record.SourceLabel) ? "Unspecified" : record.SourceLabel,
                        valueFont));
                    if (record.SourceStartDate.HasValue && record.SourceEndDate.HasValue)
                    {
                        summaryTable.AddCell(GetCell("Source Period", labelFont));
                        summaryTable.AddCell(GetCell(
                            $"{record.SourceStartDate:yyyy-MM-dd} to {record.SourceEndDate:yyyy-MM-dd}",
                            valueFont));
                    }
                    document.Add(summaryTable);

                    if (!string.IsNullOrWhiteSpace(record.SourceNote))
                    {
                        document.Add(new Paragraph($"Data provenance: {record.SourceNote}", italicFont)
                        {
                            SpacingAfter = 14f
                        });
                    }

                    PdfPTable forecastTable = new PdfPTable(2)
                    {
                        WidthPercentage = 75,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    forecastTable.SetWidths(new float[] { 1f, 2f });
                    AddTableHeader(forecastTable, new[] { "Day", "Predicted Demand (kg)" });

                    Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    if (record.Predictions != null)
                    {
                        for (int i = 0; i < record.Predictions.Count; i++)
                        {
                            forecastTable.AddCell(GetCell($"Day +{i + 1}", cellFont));
                            forecastTable.AddCell(GetCell(
                                record.Predictions[i].ToString("0.00"),
                                cellFont,
                                Element.ALIGN_RIGHT));
                        }
                    }

                    document.Add(forecastTable);
                    document.Close();

                    return Task.FromResult(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating demand forecast PDF report");
                throw;
            }
        }

        public Task<byte[]> GenerateInventoryReport(IEnumerable<TeaInventoryItem> items)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Create document with A4 size
                    var document = new Document(PageSize.A4, 36, 36, 54, 36);
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    
                    // Add metadata and header/footer
                    writer.PageEvent = new PdfHeaderFooter("Tea Inventory Report");
                    document.AddTitle("Tea Inventory Report");
                    document.AddAuthor("Tea Online Shop");
                    document.AddCreator("Tea Online Shop System");
                    
                    document.Open();
                    
                    // Add title
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 16, Font.BOLD);
                    Paragraph title = new Paragraph("Tea Inventory Report", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 15f;
                    document.Add(title);
                    
                    // Add date
                    Font italicFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC);
                    Paragraph dateInfo = new Paragraph($"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss")}", italicFont);
                    dateInfo.Alignment = Element.ALIGN_CENTER;
                    dateInfo.SpacingAfter = 20f;
                    document.Add(dateInfo);

                    // Create table
                    PdfPTable table = new PdfPTable(9);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1f, 3f, 2f, 1.5f, 2f, 2f, 2f, 2f, 2f });
                    table.SpacingAfter = 20f;
                    
                    // Add table header
                    AddTableHeader(table, new string[] { 
                        "ID", "Name", "Tea Type", "Grade", "Current Stock", 
                        "Min Stock", "Retail Price", "Value", "Status" 
                    });
                    
                    // Add table data
                    decimal totalValue = 0;
                    Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    Font statusFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    
                    foreach (var item in items)
                    {
                        BaseColor statusColor = SUCCESS_COLOR;
                        string statusText = "In Stock";
                        
                        if (item.CurrentStock <= item.MinimumStock)
                        {
                            statusColor = DANGER_COLOR;
                            statusText = "Low Stock";
                        }
                        else if (item.CurrentStock <= (item.MinimumStock * 1.5m))
                        {
                            statusColor = WARNING_COLOR;
                            statusText = "Low Stock Soon";
                        }
                        
                        var itemValue = item.CurrentStock * (item.RetailPrice ?? 0);
                        totalValue += itemValue;
                        
                        statusFont.Color = statusColor;
                        
                        table.AddCell(GetCell(item.Id.ToString(), cellFont));
                        table.AddCell(GetCell(item.Name, cellFont));
                        table.AddCell(GetCell(item.TeaType, cellFont));
                        table.AddCell(GetCell(item.Grade, cellFont));
                        table.AddCell(GetCell(item.CurrentStock.ToString("0.00"), cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell(item.MinimumStock.HasValue ? item.MinimumStock.Value.ToString("0.00") : "N/A", cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell(item.RetailPrice.HasValue ? $"${item.RetailPrice.Value.ToString("0.00")}" : "N/A", cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell($"${itemValue.ToString("0.00")}", cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell(statusText, statusFont));
                    }
                    
                    // Add table footer with total
                    Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    PdfPCell totalLabelCell = new PdfPCell(new Phrase("Total Inventory Value:", boldFont));
                    totalLabelCell.Colspan = 7;
                    totalLabelCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    totalLabelCell.Border = PdfPCell.TOP_BORDER;
                    totalLabelCell.BorderColor = BORDER_COLOR;
                    totalLabelCell.PaddingTop = 8f;
                    table.AddCell(totalLabelCell);
                    
                    PdfPCell totalValueCell = new PdfPCell(new Phrase($"${totalValue.ToString("0.00")}", boldFont));
                    totalValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    totalValueCell.Border = PdfPCell.TOP_BORDER;
                    totalValueCell.BorderColor = BORDER_COLOR;
                    totalValueCell.PaddingTop = 8f;
                    table.AddCell(totalValueCell);
                    
                    PdfPCell emptyCell = new PdfPCell(new Phrase(" "));
                    emptyCell.Border = PdfPCell.TOP_BORDER;
                    emptyCell.BorderColor = BORDER_COLOR;
                    table.AddCell(emptyCell);
                    
                    document.Add(table);
                    
                    // Add legend
                    Paragraph legend = new Paragraph("Status Legend:", boldFont);
                    legend.SpacingAfter = 8f;
                    document.Add(legend);
                    
                    PdfPTable legendTable = new PdfPTable(3);
                    legendTable.WidthPercentage = 60;
                    legendTable.HorizontalAlignment = Element.ALIGN_LEFT;
                    
                    Font legendFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    
                    legendFont.Color = SUCCESS_COLOR;
                    legendTable.AddCell(GetLegendCell("In Stock", legendFont));
                    
                    legendFont.Color = WARNING_COLOR;
                    legendTable.AddCell(GetLegendCell("Low Stock Soon", legendFont));
                    
                    legendFont.Color = DANGER_COLOR;
                    legendTable.AddCell(GetLegendCell("Low Stock", legendFont));
                    
                    document.Add(legendTable);
                    
                    document.Close();
                    
                    return Task.FromResult(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory PDF report");
                throw;
            }
        }

        public Task<byte[]> GenerateTransactionReport(IEnumerable<TeaInventoryTransaction> transactions)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Create document with landscape orientation
                    var document = new Document(PageSize.A4.Rotate(), 36, 36, 54, 36);
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    
                    // Add metadata and header/footer
                    writer.PageEvent = new PdfHeaderFooter("Transaction Log");
                    document.AddTitle("Transaction Log");
                    document.AddAuthor("Tea Online Shop");
                    document.AddCreator("Tea Online Shop System");
                    
                    document.Open();
                    
                    // Add title
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 16, Font.BOLD);
                    Paragraph title = new Paragraph("Tea Inventory Transaction Log", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 15f;
                    document.Add(title);
                    
                    // Add date
                    Font italicFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC);
                    Paragraph dateInfo = new Paragraph($"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss")}", italicFont);
                    dateInfo.Alignment = Element.ALIGN_CENTER;
                    dateInfo.SpacingAfter = 20f;
                    document.Add(dateInfo);

                    // Create table
                    PdfPTable table = new PdfPTable(9);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1f, 2.5f, 1f, 2f, 1.5f, 1.5f, 1.5f, 2f, 3f });
                    table.SpacingAfter = 20f;
                    
                    // Add table header
                    AddTableHeader(table, new string[] { 
                        "ID", "Date", "Item ID", "Type", "Quantity", 
                        "Unit Price", "Total", "Reference", "Notes" 
                    });
                    
                    // Add table data
                    decimal totalSales = 0;
                    decimal totalPurchases = 0;
                    
                    Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    Font typeFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    
                    foreach (var transaction in transactions)
                    {
                        BaseColor typeColor = INFO_COLOR;
                        
                        switch (transaction.TransactionType?.ToLower())
                        {
                            case "sale":
                                typeColor = DANGER_COLOR;
                                totalSales += transaction.Quantity * (transaction.UnitPrice ?? 0);
                                break;
                            case "purchase":
                            case "delivery":
                                typeColor = SUCCESS_COLOR;
                                totalPurchases += transaction.Quantity * (transaction.UnitPrice ?? 0);
                                break;
                            case "production":
                                typeColor = INFO_COLOR;
                                break;
                            case "adjustment":
                                typeColor = WARNING_COLOR;
                                break;
                        }
                        
                        var totalAmount = transaction.Quantity * (transaction.UnitPrice ?? 0);
                        
                        typeFont.Color = typeColor;
                        
                        table.AddCell(GetCell(transaction.Id.ToString(), cellFont));
                        table.AddCell(GetCell(transaction.TransactionDate.ToString("MMM dd, yyyy HH:mm"), cellFont));
                        table.AddCell(GetCell(transaction.InventoryItemId.ToString(), cellFont));
                        table.AddCell(GetCell(transaction.TransactionType, typeFont));
                        table.AddCell(GetCell(transaction.Quantity.ToString("0.00"), cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell(transaction.UnitPrice.HasValue ? $"${transaction.UnitPrice.Value.ToString("0.00")}" : "N/A", cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell(transaction.UnitPrice.HasValue ? $"${totalAmount.ToString("0.00")}" : "N/A", cellFont, Element.ALIGN_RIGHT));
                        table.AddCell(GetCell(transaction.ReferenceNumber, cellFont));
                        table.AddCell(GetCell(transaction.Notes ?? "", cellFont));
                    }
                    
                    // Add table footer with totals
                    Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    
                    // Total Sales row
                    PdfPCell salesLabelCell = new PdfPCell(new Phrase("Total Sales:", boldFont));
                    salesLabelCell.Colspan = 6;
                    salesLabelCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    salesLabelCell.Border = PdfPCell.TOP_BORDER;
                    salesLabelCell.BorderColor = BORDER_COLOR;
                    salesLabelCell.PaddingTop = 8f;
                    table.AddCell(salesLabelCell);
                    
                    PdfPCell salesValueCell = new PdfPCell(new Phrase($"${totalSales.ToString("0.00")}", boldFont));
                    salesValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    salesValueCell.Border = PdfPCell.TOP_BORDER;
                    salesValueCell.BorderColor = BORDER_COLOR;
                    salesValueCell.PaddingTop = 8f;
                    table.AddCell(salesValueCell);
                    
                    PdfPCell emptyCell1 = new PdfPCell(new Phrase(" "));
                    emptyCell1.Colspan = 2;
                    emptyCell1.Border = PdfPCell.TOP_BORDER;
                    emptyCell1.BorderColor = BORDER_COLOR;
                    table.AddCell(emptyCell1);
                    
                    // Total Purchases row
                    PdfPCell purchasesLabelCell = new PdfPCell(new Phrase("Total Purchases:", boldFont));
                    purchasesLabelCell.Colspan = 6;
                    purchasesLabelCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    purchasesLabelCell.Border = PdfPCell.NO_BORDER;
                    purchasesLabelCell.PaddingTop = 4f;
                    table.AddCell(purchasesLabelCell);
                    
                    PdfPCell purchasesValueCell = new PdfPCell(new Phrase($"${totalPurchases.ToString("0.00")}", boldFont));
                    purchasesValueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    purchasesValueCell.Border = PdfPCell.NO_BORDER;
                    purchasesValueCell.PaddingTop = 4f;
                    table.AddCell(purchasesValueCell);
                    
                    PdfPCell emptyCell2 = new PdfPCell(new Phrase(" "));
                    emptyCell2.Colspan = 2;
                    emptyCell2.Border = PdfPCell.NO_BORDER;
                    table.AddCell(emptyCell2);
                    
                    document.Add(table);
                    
                    // Add legend
                    Paragraph legend = new Paragraph("Transaction Type Legend:", boldFont);
                    legend.SpacingAfter = 8f;
                    document.Add(legend);
                    
                    PdfPTable legendTable = new PdfPTable(4);
                    legendTable.WidthPercentage = 80;
                    legendTable.HorizontalAlignment = Element.ALIGN_LEFT;
                    
                    Font legendFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    
                    legendFont.Color = DANGER_COLOR;
                    legendTable.AddCell(GetLegendCell("Sale", legendFont));
                    
                    legendFont.Color = SUCCESS_COLOR;
                    legendTable.AddCell(GetLegendCell("Purchase/Delivery", legendFont));
                    
                    legendFont.Color = INFO_COLOR;
                    legendTable.AddCell(GetLegendCell("Production", legendFont));
                    
                    legendFont.Color = WARNING_COLOR;
                    legendTable.AddCell(GetLegendCell("Adjustment", legendFont));
                    
                    document.Add(legendTable);
                    
                    document.Close();
                    
                    return Task.FromResult(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating transaction PDF report");
                throw;
            }
        }
        
        public Task<byte[]> GenerateAnalyticsDashboardReport(TeaOnlineShop.Models.ViewModels.DashboardViewModel model)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Create document with A4 size
                    var document = new Document(PageSize.A4, 36, 36, 54, 36);
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    
                    // Add metadata and header/footer
                    writer.PageEvent = new PdfHeaderFooter("Analytics Dashboard Report");
                    document.AddTitle("Analytics Dashboard Report");
                    document.AddAuthor("Tea Online Shop");
                    document.AddCreator("Tea Online Shop System");
                    
                    document.Open();
                    
                    // Add title
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 16, Font.BOLD);
                    Paragraph title = new Paragraph("Analytics Dashboard Report", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 15f;
                    document.Add(title);
                    
                    // Add date range
                    Font italicFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC);
                    Paragraph dateInfo = new Paragraph($"Period: {model.StartDate.ToString("MMMM dd, yyyy")} - {model.EndDate.ToString("MMMM dd, yyyy")}", italicFont);
                    dateInfo.Alignment = Element.ALIGN_CENTER;
                    dateInfo.SpacingAfter = 10f;
                    document.Add(dateInfo);

                    // Add generation timestamp
                    Paragraph genInfo = new Paragraph($"Generated on: {DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss")}", italicFont);
                    genInfo.Alignment = Element.ALIGN_CENTER;
                    genInfo.SpacingAfter = 20f;
                    document.Add(genInfo);

                    // Add summary metrics
                    Paragraph summaryTitle = new Paragraph("Key Metrics Summary", FontFactory.GetFont(FontFactory.HELVETICA, 14, Font.BOLD));
                    summaryTitle.SpacingAfter = 10f;
                    document.Add(summaryTitle);

                    // Create metrics table
                    PdfPTable metricsTable = new PdfPTable(4);
                    metricsTable.WidthPercentage = 100;
                    metricsTable.SpacingAfter = 20f;
                    
                    // Add metrics
                    Font metricLabelFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
                    Font metricValueFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, Font.BOLD);

                    // Total Sales
                    PdfPCell salesLabelCell = GetCell("Total Sales", metricLabelFont, Element.ALIGN_CENTER);
                    salesLabelCell.BackgroundColor = TABLE_HEADER_BG;
                    metricsTable.AddCell(salesLabelCell);
                    
                    // Total Units Sold
                    PdfPCell unitsLabelCell = GetCell("Total Units Sold", metricLabelFont, Element.ALIGN_CENTER);
                    unitsLabelCell.BackgroundColor = TABLE_HEADER_BG;
                    metricsTable.AddCell(unitsLabelCell);
                    
                    // Production Volume
                    PdfPCell prodLabelCell = GetCell("Production Volume", metricLabelFont, Element.ALIGN_CENTER);
                    prodLabelCell.BackgroundColor = TABLE_HEADER_BG;
                    metricsTable.AddCell(prodLabelCell);
                    
                    // Deliveries Received
                    PdfPCell delLabelCell = GetCell("Deliveries Received", metricLabelFont, Element.ALIGN_CENTER);
                    delLabelCell.BackgroundColor = TABLE_HEADER_BG;
                    metricsTable.AddCell(delLabelCell);
                    
                    // Add values
                    metricValueFont.Color = new BaseColor(67, 97, 238); // Primary color
                    metricsTable.AddCell(GetCell("$" + model.TotalSales.ToString("N2"), metricValueFont, Element.ALIGN_CENTER));
                    
                    metricValueFont.Color = new BaseColor(220, 53, 69); // Danger color
                    metricsTable.AddCell(GetCell(model.TotalSalesQuantity.ToString("N2"), metricValueFont, Element.ALIGN_CENTER));
                    
                    metricValueFont.Color = new BaseColor(40, 167, 69); // Success color
                    metricsTable.AddCell(GetCell(model.TotalProductionQuantity.ToString("N2"), metricValueFont, Element.ALIGN_CENTER));
                    
                    metricValueFont.Color = new BaseColor(23, 162, 184); // Info color
                    metricsTable.AddCell(GetCell(model.TotalDeliveryQuantity.ToString("N2"), metricValueFont, Element.ALIGN_CENTER));
                    
                    document.Add(metricsTable);
                    
                    // Add "Top Selling Items" section
                    if (model.TopSellingItems != null && model.TopSellingItems.Any())
                    {
                        Paragraph topItemsTitle = new Paragraph("Top Selling Items", FontFactory.GetFont(FontFactory.HELVETICA, 14, Font.BOLD));
                        topItemsTitle.SpacingBefore = 15f;
                        topItemsTitle.SpacingAfter = 10f;
                        document.Add(topItemsTitle);
                        
                        // Create top selling items table
                        PdfPTable itemsTable = new PdfPTable(5);
                        itemsTable.WidthPercentage = 100;
                        itemsTable.SetWidths(new float[] { 3f, 2f, 1.5f, 2f, 2f });
                        itemsTable.SpacingAfter = 20f;
                        
                        // Add table header
                        AddTableHeader(itemsTable, new string[] { 
                            "Name", "Tea Type", "Grade", "Quantity Sold", "Revenue"
                        });
                        
                        // Add table data
                        Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                        
                        foreach (var item in model.TopSellingItems)
                        {
                            itemsTable.AddCell(GetCell(item.Name, cellFont));
                            itemsTable.AddCell(GetCell(item.TeaType, cellFont));
                            itemsTable.AddCell(GetCell(item.Grade, cellFont));
                            itemsTable.AddCell(GetCell(item.QuantitySold.ToString("N2"), cellFont, Element.ALIGN_RIGHT));
                            itemsTable.AddCell(GetCell("$" + item.Revenue.ToString("N2"), cellFont, Element.ALIGN_RIGHT));
                        }
                        
                        document.Add(itemsTable);
                    }
                    else
                    {
                        Paragraph noData = new Paragraph("No top selling items data available for the selected period.", 
                                                         FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC));
                        noData.SpacingBefore = 15f;
                        document.Add(noData);
                    }
                    
                    // Add sales trends commentary
                    Paragraph trendsTitle = new Paragraph("Trends Analysis", FontFactory.GetFont(FontFactory.HELVETICA, 14, Font.BOLD));
                    trendsTitle.SpacingBefore = 15f;
                    trendsTitle.SpacingAfter = 10f;
                    document.Add(trendsTitle);
                    
                    Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                    
                    // Get some insights from the data
                    string trendInsight = GetTrendInsight(model);
                    Paragraph insight = new Paragraph(trendInsight, normalFont);
                    insight.SpacingAfter = 15f;
                    document.Add(insight);
                    
                    // Add note that charts would be available in client-side export
                    Paragraph chartNote = new Paragraph("Note: For visual charts and graphs, please use the client-side PDF export option from the Analytics Dashboard.", 
                                                       FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC));
                    chartNote.SpacingBefore = 15f;
                    document.Add(chartNote);
                    
                    document.Close();
                    
                    return Task.FromResult(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating analytics dashboard PDF report");
                throw;
            }
        }
        
        public Task<byte[]> GenerateSalesReport(TeaOnlineShop.Models.ViewModels.SalesReportViewModel model)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Create document with A4 size
                    var document = new Document(PageSize.A4, 36, 36, 54, 36);
                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    
                    // Add metadata and header/footer
                    writer.PageEvent = new PdfHeaderFooter("Sales Report");
                    document.AddTitle("Tea Shop Sales Report");
                    document.AddAuthor("Tea Online Shop");
                    document.AddCreator("Tea Online Shop System");
                    
                    document.Open();
                    
                    // Add title
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA, 16, Font.BOLD);
                    Paragraph title = new Paragraph(model.Title, titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 10f;
                    document.Add(title);
                    
                    // Add date range info
                    Font subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    Paragraph dateRange = new Paragraph($"Period: {model.StartDate.ToString("MMMM dd, yyyy")} - {model.EndDate.ToString("MMMM dd, yyyy")}", subtitleFont);
                    dateRange.Alignment = Element.ALIGN_CENTER;
                    dateRange.SpacingAfter = 5f;
                    document.Add(dateRange);
                    
                    // Add generation date
                    Font italicFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.ITALIC);
                    Paragraph dateInfo = new Paragraph($"Generated on: {model.GeneratedDate.ToString("MMMM dd, yyyy HH:mm:ss")}", italicFont);
                    dateInfo.Alignment = Element.ALIGN_CENTER;
                    dateInfo.SpacingAfter = 20f;
                    document.Add(dateInfo);
                    
                    // Add summary section
                    Font sectionFont = FontFactory.GetFont(FontFactory.HELVETICA, 14, Font.BOLD);
                    Paragraph summaryTitle = new Paragraph("Sales Summary", sectionFont);
                    summaryTitle.SpacingAfter = 10f;
                    document.Add(summaryTitle);
                    
                    // Create summary table
                    PdfPTable summaryTable = new PdfPTable(2);
                    summaryTable.WidthPercentage = 70;
                    summaryTable.HorizontalAlignment = Element.ALIGN_CENTER;
                    summaryTable.SpacingAfter = 20f;
                    
                    Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, Font.BOLD);
                    Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
                    
                    // Add summary data
                    summaryTable.AddCell(GetCell("Total Sales:", boldFont, Element.ALIGN_LEFT));
                    summaryTable.AddCell(GetCell($"${model.TotalSales.ToString("0.00")}", normalFont, Element.ALIGN_RIGHT));
                    
                    summaryTable.AddCell(GetCell("Total Items Sold:", boldFont, Element.ALIGN_LEFT));
                    summaryTable.AddCell(GetCell(model.TotalItemsSold.ToString("0"), normalFont, Element.ALIGN_RIGHT));
                    
                    summaryTable.AddCell(GetCell("Average Order Value:", boldFont, Element.ALIGN_LEFT));
                    summaryTable.AddCell(GetCell($"${model.AverageOrderValue.ToString("0.00")}", normalFont, Element.ALIGN_RIGHT));
                    
                    document.Add(summaryTable);
                    
                    // Sales by tea type section
                    if (model.SalesByTeaType != null && model.SalesByTeaType.Any())
                    {
                        Paragraph teaTypeTitle = new Paragraph("Sales by Tea Type", sectionFont);
                        teaTypeTitle.SpacingAfter = 10f;
                        document.Add(teaTypeTitle);
                        
                        PdfPTable teaTypeTable = new PdfPTable(4);
                        teaTypeTable.WidthPercentage = 100;
                        teaTypeTable.SetWidths(new float[] { 2.5f, 2f, 2f, 1.5f });
                        teaTypeTable.SpacingAfter = 20f;
                        
                        // Add table header
                        AddTableHeader(teaTypeTable, new string[] { "Tea Type", "Total Amount", "Items Sold", "Percentage" });
                        
                        // Add table data
                        foreach (var type in model.SalesByTeaType)
                        {
                            teaTypeTable.AddCell(GetCell(type.TeaType, normalFont));
                            teaTypeTable.AddCell(GetCell($"${type.TotalAmount.ToString("0.00")}", normalFont, Element.ALIGN_RIGHT));
                            teaTypeTable.AddCell(GetCell(type.ItemsSold.ToString("0"), normalFont, Element.ALIGN_RIGHT));
                            teaTypeTable.AddCell(GetCell($"{type.Percentage.ToString("0.0")}%", normalFont, Element.ALIGN_RIGHT));
                        }
                        
                        document.Add(teaTypeTable);
                    }
                    
                    // Top selling products section
                    if (model.TopSellingProducts != null && model.TopSellingProducts.Any())
                    {
                        Paragraph topProductsTitle = new Paragraph("Top Selling Products", sectionFont);
                        topProductsTitle.SpacingAfter = 10f;
                        document.Add(topProductsTitle);
                        
                        PdfPTable productsTable = new PdfPTable(5);
                        productsTable.WidthPercentage = 100;
                        productsTable.SetWidths(new float[] { 3f, 2f, 1f, 2f, 2f });
                        productsTable.SpacingAfter = 20f;
                        
                        // Add table header
                        AddTableHeader(productsTable, new string[] { "Product", "Tea Type", "Grade", "Total Amount", "Items Sold" });
                        
                        // Add table data
                        foreach (var product in model.TopSellingProducts)
                        {
                            productsTable.AddCell(GetCell(product.ProductName, normalFont));
                            productsTable.AddCell(GetCell(product.TeaType, normalFont));
                            productsTable.AddCell(GetCell(product.Grade, normalFont, Element.ALIGN_CENTER));
                            productsTable.AddCell(GetCell($"${product.TotalAmount.ToString("0.00")}", normalFont, Element.ALIGN_RIGHT));
                            productsTable.AddCell(GetCell(product.ItemsSold.ToString("0"), normalFont, Element.ALIGN_RIGHT));
                        }
                        
                        document.Add(productsTable);
                    }
                    
                    // Daily sales section
                    if (model.DailySales != null && model.DailySales.Any())
                    {
                        Paragraph dailySalesTitle = new Paragraph("Daily Sales Breakdown", sectionFont);
                        dailySalesTitle.SpacingAfter = 10f;
                        document.Add(dailySalesTitle);
                        
                        PdfPTable dailyTable = new PdfPTable(4);
                        dailyTable.WidthPercentage = 100;
                        dailyTable.SetWidths(new float[] { 2.5f, 2.5f, 2f, 2f });
                        dailyTable.SpacingAfter = 20f;
                        
                        // Add table header
                        AddTableHeader(dailyTable, new string[] { "Date", "Total Amount", "Items Sold", "Orders" });
                        
                        // Add table data
                        foreach (var day in model.DailySales)
                        {
                            dailyTable.AddCell(GetCell(day.Date.ToString("MMMM dd, yyyy"), normalFont));
                            dailyTable.AddCell(GetCell($"${day.TotalAmount.ToString("0.00")}", normalFont, Element.ALIGN_RIGHT));
                            dailyTable.AddCell(GetCell(day.ItemsSold.ToString("0"), normalFont, Element.ALIGN_RIGHT));
                            dailyTable.AddCell(GetCell(day.TransactionCount.ToString(), normalFont, Element.ALIGN_RIGHT));
                        }
                        
                        document.Add(dailyTable);
                    }
                    
                    document.Close();
                    
                    return Task.FromResult(memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report PDF");
                throw;
            }
        }
        
        // Helper method to generate trend insights
        private string GetTrendInsight(TeaOnlineShop.Models.ViewModels.DashboardViewModel model)
        {
            StringBuilder sb = new StringBuilder();
            
            // Check if we have any data
            if (model.SalesChartData == null || !model.SalesChartData.Any() || 
                model.ProductionChartData == null || !model.ProductionChartData.Any() ||
                model.ChartLabels == null || !model.ChartLabels.Any())
            {
                return "Insufficient data available for the selected period to generate trend insights.";
            }
            
            // Calculate total sales
            double totalSales = model.SalesChartData.Sum();
            double avgSales = totalSales / model.SalesChartData.Count;
            
            // Calculate total production
            double totalProduction = model.ProductionChartData.Sum();
            double avgProduction = totalProduction / model.ProductionChartData.Count;
            
            // Check if sales are increasing or decreasing
            bool salesIncreasing = IsIncreasing(model.SalesChartData);
            bool productionIncreasing = IsIncreasing(model.ProductionChartData);
            
            // Generate insights
            sb.AppendLine($"During the period from {model.StartDate.ToString("MMM dd, yyyy")} to {model.EndDate.ToString("MMM dd, yyyy")}, the tea shop generated total sales of ${totalSales:N2}.");
            sb.AppendLine();
            
            // Sales trend
            if (salesIncreasing)
            {
                sb.AppendLine("Sales demonstrated an upward trend during this period, indicating positive market response.");
            }
            else
            {
                sb.AppendLine("Sales showed some fluctuations with a general downward or stable trend during this period.");
            }
            
            // Production trend
            if (productionIncreasing)
            {
                sb.AppendLine("Production volumes increased to meet anticipated demand, suggesting planned inventory buildup.");
            }
            else
            {
                sb.AppendLine("Production volumes were either stable or decreased during this period, possibly to optimize inventory levels.");
            }
            
            sb.AppendLine();
            
            // Tea type insights
            if (model.PieChartData != null && model.PieChartData.Any() && model.PieChartLabels != null && model.PieChartLabels.Any())
            {
                int topSellingTypeIndex = model.PieChartData.IndexOf(model.PieChartData.Max());
                string topSellingType = model.PieChartLabels[topSellingTypeIndex];
                
                sb.AppendLine($"The top-selling tea type was {topSellingType}, accounting for a significant portion of total sales.");
            }
            
            return sb.ToString();
        }
        
        // Helper method to determine if data is generally increasing
        private bool IsIncreasing(List<double> data)
        {
            if (data == null || data.Count < 2)
                return false;
            
            // Use simple linear regression to determine trend
            int n = data.Count;
            double sumX = 0;
            double sumY = 0;
            double sumXY = 0;
            double sumX2 = 0;
            
            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += data[i];
                sumXY += i * data[i];
                sumX2 += i * i;
            }
            
            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            
            return slope > 0;
        }
        
        // Helper methods
        private void AddTableHeader(PdfPTable table, string[] headers)
        {
            Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
            
            foreach (var header in headers)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                cell.BackgroundColor = TABLE_HEADER_BG;
                cell.BorderColor = BORDER_COLOR;
                cell.PaddingTop = 6f;
                cell.PaddingBottom = 6f;
                table.AddCell(cell);
            }
        }
        
        private PdfPCell GetCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.BorderColor = BORDER_COLOR;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingRight = 5f;
            return cell;
        }
        
        private PdfPCell GetLegendCell(string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.BorderColor = BORDER_COLOR;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            return cell;
        }
    }
    
    // Custom class for PDF header and footer
    public class PdfHeaderFooter : PdfPageEventHelper
    {
        private readonly string _reportTitle;
        private readonly Font _headerFont;
        private readonly Font _footerFont;
        
        public PdfHeaderFooter(string reportTitle)
        {
            _reportTitle = reportTitle;
            _headerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            _footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
        }
        
        public override void OnStartPage(PdfWriter writer, Document document)
        {
            base.OnStartPage(writer, document);
            
            // Add header
            PdfPTable header = new PdfPTable(3);
            header.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
            header.DefaultCell.Border = PdfPCell.NO_BORDER;
            
            // Left: Company logo or name
            Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.BOLD);
            PdfPCell companyCell = new PdfPCell(new Phrase("Tea Online Shop", boldFont));
            companyCell.HorizontalAlignment = Element.ALIGN_LEFT;
            companyCell.Border = PdfPCell.NO_BORDER;
            header.AddCell(companyCell);
            
            // Center: Report title
            PdfPCell titleCell = new PdfPCell(new Phrase(_reportTitle, _headerFont));
            titleCell.HorizontalAlignment = Element.ALIGN_CENTER;
            titleCell.Border = PdfPCell.NO_BORDER;
            header.AddCell(titleCell);
            
            // Right: Date
            PdfPCell dateCell = new PdfPCell(new Phrase(DateTime.Now.ToString("yyyy-MM-dd"), _headerFont));
            dateCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            dateCell.Border = PdfPCell.NO_BORDER;
            header.AddCell(dateCell);
            
            header.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 10, writer.DirectContent);
        }
        
        public override void OnEndPage(PdfWriter writer, Document document)
        {
            base.OnEndPage(writer, document);
            
            // Add footer with page numbers
            PdfPTable footer = new PdfPTable(3);
            footer.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
            footer.DefaultCell.Border = PdfPCell.NO_BORDER;
            
            // Left: Company website or info
            PdfPCell leftCell = new PdfPCell(new Phrase("www.teaonlineshop.com", _footerFont));
            leftCell.HorizontalAlignment = Element.ALIGN_LEFT;
            leftCell.Border = PdfPCell.NO_BORDER;
            footer.AddCell(leftCell);
            
            // Center: Copyright
            PdfPCell centerCell = new PdfPCell(new Phrase($"© {DateTime.Now.Year} Tea Online Shop", _footerFont));
            centerCell.HorizontalAlignment = Element.ALIGN_CENTER;
            centerCell.Border = PdfPCell.NO_BORDER;
            footer.AddCell(centerCell);
            
            // Right: Page numbers
            PdfPCell rightCell = new PdfPCell(new Phrase($"Page {writer.PageNumber}", _footerFont));
            rightCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            rightCell.Border = PdfPCell.NO_BORDER;
            footer.AddCell(rightCell);
            
            footer.WriteSelectedRows(0, -1, document.LeftMargin, document.BottomMargin, writer.DirectContent);
        }
    }
} 
