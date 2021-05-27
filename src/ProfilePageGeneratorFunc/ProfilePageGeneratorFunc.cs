using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Commons;
using Commons.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Minio;

namespace ProfilePageGeneratorFunc
{
    public class ProfilePageGeneratorFunc
    {
        private readonly string MinioEndpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        private readonly string MinioAccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESSKEY");
        private readonly string MinioSecretKey = Environment.GetEnvironmentVariable("MINIO_SECRETKEY");


        [Function("ProfilePageGeneratorFunc")]
        public async Task Run(
            [KafkaTrigger("KAFKA_BROKERS", "input-to-process-profile-page", ConsumerGroup="input-processor-group")]
            KafkaMessage ev,
            FunctionContext context)
        {
            var logger = context.GetLogger("ProfilePageGeneratorFunc");
            logger.LogInformation($"Start ProfilePageGeneratorFunc");

            var msg = JsonSerializer.Deserialize<InputToProcessEvent>(ev.Value);
            if (msg is null)
            {
                logger.LogError($"value of event is empty | timestamp: {ev.Timestamp}");
                return;
            }

            var storageClient = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);
            // 1, Download input file from S3 bucket (minio)
            var input = await DownloadInputData(logger, storageClient, msg.bucketName, msg.objectName);
            logger.LogInformation($"processing {input.template} input");
            // 2. Download html template from s3 repo (should sync by other periodic function)
            var template = await GetTemplate(input.template);
            // 3. Combine these two sources into one html file
            var output = GenerateOutput(input.data, template);
            // 4. Put generated file to the S3
            await UploadGeneratedOutput(storageClient, output, input.template);

            logger.LogInformation($"end ProfilePageGeneratorFunc");
        }

        private async Task UploadGeneratedOutput(MinioClient storage, string output, string namePrefix)
        {
            var bytes = Encoding.UTF8.GetBytes(output);
            using var ms = new MemoryStream();
            await ms.WriteAsync(bytes, 0, bytes.Length, CancellationToken.None);
            ms.Seek(0, SeekOrigin.Begin);
            var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            await storage.PutObjectAsync("outputs", $"{namePrefix}_{currentTime}.html", ms, ms.Length, contentType: "text/html");
        }

        public async Task<InputTemplate<ProfilePageTemplateData>> DownloadInputData(ILogger logger, MinioClient storage, string bucketName, string objectName)
        {
            using var ms = new MemoryStream();
            await storage.GetObjectAsync(bucketName, objectName, async s =>
                {
                    await s.CopyToAsync(ms);
                });
            ms.Seek(0, SeekOrigin.Begin);

            using var sr = new StreamReader(ms);
            var yamlDeserializer = new YamlDotNet.Serialization.Deserializer();
            var profilePageData = yamlDeserializer.Deserialize<InputTemplate<ProfilePageTemplateData>>(sr);

            return profilePageData;
        }

        public async Task<string> GetTemplate(string templateName)
        {
            using var ms = new MemoryStream();
            var storage = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);
            await storage.GetObjectAsync("templates", templateName + ".template", async s =>
                {
                    await s.CopyToAsync(ms);
                });
            ms.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(ms);
            var template = sr.ReadToEnd();

            return template;
        }

        public string GenerateOutput(ProfilePageTemplateData input, string template)
        {
            template = template.Replace("{{first_name}}", input.first_name);
            template = template.Replace("{{last_name}}", input.last_name);
            template = template.Replace("{{age}}", input.age.ToString());
            template = template.Replace("{{profession}}", input.profession);
            return template;
        }
    }

}
