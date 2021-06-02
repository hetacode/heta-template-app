using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Commons.Utils;
using LibGit2Sharp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Minio;
using TemplateProcessorFunc.Models;

namespace TemplateProcessorFunc
{
    public class TemplateProcessorFunc
    {
        private const string BucketName = "templates";
        private readonly string MinioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        private readonly string MinioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESSKEY");
        private readonly string MinioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRETKEY");

        // https://github.com/hetacode/heta-template-app-data.git
        private readonly string CodeRepository = Environment.GetEnvironmentVariable("CODE_REPOSITORY");
        private readonly TemplateFuncDbContext _context;

        public TemplateProcessorFunc(TemplateFuncDbContext context)
            => _context = context;

        [Function("TemplateProcessorFunc")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
            HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("TemplateProcessorFunc");
            logger.LogInformation("start TemplateProcessorFunc");

            using var trans = await _context.Database.BeginTransactionAsync();
            using var sha = SHA256.Create();
            var repoHash = HashUtils.GetHash(sha, CodeRepository.Trim());
            var lastCommit = _context.Commits.OrderByDescending(o => o.CreatedAt).FirstOrDefault(l => l.RepoHash == repoHash)?.CommitHash ?? "";

            if (Directory.Exists("repos"))
            {
                Directory.Delete("repos", true);
            }
            Repository.Clone(CodeRepository, "repos/");
            using var repo = new Repository("repos/.git");
            var storage = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);

            // When first checkout
            if (string.IsNullOrEmpty(lastCommit))
            {
                lastCommit = repo.Head.Tip.Tree.Sha;

                var templatesDir = "repos/templates";
                var files = Directory.GetFiles(templatesDir);
                foreach (var f in files.Where(w => w.EndsWith(".template")))
                {
                    await storage.PutObjectAsync("templates", Path.GetFileName(f), f);
                }

                await _context.Commits.AddAsync(new Models.Commit { RepoHash = repoHash, CommitHash = lastCommit, CreatedAt = DateTime.Now });
            }
            else // Changes from last checkout
            {
                var commit = repo.Commits.FirstOrDefault(f => f.Sha == lastCommit);
                var newestCommitHash = repo.Head.Tip.Sha;
                var diffs = repo.Diff.Compare<TreeChanges>(commit.Tree, repo.Head.Tip.Tree);
                foreach (var c in diffs)
                {
                    if (!c.Path.StartsWith("templates"))
                    {
                        logger.LogInformation($"wrong path: {c.Path}");
                        continue;
                    }

                    switch (c.Status)
                    {
                        case ChangeKind.Modified:
                        case ChangeKind.Added:
                            logger.LogInformation($"{c.Status.ToString()} path: {c.Path}");
                            await storage.PutObjectAsync(BucketName, Path.GetFileName(c.Path), Path.Combine("repos", c.Path));
                            break;
                        case ChangeKind.Deleted:
                            logger.LogInformation($"{c.Status.ToString()} path: {c.Path}");
                            await storage.RemoveObjectAsync(BucketName, Path.GetFileName(c.Path));
                            break;
                        case ChangeKind.Renamed:
                            logger.LogInformation($"{c.Status.ToString()} old path: {c.OldPath} | new path: {c.Path}");
                            await storage.RemoveObjectAsync(BucketName, Path.GetFileName(c.OldPath));
                            await storage.PutObjectAsync(BucketName, Path.GetFileName(c.Path), Path.Combine("repos", c.Path));
                            break;
                        case ChangeKind.Unmodified:
                            break;
                        default:
                            throw new Exception($"Unimplemented status {c.Status} - commit: {newestCommitHash}");
                    }
                }

                await _context.Commits.AddAsync(new Models.Commit { RepoHash = repoHash, CommitHash = newestCommitHash, CreatedAt = DateTime.Now });
            }
            await _context.SaveChangesAsync();
            await trans.CommitAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);

            logger.LogInformation("end TemplateProcessorFunc");


            return response;
        }
    }
}
