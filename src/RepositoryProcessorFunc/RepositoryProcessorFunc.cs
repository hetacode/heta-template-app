using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Minio;
using RepositoryProcessorFunc.Models;

namespace RepositoryProcessorFunc
{
    public class RepositoryProcessorFunc
    {
        private const string BucketName = "inputs";
        private readonly string MinioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        private readonly string MinioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESSKEY");
        private readonly string MinioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRETKEY");

        // https://github.com/hetacode/heta-template-app-data.git
        private readonly string CodeRepository = Environment.GetEnvironmentVariable("CODE_REPOSITORY");

        private RepositoryFuncDbContext _context;

        public RepositoryProcessorFunc(RepositoryFuncDbContext context)
            => _context = context;


        [Function("RepositoryProcessorFunc")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("RepositoryProcessorFunc");
            logger.LogInformation("start");

            using var trans = await _context.Database.BeginTransactionAsync();
            using var sha = SHA256.Create();
            var repoHash = GetHash(sha, CodeRepository.Trim());
            var lastCommit = _context.Commits.OrderByDescending(o => o.CreatedAt).FirstOrDefault(l => l.RepoHash == repoHash)?.CommitHash ?? "";

            if (Directory.Exists("repos"))
            {
                Directory.Delete("repos", true);
            }
            Repository.Clone(CodeRepository, "repos/", new CloneOptions { BranchName = "master" });
            using var repo = new Repository("repos/.git");

            var storage = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);
            // When first checkout
            if (string.IsNullOrEmpty(lastCommit))
            {
                lastCommit = repo.Head.Tip.Sha;


                var inputsDir = "repos/inputs";
                var files = Directory.GetFiles(inputsDir);
                foreach (var f in files.Where(w => w.EndsWith(".yaml")))
                {
                    await storage.PutObjectAsync(BucketName, Path.GetFileName(f), f);
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
                    if (!c.Path.StartsWith("inputs"))
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
            return response;
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
