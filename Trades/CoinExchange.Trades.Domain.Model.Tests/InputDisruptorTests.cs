/***************************************************************************** 
* Copyright 2016 Aurora Solutions 
* 
*    http://www.aurorasolutions.io 
* 
* Aurora Solutions is an innovative services and product company at 
* the forefront of the software industry, with processes and practices 
* involving Domain Driven Design(DDD), Agile methodologies to build 
* scalable, secure, reliable and high performance products.
* 
* Coin Exchange is a high performance exchange system specialized for
* Crypto currency trading. It has different general purpose uses such as
* independent deposit and withdrawal channels for Bitcoin and Litecoin,
* but can also act as a standalone exchange that can be used with
* different asset classes.
* Coin Exchange uses state of the art technologies such as ASP.NET REST API,
* AngularJS and NUnit. It also uses design patterns for complex event
* processing and handling of thousands of transactions per second, such as
* Domain Driven Designing, Disruptor Pattern and CQRS With Event Sourcing.
* 
* Licensed under the Apache License, Version 2.0 (the "License"); 
* you may not use this file except in compliance with the License. 
* You may obtain a copy of the License at 
* 
*    http://www.apache.org/licenses/LICENSE-2.0 
* 
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, 
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
* See the License for the specific language governing permissions and 
* limitations under the License. 
*****************************************************************************/


﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinExchange.Trades.Domain.Model.OrderAggregate;
using CoinExchange.Trades.Domain.Model.TradeAggregate;
using CoinExchange.Trades.Infrastructure.Services;
using Disruptor;
using NUnit.Framework;

namespace CoinExchange.Trades.Domain.Model.Tests
{
    [TestFixture]
    public class InputDisruptorTests : IEventHandler<InputPayload>
    {
        private InputDisruptorPublisher _publisher;
        private InputPayload _receivedPayload;
        private ManualResetEvent _manualResetEvent;
        private List<string> _receivedTraderId;
        private int _counter = 0;
        public void OnNext(InputPayload data, long sequence, bool endOfBatch)
        {
            _counter--;
            _receivedPayload=new InputPayload(){OrderCancellation = new OrderCancellation(),Order = new Order()};
            if (data.IsOrder)
            {
                data.Order.MemberWiseClone(_receivedPayload.Order);
                _receivedPayload.IsOrder = true;
                _receivedTraderId.Add(data.Order.TraderId.Id);
            }
            else
            {
                data.OrderCancellation.MemberWiseClone(_receivedPayload.OrderCancellation);
                _receivedPayload.IsOrder = false;
            }
            if (_counter == 0)
            {
                _manualResetEvent.Set();
            }
        }

        [SetUp]
        public void Setup()
        {
            InputDisruptorPublisher.InitializeDisruptor(new IEventHandler<InputPayload>[] { this });
            _manualResetEvent=new ManualResetEvent(false);
            _receivedTraderId=new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            InputDisruptorPublisher.Shutdown();
        }

        [Test]
        public void PublishOrder_IfOrderIsAddedInPayload_ReceiveOrderInPayloadInConsumer()
        {
            _counter = 1;//as sending only one message
            Order order = OrderFactory.CreateOrder("1234", "XBTUSD", "limit", "buy", 5, 10,
                new StubbedOrderIdGenerator());
            InputPayload payload = InputPayload.CreatePayload(order);
            InputDisruptorPublisher.Publish(payload);
            _manualResetEvent.WaitOne(2000);
            Assert.AreEqual(payload.Order, order);
        }

        [Test]
        public void PublishCancelOrder_IfCancelOrderIsAddedInPayload_ReceiveCancelOrderInPayloadInConsumer()
        {
            _counter = 1;//as sending only one message
            OrderCancellation cancelOrder=new OrderCancellation(new OrderId("123"),new TraderId("123"),"XBTUSD" );
            InputPayload payload = InputPayload.CreatePayload(cancelOrder);
            InputDisruptorPublisher.Publish(payload);
            _manualResetEvent.WaitOne(2000);
            Assert.AreEqual(payload.OrderCancellation, cancelOrder);
        }

        [Test]
        public void PublishOrders_ToCheckPayloadRefrenceDoesnotGetMixed_AllOrdersReceived()
        {
            _counter = 14;//as sending 15 messages
            List<string> list=new List<string>();
            for (int i = 1; i < 15; i++)
            {
                Order order = OrderFactory.CreateOrder(i.ToString(), "XBTUSD", "limit", "buy", 5, 10,
                  new StubbedOrderIdGenerator());
                InputPayload payload = InputPayload.CreatePayload(order);
                InputDisruptorPublisher.Publish(payload);  
                list.Add(i.ToString(CultureInfo.InvariantCulture));
            }
            _manualResetEvent.WaitOne(10000); 
            Assert.AreEqual(CompareList(list), true);
        }

        private bool CompareList(List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != _receivedTraderId[i])
                    return false;
            }
            return true;
        }
    }
}
