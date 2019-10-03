using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Face.Presentation.App
{
    public sealed class AzureFaceClient
    {
        private readonly IFaceClient _client;

        public AzureFaceClient(IFaceClient client)
        {
            _client = client;
        }

        private static string PersonGroupName => "colleagues";

        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        public static Stream GetStreamFromImage(Bitmap image)
        {
            var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Jpeg);
            image.Save("/home/pi/test.jpg", ImageFormat.Jpeg);
            return stream;
        }

        public static async Task DetectFaceExtract(IFaceClient client, Stream stream)
        {
            Console.WriteLine("Azure detect faces");
            var faces = await client.Face.DetectWithStreamAsync(
                stream,
                recognitionModel: "recognition_02",
                detectionModel: "detection_02");

            Console.WriteLine($"{faces.Count} face(s) detected.");
            var faceIdList = faces.Where(it => it.FaceId.HasValue).Select(it => it.FaceId.Value).ToList();
            var founds = await client.Face.IdentifyAsync(faceIdList, PersonGroupName);
            Console.WriteLine($"User {founds.Count} identifierd.");
        }

        public async Task Identify(Bitmap image)
        {
            var stream = GetStreamFromImage(image);
            await DetectFaceExtract(_client, stream);
        }
    }
}