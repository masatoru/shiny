﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Shiny.Push;
using Shiny.Testing.Push;
using Xunit;


namespace Shiny.Tests.Push
{
    public class FirebaseTests : IDisposable
    {
        readonly IPushTagSupport pushManager;



        public FirebaseTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = (services) =>
                {
                    services.UseFirebaseMessaging<TestPushDelegate>();
                }
            });

            this.pushManager = (IPushTagSupport)ShinyHost.Resolve<IPushManager>();
            
            var creds = GoogleCredential.FromJson(JsonConvert.SerializeObject(new TestGoogleCredential
            {
                ProjectId = Secrets.Values.GoogleCredentialProjectId,
                PrivateKeyId = Secrets.Values.GoogleCredentialPrivateKeyId,
                PrivateKey = Secrets.Values.GoogleCredentialPrivateKey,
                ClientId = Secrets.Values.GoogleCredentialClientId,
                ClientEmail = Secrets.Values.GoogleCredentialClientEmail,
                ClientCertUrl = Secrets.Values.GoogleCredentialClientCertUrl
            }));
            FirebaseApp.Create(new AppOptions
            {
                Credential = creds
            });
        }


        public void Dispose() => FirebaseApp.DefaultInstance.Delete();


        [Fact(DisplayName = "Push - Firebase - Receive Topic")]
        public Task ReceiveTopic() => this.WrapRegister(async () =>
        {
            var stamp = Guid.NewGuid().ToString();
            var task = this.ListenForStamp(stamp);

            await this.DoSend(stamp, Guid.NewGuid().ToString());
            await task;
        });


        [Fact(DisplayName = "Push - Firebase - Observable")]
        public Task ReceviedOnForegroundObservable() => this.WrapRegister(async () =>
        {
            var stamp = Guid.NewGuid().ToString();
            var task = this.ListenForStamp(stamp);

            await this.DoSend(stamp, null);
            await task;
        });


        [Fact(DisplayName = "Push - Firebase - Delegate")]
        public Task ReceivedOnDelegate() => WrapRegister(async () =>
        {
            var stamp = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<object?>();

            TestPushDelegate.Received = (data) =>
            {
                if (data.ContainsKey("stamp") && data["stamp"] == stamp)
                    tcs?.SetResult(null);
            };
            await FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = this.pushManager.CurrentRegistrationToken,
                Data = new Dictionary<string, string>
                {
                    { "stamp", stamp }
                }
            });
            await tcs.Task.WithTimeout(10);
            tcs = null;
        });



        Task ListenForStamp(string stamp) => this.pushManager
            .WhenReceived()
            .Take(1)
            .Where(x => x.ContainsKey("stamp") && x["stamp"] == stamp)
            .Timeout(TimeSpan.FromSeconds(5))
            .ToTask();


        async Task WrapRegister(Func<Task> innerTask)
        {
            
            await this.pushManager.RequestAccess();

            try
            {
                await innerTask();
            }
            finally
            {
                await this.pushManager.UnRegister();
            }

        }


        async Task DoSend(string stamp, string? topic)
        {
            var msg = new Message
            {
                Token = this.pushManager.CurrentRegistrationToken,
                Data = new Dictionary<string, string>
                {
                    { "stamp", stamp }
                }
            };
            if (!topic.IsEmpty())
            {
                msg.Topic = topic;
                await this.pushManager.AddTag(topic);
            }
            await FirebaseMessaging.DefaultInstance.SendAsync(msg);

            if (!topic.IsEmpty())
                await this.pushManager.ClearTags();
        }
    }
}
