using System;
using System.IO;
using System.Text.Json;
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


            // TODO: 
            // 1, Download input file from S3 bucket (minio)
            var input  = await DownloadInputData(msg.bucketName, msg.objectName);
            // 2. Download html template from git repo
            // 3. Combine these two sources into one html file
            // 4. Put generated file to the S3
        }

        public async Task<InputTemplate<ProfilePageTemplateData>> DownloadInputData(string bucketName, string objectName)
        {
            using var ms = new MemoryStream();
            var storage = new MinioClient(MinioEndpoint, MinioAccessKey, MinioSecretKey);
            await storage.GetObjectAsync(bucketName, objectName, async s => 
                {
                    await s.CopyToAsync(ms);
                });
            
            var yamlDeserializer = new YamlDotNet.Serialization.Deserializer();
            var profilePageData = yamlDeserializer.Deserialize<InputTemplate<ProfilePageTemplateData>>(new StreamReader(ms));

            throw new Exception("unimplemented");
        }
    }

}
