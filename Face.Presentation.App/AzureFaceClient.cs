using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        private static async Task<IList<Guid>> DetectFaceIds(IFaceClient client, Stream stream)
        {
            Console.WriteLine("Azure detect faces");
            
            try
            {
                var faces = await client.Face.DetectWithStreamAsync(stream);
                Console.WriteLine($"{faces.Count} face(s) detected.");
                return faces.Where(it => it.FaceId.HasValue).Select(it => it.FaceId.Value).ToList();
            }
            catch (APIErrorException ex)
            {
                Console.WriteLine(ex.Response.ReasonPhrase);
                Console.WriteLine(ex.Response.Content);
                throw;
            }
        }

        private static async Task<NameAndUserDataContract> IdentifyPersonsAsync(IFaceClient client, IList<Guid> faceIdList)
        {
            var founds = await client.Face.IdentifyAsync(faceIdList, PersonGroupName, confidenceThreshold: 0.7);
            Console.WriteLine($"Identified {founds.Count} persons.");
            foreach (var identifyResult in founds)
            {
                Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                if (identifyResult.Candidates.Count == 0)
                {
                    Console.WriteLine("No one identified");
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
    }
}