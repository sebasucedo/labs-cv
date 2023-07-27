using Amazon.S3.Transfer;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;

namespace io.ucedo.labs.cv.ai
{
    public class S3Manager
    {
        const string DEFAULT_FOLDER = "cv/profile-pictures/";

        private readonly string _bucketName;
        private readonly string _awsAccessKey;
        private readonly string _awsSecretKey;

        public S3Manager(string bucketName, string awsAccessKey, string awsSecretKey)
        {
            _bucketName = bucketName;
            _awsAccessKey = awsAccessKey;
            _awsSecretKey = awsSecretKey;
        }

        public async Task<string> Upload(byte[] contenido)
        {
            try
            {
                var folder = DEFAULT_FOLDER;
                var filename = $"img-{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}.png";
                var key = $"{folder}{filename}";

                var amazonS3Client = new AmazonS3Client(_awsAccessKey, _awsSecretKey, RegionEndpoint.USEast1);
                var transferUtility = new TransferUtility(amazonS3Client);

                var request = new TransferUtilityUploadRequest
                {
                    BucketName = _bucketName,
                    StorageClass = S3StorageClass.Standard,
                    Key = key,
                    CannedACL = S3CannedACL.BucketOwnerFullControl,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                    InputStream = new MemoryStream(contenido),
                    AutoCloseStream = false,
                };

                await transferUtility.UploadAsync(request);

                return key;

            }
            catch (Exception ex)
            {
                LambdaLogger.Log($"Error uploading file to S3.\r\n{ex.Message}\r\n{ex.StackTrace}");
                return string.Empty;
            }
        }
    }
}
