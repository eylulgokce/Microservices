﻿using System;
using System.Text;
using MicroserviceCommon.Clients.Interfaces;
using MicroserviceCommon.CommonModel;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroserviceCommon.Clients
{
    public class PaymentBrokerClientRabbitMQ : IPaymentBrokerClient
    {
        public const string PaymentExchangeName = "payments-exchange";

        private readonly IModel _channel;

        public PaymentBrokerClientRabbitMQ()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(PaymentExchangeName, ExchangeType.Fanout);
        }

        public void Subscribe(string queueName, Action<Payment> onOrderReceived)
        {
            Console.WriteLine($"Subscribing to queue {queueName} in exchange {PaymentExchangeName}...");
            _channel.QueueDeclare(queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(queueName, PaymentExchangeName, string.Empty, null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (sender, eventArguments) =>
            {
                var body = eventArguments.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var order = JsonConvert.DeserializeObject<Payment>(json);
                onOrderReceived(order);
            };

            _channel.BasicConsume(queueName, true, consumer);
        }
    }
}