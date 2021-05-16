namespace Commons.Events
{
    public record InputToProcessEvent(string bucketName, string objectName);
}