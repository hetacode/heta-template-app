using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Minio;

namespace InputProcessorFunc
{
    public static class InputProcessorFunc
    {
        [Function("InputProcessorFunc")]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("InputProcessorFunc");
            logger.LogInformation("start");

            var lastCommit = "";

            Repository.Clone("https://github.com/hetacode/heta-template-app.git", "repos/");
            using var repo = new Repository("repos/.git");
            // When first checkout
            if (string.IsNullOrEmpty(lastCommit))
            {
                lastCommit = repo.Head.Tip.Tree.Sha;

                var storage = new MinioClient("localhost:9000", "minioadmin", "minioadmin");
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
    }
}
