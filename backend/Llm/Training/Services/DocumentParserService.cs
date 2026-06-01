using System.Text;

namespace backend.Llm.Training.Services;

public interface IDocumentParserService
{
    string ParseDocument(string filePath);
    string ParseText(string filePath);
    string ParseDocx(string filePath);
    string ParsePdf(string filePath);
}

public sealed class DocumentParserService : IDocumentParserService
{
    public string ParseDocument(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".txt" => ParseText(filePath),
            ".docx" => ParseDocx(filePath),
            ".pdf" => ParsePdf(filePath),
            _ => throw new NotSupportedException($"不支持的文件格式: {ext}。支持格式: .txt, .docx, .pdf")
        };
    }

    public string ParseText(string filePath)
    {
        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    public string ParseDocx(string filePath)
    {
        try
        {
            using var doc = Xceed.Words.NET.DocX.Load(filePath);
            var paragraphs = doc.Paragraphs
                .Where(p => !string.IsNullOrWhiteSpace(p.Text))
                .Select(p => p.Text);
            return string.Join("\n", paragraphs);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"DOCX 解析失败: {ex.Message}。请安装 DocX 库: dotnet add package DocX", ex);
        }
    }

    public string ParsePdf(string filePath)
    {
        try
        {
            using var reader = UglyToad.PdfPig.PdfDocument.Open(filePath);
            var pages = new List<string>();
            foreach (var page in reader.GetPages())
            {
                pages.Add(page.Text);
            }
            return string.Join("\n", pages);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PDF 解析失败: {ex.Message}。请安装 PdfPig 库: dotnet add package PdfPig", ex);
        }
    }
}
