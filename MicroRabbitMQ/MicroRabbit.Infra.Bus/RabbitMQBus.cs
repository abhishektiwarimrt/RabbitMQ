﻿using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MicroRabbit.Infra.Bus
{
    public sealed class RabbitMQBus : IEventBus
    {
        private readonly IMediator mediator;
        private readonly Dictionary<string, List<Type>> handlers;
        private readonly List<Type> eventTypes;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public RabbitMQBus(IMediator mediator, IServiceScopeFactory serviceScopeFactory)
        {
            this.mediator = mediator;
            this.serviceScopeFactory = serviceScopeFactory;
            handlers = new Dictionary<string, List<Type>>();
            eventTypes = new();
        }
        public Task SendCommand<T>(T command) where T : Command
        {
            return mediator.Send(command);
        }

        public void Publish<T>(T @event) where T : Event
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                var eventName = @event.GetType().Name;
                channel.QueueDeclare(eventName, false, false, false, null);

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("", eventName, null, body);
            }
        }       

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
           var eventName = typeof(T).Name;
            var handlerType= typeof(TH);

            if (!eventTypes.Contains(typeof(T)))
            {
                eventTypes.Add(typeof(T));
            }

            if (!handlers.ContainsKey(eventName))
            {
                handlers.Add(eventName, new List<Type>());
            }

            if (handlers[eventName].Any(s => s.GetType() == handlerType))
            {
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already is registered for  '{eventName}'", nameof(handlerType));
            }

            handlers[eventName].Add(handlerType);

            StartBasicConsumer<T>();


        }

        private void StartBasicConsumer<T>() where T : Event
        {
            var factory = new ConnectionFactory()
            { 
                HostName = "localhost",
                DispatchConsumersAsync = true 
            };

            using var connection = factory.CreateConnection();
            using (var channel = connection.CreateModel())
            {
                var eventName = typeof(T).Name;
                channel.QueueDeclare(eventName, false, false, false, null);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += Consumer_Received;

                channel.BasicConsume(eventName, true, consumer);
              
            }
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;
            string message = Encoding.UTF8.GetString(e.Body.ToArray());

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }catch(Exception ex)
            {

            }
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (handlers.ContainsKey(eventName))
            {
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var subscriptions = handlers[eventName];
                    foreach (var subscription in subscriptions)
                    {
                        var handler = scope.ServiceProvider.GetService(subscription);
                        if (handler == null) continue;
                        var eventType = eventTypes.SingleOrDefault(t => t.Name == eventName);
                        var @event = JsonConvert.DeserializeObject(message, eventType);
                        var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });
                    }
                }
                
            }
        }
    }  
}
