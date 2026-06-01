using backend.Llm.Training.Dtos;
using backend.Llm.Training.Entities;
using backend.Llm.Training.Repositories;

namespace backend.Llm.Training.Services;

public interface IManualService
{
    Task<ManualDto> UploadManualAsync(IFormFile file, string userId, CancellationToken ct);
    Task<List<ManualDto>> GetManualsAsync(CancellationToken ct);
    Task<ManualParseResultDto> ParseManualAsync(string manualId, string userId, CancellationToken ct);
    Task<List<ManualChunkDto>> ChunkManualAsync(string manualId, string userId, CancellationToken ct);
    Task DeleteManualAsync(string manualId, string userId, CancellationToken ct);
    Task<List<ManualChunkDto>> SearchChunksAsync(string keyword, string? manualId, CancellationToken ct);
}

public sealed class ManualService : IManualService
{
    private readonly ITrainingRepository _repo;
    private readonly IDocumentParserService _parser;
    private readonly ITextChunkerService _chunker;

    public ManualService(ITrainingRepository repo, IDocumentParserService parser, ITextChunkerService chunker)
    {
        _repo = repo;
        _parser = parser;
        _chunker = chunker;
    }

    public async Task<ManualDto> UploadManualAsync(IFormFile file, string userId, CancellationToken ct)
    {
        var manualId = Guid.NewGuid().ToString("N")[..16];
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "training", "manuals");
        Directory.CreateDirectory(uploadsDir);
        var savePath = Path.Combine(uploadsDir, $"{manualId}{ext}");

        await using (var stream = File.Create(savePath))
            await file.CopyToAsync(stream, ct);

        var manual = new TrainingManual
        {
            ManualId = manualId,
            ManualName = file.FileName,
            FileType = ext,
            FilePath = savePath,
            UploadUser = userId,
            UploadTime = DateTime.Now,
            Status = "uploaded"
        };

        await _repo.InsertManualAsync(manual, ct);
        await _repo.InsertOperationLogAsync(userId, "upload_manual", file.FileName, ct);

        return new ManualDto
        {
            ManualId = manualId,
            ManualName = file.FileName,
            FileType = ext,
            UploadUser = userId,
            UploadTime = manual.UploadTime,
            Status = "uploaded"
        };
    }

    public async Task<List<ManualDto>> GetManualsAsync(CancellationToken ct)
    {
        var manuals = await _repo.GetManualsAsync(ct);
        var result = new List<ManualDto>();
        foreach (var m in manuals)
        {
            var chunkCount = await _repo.GetChunkCountByManualAsync(m.ManualId, ct);
            result.Add(new ManualDto
            {
                ManualId = m.ManualId,
                ManualName = m.ManualName,
                FileType = m.FileType,
                UploadUser = m.UploadUser,
                UploadTime = m.UploadTime,
                Status = m.Status,
                ChunkCount = chunkCount
            });
        }
        return result;
    }

    public async Task<ManualParseResultDto> ParseManualAsync(string manualId, string userId, CancellationToken ct)
    {
        var manual = await _repo.GetManualByIdAsync(manualId, ct)
            ?? throw new InvalidOperationException("手册不存在");
        var text = _parser.ParseDocument(manual.FilePath!);
        await _repo.InsertOperationLogAsync(userId, "parse_manual", manual.ManualName, ct);
        return new ManualParseResultDto { ManualId = manualId, Text = text, Length = text.Length };
    }

    public async Task<List<ManualChunkDto>> ChunkManualAsync(string manualId, string userId, CancellationToken ct)
    {
        var manual = await _repo.GetManualByIdAsync(manualId, ct)
            ?? throw new InvalidOperationException("手册不存在");
        var text = _parser.ParseDocument(manual.FilePath!);
        var chunks = _chunker.ChunkText(text, manualId);

        await _repo.DeleteChunksByManualAsync(manualId, ct);
        foreach (var ch in chunks)
            await _repo.InsertChunkAsync(ch, ct);

        await _repo.UpdateManualStatusAsync(manualId, "chunked", ct);
        await _repo.InsertOperationLogAsync(userId, "chunk_manual", $"{manual.ManualName} -> {chunks.Count} chunks", ct);

        var result = await _repo.GetChunksByManualAsync(manualId, ct);
        return result.Select(c => new ManualChunkDto
        {
            ChunkId = c.ChunkId,
            ManualId = c.ManualId,
            ChapterTitle = c.ChapterTitle,
            SectionNo = c.SectionNo,
            Content = c.Content,
            PageNo = c.PageNo,
            SystemName = c.SystemName,
            CreatedTime = c.CreatedTime
        }).ToList();
    }

    public async Task DeleteManualAsync(string manualId, string userId, CancellationToken ct)
    {
        var manual = await _repo.GetManualByIdAsync(manualId, ct)
            ?? throw new InvalidOperationException("手册不存在");

        // Check if knowledge points exist from this manual
        var chunks = await _repo.GetChunksByManualAsync(manualId, ct);
        foreach (var chunk in chunks)
        {
            var kpCount = await _repo.GetKnowledgeQuestionCountAsync(chunk.ChunkId, ct);
            if (kpCount > 0)
                throw new InvalidOperationException($"该手册已关联知识点，请先清理相关知识点后再删除。");
        }

        await _repo.DeleteChunksByManualAsync(manualId, ct);
        await _repo.DeleteManualAsync(manualId, ct);

        if (manual.FilePath != null && File.Exists(manual.FilePath))
            File.Delete(manual.FilePath);

        await _repo.InsertOperationLogAsync(userId, "delete_manual", manual.ManualName, ct);
    }

    public async Task<List<ManualChunkDto>> SearchChunksAsync(string keyword, string? manualId, CancellationToken ct)
    {
        var chunks = await _repo.SearchChunksAsync(keyword, manualId, ct);
        return chunks.Select(c => new ManualChunkDto
        {
            ChunkId = c.ChunkId,
            ManualId = c.ManualId,
            ChapterTitle = c.ChapterTitle,
            SectionNo = c.SectionNo,
            Content = c.Content,
            PageNo = c.PageNo,
            SystemName = c.SystemName,
            CreatedTime = c.CreatedTime
        }).ToList();
    }
}
