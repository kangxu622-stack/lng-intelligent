using System.Text.RegularExpressions;
using backend.Llm.Training.Entities;

namespace backend.Llm.Training.Services;

public interface ITextChunkerService
{
    List<TrainingManualChunk> ChunkText(string text, string manualId, string method = "chapter");
}

public sealed class TextChunkerService : ITextChunkerService
{
    public List<TrainingManualChunk> ChunkText(string text, string manualId, string method = "chapter")
    {
        if (method == "chapter")
        {
            var chunks = ChapterChunk(text, manualId);
            if (chunks.Count <= 1 && text.Length > 500)
                return FixedChunk(text, manualId);
            return chunks;
        }
        return FixedChunk(text, manualId);
    }

    private static List<TrainingManualChunk> ChapterChunk(string text, string manualId)
    {
        var pattern = @"(第[零一二三四五六七八九十百千\d]+[章节条篇]|[\d]+\.[\d]+\s|[一二三四五六七八九十]、|[\d]+、)";
        var parts = Regex.Split(text, pattern);

        var chunks = new List<TrainingManualChunk>();
        var currentTitle = "前言";
        var currentContent = new System.Text.StringBuilder();

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (Regex.IsMatch(part, pattern))
            {
                if (currentContent.Length > 0)
                {
                    chunks.Add(MakeChunk(manualId, currentTitle, chunks.Count + 1, currentContent.ToString().Trim()));
                    currentContent.Clear();
                }
                currentTitle = part.Trim().TrimEnd('、', '.', ' ');
            }
            else
            {
                currentContent.Append(part);
            }
        }

        if (currentContent.Length > 0)
            chunks.Add(MakeChunk(manualId, currentTitle, chunks.Count + 1, currentContent.ToString().Trim()));

        return chunks.Count > 0 ? chunks : new List<TrainingManualChunk> { MakeChunk(manualId, "全文", "1", text) };
    }

    private static List<TrainingManualChunk> FixedChunk(string text, string manualId, int chunkSize = 1000, int overlap = 150)
    {
        var chunks = new List<TrainingManualChunk>();
        var start = 0;
        var seq = 1;
        while (start < text.Length)
        {
            var end = Math.Min(start + chunkSize, text.Length);
            chunks.Add(MakeChunk(manualId, $"第{seq}段", seq.ToString(), text[start..end]));
            start = end - overlap;
            seq++;
        }
        return chunks;
    }

    private static TrainingManualChunk MakeChunk(string manualId, string chapterTitle, object sectionNo, string content)
    {
        return new TrainingManualChunk
        {
            ChunkId = Guid.NewGuid().ToString("N")[..16],
            ManualId = manualId,
            ChapterTitle = chapterTitle,
            SectionNo = sectionNo.ToString(),
            Content = content,
            PageNo = "",
            SystemName = GuessSystem(content),
            EmbeddingId = "",
            CreatedTime = DateTime.Now
        };
    }

    private static string GuessSystem(string content)
    {
        var keywords = new Dictionary<string, string[]>
        {
            ["储罐系统"] = new[] { "储罐", "LNG储罐", "罐内泵", "液位", "罐压" },
            ["BOG 系统"] = new[] { "BOG", "蒸发气", "BOG压缩机", "再冷凝" },
            ["气化外输系统"] = new[] { "气化", "ORV", "SCV", "高压泵", "外输" },
            ["安全应急"] = new[] { "应急", "泄漏", "火灾", "ESD", "消防", "紧急" },
            ["设备启停"] = new[] { "启动", "停止", "启机", "停机", "切换", "投运" },
        };
        foreach (var kv in keywords)
        {
            if (kv.Value.Any(k => content.Contains(k)))
                return kv.Key;
        }
        return "其他";
    }
}
