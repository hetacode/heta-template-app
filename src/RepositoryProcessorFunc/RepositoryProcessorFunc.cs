using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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
        private readonly string MinioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        private readonly string MinioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESSKEY");
        private readonly string MinioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRETKEY");

        // https://github.com/hetacode/heta-template-app.git
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

            using var sha = SHA256.Create();
            var repoHash = GetHash(sha, CodeRepository);
            var lastCommit = _context.Commits.LastOrDefault(l => l.RepoHash == repoHash)?.CommitHash ?? "";

            if (Directory.Exists("repos"))
            {
                Directory.Delete("repos", true);
            }
            Repository.Clone(CodeRepository, "repos/");
            using var repo = new Repository("repos/.git");
            // When first checkout
            if (string.IsNullOrEmpty(lastCommit))
            {
                lastCommit = repo.Head.Tip.Tree.Sha;

                var storage = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);
                var inputsDir = "repos/inputs";
                var files = Directory.GetFiles(inputsDir);
                foreach (var f in files.Where(w => w.EndsWith(".yaml")))
                {
                    await storage.PutObjectAsync("inputs", Path.GetFileName(f), f);
                }
            }
            else // Changes from last checkout
            {
                foreach (var c in repo.Diff.Compare<TreeChanges>(repo.Lookup<Tree>(lastCommit), repo.Head.Tip.Tree))
                {
                    logger.LogInformation($"change: {c.OldPath} - {c.Path}");
                    // TOOD: take only new files paths: inputs/*
                }
            }


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
