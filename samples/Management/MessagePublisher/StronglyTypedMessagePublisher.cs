// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.SignalR.Samples.Management
{
    public class StronglyTypedMessagePublisher : IMessagePublisher
    {
        private const string HubName = "StronglyTypedHub";
        private readonly string _connectionString;
        private readonly ServiceTransportType _serviceTransportType;
        private ServiceHubContext<IMessageClient> _hubContext;

        public StronglyTypedMessagePublisher(string connectionString, ServiceTransportType serviceTransportType)
        {
            _connectionString = connectionString;
            _serviceTransportType = serviceTransportType;
        }

        public async Task InitAsync()
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = _connectionString;
                option.ServiceTransportType = _serviceTransportType;
            })
            //Uncomment the following line to get more logs
            .WithLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .BuildServiceManager();

            _hubContext = await serviceManager.CreateHubContextAsync<IMessageClient>(HubName, default);
        }


        public Task ManageUserGroup(string command, string userId, string groupName)
        {
            switch (command)
            {
                case "add":
                    return _hubContext.UserGroups.AddToGroupAsync(userId, groupName);
                case "remove":
                    return _hubContext.UserGroups.RemoveFromGroupAsync(userId, groupName);
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return Task.CompletedTask;
            }
        }

        public Task SendMessages(string command, string receiver, string message)
        {
            switch (command)
            {
                case "broadcast":
                    return _hubContext.Clients.All.Target(message);
                case "user":
                    var userId = receiver;
                    return _hubContext.Clients.User(userId).Target(message);
                case "users":
                    var userIds = receiver.Split(',');
                    return _hubContext.Clients.Users(userIds).Target(message);
                case "group":
                    var groupName = receiver;
                    return _hubContext.Clients.Group(groupName).Target(message);
                case "groups":
                    var groupNames = receiver.Split(',');
                    return _hubContext.Clients.Groups(groupNames).Target(message);
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return Task.CompletedTask;
            }
        }

        public Task CloseConnection(string connectionId, string reason)
        {
            return _hubContext.ClientManager.CloseConnectionAsync(connectionId, reason);
        }

        public Task<bool> CheckExist(string type, string id)
        {
            return type switch
            {
                "connection" => _hubContext.ClientManager.ConnectionExistsAsync(id),
                "user" => _hubContext.ClientManager.UserExistsAsync(id),
                "group" => _hubContext.ClientManager.UserExistsAsync(id),
                _ => throw new NotSupportedException(),
            };
        }

        public Task DisposeAsync() => _hubContext?.DisposeAsync().AsTask();
    }
}