﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Contents;
using Lime.Protocol;
using Newtonsoft.Json;
using Takenet.MessagingHub.Client;

namespace Buscape
{
    class Program
    {
        static void Main(string[] args)
        {
            StartListenningAndWaitForCancellation(args).Wait();
        }

        static async Task StartListenningAndWaitForCancellation(string[] args)
        {
            var config = ValidateAndLoadParameters(args);

            var webClient = PrepareWebClient(config.buscapeAppToken);

            var receiver = await PrepareReceiverAsync(config.messagingHubApplicationShortName, config.messagingHubApplicationAccessKey);

            Console.WriteLine();
            Console.WriteLine(@"Press any key to continue...");
            Console.ReadKey();

            if (receiver == null)
                return;

            using (var cts = new CancellationTokenSource())
            {

                StartReceivingMessages(cts, receiver, webClient, config.buscapeAppToken);

                await WaitForUserCancellationAsync(cts, receiver);
            }
        }

        private const string ConnectedMessage = "$Connected$";
        private const string StartMessage = "Iniciar";
        //private static read only MediaType ResponseMediaType = new MediaType("application", "vnd.omni.text", "json");

        private static JsonConfigFile ValidateAndLoadParameters(string[] args)
        {
            if (args.Length > 1)
                Console.WriteLine(@"The only argument supplied must be a JSON configuration file path!");

            JsonConfigFile jsonConfigFile = null;

            var file = args.Length == 0 ? new FileInfo("config.json") : new FileInfo(args[0]);

            try
            {
                var json = File.ReadAllText(file.FullName);
                jsonConfigFile = JsonConvert.DeserializeObject<JsonConfigFile>(json);
            }
            catch (Exception)
            {
                Console.WriteLine(@"Could not load configuration file!");
                Environment.Exit(1);
            }

            return jsonConfigFile;
        }

        private static HttpClient PrepareWebClient(string appToken)
        {
            var webClient = new HttpClient();
            webClient.DefaultRequestHeaders.Add("app-token", appToken);

            return webClient;
        }

        private static async Task<IMessagingHubClient> PrepareReceiverAsync(string appShortName, string accessKey)
        {
            try
            {
                Console.WriteLine(@"Trying to connect to Messaging Hub...");

                var receiverBuilder = new MessagingHubClientBuilder();
                receiverBuilder = receiverBuilder.UsingAccessKey(appShortName, accessKey);
                receiverBuilder = receiverBuilder.WithSendTimeout(TimeSpan.FromSeconds(30));
                var receiver = receiverBuilder.Build();
                await receiver.StartAsync();

                //Send Message to confirm connection
                await receiver.SendMessageAsync(new Message
                {
                    To = Node.Parse(appShortName),
                    Content = new PlainDocument(ConnectedMessage, MediaTypes.PlainText)
                });

                Console.WriteLine($"{DateTime.Now} -> Receiver connected to Messaging Hub!");

                return receiver;
            }
            catch (Exception)
            {
                Console.WriteLine($"{DateTime.Now} -> Could not connect to Messaging Hub!");
                return null;
            }
        }

        private static async Task WaitForUserCancellationAsync(CancellationTokenSource cts, IMessagingHubClient receiver)
        {
            Console.ReadKey();

            Console.WriteLine($"{DateTime.Now} -> Stopping service...");

            cts.Cancel();

            await receiver.StopAsync();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit!");
            Console.ReadKey();
        }

        private static void StartReceivingMessages(CancellationTokenSource cts, IMessagingHubClient receiver, HttpClient webClient, string appToken)
        {
            receiver.StartReceivingMessages(cts, async message =>
            {
                try
                {
                    await receiver.SendNotificationAsync(new Notification
                    {
                        Id = message.Id,
                        Event = Event.Received,
                        To = message.From
                    });

                    await receiver.SendNotificationAsync(new Notification
                    {
                        Id = message.Id,
                        Event = Event.Consumed,
                        To = message.From
                    });


                    var keyword = ((PlainText)message.Content)?.Text;

                    if (keyword == ConnectedMessage)
                    {
                        Console.WriteLine("Connected!");
                    }
                    else if (keyword == StartMessage)
                    {
                        Console.WriteLine($"Start message received from {message.From.Instance}!");
                        await receiver.SendMessageAsync(@"Tudo pronto. Qual produto deseja pesquisar?", message.From);
                    }
                    else
                    {
                        Console.WriteLine($"Requested search by {keyword}!");
                        var uri =
                            $"http://sandbox.buscape.com.br/service/findProductList/lomadee/{appToken}/BR?results=10&page=1&keyword={keyword}&format=json";

                        using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                        {
                            var response = await webClient.SendAsync(request, cts.Token);
                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                await receiver.SendMessageAsync(@"Falhou :(", message.From);
                            }
                            else
                            {
                                var resultJson = await response.Content.ReadAsStringAsync();
                                dynamic responseMessage = JsonConvert.DeserializeObject(resultJson);
                                foreach (var product in responseMessage.product)
                                    await
                                        receiver.SendMessageAsync(
                                            $"{product.productshortname} de {product.pricemin} até {product.pricemax}",
                                            message.From);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    await receiver.SendMessageAsync(@"Falhou :(", message.From);
                }
            });
            Console.WriteLine(@"Listening...");
        }
    }
}
