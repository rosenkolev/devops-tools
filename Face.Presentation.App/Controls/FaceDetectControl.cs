using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Logging;

namespace Face.Presentation.App.Controls
{
    public sealed class FaceDetectControl : IDisposable
    {
        private readonly IFaceClient _client;
        private readonly ILogger _logger;

        public FaceDetectControl(ILogger logger, IFaceClient client)
        {
            _client = client;
            _logger = logger;
        }

        public FaceDetectControl(ILogger logger, string key, string region)
            : this(logger, Authenticate($"https://{region}.api.cognitive.microsoft.com", key))
        {
        }

        private static string PersonGroupName => "mentormate";

        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        public async Task<NameAndUserDataContract> IdentifyAsync(string pathToFile)
        {
            using var stream = File.OpenRead(pathToFile);
            var faceIdList = await DetectFaceIds(_client, stream);
            if (faceIdList.Count > 0)
            {
                return await IdentifyPersonsAsync(_client, faceIdList);
            }

            return null;
        }

        private async Task<IList<Guid>> DetectFaceIds(IFaceClient client, Stream stream)
        {
            _logger.LogDebug("Azure detect faces");
            
            try
            {
                var faces = await client.Face.DetectWithStreamAsync(stream);
                _logger.LogDebug($"{faces.Count} face(s) detected.");
                return faces.Where(it => it.FaceId.HasValue).Select(it => it.FaceId.Value).ToList();
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex.Response.ReasonPhrase);
                _logger.LogError(ex.Response.Content);
                throw;
            }
        }

        private async Task<NameAndUserDataContract> IdentifyPersonsAsync(IFaceClient client, IList<Guid> faceIdList)
        {
            var founds = await client.Face.IdentifyAsync(faceIdList, PersonGroupName, confidenceThreshold: 0.7);
            _logger.LogDebug($"Identified {founds.Count} persons.");
            foreach (var identifyResult in founds)
            {
                _logger.LogDebug("Result of face: {0}", identifyResult.FaceId);
                if (identifyResult.Candidates.Count == 0)
                {
                    _logger.LogDebug("No one identified");
                }
                else
                {
                    // Get top 1 among all candidates returned
                    var candidateId = identifyResult.Candidates[0].PersonId;
                    var person = await client.PersonGroupPerson.GetAsync(PersonGroupName, candidateId);
                    return person;
                }
            }

            return null;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}