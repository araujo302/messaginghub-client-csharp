﻿using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lime.Protocol;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Takenet.MessagingHub.Client.Receivers;

namespace Takenet.MessagingHub.Client.AcceptanceTests
{
    [TestFixture]
    internal class AutomaticNotificationsTest
    {
        [Test]
        public async Task TestAcceptedNotificationIsSentAfterMessageIsReceived()
        {
            string appShortName1, appShortName2;
            var client1 = GetClientForNewApplication(out appShortName1);
            var client2 = GetClientForNewApplication(out appShortName2);
            try
            {
                await client1.SendMessageAsync(Beat, appShortName2);
                var notification = await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());

                notification.ShouldNotBeNull();
                notification.Event.ShouldBe(Event.Accepted);
            }
            finally
            {
                await client1.StopAsync();
                await client2.StopAsync();
            }
        }

        [Test]
        public async Task TestDispatchedNotificationIsSentAfterMessageIsReceived()
        {
            string appShortName1, appShortName2;
            var client1 = GetClientForNewApplication(out appShortName1);
            var client2 = GetClientForNewApplication(out appShortName2);
            try
            {
                await client1.SendMessageAsync(Beat, appShortName2);

                await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());
                var notification = await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());

                notification.ShouldNotBeNull();
                notification.Event.ShouldBe(Event.Dispatched);
            }
            finally
            {
                await client1.StopAsync();
                await client2.StopAsync();
            }
        }

        [Test]
        public async Task TestReceivedNotificationIsSentAfterMessageIsReceived()
        {
            string appShortName1, appShortName2;
            var client1 = GetClientForNewApplication(out appShortName1);
            var client2 = GetClientForNewApplication(out appShortName2);
            try
            {
                await client1.SendMessageAsync(Beat, appShortName2);
                await client2.ReceiveMessageAsync(GetNewReceiveTimeoutCancellationToken());

                await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());
                await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());
                var notification = await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());

                notification.ShouldNotBeNull();
                notification.Event.ShouldBe(Event.Received);
            }
            finally
            {
                await client1.StopAsync();
                await client2.StopAsync();
            }
        }

        [Test]
        public async Task TestFailedNotificationIsSentAfterMessageIsReceived()
        {
            string appShortName1, appShortName2;
            var client1 = GetClientForNewApplication(out appShortName1);
            var client2 = GetClientForNewApplication(out appShortName2, m => { throw new Exception(); });
            try
            {
                await client1.SendMessageAsync(Beat, appShortName2);
                await client2.ReceiveMessageAsync(GetNewReceiveTimeoutCancellationToken());

                var notification = await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());

                notification.ShouldNotBeNull();
                notification.Event.ShouldBe(Event.Failed);
            }
            finally
            {
                await client1.StopAsync();
                await client2.StopAsync();
            }
        }

        [Test]
        public async Task TestConsumedNotificationIsSentAfterMessageIsReceived()
        {
            string appShortName1, appShortName2;
            var client1 = GetClientForNewApplication(out appShortName1);
            var client2 = GetClientForNewApplication(out appShortName2);
            try
            {
                await client1.SendMessageAsync(Beat, appShortName2);
                await client2.ReceiveMessageAsync(GetNewReceiveTimeoutCancellationToken());

                await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());
                await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());
                await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());
                var notification = await client1.ReceiveNotificationAsync(GetNewReceiveTimeoutCancellationToken());

                notification.ShouldNotBeNull();
                notification.Event.ShouldBe(Event.Consumed);
            }
            finally
            {
                await client1.StopAsync();
                await client2.StopAsync();
            }
        }

        private const string Beat = "Beat";

        private static CancellationToken GetNewReceiveTimeoutCancellationToken()
        {
            return new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
        }

        private static IMessagingHubClient GetClientForNewApplication(out string appShortName, Action<Message> onMessageReceived = null)
        {
            appShortName = CreateAndRegisterApplicationAsync().Result;
            var appAccessKey = GetApplicationAccessKeyAsync(appShortName).Result;
            var client = GetClientForApplicationAsync(appShortName, appAccessKey, onMessageReceived).Result;
            return client;
        }

        private static async Task<IMessagingHubClient> GetClientForApplicationAsync(string appShortName, string appAccessKey, Action<Message> onMessageReceived = null)
        {
            var builder = new MessagingHubClientBuilder()
                .UsingHostName("hmg.msging.net")
                .UsingAccessKey(appShortName, appAccessKey)
                .WithSendTimeout(TimeSpan.FromSeconds(2));

            if (onMessageReceived != null)
                builder.AddMessageReceiver(new LambdaMessageReceiver(onMessageReceived));

            var client = builder.Build();
            await client.StartAsync();
            return client;
        }

        private static HttpClient _httpClient;
        private static HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "cCZkQHRha2VuZXQuY29tLmJyOlRAazNuM3Q=");
                }
                return _httpClient;
            }
        }

        private static async Task<string> GetApplicationAccessKeyAsync(string appShortName)
        {
            var uri = $"http://hmg.api.messaginghub.io/applications/{appShortName}";
            var response = await HttpClient.GetAsync(uri);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            dynamic application = JsonConvert.DeserializeObject(content);
            return application.accessKey;
        }

        private static async Task<string> CreateAndRegisterApplicationAsync()
        {
            var uri = "http://hmg.api.messaginghub.io/applications/";
            dynamic application = CreateApplication();
            var json = JsonConvert.SerializeObject(application);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var response = await HttpClient.PostAsync(uri, content);
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
            }
            return application.shortName;
        }

        private static object CreateApplication()
        {
            var id = "takeQAApp" + DateTime.UtcNow.Ticks;
            return new
            {
                shortName = id,
                name = id
            };
        }
    }

    internal class LambdaMessageReceiver : MessageReceiverBase
    {
        public Action<Message> OnMessageReceived { get; set; }

        public LambdaMessageReceiver(Action<Message> onMessageReceived)
        {
            OnMessageReceived = onMessageReceived;
        }

        public override Task ReceiveAsync(Message message)
        {
            OnMessageReceived?.Invoke(message);
            return Task.CompletedTask;
        }
    }
}
