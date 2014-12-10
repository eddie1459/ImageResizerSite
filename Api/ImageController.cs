using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using ImageResizer.Util;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using WebGrease.Css.Extensions;

namespace ImageResizerSite.Api
{
    [RoutePrefix("api/image")]
    public class ImageController : ApiController {
        static CloudBlobClient cloudBlobClient;
        [Route("UploadImage")]
        [HttpPost]
        public HttpResponseMessage UploadImage() {
            
            var httpPostedFile = HttpContext.Current.Request.Files["UploadedImage"];

            if (httpPostedFile.ContentLength <= 0) return Request.CreateResponse(HttpStatusCode.BadRequest);
            SetContainerAndPermissions();

            try {
                var cloudBlobContainer = cloudBlobClient.GetContainerReference("imageresizer");
                string filename = Guid.NewGuid() + PathUtils.GetExtension(httpPostedFile.FileName.ToLower());

                var blob = cloudBlobContainer.GetBlockBlobReference(filename);
                blob.Properties.ContentType = httpPostedFile.ContentType;
                blob.UploadFromStream(httpPostedFile.InputStream);
            } catch (Exception ex) {
                throw new Exception("Error while uploading the image: " + ex.Message);
            }
            
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [Route("GetImages")]
        [HttpGet]
        public HttpResponseMessage GetImages() {
            SetContainerAndPermissions();

            var cloudBlobContainer = cloudBlobClient.GetContainerReference("imageresizer");
            var blobs = cloudBlobContainer.ListBlobs();

            var imgList = from b in blobs
                          select b.Uri.AbsolutePath;

            return Request.CreateResponse(HttpStatusCode.OK, imgList);
        }

        private void SetContainerAndPermissions() {
            try
            {
                // Creating the container
                var cloudStorageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = cloudBlobClient.GetContainerReference("imageresizer");
                blobContainer.CreateIfNotExists();

                var containerPermissions = blobContainer.GetPermissions();
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissions(containerPermissions);
            }
            catch (Exception Ex)
            {
                throw new Exception("Error while creating the container: " + Ex.Message);
            }
        }
    }
}
