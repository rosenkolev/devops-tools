using System;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.CommandLineUtils;

namespace Face.Presentation.App
{
    public class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication(true);
            app.HelpOption("-h --help");
            app.VersionOption("-v --version", "1", "1.0");
            app.Command("create", c =>
            {
                c.HelpOption("-h --help");
                var group = c.Option("-g --group <NAME>", "The person group id", CommandOptionType.SingleValue);
                var name = c.Option("-n --name <NAME>", "The person name", CommandOptionType.SingleValue);
                var path = c.Option("-p --path <NAME>", "The path to the folder with the images to upload.", CommandOptionType.SingleValue);
                var key = c.Option("-k --key <NAME>", "The azure subscription key", CommandOptionType.SingleValue);
                var endpoint = c.Option("-e --endpoint", "The api endpoint", CommandOptionType.SingleValue);
                c.OnExecute(async () => 
                {
                    if (!group.HasValue() || !name.HasValue() || !path.HasValue() || !key.HasValue() || !endpoint.HasValue())
                    {
                        Console.WriteLine("Group, name, path, key and endpoint are non optional parametars.");
                        return 1;
                    }

                    var ok = await UploadImages(group.Value(), name.Value(), path.Value(), CreateClient(key.Value(), endpoint.Value()));
                    return ok ? 0 : 1;
                });
            });

            app.Command("delete", c =>
            {
                c.HelpOption("-h --help");
                 var group = c.Option("-g --group <NAME>", "The person group id", CommandOptionType.SingleValue);
                var name = c.Option("-n --name <NAME>", "The person name", CommandOptionType.SingleValue);
                var key = c.Option("-k --key <NAME>", "The azure subscription key", CommandOptionType.SingleValue);
                var endpoint = c.Option("-e --endpoint", "The api endpoint", CommandOptionType.SingleValue);
                c.OnExecute(async () => 
                {
                    await DeleteUser(group.Value(), name.Value(), CreateClient(key.Value(), endpoint.Value()));
                    return 0;
                });
            });

            app.Command("list", cb =>
            {
                cb.HelpOption("-h --help");
                cb.FullName  = "List information about the account";
                cb.Command("groups", c =>
                {
                    c.HelpOption("-h --help");
                    c.FullName  = "List all person groups";
                    var key = c.Option("-k --key <NAME>", "The azure subscription key", CommandOptionType.SingleValue);
                    var endpoint = c.Option("-e --endpoint", "The api endpoint", CommandOptionType.SingleValue);
                    c.OnExecute(async () => 
                    {
                        var groups = await CreateClient(key.Value(), endpoint.Value()).PersonGroup.ListAsync();
                        foreach (var group in groups)
                        {
                            Console.WriteLine($"{group.PersonGroupId} - name:{group.Name}");
                        }

                        return 0;
                    });
                });

                cb.Command("persons", c =>
                {
                    c.HelpOption("-h --help");
                    c.FullName  = "List all persons in group";
                    var group = c.Option("-g --group <NAME>", "The person group id", CommandOptionType.SingleValue);
                    var key = c.Option("-k --key <NAME>", "The azure subscription key", CommandOptionType.SingleValue);
                    var endpoint = c.Option("-e --endpoint", "The api endpoint", CommandOptionType.SingleValue);
                    c.OnExecute(async () => 
                    {
                        var persons = await CreateClient(key.Value(), endpoint.Value()).PersonGroupPerson.ListAsync(group.Value());
                        foreach (var person in persons)
                        {
                            Console.WriteLine($"{person.PersonId} - name:{person.Name} - data:{person.UserData} - id:{string.Join(",", person.PersistedFaceIds)}");
                        }

                        return 0;
                    });
                });
            });

            app.Execute(args);
        }

        private static IFaceClient CreateClient(string azureSubscriptionKey, string endpoint) =>
            new FaceClient(new ApiKeyServiceClientCredentials(azureSubscriptionKey), new DelegatingHandler[0]) { Endpoint = endpoint };

        private static async Task<bool> UploadImages(string personGroupId, string personName, string pathToImages, IFaceClient faceClient)
        {
            Console.WriteLine("Craete person group " + personGroupId);
            var personGroup = await faceClient.PersonGroup.GetAsync(personGroupId);
            if (personGroup != null)
            {
                await DoSafe(faceClient, c => c.PersonGroup.CreateAsync(personGroupId, personGroupId));
            }

            // Create person
            Console.WriteLine("Create person " + personName);
            var person = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, personName);
            
            // Add images
            foreach (string imagePath in Directory.GetFiles(pathToImages, "*.jpg"))
            {
                using (var image = File.OpenRead(imagePath))
                {
                    Console.WriteLine("Uploading" + imagePath);
                    await DoSafe(faceClient, c => c.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, image));
                }
            }

            await faceClient.PersonGroup.TrainAsync(personGroupId);

            // Wait for train to finish
            while(true)
            {
                var trainingStatus = await faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId);
                Console.WriteLine("Training");
                if (trainingStatus.Status != TrainingStatusType.Running) break;

                await Task.Delay(1000);
            } 

            return true;
        }

        private static async Task DeleteUser(string personGroupId, string personName, IFaceClient faceClient)
        {
            var persons = await faceClient.PersonGroupPerson.ListAsync(personGroupId);
            var currentPersons = persons.Where(it => it.Name.Equals(personName, StringComparison.InvariantCultureIgnoreCase));
            foreach(var person in currentPersons)
            {
                await faceClient.PersonGroupPerson.DeleteAsync(personGroupId, person.PersonId);
            }
        }

        private static async Task DoSafe(IFaceClient faceClient, Func<IFaceClient, Task> action)
        {
            try
            {
                await action(faceClient);
            }
            catch (APIErrorException ex)
            {
                Console.WriteLine("Azure API Error "+ ex.Response.StatusCode.ToString());
                Console.WriteLine("Message:" + ex.Message);
                Console.WriteLine("Reason:" + ex.Response.ReasonPhrase);
                Console.WriteLine("Reason:" + ex.Response.Content);
            }
        }
    }
}
